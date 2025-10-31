using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;
using System.Security.Claims;

namespace MobyPark.Controllers
{
    [Route("api/")]
    [ApiController]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly UserDbContext _db;

        public ProfileController(UserDbContext db)
        {
            _db = db;
        }

        [HttpGet("my-profile")]
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

        [HttpPut("name")]
        public async Task<IActionResult> UpdateName(UpdateNameRequestDto req)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null) return NotFound();

            user.Name = req.Name;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("username")]
        public async Task<IActionResult> UpdateUsername(UpdateUsernameRequestDto req)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null) return NotFound();

            var newUsername = req.Username.Trim().ToLowerInvariant();
            var taken = await _db.Users.AnyAsync(u => u.Username == newUsername && u.Id != userId);
            if (taken) return BadRequest("Username already exists.");

            user.Username = newUsername;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("email")]
        public async Task<IActionResult> UpdateEmail(UpdateEmailRequestDto req)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null) return NotFound();

            var newEmail = req.Email.Trim().ToLowerInvariant();
            var taken = await _db.Users.AnyAsync(u => u.Email == newEmail && u.Id != userId);
            if (taken) return BadRequest("Email already exists.");

            user.Email = newEmail;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("phone")]
        public async Task<IActionResult> UpdatePhone(UpdatePhoneRequestDto req)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null) return NotFound();

            user.PhoneNumber = req.PhoneNumber;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("birthyear")]
        public async Task<IActionResult> UpdateBirthYear(UpdateBirthYearRequestDto req)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null) return NotFound();

            user.BirthYear = req.BirthYear;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("password")]
        public async Task<IActionResult> ChangePassword(UpdatePasswordRequestDto req)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null) return NotFound();

            var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();
            var verify = hasher.VerifyHashedPassword(user, user.PasswordHash, req.CurrentPassword);
            if (verify == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed)
                return BadRequest("Current password is incorrect.");

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
