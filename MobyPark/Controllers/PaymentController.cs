using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MobyPark.Constants;
using MobyPark.Models;
using MobyPark.Services;

namespace MobyPark.Controllers
{
    [ApiController]
    [Route("payments")]
    [Authorize]
    public class PaymentsController(IPaymentService paymentService) : ControllerBase
    {

        [HttpPost("fulfill")]
        public async Task<ActionResult<PaymentReadDto>> FulfillPayment([FromBody] PaymentsDto paymentRequest)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized("Invalid or missing user ID.");

            var (dto, error, status) = await paymentService.FulfillPaymentAsync(userId, paymentRequest);

            if (status == 400) return BadRequest(error);
            if (status == 401) return Unauthorized(error);
            if (status == 403) return Forbid();
            if (status == 404) return NotFound(error);
            if (status == 409) return Conflict(error);
            if (status == 422) return UnprocessableEntity(error);
            if (status.HasValue) return StatusCode(status.Value, error);

            if (dto is null) return StatusCode(500, "Unexpected null payment.");
            return Ok(dto);
        }

        [HttpPut("{transactionId}")]
        public async Task<ActionResult<PaymentReadDto>> CompletePayment(string transactionId, [FromBody] PaymentValidationDto request)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized("Invalid or missing user ID.");

            var (dto, error, status) = await paymentService.CompletePaymentAsync(userId, transactionId, request);

            if (status == 400) return BadRequest(error);
            if (status == 401) return Unauthorized(error);
            if (status == 403) return Forbid();
            if (status == 404) return NotFound(error);
            if (status == 409) return Conflict(error);
            if (status.HasValue) return StatusCode(status.Value, error);

            if (dto is null) return StatusCode(500, "Unexpected null payment.");
            return Ok(dto);
        }

        [HttpGet]
        public async Task<ActionResult<List<PaymentReadDto>>> GetMyPayments()
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized("Invalid or missing user ID.");

            var (dto, error, status) = await paymentService.GetPaymentsForUserAsync(userId);

            if (status == 400) return BadRequest(error);
            if (status == 404) return NotFound(error);
            if (status.HasValue) return StatusCode(status.Value, error);

            return Ok(dto ?? new List<PaymentReadDto>());
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpGet("user/{username}")]
        public async Task<ActionResult<List<PaymentReadDto>>> GetPaymentsForUser(string username)
        {
            var (dto, error, status) = await paymentService.GetPaymentsForAnyUserAsync(username);

            if (status == 400) return BadRequest(error);
            if (status == 404) return NotFound(error);
            if (status.HasValue) return StatusCode(status.Value, error);

            return Ok(dto ?? new List<PaymentReadDto>());
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpDelete("{transactionId}")]
        public async Task<IActionResult> DeletePayment(string transactionId)
        {
            var (deleted, error, status) = await paymentService.DeletePaymentByTransactionId(transactionId);

            if (status == 400) return BadRequest(error);
            if (status == 404) return NotFound(error);
            if (status.HasValue) return StatusCode(status.Value, error);

            return deleted ? NoContent() : StatusCode(500, "Failed to delete payment.");
        }
    }
}