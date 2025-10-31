using Microsoft.AspNetCore.Mvc;
using MobyPark.Data;
using MobyPark.Entities;  // your entity
using MobyPark.Dtos;      // your new DTO
using System;
using Microsoft.AspNetCore.Authorization;

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
