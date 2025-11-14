using MobyPark.Models;

namespace MobyPark.Services
{
    public interface IProfileService
    {
        Task<UserReadDto?> GetProfileAsync(Guid userId);
        Task<(UserReadDto? dto, string? error, int? status)> UpdateProfileAsync(Guid userId, UpdateProfileDto dto);
        Task<(bool changed, string? error, int? status)> ChangePasswordAsync(Guid userId, string? currentPassword, string? newPassword);
    }
}
