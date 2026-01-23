using MobyPark.Models;

namespace MobyPark.Services
{
    public interface IBillingService
    {
        Task<(List<BillingReceiptDto>? dto, string? error, int? status)> GetReceiptsForUserAsync(string username);
        Task<(List<BillingReceiptDto>? dto, string? error, int? status)> GetAllReceiptsAsync();
        Task<(BillingReceiptDto? dto, string? error, int? status)> GetByIdAsync(Guid billingId);
    }
}
