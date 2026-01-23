using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public sealed class DuplicateUsersService : IDuplicateUsersService
    {
        private readonly UserDbContext _db;
        private readonly IEncryptionService _enc;

        public DuplicateUsersService(UserDbContext db, IEncryptionService enc)
        {
            _db = db;
            _enc = enc;
        }

        public async Task<(DuplicateUserReadDto? dto, string? error, int status)> GetByUsernameAsync(string username)
        {
            username = (username ?? "").Trim().ToLowerInvariant();
            if (username.Length == 0)
                return (null, "Username is required.", 400);

            var dupe = await _db.DuplicateUsers.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Username.ToLower() == username);

            if (dupe is null)
                return (null, "Duplicate user not found for this username.", 404);

            return (MapReadDto(dupe), BuildFoundMessage(dupe), 200);
        }

        public async Task<(DuplicateUserReadDto? dto, string? error, int status)> GetByEmailAsync(string email)
        {
            email = (email ?? "").Trim();
            if (email.Length == 0)
                return (null, "Email is required.", 400);

            var all = await _db.DuplicateUsers.AsNoTracking()
                .Select(d => new { d.Id, d.Email })
                .ToListAsync();

            var foundId = all.FirstOrDefault(x =>
                string.Equals((_enc.Decrypt(x.Email) ?? ""), email, StringComparison.OrdinalIgnoreCase)
            )?.Id;

            if (foundId is null)
                return (null, "Duplicate user not found for this email.", 404);

            var dupe = await _db.DuplicateUsers.AsNoTracking()
                .FirstAsync(d => d.Id == foundId.Value);

            return (MapReadDto(dupe), BuildFoundMessage(dupe), 200);
        }

        public async Task<(DuplicateUserReadDto? dto, string? error, int status)> GetByIdAsync(Guid id)
        {
            if (id == Guid.Empty)
                return (null, "Invalid id.", 400);

            var dupe = await _db.DuplicateUsers.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);

            if (dupe is null)
                return (null, "Duplicate user not found for this id.", 404);

            return (MapReadDto(dupe), BuildFoundMessage(dupe), 200);
        }

        public async Task<(DuplicateUserReadDto? dto, string? error, int status)> GetByLegacyIdAsync(string legacyId)
        {
            legacyId = (legacyId ?? "").Trim();
            if (legacyId.Length == 0)
                return (null, "LegacyId is required.", 400);

            var dupe = await _db.DuplicateUsers.AsNoTracking()
                .FirstOrDefaultAsync(d => d.LegacyId == legacyId);

            if (dupe is null)
                return (null, "Duplicate user not found for this legacy id.", 404);

            return (MapReadDto(dupe), BuildFoundMessage(dupe), 200);
        }

        public async Task<(DuplicateUserResolveResponseDto? dto, string? error, int status)> ResolveAsync(
            Guid duplicateUserId,
            DuplicateUserResolveRequestDto request)
        {
            if (duplicateUserId == Guid.Empty)
                return (null, "Invalid duplicate user id.", 400);

            if (request is null)
                return (null, "Request body is required.", 400);

            var dupe = await _db.DuplicateUsers
                .FirstOrDefaultAsync(d => d.Id == duplicateUserId);

            if (dupe is null)
                return (null, "Duplicate user not found.", 404);

            var username = (request.Username ?? "").Trim().ToLowerInvariant();
            var name = (request.Name ?? "").Trim();
            var email = (request.Email ?? "").Trim();
            var phone = (request.PhoneNumber ?? "").Trim();

            if (username.Length == 0 || name.Length == 0 || email.Length == 0 || phone.Length == 0)
                return (null, "Username, Name, Email and PhoneNumber are required.", 400);

            if (username.Length > 40)
                return (null, "Username is too long (max 40).", 400);

            if (name.Length > 70)
                return (null, "Name is too long (max 70).", 400);

            var usernameExists = await _db.Users.AsNoTracking()
                .AnyAsync(u => u.Username.ToLower() == username);

            if (usernameExists)
                return (null, "Username already exists in Users table. Choose another one.", 409);

            var users = await _db.Users.AsNoTracking()
                .Select(u => new { u.Id, u.Email, u.PhoneNumber })
                .ToListAsync();

            if (users.Any(u => string.Equals((_enc.Decrypt(u.Email) ?? ""), email, StringComparison.OrdinalIgnoreCase)))
                return (null, "Email already exists in Users table. Choose another email.", 409);

            if (users.Any(u => string.Equals((_enc.Decrypt(u.PhoneNumber) ?? ""), phone, StringComparison.OrdinalIgnoreCase)))
                return (null, "PhoneNumber already exists in Users table. Choose another phone number.", 409);

            string? tempPassword = null;
            string passwordToUse;

            if (!string.IsNullOrWhiteSpace(request.NewPassword))
            {
                passwordToUse = request.NewPassword.Trim();
                if (passwordToUse.Length < 6)
                    return (null, "NewPassword must be at least 6 characters.", 400);
            }
            else
            {
                tempPassword = "Temp123!";
                passwordToUse = tempPassword;
            }

            var role = MapRole(request.Role);

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                dupe.Username = username;
                dupe.Name = name;
                dupe.Email = _enc.Encrypt(email) ?? "";
                dupe.PhoneNumber = _enc.Encrypt(phone) ?? "";
                dupe.BirthYear = request.BirthYear;
                dupe.Role = role;

                var newUser = new User
                {
                    Id = Guid.NewGuid(),
                    LegacyId = dupe.LegacyId,
                    Username = username,
                    Name = name,
                    Email = _enc.Encrypt(email) ?? "",
                    PhoneNumber = _enc.Encrypt(phone) ?? "",
                    BirthYear = request.BirthYear ?? 1900,
                    CreatedAt = dupe.CreatedAt,
                    Role = role,

                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(passwordToUse),

                    LegacyPasswordHash = dupe.LegacyPasswordHash,
                    LegacyPasswordAlgo = dupe.LegacyPasswordAlgo ?? "MD5"
                };

                _db.Users.Add(newUser);

                dupe.ExistingUserId = newUser.Id;

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return (new DuplicateUserResolveResponseDto
                {
                    Message = "Duplicate user updated and moved to Users table successfully.",
                    CreatedUserId = newUser.Id,
                    DuplicateUserId = dupe.Id,
                    TempPassword = tempPassword
                }, null, 200);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return (null, "Resolve failed: " + ex.Message, 500);
            }
        }

        private DuplicateUserReadDto MapReadDto(DuplicateUser d)
        {
            return new DuplicateUserReadDto
            {
                Id = d.Id,
                LegacyId = d.LegacyId,
                Username = d.Username,
                Name = d.Name,
                Email = _enc.Decrypt(d.Email) ?? "",
                PhoneNumber = _enc.Decrypt(d.PhoneNumber) ?? "",
                BirthYear = d.BirthYear,
                Role = d.Role.ToString(),
                CreatedAt = d.CreatedAt,
                DuplicateReason = d.DuplicateReason,
                ImportedAt = d.ImportedAt,
                ExistingUserId = d.ExistingUserId
            };
        }

        private static string BuildFoundMessage(DuplicateUser d)
        {
            return $"Duplicate user found (Reason: {d.DuplicateReason}). Please update and resolve this record.";
        }

        private static UserRole MapRole(string? role)
        {
            if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
                return UserRole.Admin;

            return UserRole.Customer;
        }
    }
}
