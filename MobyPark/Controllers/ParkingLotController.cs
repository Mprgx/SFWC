using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;
using System.Security.Claims;
using System.Text.Json;

namespace MobyPark.Controllers
{
    [ApiController]
    [Authorize]
    public class ParkingLotController(UserDbContext db) : ControllerBase
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

        //POST
        [HttpPost("/parking-lots")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Create([FromBody] ParkingLotRequestDto body)
        {
            var coords = JsonSerializer.Serialize(body.Coordinates);

            var lot = new ParkingLot
            {
                Id = 0,
                Name = body.Name,
                Location = body.Location,
                Address = body.Address,
                Capacity = body.Capacity,
                Tariff = body.Tariff,
                DayTariff = body.DayTariff,
                Coordinates = coords
            };

            db.ParkingLots.Add(lot);
            await db.SaveChangesAsync();

            return Ok(lot);
        }

        //GET
        [HttpGet("/parking-lots")]
        public async Task<ActionResult<List<ParkingLotRequestDto>>> GetAll()
        {

            var lots = await db.ParkingLots.ToListAsync();

            var dtos = lots.Select(l => new ParkingLotRequestDto(
                l.Name,
                l.Location,
                l.Address,
                l.Capacity,
                l.Tariff,
                l.DayTariff,
                JsonSerializer.Deserialize<Dictionary<string, double>>(l.Coordinates) ?? default
            )).ToList();

            return Ok(dtos);
        }

        //GET
        [HttpGet("/parking-lots/{lid:int}")]
        public async Task<ActionResult<ParkingLotRequestDto>> GetById(int lid)
        {
            var lot = await db.ParkingLots.FindAsync(lid);
            if (lot is null) return NotFound("Parking lot not found.");

            var dto = new ParkingLotRequestDto(
                lot.Name, lot.Location, lot.Address, lot.Capacity,
                lot.Tariff, lot.DayTariff,
                JsonSerializer.Deserialize<Dictionary<string, double>>(lot.Coordinates) ?? default

            );

            return Ok(dto);
        }

        //GET
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

        //GET
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

        //DELETE
        [HttpDelete("/parking-lots/{lid:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteParkingLot(int lid)
        {
            var lot = await db.ParkingLots.FindAsync(lid);
            if (lot is null)
                return NotFound("Parking lot not found");

            db.ParkingLots.Remove(lot);
            await db.SaveChangesAsync();

            return Ok("Parking lot deleted");
        }

        //DELETE
        [HttpDelete("/parking-lots/{lid:int}/sessions/{sid:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteSession(int lid, Guid sid)
        {
            var session = await db.Sessions
                .FirstOrDefaultAsync(s => s.ParkingLotId == lid && s.Id == sid);

            if (session is null)
                return NotFound("Session not found");

            db.Sessions.Remove(session);
            await db.SaveChangesAsync();

            return Ok("Session deleted");
        }
    }
}
