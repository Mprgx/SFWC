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
        public async Task<ActionResult<SessionReadDto>> StartSession(SessionStartDto request)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized("Invalid or missing user ID.");

            var (session, error, status) = await service.StartSessionAsync(userId, request);

            if (status == 400) return BadRequest(new { message = error });
            if (status == 404) return NotFound(new { message = error });
            if (status == 409) return Conflict(new { message = error });

            if (status.HasValue)
                return StatusCode(status.Value, new { message = error });

            if (session is null)
                return StatusCode(500, new { message = "Unexpected null session." });

            return Ok(session);
        }

        [HttpPost("stop-session")]
        public async Task<ActionResult<StopSessionResponseDto>> StopSessionByPlate(SessionStopDto request)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized("Invalid or missing user ID.");

            var (result, error, status) = await service.StopSessionByPlateAsync(userId, request);

            if (status == 400) return BadRequest(new { message = error });
            if (status == 404) return NotFound(new { message = error });
            if (status == 409) return Conflict(new { message = error });

            if (status.HasValue)
                return StatusCode(status.Value, new { message = error });

            if (result is null)
                return StatusCode(500, new { message = "Unexpected null stop-session result." });

            return Ok(result);
        }

        [HttpGet("sessions")]
        public async Task<ActionResult<List<SessionReadDto>>> GetMySessions([FromQuery] bool onlyActive = false)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized("Invalid or missing user ID.");

            var (sessions, error, status) = await service.GetAllForUserAsync(userId, onlyActive);

            if (status == 400) return BadRequest(new { message = error });
            if (status.HasValue) return StatusCode(status.Value, new { message = error });


            return Ok(sessions ?? new List<SessionReadDto>());
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpPut("stop-session/{id:guid}")]
        public async Task<ActionResult<StopSessionResponseDto>> StopSession(Guid id)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized("Invalid or missing user ID.");

            var (result, error, status) = await service.StopSessionByIdAsync(userId, id);

            if (status == 400) return BadRequest(new { message = error });
            if (status == 404) return NotFound(new { message = error });
            if (status == 409) return Conflict(new { message = error });

            if (status.HasValue)
                return StatusCode(status.Value, new { message = error });

            if (result is null)
                return StatusCode(500, new { message = "Unexpected null stop-session result." });

            return Ok(result);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpGet("get-session-by-id")]
        public async Task<ActionResult<SessionReadDto>> GetSession([FromQuery] Guid id)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized("Invalid or missing user ID.");

            var (session, error, status) = await service.GetSessionByIdAsync(userId, id);

            if (status == 400) return BadRequest(new { message = error });
            if (status == 404) return NotFound(new { message = error });

            if (status.HasValue)
                return StatusCode(status.Value, new { message = error });

            if (session is null)
                return StatusCode(500, new { message = "Unexpected null session." });

            return Ok(session);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpPut("cancel-session/{id:guid}")]
        public async Task<ActionResult<SessionReadDto>> CancelSession(Guid id, CancelSessionDto request)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized("Invalid or missing user ID.");

            var (session, error, status) = await service.CancelSessionAsync(userId, id, request);

            if (status == 400) return BadRequest(new { message = error });
            if (status == 404) return NotFound(new { message = error });
            if (status == 409) return Conflict(new { message = error });

            if (status.HasValue)
                return StatusCode(status.Value, new { message = error });

            if (session is null)
                return StatusCode(500, new { message = "Unexpected null session." });

            return Ok(session);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpDelete("parking-lots/{parkingLotId:int}/sessions/{sessionId:guid}")]
        public async Task<IActionResult> DeleteSession(int parkingLotId, Guid sessionId)
        {
            var (deleted, error, status) = await service.DeleteSessionAsync(parkingLotId, sessionId);

            if (status == 400) return BadRequest(new { message = error });
            if (status == 404) return NotFound(new { message = error });

            if (status.HasValue)
                return StatusCode(status.Value, new { message = error });

            if (!deleted)
                return StatusCode(500, new { message = "Failed to delete session." });

            return NoContent();
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpPost("refund-session/{id:guid}")]
        public async Task<ActionResult<RefundResponseDto>> RefundSession(Guid id, RefundRequestDto request)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized("Invalid or missing user ID.");

            var (refund, error, status) = await service.RequestRefundAsync(userId, id, request);

            if (status == 400) return BadRequest(new { message = error });
            if (status == 404) return NotFound(new { message = error });
            if (status == 409) return Conflict(new { message = error });

            if (status.HasValue)
                return StatusCode(status.Value, new { message = error });

            if (refund is null)
                return StatusCode(500, new { message = "Unexpected null refund response." });

            return Ok(refund);
        }

        private bool TryGetUserId(out Guid id)
        {
            var s = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(s, out id);
        }
    }
}
