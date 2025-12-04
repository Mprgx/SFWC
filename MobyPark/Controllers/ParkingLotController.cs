using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;
using System.Security.Claims;
using System.Text.Json;
using MobyPark.Services;

namespace MobyPark.Controllers
{
    [ApiController]
    [Authorize]
    public class ParkingLotController(UserDbContext db, IParkingLotService service) : ControllerBase
    {
        private static SessionReadDto ToSessionDto(Session s) => new(
            s.Id,
            s.UserId,
            s.VehicleId,
            s.ParkingLotId,
            s.LicensePlate,
            s.Started,
            s.Stopped,
            s.DurationMinutes,
            s.Cost,
            s.PaymentStatus,
            s.IsCancelled,
            s.CancelledAt,
            s.IsRefunded,
            s.RefundDate
        );

        [HttpGet("/parking-lots")]
        public async Task<ActionResult<List<ParkingLotRequestDto>>> GetAll()
        {

            var lots = await db.ParkingLots.ToListAsync();

            var dtos = lots.Select(l => new ParkingLotRequestDto(
                l.Name,
                l.Location,
                l.Address,
                l.Capacity,
                l.ReservedSpots,
                l.Tariff,
                l.DayTariff,
                JsonSerializer.Deserialize<Dictionary<string, double>>(l.Coordinates) ?? default
            )).ToList();

            return Ok(dtos);
        }

        [HttpGet("/parking-lots/{lid:int}")]
        public async Task<ActionResult<ParkingLotRequestDto>> GetById(int lid)
        {
            var lot = await db.ParkingLots.FindAsync(lid);
            if (lot is null) return NotFound("Parking lot not found.");

            var dto = new ParkingLotRequestDto(
                lot.Name, lot.Location, lot.Address, lot.Capacity,
                lot.ReservedSpots, lot.Tariff, lot.DayTariff,
                JsonSerializer.Deserialize<Dictionary<string, double>>(lot.Coordinates) ?? default

            );

            return Ok(dto);
        }

        [HttpGet("/parking-lots/{lid:int}/sessions")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<SessionReadDto>>> GetSessions(int lid)
        {
            var exists = await db.ParkingLots.AnyAsync(p => p.Id == lid);
            if (!exists) return NotFound("Parking lot not found.");

            var sessions = await db.Sessions
                .Where(s => s.ParkingLotId == lid)
                .ToListAsync();

            var dtoList = sessions.Select(ToSessionDto).ToList();
            return Ok(dtoList);
        }

        [HttpGet("/parking-lots/{lid:int}/sessions/{sid:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<SessionReadDto>> GetSessionById(int lid, Guid sid)
        {
            var session = await db.Sessions
                .FirstOrDefaultAsync(s => s.ParkingLotId == lid && s.Id == sid);

            if (session is null)
                return NotFound("Session not found.");

            return Ok(ToSessionDto(session));
        }

        [HttpPut("/parking-lots/{lid:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int lid, [FromBody] ParkingLotRequestDto dto)
        {
            var updated = await service.UpdateAsync(lid, dto);

            if (updated is null)
                return NotFound("Parking lot not found.");

            return Ok("Parking lot updated successfully.");
        }

    }
}
