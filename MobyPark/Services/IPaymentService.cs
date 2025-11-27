using MobyPark.Models;
using MobyPark.Entities;

namespace MobyPark.Services
{
    public interface IPaymentService
    {
        Task<Payment> CompletePaymentAsync(Guid userId, string transactionId, PaymentValidationDto request);
    }
}
