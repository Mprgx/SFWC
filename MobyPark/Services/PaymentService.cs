using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public class PaymentService(UserDbContext context) : IPaymentService
    {
        public async Task<PaymentResponseDto?> CompletePaymentAsync(
            Guid userId,
            string transactionId,
            PaymentValidationDto request)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            // Basic request validation
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

            // Update session payment status if there is a linked session
            if (payment.SessionId != Guid.Empty)
            {
                var session = payment.Session
                              ?? await context.Sessions.FirstOrDefaultAsync(s => s.Id == payment.SessionId);

                if (session is not null)
                {
                    session.PaymentStatus = "paid";
                    // EF is tracking, no need for Update()
                }
            }

            await context.SaveChangesAsync();

            // Ensure nav properties are loaded
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
            var payment = await context.Payments.FindAsync(transactionId);
            if (payment is null)
                return false;

            context.Payments.Remove(payment);
            context.SaveChangesAsync();
            return true;
        }

        private static PaymentResponseDto ToPaymentResponseDto(Payment p)
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
                        Email = p.User.Email,
                        PhoneNumber = p.User.PhoneNumber,
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
                    : new SessionReadDto(
                        p.Session.Id,
                        p.Session.UserId,
                        p.Session.VehicleId,
                        p.Session.ParkingLotId,
                        p.Session.LicensePlate,
                        p.Session.Started,
                        p.Session.Stopped,
                        p.Session.DurationMinutes,
                        p.Session.Cost,
                        p.Session.PaymentStatus,
                        p.Session.IsCancelled,
                        p.Session.CancelledAt,
                        p.Session.IsRefunded,
                        p.Session.RefundDate
                    ),

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
                        ReservedSpots = p.ParkingLot.ReservedSpots,
                        Tariff = p.ParkingLot.Tariff,
                        DayTariff = p.ParkingLot.DayTariff
                    }
            };
        }
    }
}
