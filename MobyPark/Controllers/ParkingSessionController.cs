using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;
using MobyPark.Services;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace MobyPark.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ParkingSessionController(UserDbContext context, IEncryptionService encryption) : ControllerBase
    {

        [HttpPost("/start-parking-session")]
        public async Task<IActionResult> StartSession(ParkingSessionStartDto dto)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized("Invalid or missing token.");

            var vehicle = await context.Vehicles
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == dto.VehicleId && v.UserId == userId);

            if (vehicle is null) return BadRequest("Vehicle not found for this user.");

            var existsActive = await context.ParkingSessions
                .AsNoTracking()
                .AnyAsync(s => s.VehicleId == vehicle.Id && s.Stopped == null);

            if (existsActive) return Conflict("This vehicle already has an active session.");

            var session = new ParkingSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                VehicleId = vehicle.Id,
                LicensePlate = vehicle.LicensePlate, // encrypted in DB
                ParkingLotId = dto.ParkingLotId,
                Started = DateTimeOffset.UtcNow,
                Stopped = null,
                DurationMinutes = 0,
                Cost = 0m,
                PaymentStatus = "unpaid"
            };

            await context.ParkingSessions.AddAsync(session);
            await context.SaveChangesAsync();

            return Ok(new
            {
                session.Id,
                session.UserId,
                session.VehicleId,
                LicensePlate = string.IsNullOrEmpty(session.LicensePlate) ? string.Empty : encryption.Decrypt(session.LicensePlate) ?? string.Empty,
                session.ParkingLotId,
                session.Started,
                session.Stopped,
                session.DurationMinutes,
                session.Cost,
                session.PaymentStatus
            });
        }

        [HttpPost("stopsession")]
        public async Task<IActionResult> StopSession([FromBody] ParkingSessionStopDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.LicensePlate))
                return BadRequest("licensePlate is required in the request body.");

            var lp = dto.LicensePlate.Trim().ToUpperInvariant();

            string pattern =
                @"^(?:[A-Z]{2}-\d{2}-\d{2}|\d{2}-\d{2}-[A-Z]{2}|\d{2}-[A-Z]{2}-\d{2}|[A-Z]{2}-\d{2}-[A-Z]{2}|[A-Z]{2}-[A-Z]{2}-\d{2}|\d{2}-[A-Z]{2}-[A-Z]{2})$";
            if (!Regex.IsMatch(lp, pattern, RegexOptions.IgnoreCase))
                return BadRequest("licensePlate filled in incorrectly.");

            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized("Invalid or missing token.");

            // Because LicensePlate is encrypted in DB, we must decrypt to compare
            var activeSessions = await context.ParkingSessions
                .Where(s => s.Stopped == null)
                .ToListAsync();

            var session = activeSessions.FirstOrDefault(s =>
            {
                if (string.IsNullOrEmpty(s.LicensePlate)) return false;
                var platePlain = encryption.Decrypt(s.LicensePlate);
                if (string.IsNullOrWhiteSpace(platePlain)) return false;

                var normalized = platePlain.Trim().ToUpperInvariant();
                return normalized == lp;
            });

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

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                Transaction = GenerateTransactionNumber(),
                Amount = amount,
                Initiator = userId,
                Completed = false,
                Hash = null,
            };

            context.ParkingSessions.Update(session);
            await context.Payments.AddAsync(payment);
            await context.SaveChangesAsync();

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
        public async Task<IActionResult> GetSessionById(Guid id)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();

            var session = await context.ParkingSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            if (session is null)
                return NotFound("Parking session not found.");

            return Ok(new
            {
                session.Id,
                session.UserId,
                session.VehicleId,
                LicensePlate = string.IsNullOrEmpty(session.LicensePlate)
                    ? string.Empty
                    : encryption.Decrypt(session.LicensePlate) ?? string.Empty,
                session.ParkingLotId,
                session.Started,
                session.Stopped,
                session.DurationMinutes,
                session.Cost,
                session.PaymentStatus,
                session.IsCancelled,
                session.CancelledAt,
                session.IsRefunded,
                session.RefundDate
            });
        }

        private static string GenerateTransactionNumber()
        {
            var random = new Random();
            return random.Next(100000000, 999999999).ToString("D12");
        }

        private string GeneratePaymentHash(string transaction, decimal amt)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var input = $"{transaction}:{amt}:{DateTimeOffset.UtcNow.Ticks}";
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        [HttpPut("/stop-parking-session/{id:guid}")]
        public async Task<IActionResult> StopSession(Guid id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid or missing token.");

            var session = await context.ParkingSessions.FirstOrDefaultAsync(s => s.Id == id);
            if (session is null)
                return NotFound("Parking session not found.");

            var user = await context.Users.FindAsync(userId);
            var isAdmin = user?.Role == UserRole.Admin;

            if (session.UserId != userId && !isAdmin)
                return Forbid("You can only stop your own parking sessions.");

            if (session.Stopped is not null)
                return BadRequest("This parking session has already been stopped.");

            if (session.IsCancelled)
                return BadRequest("Cancelled sessions cannot be stopped.");

            session.Stopped = DateTimeOffset.UtcNow;
            session.DurationMinutes = (int)(session.Stopped.Value - session.Started).TotalMinutes;
            session.Cost = Math.Round((decimal)session.DurationMinutes * 0.05m, 2);
            session.PaymentStatus = "awaiting_payment";

            await context.SaveChangesAsync();

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

            var session = await context.ParkingSessions.FirstOrDefaultAsync(s => s.Id == id);
            if (session is null)
                return NotFound("Parking session not found.");

            var user = await context.Users.FindAsync(userId);
            var isAdmin = user?.Role == UserRole.Admin;

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

            await context.SaveChangesAsync();

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

            var session = await context.ParkingSessions.FirstOrDefaultAsync(s => s.Id == id);
            if (session is null)
                return NotFound("Parking session not found.");

            var user = await context.Users.FindAsync(userId);
            var isAdmin = user?.Role == UserRole.Admin;

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

            const int MAX_REFUNDS_PER_USER = 3;
            if (!RefundAttempts.TryGetValue(userId, out int attempts))
                RefundAttempts[userId] = 0;

            if (RefundAttempts[userId] >= MAX_REFUNDS_PER_USER)
                return BadRequest("Refund attempt limit reached. Please try again later.");

            RefundAttempts[userId]++;

            const decimal RatePerMinute = 0.05m;
            session.DurationMinutes = (int)(session.Stopped.Value - session.Started).TotalMinutes;
            session.Cost = Math.Max(Math.Round(session.DurationMinutes * RatePerMinute, 2), 0.50m);

            decimal refundPercentage;
            if (session.DurationMinutes <= 10)
                refundPercentage = 1.0m;
            else if (session.DurationMinutes <= 30)
                refundPercentage = 0.5m;
            else
                refundPercentage = 0.0m;

            var refundAmount = Math.Round(session.Cost * refundPercentage, 2);

            if (refundAmount <= 0)
                return BadRequest("Refund not applicable for this session duration.");

            if (string.IsNullOrWhiteSpace(dto?.IBAN))
                return BadRequest("IBAN is required for refund processing.");

            if (!Regex.IsMatch(dto.IBAN, @"^[A-Z]{2}[0-9]{2}[A-Z0-9]{1,30}$"))
                return BadRequest("Invalid IBAN format. Example: NL91ABNA0417164300");

            session.IsRefunded = true;
            session.RefundDate = DateTimeOffset.UtcNow;
            session.PaymentStatus = "refunded";

            await context.SaveChangesAsync();

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
