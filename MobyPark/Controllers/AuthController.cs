using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobyPark.Models;
using MobyPark.Services;
using System.Security.Claims;

namespace MobyPark.Controllers
{
    [ApiController]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        [HttpPost("/register")]
        public async Task<ActionResult<UserReadDto>> Register(RegisterRequestDto request)
        {
            var by = request.BirthYear ?? 0;
            if (by != 0 && (by < 1900 || by > 2030))
                return BadRequest("BirthYear must be 0 (unset) or between 1900 and 2030.");

            var user = await authService.RegisterAsync(request);
            if (user is null) return Conflict("Username or email already exists.");

            var dto = new UserReadDto(user.Id, user.Username, user.Name, user.Email,
                                      user.PhoneNumber, user.BirthYear, user.Role, user.CreatedAt);
            return Ok(dto);
        }

        [HttpPost("/login")]
        public async Task<ActionResult<TokenResponseDto>> Login(LoginRequestDto request)
        {
            var result = await authService.LoginAsync(request);
            if (result is null) return Unauthorized("Invalid username or password.");
            return Ok(result);
        }

        [Authorize]
        [HttpPost("/logout")]
        public async Task<IActionResult> Logout()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();
            await authService.LogoutAsync(userId);
            return NoContent();
        }

        [HttpPost("/refresh-token")]
        public async Task<ActionResult<TokenResponseDto>> RefreshToken(RefreshTokenRequestDto request)
        {
            var result = await authService.RefreshTokensAsync(request);
            if (result is null) return Unauthorized("Invalid refresh token.");
            return Ok(result);
        }

        [Authorize]
        [HttpGet("/test-authenticated-only")]
        public IActionResult AuthenticatedOnlyEndpoint()
        {
            return Ok("You are authenticated!");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("/test-admin-only")]
        public IActionResult AdminOnlyEndpoint()
        {
            return Ok("You are an admin!");
        }
    }
}
