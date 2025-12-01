using MobyPark.Models;

namespace MobyPark.Services
{
    public interface IPaymentService
    {
        Task<PaymentResponseDto?> FulfillPaymentAsync(Guid userId, PaymentsDto request);
        Task<List<PaymentResponseDto?>> GetPaymentsForUserAsync(Guid userId);
        Task<List<PaymentResponseDto?>> GetPaymentsForAnyUserAsync(string username);

    }
}
