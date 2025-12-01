using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public class SessionService(UserDbContext db, IEncryptionService encryption) : ISessionService
    {
        private static readonly Dictionary<Guid, int> RefundAttempts = new();

        private const string PlatePattern =
            @"^(?:[A-Z]{2}-\d{2}-\d{2}|\d{2}-\d{2}-[A-Z]{2}|\d{2}-[A-Z]{2}-\d{2}|[A-Z]{2}-\d{2}-[A-Z]{2}|[A-Z]{2}-[A-Z]{2}-\d{2}|\d{2}-[A-Z]{2}-[A-Z]{2})$";

        public async Task<Session?> StartSessionAsync(Guid userId, SessionStartDto dto)
        {
            var vehicle = await db.Vehicles
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == dto.VehicleId && v.UserId == userId);

            if (vehicle is null) return null;

            var existsActive = await db.Sessions
                .AnyAsync(s => s.VehicleId == vehicle.Id && s.Stopped == null);

            if (existsActive) return null;

            var session = new Session
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                VehicleId = vehicle.Id,
                LicensePlate = vehicle.LicensePlate,
                ParkingLotId = dto.ParkingLotId,
                Started = DateTimeOffset.UtcNow,
                DurationMinutes = 0,
                Cost = 0,
                PaymentStatus = "unpaid"
            };

            await db.Sessions.AddAsync(session);
            await db.SaveChangesAsync();

            return session;
        }

        public async Task<Session?> GetSessionByIdAsync(Guid userId, Guid sessionId)
        {
            return await db.Sessions
                .Where(s => s.Id == sessionId && s.UserId == userId)
                .FirstOrDefaultAsync();
        }

        public async Task<Session?> StopSessionByPlateAsync(string username, Guid userId, SessionStopDto dto)
        {
            var plate = dto.LicensePlate?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(plate)) return null;
            if (!Regex.IsMatch(plate, PlatePattern)) return null;

            var sessions = await db.Sessions
                .Where(s => s.Stopped == null)
                .ToListAsync();

            var session = sessions.FirstOrDefault(s =>
            {
                if (string.IsNullOrEmpty(s.LicensePlate)) return false;

                var plain = encryption.Decrypt(s.LicensePlate);
                if (string.IsNullOrWhiteSpace(plain)) return false;

                return string.Equals(
                    plain.Trim().ToUpperInvariant(),
                    plate,
                    StringComparison.Ordinal
                );
            });

            if (session is null) return null;
            if (session.UserId != userId) return null;

            session.Stopped = DateTimeOffset.UtcNow;

            session.DurationMinutes = (int)Math.Ceiling(
                (session.Stopped.Value - session.Started).TotalMinutes
            );

            const decimal Rate = 2.00m;
            var hours = Math.Ceiling(session.DurationMinutes / 60m);
            session.Cost = hours * Rate;
            session.PaymentStatus = "unpaid";

            var payment = new Payment
            {
                Transaction = GenerateTransactionNumber(),
                Amount = session.Cost,
                Initiator = username,
                UserId = userId,
                Completed = null,
                Hash = GeneratePaymentHash(),
                T_Data = null
            };

            db.Sessions.Update(session);
            await db.Payments.AddAsync(payment);
            await db.SaveChangesAsync();

            return session;
        }

        public async Task<Session?> StopSessionByIdAsync(Guid userId, Guid sessionId)
        {
            var s = await db.Sessions.FindAsync(sessionId);
            if (s is null) return null;
            if (s.Stopped is not null) return null;
            if (s.IsCancelled) return null;

            s.Stopped = DateTimeOffset.UtcNow;
            s.DurationMinutes = (int)(s.Stopped.Value - s.Started).TotalMinutes;
            s.Cost = Math.Round((decimal)s.DurationMinutes * 0.05m, 2);
            s.PaymentStatus = "awaiting_payment";

            await db.SaveChangesAsync();
            return s;
        }

        public async Task<Session?> CancelSessionAsync(Guid userId, Guid sessionId, CancelSessionDto dto)
        {
            var s = await db.Sessions.FindAsync(sessionId);
            if (s is null) return null;

            if (s.IsCancelled)
                return null;

            if (s.Stopped is null)
                s.Stopped = DateTimeOffset.UtcNow;

            s.IsCancelled = true;
            s.CancelledAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync();
            return s;
        }

        public async Task<bool> DeleteSessionAsync(int parkingLotId, Guid sessionId)
        {
            // Check of parking lot bestaat
            var parkingLotExists = await db.ParkingLots
                .AnyAsync(p => p.Id == parkingLotId);

            if (!parkingLotExists)
                return false;

            // Zoek de session
            var session = await db.Sessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.ParkingLotId == parkingLotId);

            if (session is null)
                return false;

            db.Sessions.Remove(session);
            await db.SaveChangesAsync();

            return true;
        }


        public async Task<object?> RequestRefundAsync(Guid userId, Guid sessionId, RefundRequestDto? dto)
        {
            var s = await db.Sessions.FindAsync(sessionId);
            if (s is null) return null;

            if (!s.IsCancelled) return null;
            if (s.Stopped is null) return null;
            if (s.IsRefunded) return null;

            if (!RefundAttempts.ContainsKey(userId))
                RefundAttempts[userId] = 0;

            if (RefundAttempts[userId] >= 3)
                return null;

            RefundAttempts[userId]++;

            const decimal Rate = 0.05m;
            s.DurationMinutes = (int)(s.Stopped.Value - s.Started).TotalMinutes;
            s.Cost = Math.Max(Math.Round(s.DurationMinutes * Rate, 2), 0.50m);

            decimal pct =
                s.DurationMinutes <= 10 ? 1.0m :
                s.DurationMinutes <= 30 ? 0.5m :
                0.0m;

            var refundAmount = Math.Round(s.Cost * pct, 2);
            if (refundAmount <= 0) return null;

            if (string.IsNullOrWhiteSpace(dto?.IBAN)) return null;

            s.IsRefunded = true;
            s.RefundDate = DateTimeOffset.UtcNow;
            s.PaymentStatus = "refunded";

            await db.SaveChangesAsync();

            return new
            {
                sessionId = s.Id,
                refunded = refundAmount,
                percentage = pct * 100,
                s.DurationMinutes,
                s.Cost,
                s.RefundDate
            };
        }

        public async Task<List<Session>> GetAllForUserAsync(Guid userId, bool onlyActive)
        {
            var query = db.Sessions
                .Include(s => s.Vehicle)
                .Include(s => s.ParkingLot)
                .Where(s => s.UserId == userId);

            if (onlyActive)
            {
                query = query.Where(s => s.Stopped == null && !s.IsCancelled);
            }

            return await query
                .OrderByDescending(s => s.Started)
                .ToListAsync();
        }

        private static string GeneratePaymentHash()
        {
            return Guid.NewGuid().ToString("N");
        }

        private static string GenerateTransactionNumber()
        {
            return Random.Shared.NextInt64(100000000, 999999999).ToString("D12");
        }
    }
}
