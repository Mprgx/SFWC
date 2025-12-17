using Microsoft.AspNetCore.Mvc;

using MobyPark.Models;

namespace MobyPark.Services
{
    public interface IDiscountService
    {
        Task<(int statusCode, string message, DiscountReadDto?)> CreateDiscountAsync(DiscountPostDto dto, Guid userId);
    }
}
