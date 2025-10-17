using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MobyPark.Entities;
using MobyPark.Models;
using MobyPark.Services;

namespace MobyPark.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<ActionResult<UserReadDto>> Register(RegisterRequestDto request)
        {
            var user = await authService.RegisterAsync(request);
            if (user is null)
                return BadRequest("Username or email already exists.");

            var dto = new UserReadDto(user.Id, user.Username, user.Name, user.Email,
                                      user.PhoneNumber, user.BirthYear, user.Role, user.CreatedAt);
            return Ok(dto);
        }

        [HttpPost("login")]
        public async Task<ActionResult<TokenResponseDto>> Login(LoginRequestDto request)
        {
            var result = await authService.LoginAsync(request);
            if (result is null)
                return BadRequest("Invalid username or password.");

            return Ok(result);
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<TokenResponseDto>> RefreshToken(RefreshTokenRequestDto request)
        {
            var result = await authService.RefreshTokensAsync(request);
            if (result is null || result.AccessToken is null || result.RefreshToken is null)
                return Unauthorized("Invalid refresh token.");

            return Ok(result);
        }

        [Authorize]
        [HttpGet]
        public IActionResult AuthenticatedOnlyEndpoint()
        {
            return Ok("You are authenticated!");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin-only")]
        public IActionResult AdminOnlyEndpoint()
        {
            return Ok("You are an admin!");
        }
    }
}
