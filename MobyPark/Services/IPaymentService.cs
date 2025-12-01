using MobyPark.Models;
using MobyPark.Entities;

namespace MobyPark.Services
{
    public interface IPaymentService
    {
        Task<Payment> CompletePaymentAsync(Guid userId, string transactionId, PaymentValidationDto request);
        Task<List<PaymentResponseDto?>> GetPaymentsForUserAsync(Guid userId);
        Task<List<PaymentResponseDto?>> GetPaymentsForAnyUserAsync(string username);
    }
}
