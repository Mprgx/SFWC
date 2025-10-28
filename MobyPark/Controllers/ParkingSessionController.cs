using Microsoft.AspNetCore.Mvc;
using MobyPark.Data;
using MobyPark.Entities;  // your entity
using MobyPark.Dtos;      // your new DTO
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Text.RegularExpressions;

namespace MobyPark.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ParkingSessionController : ControllerBase
    {
        private readonly UserDbContext _context;

        public ParkingSessionController(UserDbContext context)
        {
            _context = context;
        }

        [Authorize]
        [HttpPost("startsession")]
        public IActionResult StartSession([FromBody] ParkingSessionStartDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized("Invalid or missing token.");

            var userExists = _context.Users.Any(u => u.Username == username);
            if (!userExists)
                return Unauthorized("User not found.");

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

            _context.Add(session);
            _context.SaveChanges();

            return Ok(session);
        }

        [Authorize]
        [HttpPost("stopsession")]
        public async Task<IActionResult> StopSession([FromBody] ParkingSessionStopDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.LicensePlate))
                return BadRequest("licensePlate is required in the request body.");

            var lp = dto.LicensePlate.Trim().ToUpperInvariant();

            string pattern = @"^(?:[A-Z]{2}-\d{2}-\d{2}|\d{2}-\d{2}-[A-Z]{2}|\d{2}-[A-Z]{2}-\d{2}|[A-Z]{2}-\d{2}-[A-Z]{2}|[A-Z]{2}-[A-Z]{2}-\d{2}|\d{2}-[A-Z]{2}-[A-Z]{2})$";
            if (!Regex.IsMatch(lp, pattern, RegexOptions.IgnoreCase))
                return BadRequest("licensePlate filled in incorrectly.");

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized("Invalid or missing token.");

            var session = await _context.Set<ParkingSession>()
                .FirstOrDefaultAsync(s =>
                    s.Stopped == null &&
                    s.LicensePlate.ToUpper() == lp);

            if (session == null)
                return NotFound("No parking session found for this license plate.");

            if (!string.Equals(session.User, username, StringComparison.OrdinalIgnoreCase))
                return Forbid("You can only stop your own sessions.");

            session.Stopped = DateTimeOffset.UtcNow;
            var minutes = (session.Stopped.Value - session.Started).TotalMinutes;
            session.DurationMinutes = (int)Math.Ceiling(minutes);

            _context.Update(session);
            await _context.SaveChangesAsync();

            return Ok(session);
        }


        [HttpGet("{id}")]
        public IActionResult GetSessionById(Guid id)
        {
            var session = _context.Set<ParkingSession>().Find(id);
            if (session == null)
                return NotFound("Parking session not found.");
            return Ok(session);
        }
    }
}
