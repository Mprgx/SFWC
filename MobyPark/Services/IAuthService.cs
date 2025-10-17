using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public interface IAuthService
    {
        Task<User?> RegisterAsync(RegisterRequestDto request);
        Task<TokenResponseDto?> LoginAsync(LoginRequestDto request);
        Task<TokenResponseDto?> RefreshTokensAsync(RefreshTokenRequestDto request);
    }
}
