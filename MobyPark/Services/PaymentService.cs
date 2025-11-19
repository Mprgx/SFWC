using Microsoft.EntityFrameworkCore;
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

        public async Task<PaymentResponseDto?> FulfillPaymentAsync(Guid userId, PaymentsDto request)
        {
            var session = await _context.Sessions
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (session == null)
                return null;

            if (request.Amount != session.Cost)
                return null;

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.Transaction == request.Transaction);

            if (payment == null)
                return null;

            payment.Completed = true;
            payment.Hash = GeneratePaymentHash(payment.Transaction, payment.Amount);
            session.PaymentStatus = "paid";

            _context.Update(payment);
            _context.Update(session);
            await _context.SaveChangesAsync();

            return new PaymentResponseDto
            {
                Transaction = payment.Transaction,
                Amount = payment.Amount,
                Initiator = payment.Initiator,
                Completed = payment.Completed,
                Hash = payment.Hash
            };
        }

        private static string GeneratePaymentHash(string transaction, decimal amount)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var input = $"{transaction}:{amount}:{DateTimeOffset.UtcNow.Ticks}";
            return Convert.ToBase64String(
                sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input))
            );
        }
    }
}
