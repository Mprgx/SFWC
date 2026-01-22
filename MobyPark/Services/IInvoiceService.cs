using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public interface IInvoiceService
    {
        Task<byte[]?> GenerateMonthlyInvoicePdfAsync(Guid userId, Guid companyIdGiven, int month, int year);
        Task<byte[]?> GenerateAllPdfAsync(Guid userId, Guid companyIdGiven, DateTimeOffset? startDate, DateTimeOffset? endDate);
    }

}
