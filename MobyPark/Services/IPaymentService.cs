using MobyPark.Models;
using MobyPark.Entities;

namespace MobyPark.Services
{
    public interface IPaymentService
    {
        Task<PaymentResponseDto?> CompletePaymentAsync(Guid userId, string transactionId, PaymentValidationDto request);
        Task<List<PaymentResponseDto?>> GetPaymentsForUserAsync(Guid userId);
        Task<List<PaymentResponseDto?>> GetPaymentsForAnyUserAsync(string username);
        Task<bool> DeletePaymentByTransactionId(string transactionId);
        Task<PaymentResponseDto> FulfillPaymentAsync(string userId, PaymentsDto paymentRequest);
    }
}
