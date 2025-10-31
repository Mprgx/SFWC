using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;
using System.Security.Claims;

namespace MobyPark.Controllers
{
    [ApiController]
    [Route("api/")]
    [Authorize]
    public class ParkingSessionController : ControllerBase
    {
        private readonly UserDbContext _context;

        public ParkingSessionController(UserDbContext context)
        {
            _context = context;
        }

        [HttpPost("start-parking-session")]
        public async Task<IActionResult> StartSession(ParkingSessionStartDto dto, CancellationToken ct)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized("Invalid or missing token.");

            var vehicle = await _context.Vehicles
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == dto.VehicleId && v.UserId == userId, ct);

            if (vehicle is null) return BadRequest("Vehicle not found for this user.");

            var existsActive = await _context.ParkingSessions
                .AsNoTracking()
                .AnyAsync(s => s.VehicleId == vehicle.Id && s.Stopped == null, ct);

            if (existsActive) return Conflict("This vehicle already has an active session.");

            var session = new ParkingSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                VehicleId = vehicle.Id,
                LicensePlate = vehicle.LicensePlate,
                Started = DateTimeOffset.UtcNow,
                Stopped = null,
                DurationMinutes = 0,
                Cost = 0m,
                PaymentStatus = "unpaid"
            };

            await _context.ParkingSessions.AddAsync(session, ct);
            await _context.SaveChangesAsync(ct);

            return Ok(session);
        }

        [HttpGet("get-parking-session-by-id")]
        public async Task<IActionResult> GetSessionById(Guid id, CancellationToken ct)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();

            var session = await _context.ParkingSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId, ct);

            return session is null ? NotFound("Parking session not found.") : Ok(session);
        }
    }
}
