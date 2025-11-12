using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;
using System.Security.Claims;

namespace MobyPark.Controllers
{
    [Route("profile")]
    [ApiController]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly UserDbContext _db;

        public ProfileController(UserDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<UserReadDto>> GetMe()
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var me = await _db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new UserReadDto(
                    u.Id, u.Username, u.Name, u.Email,
                    u.PhoneNumber, u.BirthYear, u.Role, u.CreatedAt))
                .FirstOrDefaultAsync();

            return me is null ? NotFound() : Ok(me);
        }

        [HttpPut]
        public async Task<ActionResult<UserReadDto>> Update([FromBody] UpdateProfileDto dto)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null) return NotFound();

            bool changed = false;

            var newUsername = dto.Username?.Trim().ToLowerInvariant();
            var newEmail = dto.Email?.Trim().ToLowerInvariant();
            var newName = dto.Name?.Trim();
            var newPhone = dto.PhoneNumber?.Trim();

            if (!string.IsNullOrEmpty(newUsername) && !string.Equals(newUsername, user.Username, StringComparison.Ordinal))
            {
                var usernameTaken = await _db.Users.AnyAsync(u => u.Id != userId && u.Username == newUsername);
                if (usernameTaken) return Conflict("Username already in use.");
                user.Username = newUsername;
                changed = true;
            }

            if (!string.IsNullOrEmpty(newEmail) && !string.Equals(newEmail, user.Email, StringComparison.Ordinal))
            {
                var emailTaken = await _db.Users.AnyAsync(u => u.Id != userId && u.Email == newEmail);
                if (emailTaken) return Conflict("Email already in use.");
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

            var updated = new UserReadDto(
                user.Id, user.Username, user.Name, user.Email,
                user.PhoneNumber, user.BirthYear, user.Role, user.CreatedAt);

            return Ok(updated);
        }

        [HttpPut("password")]
        public async Task<IActionResult> ChangePassword(UpdatePasswordRequestDto req)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            if (string.IsNullOrWhiteSpace(req.NewPassword))
                return NoContent();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null) return NotFound();

            if (string.IsNullOrEmpty(req.CurrentPassword))
                return BadRequest("Current password is required.");

            var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();
            var verify = hasher.VerifyHashedPassword(user, user.PasswordHash, req.CurrentPassword);
            if (verify == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed)
                return BadRequest("Current password is incorrect.");

            bool strong = req.NewPassword.Length >= 8
                          && req.NewPassword.Any(char.IsDigit)
                          && req.NewPassword.Any(ch => !char.IsLetterOrDigit(ch));
            if (!strong) return BadRequest("New password must be 8+ chars with a number and a special character.");

            user.PasswordHash = hasher.HashPassword(user, req.NewPassword);
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        private bool TryGetUserId(out Guid id)
        {
            var s = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(s, out id);
        }
    }
}
