using MobyPark.Models;

namespace MobyPark.Services
{
    public interface IBillingService
    {
        Task<List<BillingReceiptDto>> GetReceiptsForUserAsync(string username);
        Task<List<BillingReceiptDto>> GetAllReceiptsAsync();
        Task<BillingReceiptDto?> GetByIdAsync(Guid billingId);
    }
}
