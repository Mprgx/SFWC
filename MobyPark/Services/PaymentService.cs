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

            if (payment.Hash != request.Validation)
                throw new UnauthorizedAccessException("Validation failed");

            payment.Completed = DateTimeOffset.UtcNow;
            payment.T_Data = request.T_Data.GetRawText();

            await _context.SaveChangesAsync();

            return payment;
        }
    }
}
