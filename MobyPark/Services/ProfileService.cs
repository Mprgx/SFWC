using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public class ProfileService : IProfileService
    {
        private readonly UserDbContext _db;

        public ProfileService(UserDbContext db) => _db = db;

        public async Task<UserReadDto?> GetProfileAsync(Guid userId)
        {
            return await _db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new UserReadDto(
                    u.Id, u.Username, u.Name, u.Email, u.PhoneNumber, u.BirthYear, u.Role, u.CreatedAt))
                .FirstOrDefaultAsync();
        }

        public async Task<(UserReadDto? dto, string? error, int? status)> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null) return (null, null, 404);

            bool changed = false;

            var newUsername = dto.Username?.Trim().ToLowerInvariant();
            var newEmail = dto.Email?.Trim().ToLowerInvariant();
            var newName = dto.Name?.Trim();
            var newPhone = dto.PhoneNumber?.Trim();

            if (!string.IsNullOrEmpty(newUsername) && !string.Equals(newUsername, user.Username, StringComparison.Ordinal))
            {
                var taken = await _db.Users.AnyAsync(u => u.Id != userId && u.Username == newUsername);
                if (taken) return (null, "Username already in use.", 409);
                user.Username = newUsername;
                changed = true;
            }

            if (!string.IsNullOrEmpty(newEmail) && !string.Equals(newEmail, user.Email, StringComparison.Ordinal))
            {
                var taken = await _db.Users.AnyAsync(u => u.Id != userId && u.Email == newEmail);
                if (taken) return (null, "Email already in use.", 409);
                user.Email = newEmail;
                changed = true;
            }

            if (!string.IsNullOrWhiteSpace(newName) && !string.Equals(newName, user.Name, StringComparison.Ordinal))
            {
                user.Name = newName;
                changed = true;
            }

            if (!string.IsNullOrWhiteSpace(newPhone) && !string.Equals(newPhone, user.PhoneNumber, StringComparison.Ordinal))
            {
                user.PhoneNumber = newPhone;
                changed = true;
            }

            if (dto.BirthYear.HasValue)
            {
                var by = dto.BirthYear.Value;
                if (by == 0)
                {

                }
                else if (by >= 1900 && by <= 2030 && by != user.BirthYear)
                {
                    user.BirthYear = by;
                    changed = true;
                }
            }

            if (changed) await _db.SaveChangesAsync();

            var result = new UserReadDto(
                user.Id, user.Username, user.Name, user.Email,
                user.PhoneNumber, user.BirthYear, user.Role, user.CreatedAt);

            return (result, null, null);
        }

        public async Task<(bool changed, string? error, int? status)> ChangePasswordAsync(Guid userId, string? currentPassword, string? newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword)) return (false, null, 204);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null) return (false, null, 404);

            if (string.IsNullOrEmpty(currentPassword))
                return (false, "Current password is required.", 400);

            var hasher = new PasswordHasher<User>();
            var verify = hasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword);
            if (verify == PasswordVerificationResult.Failed)
                return (false, "Current password is incorrect.", 400);

            bool strong = newPassword.Length >= 8
                          && newPassword.Any(char.IsDigit)
                          && newPassword.Any(ch => !char.IsLetterOrDigit(ch));
            if (!strong)
                return (false, "New password must be 8+ chars with a number and a special character.", 400);

            user.PasswordHash = hasher.HashPassword(user, newPassword);
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;

            await _db.SaveChangesAsync();
            return (true, null, 204);
        }
    }
}
