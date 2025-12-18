using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MobyPark.Constants;
using MobyPark.Entities;
using MobyPark.Models;
using MobyPark.Services;

namespace MobyPark.Controllers
{
    [ApiController]
    [Authorize]
    [Route("parkinglots")]
    public class SessionController(ISessionService service) : ControllerBase
    {
        [HttpPost("start-session")]
        public async Task<ActionResult<SessionReadDto>> StartSession(SessionStartDto dto)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();

            var (session, error, status) = await service.StartSessionAsync(userId, dto);

            if (status == 404) return NotFound(error);
            if (status == 409) return Conflict(error);
            if (status == 400) return BadRequest(error);

            if (status.HasValue)
                return StatusCode(status.Value, error);

            if (session is null)
                return StatusCode(500, "Unexpected null session.");

            return Ok(session);
        }

        [HttpPost("stop-session")]
        public async Task<ActionResult<StopSessionResponseDto>> StopSessionByPlate(SessionStopDto dto)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();

            var (result, error, status) = await service.StopSessionByPlateAsync(userId, dto);

            if (status == 400) return BadRequest(error);
            if (status == 404) return NotFound(error);
            if (status == 409) return Conflict(error);

            if (status.HasValue)
                return StatusCode(status.Value, error);

            if (result is null)
                return StatusCode(500, "Unexpected null stop-session result.");

            return Ok(result);
        }

        [HttpGet("sessions")]
        public async Task<ActionResult<List<SessionReadDto>>> GetMySessions([FromQuery] bool onlyActive = false)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();

            var (sessions, error, status) = await service.GetAllForUserAsync(userId, onlyActive);

            if (status == 400) return BadRequest(error);
            if (status.HasValue) return StatusCode(status.Value, error);

            return Ok(sessions ?? new List<SessionReadDto>());
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpPut("stop-session/{id:guid}")]
        public async Task<ActionResult<StopSessionResponseDto>> StopSession(Guid id, [FromQuery] string? discountcode)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();

            var (result, error, status) = await service.StopSessionByIdAsync(userId, id);

            if (status == 400) return BadRequest(error);
            if (status == 404) return NotFound(error);
            if (status == 409) return Conflict(error);

            if (status.HasValue)
                return StatusCode(status.Value, error);

            if (result is null)
                return StatusCode(500, "Unexpected null stop-session result.");

            return Ok(result);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpGet("get-session-by-id")]
        public async Task<ActionResult<SessionReadDto>> GetSession(Guid id)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();

            var (session, error, status) = await service.GetSessionByIdAsync(userId, id);

            if (status == 400) return BadRequest(error);
            if (status == 404) return NotFound(error);

            if (status.HasValue)
                return StatusCode(status.Value, error);

            if (session is null)
                return StatusCode(500, "Unexpected null session.");

            return Ok(session);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpPut("cancel-session/{id:guid}")]
        public async Task<ActionResult<SessionReadDto>> CancelSession(Guid id, CancelSessionDto dto)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();

            var (session, error, status) = await service.CancelSessionAsync(userId, id, dto);

            if (status == 400) return BadRequest(error);
            if (status == 404) return NotFound(error);
            if (status == 409) return Conflict(error);

            if (status.HasValue)
                return StatusCode(status.Value, error);

            if (session is null)
                return StatusCode(500, "Unexpected null session.");

            return Ok(session);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpDelete("parking-lots/{parkingLotId:int}/sessions/{sessionId:guid}")]
        public async Task<IActionResult> DeleteSession(int parkingLotId, Guid sessionId)
        {
            var (deleted, error, status) = await service.DeleteSessionAsync(parkingLotId, sessionId);

            if (status == 400) return BadRequest(error);
            if (status == 404) return NotFound(error);

            if (status.HasValue)
                return StatusCode(status.Value, error);

            if (!deleted)
                return StatusCode(500, "Failed to delete session.");

            return NoContent();
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpPost("refund-session/{id:guid}")]
        public async Task<ActionResult<RefundResponseDto>> RefundSession(Guid id, RefundRequestDto dto)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();

            var (refund, error, status) = await service.RequestRefundAsync(userId, id, dto);

            if (status == 400) return BadRequest(error);
            if (status == 404) return NotFound(error);
            if (status == 409) return Conflict(error);

            if (status.HasValue)
                return StatusCode(status.Value, error);

            if (refund is null)
                return StatusCode(500, "Unexpected null refund response.");

            return Ok(refund);
        }
    }
}
