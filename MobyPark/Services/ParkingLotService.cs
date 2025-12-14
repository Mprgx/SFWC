using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;
using System.Text.Json;
using MobyPark.Models;

namespace MobyPark.Services
{
    public class ParkingLotService(UserDbContext db, IEncryptionService encryption) : IParkingLotService
    {
        private static ParkingLotReadDto ToDto(ParkingLot lot)
        {
            return new ParkingLotReadDto
            {
                Id = lot.Id,
                Name = lot.Name,
                Location = lot.Location,
                Address = lot.Address,
                Capacity = lot.Capacity,
                Tariff = lot.Tariff,
                DayTariff = lot.DayTariff,
                Coordinates = JsonSerializer.Deserialize<Dictionary<string, double>>(lot.Coordinates) ?? new()
            };
        }

        private SessionReadDto ToSessionDto(Session s)
        {
            return new SessionReadDto
            {
                Id = s.Id,
                UserId = s.UserId,
                VehicleId = s.VehicleId,
                ParkingLotId = s.ParkingLotId,
                LicensePlate = encryption.Decrypt(s.LicensePlate) ?? string.Empty,
                Started = s.Started,
                Stopped = s.Stopped,
                DurationMinutes = s.DurationMinutes,
                Cost = s.Cost,
                PaymentStatus = s.PaymentStatus,
                IsCancelled = s.IsCancelled,
                CancelledAt = s.CancelledAt,
                IsRefunded = s.IsRefunded,
                RefundDate = s.RefundDate
            };
        }

        public async Task<ParkingLotReadDto> CreateParkingLotAsync(ParkingLotRequestDto parkinglot)
        {
            var lot = new ParkingLot
            {
                Id = 0,
                Name = parkinglot.Name,
                Location = parkinglot.Location,
                Address = parkinglot.Address,
                Capacity = parkinglot.Capacity,
                Tariff = parkinglot.Tariff,
                DayTariff = parkinglot.DayTariff,
                Coordinates = JsonSerializer.Serialize(parkinglot.Coordinates)
            };

            db.ParkingLots.Add(lot);
            await db.SaveChangesAsync();
            return ToDto(lot);
        }

        public async Task<List<ParkingLotReadDto>> GetAllAsync()
        {
            var lots = await db.ParkingLots.ToListAsync();
            return lots.Select(ToDto).ToList();
        }

        public async Task<ParkingLotReadDto?> GetByIdAsync(int lid)
        {
            var lot = await db.ParkingLots.FindAsync(lid);
            return lot is null ? null : ToDto(lot);
        }

        public async Task<List<SessionReadDto>> GetSessionsAsync(int lid, string? username = null, bool isAdmin = false)
        {
            var exists = await db.ParkingLots.AnyAsync(p => p.Id == lid);
            if (!exists) return new List<SessionReadDto>();

            var sessions = db.Sessions.Where(s => s.ParkingLotId == lid);
            if (!isAdmin && username is not null)
                sessions = sessions.Where(s => s.User.Username == username);

            return (await sessions.ToListAsync()).Select(ToSessionDto).ToList();
        }

        public async Task<SessionReadDto?> GetSessionByIdAsync(int lid, Guid sid, string? username = null, bool isAdmin = false)
        {
            var session = await db.Sessions.FirstOrDefaultAsync(s => s.ParkingLotId == lid && s.Id == sid);
            if (session is null) return null;
            if (!isAdmin && session.User.Username != username) return null;
            return ToSessionDto(session);
        }

        public async Task<bool> DeleteParkingLotAsync(int lid)
        {
            var lot = await db.ParkingLots.FindAsync(lid);
            if (lot is null) return false;
            db.ParkingLots.Remove(lot);
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteParkingLotSessionAsync(int lid, Guid sid)
        {
            var session = await db.Sessions.FirstOrDefaultAsync(s => s.ParkingLotId == lid && s.Id == sid);
            if (session is null) return false;
            db.Sessions.Remove(session);
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<ParkingLotReadDto?> UpdateParkingLotAsync(int lid, ParkingLotUpdateDto dto)
        {
            var lot = await db.ParkingLots.FindAsync(lid);
            if (lot is null) return null;

            if (!string.IsNullOrWhiteSpace(dto.Name)) lot.Name = dto.Name;
            if (!string.IsNullOrWhiteSpace(dto.Location)) lot.Location = dto.Location;
            if (!string.IsNullOrWhiteSpace(dto.Address)) lot.Address = dto.Address;
            if (dto.Capacity.HasValue) lot.Capacity = dto.Capacity.Value;
            if (dto.Tariff.HasValue) lot.Tariff = dto.Tariff.Value;
            if (dto.DayTariff.HasValue) lot.DayTariff = dto.DayTariff.Value;
            if (dto.Coordinates is not null) lot.Coordinates = JsonSerializer.Serialize(dto.Coordinates);

            await db.SaveChangesAsync();
            return ToDto(lot);
        }
    }
}
