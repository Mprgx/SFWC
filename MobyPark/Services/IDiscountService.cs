using MobyPark.Models;

namespace MobyPark.Services
{
    public interface IDiscountService
    {
        Task<(int statusCode, string message, DiscountReadDto?)> CreateDiscountAsync(DiscountPostDto dto, Guid userId);
        Task<(int statusCode, string message)> ApplyDiscountAsync(string? discountCode, string transaction, Guid userId);
        Task<(int statusCode, string message, decimal? amountWithDiscount, string? normalizedCode)> PreviewDiscountAsync(string? discountCode, Guid userId, int parkingLotId, DateTimeOffset atTime, decimal amount);
    }
}
