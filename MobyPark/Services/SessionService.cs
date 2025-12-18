using System.Text.RegularExpressions;

using Microsoft.EntityFrameworkCore;

using MobyPark.Constants;
using MobyPark.Data;
using MobyPark.EncryptionHelper;
using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public class SessionService(UserDbContext db, IEncryptionService encryption) : ISessionService
    {
        private static readonly Dictionary<Guid, int> RefundAttempts = new();

        private const string PlatePattern =
            @"^(?:[A-Z]{2}-\d{2}-\d{2}|\d{2}-\d{2}-[A-Z]{2}|\d{2}-[A-Z]{2}-\d{2}|[A-Z]{2}-\d{2}-[A-Z]{2}|[A-Z]{2}-[A-Z]{2}-\d{2}|\d{2}-[A-Z]{2}-[A-Z]{2})$";

        public async Task<(SessionReadDto? dto, string? error, int? status)> StartSessionAsync(Guid userId, SessionStartDto dto)
        {
            var vehicle = await db.Vehicles
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == dto.VehicleId && v.UserId == userId);

            if (vehicle is null)
            {
                return (null, "Vehicle not found for this user.", 404);
            }

            var existsActive = await db.Sessions
                .AnyAsync(s => s.VehicleId == vehicle.Id && s.Stopped == null && !s.IsCancelled);

            if (existsActive)
            {
                return (null, "There is already an active session for this vehicle.", 409);
            }

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
                PaymentStatus = PaymentStatuses.Unpaid
            };

            await db.Sessions.AddAsync(session);
            await db.SaveChangesAsync();

            var readDto = ToSessionDto(session);
            return (readDto, null, null);
        }

        public async Task<(StopSessionResponseDto? dto, string? error, int? status)> StopSessionByPlateAsync(Guid userId, SessionStopDto dto)
        {
            if (userId == Guid.Empty)
                return (null, "User ID is required.", 400);

            if (dto is null)
                return (null, "Request body is required.", 400);

            var plate = LicensePlateProtector.Normalize(dto.LicensePlate);

            if (string.IsNullOrWhiteSpace(plate))
                return (null, "License plate is required.", 400);

            if (!Regex.IsMatch(plate, PlatePattern))
                return (null, "License plate format is invalid.", 400);

            var sessions = await db.Sessions
                .Include(s => s.User)
                .Where(s => s.UserId == userId && s.Stopped == null && !s.IsCancelled)
                .ToListAsync();

            var session = sessions.FirstOrDefault(s =>
            {
                if (string.IsNullOrEmpty(s.LicensePlate))
                    return false;

                var stored = LicensePlateProtector.DecryptNormalized(encryption, s.LicensePlate);
                return string.Equals(stored, plate, StringComparison.Ordinal);
            });

            if (session is null)
                return (null, "Active session for this license plate was not found.", 404);

            var username = session.User?.Username ?? userId.ToString();


            session.Stopped = DateTimeOffset.UtcNow;
            session.DurationMinutes = (int)Math.Ceiling((session.Stopped.Value - session.Started).TotalMinutes);

            const decimal Rate = 2.00m;
            var hours = Math.Ceiling(session.DurationMinutes / 60m);
            session.Cost = hours * Rate;

            session.PaymentStatus = PaymentStatuses.Unpaid;

            var payment = new Payment
            {
                Transaction = GenerateTransactionNumber(),
                Amount = session.Cost,
                DiscountCode = null,
                AmountWithDiscount = session.Cost,
                Initiator = username,
                UserId = userId,
                ParkingLotId = session.ParkingLotId,
                SessionId = session.Id,
                Completed = null,
                Hash = GeneratePaymentHash(),
                T_Data = null
            };

            var billing = new Billing
            {
                Id = Guid.NewGuid(),
                ParkingLotId = session.ParkingLotId,
                LicensePlate = session.LicensePlate,
                Started = session.Started,
                Stopped = session.Stopped.Value,
                Username = username,
                DurationMinutes = session.DurationMinutes,
                Cost = payment.AmountWithDiscount,
                PaymentStatus = PaymentStatuses.Unpaid
            };

            db.Sessions.Update(session);
            await db.Billings.AddAsync(billing);
            await db.Payments.AddAsync(payment);
            await db.SaveChangesAsync();

            var refreshedPayment = await db.Payments
                .Where(p => p.Transaction == payment.Transaction)
                .Include(p => p.Discount)
                .FirstAsync();

            return (ToStopSessionResponse(session, refreshedPayment), null, null);
        }

        public async Task<(StopSessionResponseDto? dto, string? error, int? status)> StopSessionByIdAsync(Guid userId, Guid sessionId)
        {
            if (sessionId == Guid.Empty)
                return (null, "Session ID is required.", 400);

            var session = await db.Sessions
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == sessionId);

            if (session is null)
                return (null, "Session not found.", 404);

            if (session.Stopped is not null)
                return (null, "Session is already stopped.", 409);

            if (session.IsCancelled)
                return (null, "Cancelled sessions cannot be stopped.", 409);

            session.Stopped = DateTimeOffset.UtcNow;
            session.DurationMinutes = (int)Math.Ceiling((session.Stopped.Value - session.Started).TotalMinutes);

            const decimal Rate = 2.00m;
            var hours = Math.Ceiling(session.DurationMinutes / 60m);
            session.Cost = hours * Rate;

            session.PaymentStatus = PaymentStatuses.AwaitingPayment;

            var username = session.User?.Username ?? session.UserId.ToString();

            var payment = new Payment
            {
                Transaction = GenerateTransactionNumber(),
                Amount = session.Cost,
                DiscountCode = null,
                AmountWithDiscount = session.Cost,
                Initiator = username,
                UserId = session.UserId,
                ParkingLotId = session.ParkingLotId,
                SessionId = session.Id,
                Completed = null,
                Hash = GeneratePaymentHash(),
                T_Data = null
            };

            var billing = new Billing
            {
                Id = Guid.NewGuid(),
                ParkingLotId = session.ParkingLotId,
                LicensePlate = session.LicensePlate,
                Started = session.Started,
                Stopped = session.Stopped.Value,
                Username = username,
                DurationMinutes = session.DurationMinutes,
                Cost = payment.AmountWithDiscount,
                PaymentStatus = PaymentStatuses.AwaitingPayment
            };

            db.Sessions.Update(session);
            await db.Billings.AddAsync(billing);
            await db.Payments.AddAsync(payment);
            await db.SaveChangesAsync();

            var refreshedPayment = await db.Payments
                .Where(p => p.Transaction == payment.Transaction)
                .Include(p => p.Discount)
                .FirstAsync();

            return (ToStopSessionResponse(session, refreshedPayment), null, null);
        }

        public async Task<(SessionReadDto? dto, string? error, int? status)> GetSessionByIdAsync(Guid userId, Guid sessionId)
        {
            if (sessionId == Guid.Empty)
            {
                return (null, "Session ID is required.", 400);
            }

            var session = await db.Sessions
                .Include(s => s.Vehicle)
                .Include(s => s.ParkingLot)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session is null)
            {
                return (null, "Session not found.", 404);
            }

            var dto = ToSessionDto(session);
            return (dto, null, null);
        }

        public async Task<(SessionReadDto? dto, string? error, int? status)> CancelSessionAsync(Guid userId, Guid sessionId, CancelSessionDto dto)
        {
            if (sessionId == Guid.Empty)
            {
                return (null, "Session ID is required.", 400);
            }

            var session = await db.Sessions.FindAsync(sessionId);

            if (session is null)
            {
                return (null, "Session not found.", 404);
            }

            if (session.IsCancelled)
            {
                return (null, "Session is already cancelled.", 409);
            }

            if (session.Stopped is null)
            {
                session.Stopped = DateTimeOffset.UtcNow;
            }

            session.IsCancelled = true;
            session.CancelledAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync();

            var readDto = ToSessionDto(session);
            return (readDto, null, null);
        }

        public async Task<(bool dto, string? error, int? status)> DeleteSessionAsync(int parkingLotId, Guid sessionId)
        {
            if (parkingLotId <= 0)
            {
                return (false, "Parking lot ID must be a positive integer.", 400);
            }

            if (sessionId == Guid.Empty)
            {
                return (false, "Session ID is required.", 400);
            }

            var parkingLotExists = await db.ParkingLots
                .AnyAsync(p => p.Id == parkingLotId);

            if (!parkingLotExists)
            {
                return (false, "Parking lot not found.", 404);
            }

            var session = await db.Sessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.ParkingLotId == parkingLotId);

            if (session is null)
            {
                return (false, "Session not found for this parking lot.", 404);
            }

            db.Sessions.Remove(session);
            await db.SaveChangesAsync();

            return (true, null, null);
        }

        public async Task<(List<SessionReadDto>? dto, string? error, int? status)> GetAllForUserAsync(Guid userId, bool onlyActive)
        {
            if (userId == Guid.Empty)
            {
                return (null, "User ID is required.", 400);
            }

            var query = db.Sessions
                .Include(s => s.Vehicle)
                .Include(s => s.ParkingLot)
                .Where(s => s.UserId == userId);

            if (onlyActive)
            {
                query = query.Where(s => s.Stopped == null && !s.IsCancelled);
            }

            var sessions = await query
                .OrderByDescending(s => s.Started)
                .ToListAsync();

            var dtos = sessions
                .Select(ToSessionDto)
                .ToList();

            return (dtos, null, null);
        }

        public async Task<(RefundResponseDto? dto, string? error, int? status)> RequestRefundAsync(Guid userId, Guid sessionId, RefundRequestDto? dto)
        {
            if (sessionId == Guid.Empty)
            {
                return (null, "Session ID is required.", 400);
            }

            var session = await db.Sessions.FindAsync(sessionId);
            if (session is null)
            {
                return (null, "Session not found.", 404);
            }

            if (!session.IsCancelled)
            {
                return (null, "Only cancelled sessions can be refunded.", 409);
            }

            if (session.Stopped is null)
            {
                return (null, "Session must be stopped before refund.", 409);
            }

            if (session.IsRefunded)
            {
                return (null, "Session has already been refunded.", 409);
            }

            var attemptKey = session.UserId;

            if (!RefundAttempts.ContainsKey(attemptKey))
                RefundAttempts[attemptKey] = 0;

            if (RefundAttempts[attemptKey] >= 3)
            {
                return (null, "Maximum number of refund attempts reached.", 429);
            }

            RefundAttempts[attemptKey]++;

            const decimal Rate = 0.05m;
            session.DurationMinutes = (int)(session.Stopped.Value - session.Started).TotalMinutes;
            session.Cost = Math.Max(Math.Round(session.DurationMinutes * Rate, 2), 0.50m);

            decimal pct =
                session.DurationMinutes <= 10 ? 1.0m :
                session.DurationMinutes <= 30 ? 0.5m :
                0.0m;

            var refundAmount = Math.Round(session.Cost * pct, 2);
            if (refundAmount <= 0)
            {
                return (null, "No refundable amount for this session.", 400);
            }

            if (string.IsNullOrWhiteSpace(dto?.IBAN))
            {
                return (null, "IBAN is required.", 400);
            }

            session.IsRefunded = true;
            session.RefundDate = DateTimeOffset.UtcNow;
            session.PaymentStatus = PaymentStatuses.Refunded;

            await db.SaveChangesAsync();

            var response = new RefundResponseDto
            {
                SessionId = session.Id,
                Refunded = refundAmount,
                Percentage = pct * 100,
                DurationMinutes = session.DurationMinutes,
                Cost = session.Cost,
                RefundDate = session.RefundDate!.Value
            };

            return (response, null, null);
        }

        private static string GeneratePaymentHash()
        {
            return Guid.NewGuid().ToString("N");
        }

        private static string GenerateTransactionNumber()
        {
            return Random.Shared.NextInt64(100000000, 999999999).ToString("D12");
        }

        private StopSessionResponseDto ToStopSessionResponse(Session session, Payment payment)
        {
            return new StopSessionResponseDto
            {
                Session = ToSessionDto(session),
                Payment = new PaymentInitiationDto
                {
                    Transaction = payment.Transaction,
                    Amount = payment.AmountWithDiscount,
                    Validation = payment.Hash
                }
            };
        }

        private SessionReadDto ToSessionDto(Session s) => new SessionReadDto
        {
            Id = s.Id,
            UserId = s.UserId,
            VehicleId = s.VehicleId,
            ParkingLotId = s.ParkingLotId,
            LicensePlate = LicensePlateProtector.DecryptNormalized(encryption, s.LicensePlate),
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
}
