using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobyPark.Models;
using MobyPark.Services;
using System.Security.Claims;

namespace MobyPark.Controllers
{
    [Route("profile")]
    [ApiController]
    [Authorize]
    public class ProfileController(IProfileService profileService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<UserReadDto>> GetMe()
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var me = await profileService.GetProfileAsync(userId);
            return me is null ? NotFound() : Ok(me);
        }

        [HttpPut]
        public async Task<ActionResult<UserReadDto>> Update([FromBody] UpdateProfileDto dto)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var (updated, error, status) = await profileService.UpdateProfileAsync(userId, dto);

            if (status == 404) return NotFound();
            if (status == 409) return Conflict(error);

            return Ok(updated);
        }

        [HttpPut("password")]
        public async Task<IActionResult> ChangePassword(UpdatePasswordRequestDto req)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var (changed, error, status) = await profileService.ChangePasswordAsync(userId, req.CurrentPassword, req.NewPassword);

            if (status == 204) return NoContent();
            if (status == 404) return NotFound();
            if (status == 400) return BadRequest(error);

            return NoContent();
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteMe()
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var (deleted, error, status) = await profileService.DeleteProfileAsync(userId);

            if (status == 404) return NotFound();
            if (status == 400) return BadRequest(error);
            if (status == 409) return Conflict(error);
            if (status == 204) return NoContent();

            if (status.HasValue)
                return StatusCode(status.Value, error);

            return StatusCode(500, error ?? "Unexpected error while deleting profile.");
        }

        private bool TryGetUserId(out Guid id)
        {
            var s = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(s, out id);
        }
    }
}