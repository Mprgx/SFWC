using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Models;
using System.Security.Claims;
using MobyPark.Entities;
using MobyPark.Services;

namespace MobyPark.Controllers
{
    [ApiController]
    [Route("api/")]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly UserDbContext _context;

        private readonly IPaymentService _paymentService;

        public PaymentsController(UserDbContext context, IPaymentService paymentService)
        {
            _context = context;
            _paymentService = paymentService;
        }

        // [HttpPost("payments")]
        // public async Task<ActionResult> FulfillPayment(PaymentsDto paymentRequest)
        // {
        //     if (paymentRequest == null)
        //         return BadRequest("Request body is required");

        //     if (string.IsNullOrEmpty(paymentRequest.Transaction))
        //         return BadRequest("Transaction number is required");

        //     if (paymentRequest.Amount <= 0)
        //         return BadRequest("Amount must be a positive number");

        //     var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //     if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        //         return Unauthorized("Invalid or missing user ID.");

        //     var result = await _paymentService.FulfillPaymentAsync(userId, paymentRequest);

        //     return Ok(result);
        // }

        [HttpPut("payments/{transactionId}")]
        public async Task<ActionResult<Payment>> CompletePayment(string transactionId, [FromBody] PaymentValidationDto request)
        {
            if (request == null)
                return BadRequest("Body is required");

            if (string.IsNullOrWhiteSpace(request.Validation))
                return BadRequest("validation field is missing");

            if (string.IsNullOrWhiteSpace(request.T_Data.GetRawText()))
                return BadRequest("t_data field is missing");

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid or missing user ID");

            try
            {
                var payment = await _paymentService
                    .CompletePaymentAsync(userId, transactionId, request);

                return Ok(payment);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpGet("payments")]
        public async Task<ActionResult> GetMyPayments()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid or missing user ID.");

            var paymentList = await _paymentService.GetPaymentsForUserAsync(userId);

            return Ok(paymentList);
        }


        [HttpGet("payments/{username}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> GetPaymentsForUser(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return BadRequest("Username is required");

            var paymentList = await _paymentService.GetPaymentsForAnyUserAsync(username);

            return Ok(paymentList);
        }
    }
}