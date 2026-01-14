using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public interface IInvoiceService
    {
        Task<byte[]?> GenerateMonthlyInvoicePdfAsync(Guid userId, int month, int year);
        Task<byte[]?> GenerateAllPdfAsync(Guid userId);
    }

}
