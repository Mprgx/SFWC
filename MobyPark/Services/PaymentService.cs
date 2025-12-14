using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public class PaymentService(UserDbContext context, IEncryptionService encryption) : IPaymentService
    {
        public async Task<PaymentResponseDto?> CompletePaymentAsync(Guid userId, string transactionId, PaymentValidationDto request)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            if (request.T_Data.ValueKind == JsonValueKind.Undefined ||
                string.IsNullOrWhiteSpace(request.Validation))
            {
                throw new UnauthorizedAccessException("Required field missing");
            }

            var payment = await context.Payments
                .Include(p => p.User)
                .Include(p => p.Session)
                .Include(p => p.ParkingLot)
                .FirstOrDefaultAsync(p => p.Transaction == transactionId);

            if (payment is null)
                throw new KeyNotFoundException("Payment not found");

            if (payment.UserId != userId)
                throw new UnauthorizedAccessException("You may only complete your own payments");

            var expectedHash = GeneratePaymentHash(payment.Transaction, payment.Amount);
            if (!string.Equals(expectedHash, request.Validation, StringComparison.Ordinal))
                throw new UnauthorizedAccessException("Validation failed");

            payment.Completed = DateTimeOffset.UtcNow;
            payment.Hash = expectedHash;
            payment.T_Data = request.T_Data.GetRawText();

            if (payment.SessionId != Guid.Empty)
            {
                var session = payment.Session
                              ?? await context.Sessions.FirstOrDefaultAsync(s => s.Id == payment.SessionId);

                if (session is not null)
                {
                    session.PaymentStatus = "paid";
                }
            }

            await context.SaveChangesAsync();

            await context.Entry(payment).Reference(p => p.User).LoadAsync();
            await context.Entry(payment).Reference(p => p.Session).LoadAsync();
            await context.Entry(payment).Reference(p => p.ParkingLot).LoadAsync();

            return ToPaymentResponseDto(payment);
        }

        private static string GeneratePaymentHash(string transaction, decimal amount)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var input = $"{transaction}:{amount}:{DateTimeOffset.UtcNow.Ticks}";
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public async Task<List<PaymentResponseDto?>> GetPaymentsForAnyUserAsync(string username)
        {
            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user is null)
                return new List<PaymentResponseDto?>();

            var payments = await context.Payments
                .Include(p => p.User)
                .Include(p => p.Session)
                .Include(p => p.ParkingLot)
                .Where(p => p.Initiator == user.Username)
                .ToListAsync();

            return payments
                .Select(p => (PaymentResponseDto?)ToPaymentResponseDto(p))
                .ToList();
        }

        public async Task<List<PaymentResponseDto?>> GetPaymentsForUserAsync(Guid userId)
        {
            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user is null)
                return new List<PaymentResponseDto?>();

            var payments = await context.Payments
                .Include(p => p.User)
                .Include(p => p.Session)
                .Include(p => p.ParkingLot)
                .Where(p => p.UserId == userId)
                .ToListAsync();

            return payments
                .Select(p => (PaymentResponseDto?)ToPaymentResponseDto(p))
                .ToList();
        }

        public async Task<bool> DeletePaymentByTransactionId(string transactionId)
        {
            var payment = await context.Payments
                .FirstOrDefaultAsync(p => p.Transaction == transactionId);

            if (payment is null)
                return false;

            context.Payments.Remove(payment);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<PaymentResponseDto?> FulfillPaymentAsync(string userId, PaymentsDto paymentRequest)
        {
            var payment = await context.Payments
                .Include(p => p.User)
                .Include(p => p.Session)
                .Include(p => p.ParkingLot)
                .FirstOrDefaultAsync(p => p.Transaction == paymentRequest.Transaction
                                         && p.Completed == null);

            if (payment is null)
                return null;

            payment.Completed = DateTimeOffset.UtcNow;

            if (payment.SessionId != Guid.Empty)
            {
                var session = payment.Session
                              ?? await context.Sessions.FirstOrDefaultAsync(s => s.Id == payment.SessionId);

                if (session is not null)
                {
                    session.PaymentStatus = "paid";
                }
            }

            await context.SaveChangesAsync();

            return new PaymentResponseDto
            {
                Transaction = payment.Transaction,
                Amount = payment.Amount,
                Initiator = payment.Initiator,

                UserId = payment.UserId,
                User = payment.User == null
                    ? null
                    : new UserReadDto
                    {
                        Id = payment.User.Id,
                        Username = payment.User.Username,
                        Name = payment.User.Name,
                        Email = encryption.Decrypt(payment.User.Email) ?? string.Empty,
                        PhoneNumber = encryption.Decrypt(payment.User.PhoneNumber) ?? string.Empty,
                        BirthYear = payment.User.BirthYear,
                        Role = payment.User.Role,
                        CreatedAt = payment.User.CreatedAt
                    },

                CreatedAt = payment.Created_At,
                Completed = payment.Completed,

                Hash = payment.Hash,
                T_Data = payment.T_Data,

                SessionId = payment.SessionId,
                Session = payment.Session == null
                    ? null
                    : new SessionReadDto
                    {
                        Id = payment.Session.Id,
                        UserId = payment.Session.UserId,
                        VehicleId = payment.Session.VehicleId,
                        ParkingLotId = payment.Session.ParkingLotId,
                        LicensePlate = encryption.Decrypt(payment.Session.LicensePlate) ?? string.Empty,
                        Started = payment.Session.Started,
                        Stopped = payment.Session.Stopped,
                        DurationMinutes = payment.Session.DurationMinutes,
                        Cost = payment.Session.Cost,
                        PaymentStatus = payment.Session.PaymentStatus,
                        IsCancelled = payment.Session.IsCancelled,
                        CancelledAt = payment.Session.CancelledAt,
                        RefundDate = payment.Session.RefundDate,
                        IsRefunded = payment.Session.IsRefunded
                    },

                ParkingLotId = payment.ParkingLotId,
                ParkingLot = payment.ParkingLot == null
                    ? null
                    : new ParkingLotSummaryDto
                    {
                        Id = payment.ParkingLot.Id,
                        Name = payment.ParkingLot.Name,
                        Location = payment.ParkingLot.Location,
                        Address = payment.ParkingLot.Address,
                        Capacity = payment.ParkingLot.Capacity,
                        Tariff = payment.ParkingLot.Tariff,
                        DayTariff = payment.ParkingLot.DayTariff
                    }
            };
        }

        private PaymentResponseDto ToPaymentResponseDto(Payment p)
        {
            return new PaymentResponseDto
            {
                Transaction = p.Transaction,
                Amount = p.Amount,
                Initiator = p.Initiator,

                UserId = p.UserId,
                User = p.User == null
                    ? null
                    : new UserReadDto
                    {
                        Id = p.User.Id,
                        Username = p.User.Username,
                        Name = p.User.Name,
                        Email = encryption.Decrypt(p.User.Email) ?? string.Empty,
                        PhoneNumber = encryption.Decrypt(p.User.PhoneNumber) ?? string.Empty,
                        BirthYear = p.User.BirthYear,
                        Role = p.User.Role,
                        CreatedAt = p.User.CreatedAt
                    },

                CreatedAt = p.Created_At,
                Completed = p.Completed,

                Hash = p.Hash,
                T_Data = p.T_Data,

                SessionId = p.SessionId,
                Session = p.Session == null
                    ? null
                    : new SessionReadDto
                    {
                        Id = p.Session.Id,
                        UserId = p.Session.UserId,
                        VehicleId = p.Session.VehicleId,
                        ParkingLotId = p.Session.ParkingLotId,
                        LicensePlate = encryption.Decrypt(p.Session.LicensePlate) ?? string.Empty,
                        Started = p.Session.Started,
                        Stopped = p.Session.Stopped,
                        DurationMinutes = p.Session.DurationMinutes,
                        Cost = p.Session.Cost,
                        PaymentStatus = p.Session.PaymentStatus,
                        IsCancelled = p.Session.IsCancelled,
                        CancelledAt = p.Session.CancelledAt,
                        RefundDate = p.Session.RefundDate,
                        IsRefunded = p.Session.IsRefunded
                    },

                ParkingLotId = p.ParkingLotId,
                ParkingLot = p.ParkingLot == null
                    ? null
                    : new ParkingLotSummaryDto
                    {
                        Id = p.ParkingLot.Id,
                        Name = p.ParkingLot.Name,
                        Location = p.ParkingLot.Location,
                        Address = p.ParkingLot.Address,
                        Capacity = p.ParkingLot.Capacity,
                        Tariff = p.ParkingLot.Tariff,
                        DayTariff = p.ParkingLot.DayTariff
                    }
            };
        }
    }
}
