using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MobyPark.Services
{
    public class AuthService(UserDbContext context, IConfiguration configuration) : IAuthService
    {
        public async Task<TokenResponseDto?> LoginAsync(LoginRequestDto request)
        {
            var uname = request.Username.Trim().ToLowerInvariant();
            var user = await context.Users.FirstOrDefaultAsync(u => u.Username == uname);
            if (user is null) return null;

            var result = new PasswordHasher<User>()
                .VerifyHashedPassword(user, user.PasswordHash, request.Password);

            if (result == PasswordVerificationResult.Failed) return null;

            return await CreateTokenResponse(user);
        }

        public async Task<User?> RegisterAsync(RegisterRequestDto request)
        {
            var username = request.Username.Trim().ToLowerInvariant();
            var email = request.Email.Trim().ToLowerInvariant();

            if (await context.Users.AnyAsync(u => u.Username == username)) return null;
            if (await context.Users.AnyAsync(u => u.Email == email)) return null;

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                Email = email,
                Name = request.Name,
                PhoneNumber = request.PhoneNumber,
                BirthYear = request.BirthYear,
                CreatedAt = DateTimeOffset.UtcNow,
                Role = UserRole.Customer
            };

            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, request.Password);

            context.Users.Add(user);
            await context.SaveChangesAsync();
            return user;
        }

        public async Task<TokenResponseDto?> RefreshTokensAsync(RefreshTokenRequestDto request)
        {
            var user = await ValidateRefreshTokenAsync(request.UserId, request.RefreshToken);
            if (user is null)
                return null;

            return await CreateTokenResponse(user);
        }

        private async Task<User?> ValidateRefreshTokenAsync(Guid userId, string refreshToken)
        {
            var user = await context.Users.FindAsync(userId);
            if (user is null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTimeOffset.UtcNow)
                return null;

            return user;
        }

        private async Task<TokenResponseDto> CreateTokenResponse(User user)
        {
            return new TokenResponseDto
            {
                AccessToken = CreateToken(user),
                RefreshToken = await GenerateAndSaveRefreshTokenAsync(user)
            };
        }
       
        private string GenerateRefreshToken()
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        private async Task<string> GenerateAndSaveRefreshTokenAsync(User user)
        {
            var refreshToken = GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTimeOffset.UtcNow.AddDays(7);
            await context.SaveChangesAsync();
            return refreshToken;
        }

        private string CreateToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration.GetValue<string>("AppSettings:Token")!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

            var token = new JwtSecurityToken(
                issuer: configuration["AppSettings:Issuer"],
                audience: configuration["AppSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
