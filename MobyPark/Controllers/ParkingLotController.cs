using System.Text.Json;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MobyPark.Constants;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;
using MobyPark.Services;

namespace MobyPark.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ParkingLotController(IParkingLotService service) : ControllerBase
    {
        [Authorize(Roles = Roles.Admin)]
        [HttpPost]
        public async Task<ActionResult<ParkingLotReadDto>> Create([FromBody] ParkingLotRequestDto dto)
        {
            var lot = await service.CreateParkingLotAsync(dto);
            return CreatedAtAction(nameof(GetById), new { lid = lot.Id }, lot);
        }

        [HttpGet]
        public async Task<ActionResult<List<ParkingLotReadDto>>> GetAll()
        {
            var lots = await service.GetAllAsync();
            return Ok(lots);
        }

        [HttpGet("{lid:int}")]
        public async Task<ActionResult<ParkingLotReadDto>> GetById(int lid)
        {
            var lot = await service.GetByIdAsync(lid);
            if (lot is null) return NotFound();
            return Ok(lot);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpPut("{lid:int}")]
        public async Task<ActionResult<ParkingLotReadDto>> Update(int lid, [FromBody] ParkingLotUpdateDto dto)
        {
            var updated = await service.UpdateParkingLotAsync(lid, dto);
            if (updated is null) return NotFound();
            return Ok(updated);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpDelete("{lid:int}")]
        public async Task<ActionResult> Delete(int lid)
        {
            var deleted = await service.DeleteParkingLotAsync(lid);
            if (!deleted) return NotFound();
            return NoContent();
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpGet("{lid:int}/sessions")]
        public async Task<ActionResult<List<SessionReadDto>>> GetSessions(int lid)
        {
            var sessions = await service.GetSessionsAsync(lid);
            return Ok(sessions);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpGet("{lid:int}/sessions/{sid:guid}")]
        public async Task<ActionResult<SessionReadDto>> GetSessionById(int lid, Guid sid)
        {
            var session = await service.GetSessionByIdAsync(lid, sid);
            if (session is null) return NotFound();
            return Ok(session);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpDelete("{lid:int}/sessions/{sid:guid}")]
        public async Task<ActionResult> DeleteSession(int lid, Guid sid)
        {
            var deleted = await service.DeleteParkingLotSessionAsync(lid, sid);
            if (!deleted) return NotFound();
            return NoContent();
        }
    }
}
