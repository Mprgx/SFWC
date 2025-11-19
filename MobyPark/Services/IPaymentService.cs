using MobyPark.Models;

namespace MobyPark.Services
{
    public interface IPaymentService
    {
        Task<PaymentResponseDto?> FulfillPaymentAsync(Guid userId, PaymentsDto request);
    }
}
