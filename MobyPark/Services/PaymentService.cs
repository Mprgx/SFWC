using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly UserDbContext _context;

        public PaymentService(UserDbContext context)
        {
            _context = context;
        }

        public async Task<PaymentResponseDto> CompletePaymentAsync(
            Guid userId,
            string transactionId,
            PaymentValidationDto request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // basic request validation
            if (request.T_Data.ValueKind == JsonValueKind.Undefined || string.IsNullOrWhiteSpace(request.Validation))
                throw new UnauthorizedAccessException("Required field missing");

            var payment = await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Session)
                .FirstOrDefaultAsync(p => p.Transaction == transactionId);

            if (payment == null)
                throw new KeyNotFoundException("Payment not found");

            if (payment.UserId != userId)
                throw new UnauthorizedAccessException("You may only complete your own payments");

            var expectedHash = GeneratePaymentHash(payment.Transaction, payment.Amount);
            if (!string.Equals(expectedHash, request.Validation, StringComparison.Ordinal))
                throw new UnauthorizedAccessException("Validation failed");

            payment.Completed = DateTimeOffset.UtcNow;
            payment.Hash = expectedHash;
            payment.T_Data = request.T_Data.GetRawText();


            if (payment.Session != null)
            {
                payment.Session.PaymentStatus = "paid";
                _context.Sessions.Update(payment.Session);
            }
            else
            {
                var session = await _context.Sessions.FirstOrDefaultAsync(s => s.Id == payment.Session.Id);
                if (session != null)
                {
                    session.PaymentStatus = "paid";
                    _context.Sessions.Update(session);
                }
            }

            await _context.SaveChangesAsync();

            return new PaymentResponseDto
            {
                Transaction = payment.Transaction,
                Amount = payment.Amount,
                Initiator = payment.Initiator,
                User = payment.User != null ? new UserReadDto
                {
                    Id = payment.User.Id,
                    Username = payment.User.Username,
                    Name = payment.User.Name,
                    Email = payment.User.Email,
                    PhoneNumber = payment.User.PhoneNumber,
                    BirthYear = payment.User.BirthYear,
                    Role = payment.User.Role,
                    CreatedAt = payment.User.CreatedAt
                } : null,
                Completed = payment.Completed.Value,
                Hash = payment.Hash
            };
        }
        private static string GeneratePaymentHash(string transaction, decimal amount)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var input = $"{transaction}:{amount}:{DateTimeOffset.UtcNow.Ticks}";
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }


        public async Task<List<PaymentResponseDto>> GetPaymentsForAnyUserAsync(string username)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
                return new List<PaymentResponseDto>();

            var payments = await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Session)
                .Include(p => p.ParkingLot)
                .Where(p => p.Initiator == user.Username)
                .ToListAsync();

            return payments.Select(p => new PaymentResponseDto
            {
                Transaction = p.Transaction,
                Amount = p.Amount,
                Initiator = p.Initiator,

                UserId = p.UserId,
                User = p.User == null ? null : new UserReadDto
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
                Session = p.Session == null ? null : new SessionReadDto(
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
                ParkingLot = p.ParkingLot
            })
            .ToList();

        }


        public async Task<List<PaymentResponseDto>> GetPaymentsForUserAsync(Guid userId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return new List<PaymentResponseDto>();

            var payments = await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Session)
                .Include(p => p.ParkingLot)
                .Where(p => p.UserId == userId)
                .ToListAsync();

            return payments.Select(p => new PaymentResponseDto
            {
                Transaction = p.Transaction,
                Amount = p.Amount,
                Initiator = p.Initiator,

                UserId = p.UserId,
                User = p.User == null ? null : new UserReadDto
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
                Session = p.Session == null ? null : new SessionReadDto(
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
                ParkingLot = p.ParkingLot
            })
            .ToList();
        }
    }
}
