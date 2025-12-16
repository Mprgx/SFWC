using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MobyPark.Constants;
using MobyPark.Models;
using MobyPark.Services;

namespace MobyPark.Controllers
{
    [ApiController]
    [Route("parking-lots")]
    [Authorize]
    public class ParkingLotController(IParkingLotService service) : ControllerBase
    {
        [Authorize(Roles = Roles.Admin)]
        [HttpPost]
        public async Task<ActionResult<ParkingLotReadDto>> Create(ParkingLotRequestDto dto)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var (created, error, status) = await service.CreateAsync(dto, true);
            if (status == 201)
                return CreatedAtAction(nameof(GetById), new { lid = created!.Id }, created);

            return StatusCode(status ?? 500, error);
        }

        [HttpGet]
        public async Task<ActionResult<List<ParkingLotReadDto>>> GetAll()
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            return Ok(await service.GetAllAsync());
        }

        [HttpGet("{lid:int}")]
        public async Task<ActionResult<ParkingLotReadDto>> GetById(int lid)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var lot = await service.GetByIdAsync(lid);
            return lot is null ? NotFound() : Ok(lot);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpPut("{lid:int}")]
        public async Task<ActionResult<ParkingLotReadDto>> Update(int lid, ParkingLotUpdateDto dto)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var (updated, error, status) = await service.UpdateAsync(lid, dto, true);
            if (status == 404) return NotFound();
            return Ok(updated);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpDelete("{lid:int}")]
        public async Task<IActionResult> Delete(int lid)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var (_, error, status) = await service.DeleteAsync(lid, true);
            if (status == 404) return NotFound();
            if (status == 204) return NoContent();
            return StatusCode(status ?? 500, error);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpGet("{lid:int}/sessions")]
        public async Task<ActionResult<List<SessionReadDto>>> GetSessions(int lid)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var (sessions, error, status) = await service.GetSessionsAsync(
                lid,
                User.FindFirstValue(ClaimTypes.Name),
                User.IsInRole(Roles.Admin)
            );

            if (status == 404) return NotFound();
            return Ok(sessions);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpGet("{lid:int}/sessions/{sid:guid}")]
        public async Task<ActionResult<SessionReadDto>> GetSessionById(int lid, Guid sid)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var (session, error, status) = await service.GetSessionByIdAsync(
                lid, sid,
                User.FindFirstValue(ClaimTypes.Name),
                User.IsInRole(Roles.Admin)
            );

            if (status == 404) return NotFound();
            return Ok(session);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpDelete("{lid:int}/sessions/{sid:guid}")]
        public async Task<IActionResult> DeleteSession(int lid, Guid sid)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var (_, error, status) = await service.DeleteSessionAsync(lid, sid, true);
            if (status == 404) return NotFound();
            if (status == 204) return NoContent();
            return StatusCode(status ?? 500, error);
        }

        private bool TryGetUserId(out Guid id)
        {
            var s = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(s, out id);
        }
    }
}
