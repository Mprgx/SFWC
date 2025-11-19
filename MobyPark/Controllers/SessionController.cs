using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobyPark.Models;
using MobyPark.Services;
using System.Security.Claims;

namespace MobyPark.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class SessionController(ISessionService service) : ControllerBase
    {

        [HttpPost("/start-session")]
        public async Task<IActionResult> StartSession(SessionStartDto dto)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();

            var result = await service.StartSessionAsync(userId, dto);

            if (result is null)
                return BadRequest("Vehicle not found or session already active.");

            return Ok(result);
        }

        [HttpPost("/stop-session")]
        public async Task<IActionResult> StopSessionByPlate(SessionStopDto dto)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();

            var result = await service.StopSessionByPlateAsync(userId, dto);

            if (result is null)
                return BadRequest("Could not stop session. Check licensePlate.");

            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("/stop-session/{id:guid}")]
        public async Task<IActionResult> StopSession(Guid id)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();

            var result = await service.StopSessionByIdAsync(userId, id);

            if (result is null)
                return BadRequest("Could not stop this session.");

            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("/get-session-by-id")]
        public async Task<IActionResult> GetSession(Guid id)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();

            var session = await service.GetSessionByIdAsync(userId, id);

            if (session is null)
                return NotFound("Parking session not found.");

            return Ok(session);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("/cancel-session/{id:guid}")]
        public async Task<IActionResult> CancelSession(Guid id, CancelSessionDto dto)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();

            var result = await service.CancelSessionAsync(userId, id, dto);

            if (result is null)
                return BadRequest("Could not cancel session.");

            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("/refund-session/{id:guid}")]
        public async Task<IActionResult> RefundSession(Guid id, RefundRequestDto dto)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();

            var result = await service.RequestRefundAsync(userId, id, dto);

            if (result is null)
                return BadRequest("Refund not applicable.");

            return Ok(result);
        }
    }
}
