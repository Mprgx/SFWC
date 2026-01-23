using MobyPark.Models;

namespace MobyPark.Services
{
    public interface IDuplicateUsersService
    {
        Task<(DuplicateUserReadDto? dto, string? error, int status)> GetByUsernameAsync(string username);
        Task<(DuplicateUserReadDto? dto, string? error, int status)> GetByEmailAsync(string email);
        Task<(DuplicateUserReadDto? dto, string? error, int status)> GetByIdAsync(Guid id);
        Task<(DuplicateUserReadDto? dto, string? error, int status)> GetByLegacyIdAsync(string legacyId);

        Task<(DuplicateUserResolveResponseDto? dto, string? error, int status)> ResolveAsync(Guid duplicateUserId, DuplicateUserResolveRequestDto request);
    }
}
