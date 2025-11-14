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
    public class ParkingLotsController(UserDbContext db) : ControllerBase
    {
        private bool IsAdmin =>
            User.IsInRole("ADMIN") || User.IsInRole("Admin");

        private string? Username =>
            User.FindFirstValue(ClaimTypes.Name);

        // GET /parking-lots
        [HttpGet("/parking-lots")]
        public async Task<ActionResult<List<ParkingLotRequestDto>>> GetAll()
        {
            var lots = await db.ParkingLots.ToListAsync();

            var dtos = lots.Select(l => new ParkingLotRequestDto(
                l.Name, l.Location, l.Address, l.Capacity,
                l.Reserved, l.Tariff, l.DayTariff,
                JsonSerializer.Deserialize<Dictionary<string, double>>(l.Coordinates) ?? default
            )).ToList();

            return Ok(dtos);
        }

        // GET /parking-lots/{lid}
        [HttpGet("/parking-lots/{lid}")]
        public async Task<ActionResult<ParkingLotRequestDto>> GetById(string lid)
        {
            var lot = await db.ParkingLots.FindAsync(lid);
            if (lot is null) return NotFound("Parking lot not found.");

            var dto = new ParkingLotRequestDto(
                lot.Name, lot.Location, lot.Address, lot.Capacity,
                lot.Reserved, lot.Tariff, lot.DayTariff,
                JsonSerializer.Deserialize<Dictionary<string, double>>(lot.Coordinates) ?? default

            );

            return Ok(dto);
        }

        // GET /parking-lots/{lid}/sessions
        [HttpGet("/parking-lots/{lid}/sessions")]
        public async Task<ActionResult> GetSessions(int lid)
        {
            var exists = await db.ParkingLots.AnyAsync(p => p.Id == lid);
            if (!exists) return NotFound("Parking lot not found.");

            var query = db.ParkingSessions
                .Include(s => s.User)
                .Include(s => s.Vehicle)
                .Where(s => lid == lid); //s.ParkingLotId == lid

            if (!IsAdmin)
                query = query.Where(s => s.User.Username == Username);

            var sessions = await query.ToListAsync();

            return Ok(sessions);
        }

        // GET /parking-lots/{lid}/sessions/{sid}
        [HttpGet("/parking-lots/{lid}/sessions/{sid}")]
        public async Task<ActionResult> GetSessionById(int lid, string sid)
        {
            var session = await db.ParkingSessions
                .Include(s => s.User)
                .Include(s => s.Vehicle)
                .FirstOrDefaultAsync(s =>
                    s.ParkingLotId == lid && //s.ParkingLotId == lid
                    s.Id.ToString() == sid);

            if (session is null)
                return NotFound("Session not found.");

            if (!IsAdmin && session.User.Username != Username)
                return StatusCode(403, "Access denied.");

            return Ok(session);
        }
    }
}
