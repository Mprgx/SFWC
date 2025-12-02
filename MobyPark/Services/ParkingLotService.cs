using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;

namespace MobyPark.Services
{
    public class ParkingLotService(UserDbContext db) : IParkingLotService
    {
        public async Task<List<ParkingLot>> GetAllAsync()
        {
            return await db.Set<ParkingLot>().ToListAsync();
        }

        public async Task<ParkingLot?> GetByIdAsync(int lid)
        {
            return await db.Set<ParkingLot>()
                .FirstOrDefaultAsync(p => p.Id == lid);
        }

        public async Task<List<Session>> GetSessionsAsync(
            int lid, string? username, bool isAdmin)
        {
            var lot = await db.Set<ParkingLot>().AnyAsync(p => p.Id == lid);
            if (!lot)
                return new List<Session>();

            var query = db.Sessions
                .Include(s => s.User)
                .Include(s => s.Vehicle)
                .Where(s => s.ParkingLotId == lid); //s.ParkingLotId == lid

            if (!isAdmin && username is not null)
                query = query.Where(s => s.User.Username == username);

            return await query.ToListAsync();
        }

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

        private static string GenerateTransactionNumber()
        {
            var random = new Random();
            return random.Next(100000000, 999999999).ToString("D12");
        }
    }
}
