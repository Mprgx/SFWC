using MobyPark.Models;

namespace MobyPark.Services
{
    public interface IPaymentService
    {
        Task<(PaymentReadDto? dto, string? error, int? status)> CompletePaymentAsync(Guid userId, string transactionId, PaymentValidationDto request);
        Task<(PaymentReadDto? dto, string? error, int? status)> FulfillPaymentAsync( Guid userId, PaymentsDto paymentRequest);
        Task<(List<PaymentReadDto>? dto, string? error, int? status)> GetPaymentsForUserAsync(Guid userId);
        Task<(List<PaymentReadDto>? dto, string? error, int? status)> GetPaymentsForAnyUserAsync(string username);
        Task<(bool dto, string? error, int? status)> DeletePaymentByTransactionId(string transactionId);
    }
}
