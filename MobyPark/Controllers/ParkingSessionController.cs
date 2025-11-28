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
    public class ParkingSessionController : ControllerBase
    {
        private readonly IParkingSessionService _service;

        public ParkingSessionController(IParkingSessionService service)
        {
            _service = service;
        }


        [HttpPost("/start-parking-session")]
        public async Task<IActionResult> StartSession(ParkingSessionStartDto dto)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();

            var result = await _service.StartSessionAsync(userId, dto);

            if (result is null)
                return BadRequest("Vehicle not found or session already active.");

            return Ok(result);
        }

        [HttpPost("stopsession")]
        public async Task<IActionResult> StopSessionByPlate(ParkingSessionStopDto dto)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();

            var result = await _service.StopSessionByPlateAsync(User.Identity.Name, userId, dto);

            if (result is null)
                return BadRequest("Could not stop session. Check licensePlate.");

            return Ok(result);
        }


        [HttpPut("/stop-parking-session/{id:guid}")]
        public async Task<IActionResult> StopSession(Guid id)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();

            bool isAdmin = User.FindFirstValue(ClaimTypes.Role) == "Admin";

            var result = await _service.StopSessionByIdAsync(userId, isAdmin, id);

            if (result is null)
                return BadRequest("Could not stop this session.");

            return Ok(result);
        }


        [HttpGet("/get-parking-session-by-id")]
        public async Task<IActionResult> GetSession(Guid id)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();

            var session = await _service.GetSessionByIdAsync(userId, id);

            if (session is null)
                return NotFound("Parking session not found.");

            return Ok(session);
        }


        [HttpPut("/cancel-parking-session/{id:guid}")]
        public async Task<IActionResult> CancelSession(Guid id, CancelParkingSessionDto dto)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();

            bool isAdmin = User.FindFirstValue(ClaimTypes.Role) == "Admin";

            var result = await _service.CancelSessionAsync(userId, isAdmin, id, dto);

            if (result is null)
                return BadRequest("Could not cancel session.");

            return Ok(result);
        }


        [HttpPost("/refund-parking-session/{id:guid}")]
        public async Task<IActionResult> RefundSession(Guid id, RefundRequestDto dto)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();

            bool isAdmin = User.FindFirstValue(ClaimTypes.Role) == "Admin";

            var result = await _service.RequestRefundAsync(userId, isAdmin, id, dto);

            if (result is null)
                return BadRequest("Refund not applicable.");

            return Ok(result);
        }
    }
}
