using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Models;
using System.Security.Claims;
using MobyPark.Entities;

namespace MobyPark.Controllers
{
    [ApiController]
    [Route("api/")]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly UserDbContext _context;

        public PaymentsController(UserDbContext context)
        {
            _context = context;
        }

        [HttpPost("payments")]
        public async Task<ActionResult> FulfillPayment(PaymentsDto paymentRequest)
        {
            if (paymentRequest == null)
                return BadRequest("Request body is required");

            if (string.IsNullOrEmpty(paymentRequest.Transaction))
                return BadRequest("Transaction number is required");

            if (paymentRequest.Amount <= 0)
                return BadRequest("Amount must be a positive number");

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid or missing user ID.");

            var session = await _context.ParkingSessions
                .FirstOrDefaultAsync(s => s.UserId == userId);


            decimal expectedAmount = session.Cost;
            if (paymentRequest.Amount != expectedAmount)
                return BadRequest($"Invalid payment amount. Expected: {expectedAmount:C}, Received: {paymentRequest.Amount:C}");

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.Transaction == paymentRequest.Transaction);

            if (payment == null)
                return NotFound($"No payment record found for transaction: {paymentRequest.Transaction}");

            payment.Completed = true;
            payment.Hash = GeneratePaymentHash(payment.Transaction, payment.Amount);
            session.PaymentStatus = "paid";

            _context.Payments.Update(payment);
            _context.ParkingSessions.Update(session);

            await _context.SaveChangesAsync();

            return Ok(new
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

        private static string GeneratePaymentHash(string transaction, decimal amount)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var input = $"{transaction}:{amount}:{DateTimeOffset.UtcNow.Ticks}";
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}