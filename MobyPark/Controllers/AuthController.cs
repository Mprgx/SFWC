using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MobyPark.Constants;
using MobyPark.Models;
using MobyPark.Services;

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
            {
                return BadRequest(new
                {
                    status = "error",
                    message = "BirthYear must be 0 (unset) or between 1900 and 2030."
                });
            }

            var dto = await authService.RegisterAsync(request);
            if (dto is null)
            {
                return Conflict(new
                {
                    status = "error",
                    message = "Username or email already exists."
                });
            }

            return Created(string.Empty, new
            {
                status = "success",
                user = dto
            });
        }

        [HttpPost("/login")]
        public async Task<ActionResult<TokenResponseDto>> Login(LoginRequestDto request)
        {
            var result = await authService.LoginAsync(request);
            if (result is null)
            {
                return Unauthorized(new
                {
                    status = "error",
                    message = "Invalid username or password. Or account may be disabled. Check Duplicate Accounts for more info."
                });
            }

            return Ok(new
            {
                status = "success",
                accessToken = result.AccessToken,
                refreshToken = result.RefreshToken
            });
        }

        [Authorize]
        [HttpPost("/logout")]
        public async Task<IActionResult> Logout()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new
                {
                    status = "error",
                    message = "Missing or invalid user id claim."
                });
            }

            await authService.LogoutAsync(userId);

            return Ok(new
            {
                status = "success",
                message = "Logged out successfully."
            });
        }

        [HttpPost("/refresh-token")]
        public async Task<ActionResult<TokenResponseDto>> RefreshToken(RefreshTokenRequestDto request)
        {
            var result = await authService.RefreshTokensAsync(request);
            if (result is null)
            {
                return Unauthorized(new
                {
                    status = "error",
                    message = "Invalid refresh token."
                });
            }

            return Ok(new
            {
                status = "success",
                accessToken = result.AccessToken,
                refreshToken = result.RefreshToken
            });
        }

        [Authorize]
        [HttpGet("/test-authenticated-only")]
        public IActionResult AuthenticatedOnlyEndpoint()
        {
            return Ok(new
            {
                status = "success",
                message = "You are authenticated!"
            });
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpGet("/test-admin-only")]
        public IActionResult AdminOnlyEndpoint()
        {
            return Ok(new
            {
                status = "success",
                message = "You are an admin!"
            });
        }
    }
}
