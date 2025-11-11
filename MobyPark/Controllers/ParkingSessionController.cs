using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace MobyPark.Controllers
{
    [ApiController]
    [Authorize]
    public class ParkingSessionController : ControllerBase
    {
        private readonly UserDbContext _context;

        public ParkingSessionController(UserDbContext context)
        {
            _context = context;
        }

        [HttpPost("/start-parking-session")]
        public async Task<IActionResult> StartSession(ParkingSessionStartDto dto)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized("Invalid or missing token.");

            var vehicle = await _context.Vehicles
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == dto.VehicleId && v.UserId == userId);

            if (vehicle is null) return BadRequest("Vehicle not found for this user.");

            var existsActive = await _context.ParkingSessions
                .AsNoTracking()
                .AnyAsync(s => s.VehicleId == vehicle.Id && s.Stopped == null);

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

            await _context.ParkingSessions.AddAsync(session);
            await _context.SaveChangesAsync();

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

            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized("Invalid or missing token.");

            var session = await _context.ParkingSessions
                .FirstOrDefaultAsync(s =>
                    s.Stopped == null &&
                    s.LicensePlate.ToUpper() == lp);

            if (session == null)
                return NotFound("No parking session found for this license plate.");

            if (session.UserId != userId)
                return Forbid("You can only stop your own sessions.");

            session.Stopped = DateTimeOffset.UtcNow;
            var minutes = (session.Stopped.Value - session.Started).TotalMinutes;
            session.DurationMinutes = (int)Math.Ceiling(minutes);

            const decimal RATE_PER_HOUR = 2.00m;
            var hours = Math.Ceiling(session.DurationMinutes / 60.0m);
            var amount = hours * RATE_PER_HOUR;
            session.Cost = amount;
            session.PaymentStatus = "unpaid";

            var initiator = User.Identity?.Name ?? string.Empty;


            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                Transaction = GenerateTransactionNumber(),
                Amount = amount,
                Initiator = userId,
                Completed = false,
                Hash = null,
            };

            _context.ParkingSessions.Update(session);
            await _context.Payments.AddAsync(payment);
            await _context.SaveChangesAsync();

            return Created("payments", new
            {
                status = "Success",
                payment = new
                {
                    transaction = payment.Transaction,
                    amount = payment.Amount,
                    initiator = payment.Initiator,
                    completed = payment.Completed,
                    hash = payment.Hash
                }
            });
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

        private static string GenerateTransactionNumber()
        {
            var random = new Random();
            return random.Next(100000000, 999999999).ToString("D12");
        }

        string GeneratePaymentHash(string transaction, decimal amt)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var input = $"{transaction}:{amt}:{DateTimeOffset.UtcNow.Ticks}";
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }

}
