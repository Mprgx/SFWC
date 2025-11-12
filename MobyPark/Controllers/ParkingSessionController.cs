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
    [Authorize]
    [Route("api/[controller]")]
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

            if (vehicle is null)
                return BadRequest("Vehicle not found for this user.");

            var existsActive = await _context.ParkingSessions
                .AsNoTracking()
                .AnyAsync(s => s.VehicleId == vehicle.Id && s.Stopped == null);

            if (existsActive)
                return Conflict("This vehicle already has an active session.");

            var session = new ParkingSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                VehicleId = vehicle.Id,
                LicensePlate = vehicle.LicensePlate,
                Started = DateTimeOffset.UtcNow,
                DurationMinutes = 0,
                Cost = 0m,
                PaymentStatus = "unpaid"
            };

            await _context.ParkingSessions.AddAsync(session);
            await _context.SaveChangesAsync();

            return Ok(session);
        }

        [HttpGet("/get-parking-session-by-id")]
        public async Task<IActionResult> GetSessionById(Guid id)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();

            var session = await _context.ParkingSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            return session is null ? NotFound("Parking session not found.") : Ok(session);
        }

        [HttpPut("/stop-parking-session/{id:guid}")]
        public async Task<IActionResult> StopSession(Guid id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid or missing token.");

            var session = await _context.ParkingSessions.FirstOrDefaultAsync(s => s.Id == id);
            if (session is null)
                return NotFound("Parking session not found.");

            var user = await _context.Users.FindAsync(userId);
            var isAdmin = user?.Role == UserRole.Admin;

            if (session.UserId != userId && !isAdmin)
                return Forbid("You can only stop your own parking sessions.");

            if (session.Stopped is not null)
                return BadRequest("This parking session has already been stopped.");

            if (session.IsCancelled)
                return BadRequest("Cancelled sessions cannot be stopped.");

            // Stop the session
            session.Stopped = DateTimeOffset.UtcNow;

            // Calculate duration and simple cost (for demo)
            session.DurationMinutes = (int)(session.Stopped.Value - session.Started).TotalMinutes;
            session.Cost = Math.Round((decimal)session.DurationMinutes * 0.05m, 2); // €0.05/min
            session.PaymentStatus = "awaiting_payment";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Parking session stopped successfully.",
                session.Id,
                session.DurationMinutes,
                session.Cost,
                session.PaymentStatus
            });
        }

        [HttpPut("/cancel-parking-session/{id:guid}")]
        public async Task<IActionResult> CancelSession(Guid id, [FromBody] CancelParkingSessionDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid or missing token.");

            var session = await _context.ParkingSessions.FirstOrDefaultAsync(s => s.Id == id);
            if (session is null)
                return NotFound("Parking session not found.");

            var user = await _context.Users.FindAsync(userId);
            var isAdmin = user?.Role == UserRole.Admin;

            // 🧑‍⚖️ Admin must provide a reason
            if (isAdmin && string.IsNullOrWhiteSpace(dto?.Reason))
                return BadRequest("Admin must provide a reason when cancelling a parking session.");

            if (session.UserId != userId && !isAdmin)
                return Forbid("You can only cancel your own sessions.");

            if (session.IsCancelled)
                return BadRequest("This session is already cancelled.");

            if (session.Stopped is null)
                session.Stopped = DateTimeOffset.UtcNow;

            session.IsCancelled = true;
            session.CancelledAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = isAdmin
                    ? $"Parking session cancelled by admin. Reason: {dto?.Reason}"
                    : "Parking session cancelled successfully.",
                session.Id,
                session.IsCancelled,
                session.CancelledAt
            });
        }


        [HttpPost("/refund-parking-session/{id:guid}")]
        public async Task<IActionResult> RequestRefund(Guid id, [FromBody] RefundRequestDto? dto = null)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid or missing token.");

            var session = await _context.ParkingSessions.FirstOrDefaultAsync(s => s.Id == id);
            if (session is null)
                return NotFound("Parking session not found.");

            var user = await _context.Users.FindAsync(userId);
            var isAdmin = user?.Role == UserRole.Admin;

            // 1️⃣ Eligibility checks
            if (!session.IsCancelled)
                return BadRequest("Refunds can only be issued for cancelled sessions.");

            if (session.Stopped is null)
                return BadRequest("Session must be stopped before requesting a refund.");

            if (session.IsRefunded)
                return BadRequest("Refund already processed for this session.");

            if (session.UserId != userId && !isAdmin)
                return Forbid("You can only refund your own sessions.");

            if (isAdmin && string.IsNullOrWhiteSpace(dto?.Reason))
                return BadRequest("Admin must provide a reason for refund.");

            // 2️⃣ Prevent refund abuse
            const int MAX_REFUNDS_PER_USER = 3;
            if (!RefundAttempts.TryGetValue(userId, out int attempts))
                RefundAttempts[userId] = 0;

            if (RefundAttempts[userId] >= MAX_REFUNDS_PER_USER)
                return BadRequest("Refund attempt limit reached. Please try again later.");

            RefundAttempts[userId]++;

            // 3️⃣ Calculate parking cost
            const decimal RatePerMinute = 0.05m; // €0.05/minute
            session.DurationMinutes = (int)(session.Stopped.Value - session.Started).TotalMinutes;
            session.Cost = Math.Max(Math.Round(session.DurationMinutes * RatePerMinute, 2), 0.50m);

            // 4️⃣ Determine refund percentage (tiered logic)
            decimal refundPercentage;
            if (session.DurationMinutes <= 10)
                refundPercentage = 1.0m; // 100%
            else if (session.DurationMinutes <= 30)
                refundPercentage = 0.5m; // 50%
            else
                refundPercentage = 0.0m; // 0%

            var refundAmount = Math.Round(session.Cost * refundPercentage, 2);

            if (refundAmount <= 0)
                return BadRequest("Refund not applicable for this session duration.");

            // 5️⃣ Validate IBAN
            if (string.IsNullOrWhiteSpace(dto?.IBAN))
                return BadRequest("IBAN is required for refund processing.");

            if (!System.Text.RegularExpressions.Regex.IsMatch(dto.IBAN, @"^[A-Z]{2}[0-9]{2}[A-Z0-9]{1,30}$"))
                return BadRequest("Invalid IBAN format. Example: NL91ABNA0417164300");

            // 6️⃣ Process refund
            session.IsRefunded = true;
            session.RefundDate = DateTimeOffset.UtcNow;
            session.PaymentStatus = "refunded";

            await _context.SaveChangesAsync();

            // ✅ Return full response
            return Ok(new
            {
                message = "Refund processed successfully.",
                session.Id,
                session.DurationMinutes,
                session.Cost,
                refundPercentage = refundPercentage * 100,
                refundedAmount = refundAmount,
                session.RefundDate,
                session.PaymentStatus,
                attemptsLeft = MAX_REFUNDS_PER_USER - RefundAttempts[userId]
            });
        }


        private static readonly Dictionary<Guid, int> RefundAttempts = new();
    }
}
