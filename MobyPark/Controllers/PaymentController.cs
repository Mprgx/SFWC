using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MobyPark.Constants;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;
using MobyPark.Services;

namespace MobyPark.Controllers
{
    [ApiController]
    [Route("api/")]
    [Authorize]
    public class PaymentsController(UserDbContext context, IPaymentService paymentService) : ControllerBase
    {

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

            var result = await paymentService.FulfillPaymentAsync(userId.ToString(), paymentRequest);

            return Ok(result);
        }

        [HttpPut("payments/{transactionId}")]
        public async Task<ActionResult<Payment>> UpdatePayment(string transactionId, [FromBody] PaymentValidationDto request)
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
                var payment = await paymentService
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

            var paymentList = await paymentService.GetPaymentsForUserAsync(userId);

            return Ok(paymentList);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpGet("payments/{username}")]
        public async Task<ActionResult> GetPaymentsForUser(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return BadRequest("Username is required");

            var paymentList = await paymentService.GetPaymentsForAnyUserAsync(username);

            return Ok(paymentList);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpDelete("Payments/{transactionId}")]
        public async Task<ActionResult> DeletePaymentBytransactionId(string transactionId)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
                return BadRequest("transactionId is required");

            var deleted = await paymentService.DeletePaymentByTransactionId(transactionId);

            if (!deleted)
                return NotFound(new
                {
                    message = $"Payment with id {transactionId} not found"
                });

            return Ok(new
            {
                status = "Success",
                message = $"Transaction with transactionId:{transactionId} is succesfully deleted."
            });
        }
    }
}