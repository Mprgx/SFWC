using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;
using System.Text.Json;

namespace MobyPark.Services
{
    public class ParkingLotService(UserDbContext db) : IParkingLotService
    {

        //POST
        public async Task<ParkingLot> CreateParkingLotAsync(ParkingLot parkingLot)
        {
            db.ParkingLots.Add(parkingLot);
            await db.SaveChangesAsync();
            return parkingLot;
        }

        //GET
        public async Task<List<ParkingLot>> GetAllAsync()
        {
            return await db.Set<ParkingLot>().ToListAsync();
        }

        //GET
        public async Task<ParkingLot?> GetByIdAsync(int lid)
        {
            return await db.Set<ParkingLot>()
                .FirstOrDefaultAsync(p => p.Id == lid);
        }

        //GET
        public async Task<List<Session>> GetSessionsAsync(
            int lid, string? username, bool isAdmin)
        {
            var lot = await db.Set<ParkingLot>().AnyAsync(p => p.Id == lid);
            if (!lot)
                return new List<Session>();

            var query = db.Sessions
                .Include(s => s.User)
                .Include(s => s.Vehicle)
                .Where(s => s.ParkingLotId == lid);

            if (!isAdmin && username is not null)
                query = query.Where(s => s.User.Username == username);

            return await query.ToListAsync();
        }

        //GET
        public async Task<Session?> GetSessionByIdAsync(
            int lid, string sid, string? username, bool isAdmin)
        {
            var session = await db.Sessions
                .Include(s => s.User)
                .Include(s => s.Vehicle)
                .FirstOrDefaultAsync(s =>
                    s.ParkingLotId == lid && s.Id.ToString() == sid);

            if (session is null)
                return null;

            if (!isAdmin && session.User.Username != username)
                return null;

            return session;
        }

        //DELETE
        public async Task<bool> DeleteParkingLotAsync(int id)
        {
            var lot = await db.ParkingLots.FindAsync(id);

            if (lot == null)
                return false;

            db.ParkingLots.Remove(lot);
            await db.SaveChangesAsync();

            return true;
        }

        //DELETE
        public async Task<bool> DeleteParkingLotSessionAsync(int parkingLotId, Guid sessionId)
        {
            var session = await db.Sessions
                .Where(s => s.ParkingLotId == parkingLotId && s.Id == sessionId)
                .FirstOrDefaultAsync();

            if (session == null)
                return false;

            db.Sessions.Remove(session);
            await db.SaveChangesAsync();

            return true;
        }

        public async Task<Payment?> StopSessionAsync(string licensePlate, string username, Guid userid, IEncryptionService encryption)
        {
            if (string.IsNullOrWhiteSpace(licensePlate))
                return null;

            var lp = licensePlate.Trim().ToUpperInvariant();

            var activeSessions = await db.Sessions
                .Include(s => s.User)
                .Include(s => s.Vehicle)
                .Where(s => s.Stopped == null)
                .ToListAsync();

            var session = activeSessions.FirstOrDefault(s =>
            {
                if (string.IsNullOrEmpty(s.LicensePlate)) return false;
                var platePlain = encryption.Decrypt(s.LicensePlate);
                if (string.IsNullOrWhiteSpace(platePlain)) return false;

                return platePlain.Trim().ToUpperInvariant() == lp;
            });

            if (session == null || session.UserId != userid)
                return null;

            session.Stopped = DateTimeOffset.UtcNow;
            session.DurationMinutes = (int)Math.Ceiling((session.Stopped.Value - session.Started).TotalMinutes);

            const decimal RATE_PER_HOUR = 2.00m;
            var hours = Math.Ceiling(session.DurationMinutes / 60.0m);
            session.Cost = hours * RATE_PER_HOUR;
            session.PaymentStatus = "unpaid";
            string username1 = session.User.Username;

            var payment = new Payment
            {
                Transaction = GenerateTransactionNumber(),
                Amount = session.Cost,
                Initiator = username1,
                UserId = session.UserId,
                User = session.User,
                Created_At = DateTimeOffset.UtcNow,
                Completed = null,
                Hash = Guid.NewGuid().ToString(),
                T_Data = "",
                SessionId = session.Id,
                Session = session,
                ParkingLotId = session.ParkingLotId,
                ParkingLot = session.ParkingLot
            };


            db.Sessions.Update(session);
            await db.Payments.AddAsync(payment);
            await db.SaveChangesAsync();

            return payment;
        }

        public async Task<ParkingLot?> UpdateParkingLotAsync(int lid, ParkingLotUpdateDto dto)
        {
            var lot = await db.ParkingLots.FindAsync(lid);
            if (lot is null)
                return null;

            if (!string.IsNullOrWhiteSpace(dto.Name))
                lot.Name = dto.Name;

            if (!string.IsNullOrWhiteSpace(dto.Location))
                lot.Location = dto.Location;

            if (!string.IsNullOrWhiteSpace(dto.Address))
                lot.Address = dto.Address;

            if (dto.Capacity is not null)
                lot.Capacity = dto.Capacity.Value;

            // if (dto.Reserved is not null)
            //     lot.ReservedSpots = dto.Reserved.Value;

            if (dto.Tariff is not null)
                lot.Tariff = dto.Tariff.Value;

            if (dto.DayTariff is not null)
                lot.DayTariff = dto.DayTariff.Value;

            if (dto.Coordinates is not null)
                lot.Coordinates = JsonSerializer.Serialize(dto.Coordinates);

            await db.SaveChangesAsync();

            return lot;
        }


        private static string GenerateTransactionNumber()
        {
            var random = new Random();
            return random.Next(100000000, 999999999).ToString("D12");
        }
    }
}
