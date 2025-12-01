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

        public async Task<Payment> CompletePaymentAsync(
            Guid userId,
            string transactionId,
            PaymentValidationDto request)
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.Transaction == transactionId);

            if (payment == null)
                throw new KeyNotFoundException("Payment not found");

            if (request.T_Data.ValueKind == JsonValueKind.Undefined ||
                string.IsNullOrWhiteSpace(request.Validation))
                throw new UnauthorizedAccessException("Required field missing");
            payment.Completed = DateTime.UtcNow;
            payment.Hash = GeneratePaymentHash(payment.Transaction, payment.Amount);
            session.PaymentStatus = "paid";

            if (payment.Hash != request.Validation)
                throw new UnauthorizedAccessException("Validation failed");

            payment.Completed = DateTimeOffset.UtcNow;
            payment.T_Data = request.T_Data.GetRawText();
            return new PaymentResponseDto
            {
                Transaction = payment.Transaction,
                Amount = payment.Amount,
                Initiator = payment.Initiator,
                User = new UserReadDto
                {
                    Id = payment.User.Id,
                    Username = payment.User.Username,
                    Name = payment.User.Name,
                    Email = payment.User.Email,
                    PhoneNumber = payment.User.PhoneNumber,
                    BirthYear = payment.User.BirthYear,
                    Role = payment.User.Role,
                    CreatedAt = payment.User.CreatedAt
                },
                Completed = payment.Completed.Value,
                Hash = payment.Hash
            };
        }

            await _context.SaveChangesAsync();

            return payment;
        }

        public async Task<List<PaymentResponseDto?>> GetPaymentsForUserAsync(Guid userId)
        {
            // Haal de juiste user op
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return new List<PaymentResponseDto?>();

            // Haal payments op van de gebruiker
            var payments = await _context.Payments
                .Where(p => p.Initiator == user.Id)
                .ToListAsync();

            // Map naar DTO
            return payments.Select(p => (PaymentResponseDto?)new PaymentResponseDto
            {
                Transaction = p.Transaction,
                Amount = p.Amount,
                Initiator = p.Initiator,
                Completed = p.Completed,
                Hash = p.Hash
            }).ToList();
        }

        public async Task<List<PaymentResponseDto?>> GetPaymentsForAnyUserAsync(string username)
        {
            // Zoek de gebruiker waar de admin informatie van wil
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return new List<PaymentResponseDto?>();

            // Haal payments op
            var payments = await _context.Payments
                .Where(p => p.Initiator == user.Id)
                .ToListAsync();

            // Map naar DTO
            return payments.Select(p => (PaymentResponseDto?)new PaymentResponseDto
            {
                Transaction = p.Transaction,
                Amount = p.Amount,
                Initiator = p.Initiator,
                Completed = p.Completed,
                Hash = p.Hash
            }).ToList();
        }

    }
}
