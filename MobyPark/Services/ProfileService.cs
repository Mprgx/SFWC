using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public class ProfileService(UserDbContext context, IEncryptionService encryption) : IProfileService
    {
        //GET
        public async Task<UserReadDto?> GetProfileAsync(Guid userId)
        {
            var user = await context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user is null) return null;

            var emailPlain = string.IsNullOrEmpty(user.Email)
                ? string.Empty
                : encryption.Decrypt(user.Email) ?? string.Empty;

            var phonePlain = string.IsNullOrEmpty(user.PhoneNumber)
                ? string.Empty
                : encryption.Decrypt(user.PhoneNumber) ?? string.Empty;

            return new UserReadDto
            {
                Id = user.Id,
                Username = user.Username,
                Name = user.Name,
                Email = emailPlain,
                PhoneNumber = phonePlain,
                BirthYear = user.BirthYear,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };
        }

        //PUT
        public async Task<(UserReadDto? dto, string? error, int? status)> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null) return (null, null, 404);

            bool changed = false;

            var newUsername = dto.Username?.Trim().ToLowerInvariant();
            var newEmailPlain = dto.Email?.Trim().ToLowerInvariant();
            var newName = dto.Name?.Trim();
            var newPhonePlain = dto.PhoneNumber?.Trim();

            var currentEmailPlain = string.IsNullOrEmpty(user.Email)
                ? string.Empty
                : encryption.Decrypt(user.Email)!.Trim().ToLowerInvariant();

            var currentPhonePlain = string.IsNullOrEmpty(user.PhoneNumber)
                ? string.Empty
                : encryption.Decrypt(user.PhoneNumber)!.Trim();

            if (!string.IsNullOrEmpty(newUsername) &&
                !string.Equals(newUsername, user.Username, StringComparison.Ordinal))
            {
                var taken = await context.Users
                    .AnyAsync(u => u.Id != userId && u.Username == newUsername);
                if (taken) return (null, "Username already in use.", 409);

                user.Username = newUsername;
                changed = true;
            }

            if (!string.IsNullOrEmpty(newEmailPlain) &&
                !string.Equals(newEmailPlain, currentEmailPlain, StringComparison.OrdinalIgnoreCase))
            {
                var others = await context.Users.AsNoTracking()
                    .Where(u => u.Id != userId)
                    .ToListAsync();

                var taken = others.Any(u =>
                {
                    if (string.IsNullOrEmpty(u.Email)) return false;
                    var existingPlain = encryption.Decrypt(u.Email)!.Trim().ToLowerInvariant();
                    return string.Equals(existingPlain, newEmailPlain, StringComparison.OrdinalIgnoreCase);
                });

                if (taken) return (null, "Email already in use.", 409);

                user.Email = encryption.Encrypt(newEmailPlain)!;
                changed = true;
            }

            if (!string.IsNullOrWhiteSpace(newName) &&
                !string.Equals(newName, user.Name, StringComparison.Ordinal))
            {
                user.Name = newName;
                changed = true;
            }

            if (!string.IsNullOrWhiteSpace(newPhonePlain) &&
                !string.Equals(newPhonePlain, currentPhonePlain, StringComparison.Ordinal))
            {
                user.PhoneNumber = encryption.Encrypt(newPhonePlain)!;
                changed = true;
            }

            if (dto.BirthYear.HasValue)
            {
                var by = dto.BirthYear.Value;
                if (by == 0)
                {
                    // no change
                }
                else if (by >= 1900 && by <= 2030 && by != user.BirthYear)
                {
                    user.BirthYear = by;
                    changed = true;
                }
            }

            if (changed) await context.SaveChangesAsync();

            var updatedEmail = string.IsNullOrEmpty(user.Email)
                ? string.Empty
                : encryption.Decrypt(user.Email) ?? string.Empty;

            var updatedPhone = string.IsNullOrEmpty(user.PhoneNumber)
                ? string.Empty
                : encryption.Decrypt(user.PhoneNumber) ?? string.Empty;

            var result = new UserReadDto
            {
                Id = user.Id,
                Username = user.Username,
                Name = user.Name,
                Email = updatedEmail,
                PhoneNumber = updatedPhone,
                BirthYear = user.BirthYear,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };

            return (result, null, null);
        }

        //PUT
        public async Task<(bool changed, string? error, int? status)> ChangePasswordAsync(Guid userId, string? currentPassword, string? newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword)) return (false, null, 204);

            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
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

            await context.SaveChangesAsync();
            return (true, null, 204);
        }
    }
}
