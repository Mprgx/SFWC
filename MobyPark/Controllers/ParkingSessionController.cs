using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using MobyPark.Models;

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
        public async Task<IActionResult> StartSession(ParkingSessionStartDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return Unauthorized("Invalid or missing token.");
            
            var userExists = await _context.Users.AnyAsync(u => u.Username == username);
            if (!userExists) return Unauthorized("User not found.");

            var session = new ParkingSession
            {
                Id = Guid.NewGuid(),
                LicensePlate = dto.LicensePlate,
                ParkingLotId = dto.ParkingLotId,
                User = username,
                Started = DateTimeOffset.UtcNow,
                Stopped = null,
                DurationMinutes = 0,
                Cost = 0,
                PaymentStatus = "unpaid"
            };

            await _context.AddAsync(session);
            await _context.SaveChangesAsync();

            return Ok(session);
        }

        [HttpGet("get-parking-session-by-id")]
        public async Task<IActionResult> GetSessionById(Guid id)
        {
            var session = await _context.Set<ParkingSession>().FindAsync(id);
            if (session == null) return NotFound("Parking session not found.");
            return Ok(session);
        }
    }
}
