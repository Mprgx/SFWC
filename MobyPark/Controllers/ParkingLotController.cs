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
        [HttpGet("/parking-lots")]
        public async Task<ActionResult<List<ParkingLotRequestDto>>> GetAll()
        {
            var lots = await db.ParkingLots.ToListAsync();

            var dtos = lots.Select(l => new ParkingLotRequestDto(
                l.Name,
                l.Location,
                l.Address,
                l.Capacity,
                l.Reserved,
                l.Tariff,
                l.DayTariff,
                JsonSerializer.Deserialize<Dictionary<string, double>>(l.Coordinates) ?? new Dictionary<string, double>() 
            )).ToList();

            return Ok(dtos);
        }

        [HttpGet("/parking-lots/{lid:int}")]
        public async Task<ActionResult<ParkingLotRequestDto>> GetById(int lid)
        {
            var lot = await db.ParkingLots.FindAsync(lid);
            if (lot is null) return NotFound("Parking lot not found.");

            var dto = new ParkingLotRequestDto(
                lot.Name,
                lot.Location,
                lot.Address,
                lot.Capacity,
                lot.Reserved,
                lot.Tariff,
                lot.DayTariff,
                JsonSerializer.Deserialize<Dictionary<string, double>>(lot.Coordinates) ?? new Dictionary<string, double>()
            );

            return Ok(dto);
        }

        [HttpGet("/parking-lots/{lid:int}/sessions")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> GetSessions(int lid)
        {
            var exists = await db.ParkingLots.AnyAsync(p => p.Id == lid);
            if (!exists) return NotFound("Parking lot not found.");

            var sessions = await db.Sessions
                .Include(s => s.User)
                .Include(s => s.Vehicle)
                .Where(s => s.ParkingLotId == lid)
                .ToListAsync();

            return Ok(sessions);
        }

        [HttpGet("/parking-lots/{lid:int}/sessions/{sid:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> GetSessionById(int lid, Guid sid)
        {
            var session = await db.Sessions
                .Include(s => s.User)
                .Include(s => s.Vehicle)
                .FirstOrDefaultAsync(s => s.ParkingLotId == lid && s.Id == sid);

            if (session is null)
                return NotFound("Session not found.");

            return Ok(session);
        }
    }
}
