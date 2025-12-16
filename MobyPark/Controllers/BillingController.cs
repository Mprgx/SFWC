using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MobyPark.Constants;
using MobyPark.Models;
using MobyPark.Services;

namespace MobyPark.Controllers
{
    [ApiController]
    [Route("billing")]
    [Authorize]
    public class BillingController(IBillingService billingService) : ControllerBase
    {
        [HttpGet("receipts")]
        public async Task<ActionResult<List<BillingReceiptDto>>> GetMyReceipts()
        {
            var username = User.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized();
            }

            var (dto, error, status) = await billingService.GetReceiptsForUserAsync(username);

            if (status == 400) return BadRequest(error);
            if (status == 404) return NotFound(error);
            if (status == 204) return NoContent();

            if (status.HasValue)
                return StatusCode(status.Value, error);

            return Ok(dto ?? new List<BillingReceiptDto>());
        }

        [HttpGet]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<List<BillingReceiptDto>>> GetAll()
        {
            var (dto, error, status) = await billingService.GetAllReceiptsAsync();

            if (status == 400) return BadRequest(error);
            if (status == 404) return NotFound(error);
            if (status == 204) return NoContent();

            if (status.HasValue)
                return StatusCode(status.Value, error);

            return Ok(dto ?? new List<BillingReceiptDto>());
        }

        [HttpGet("{id:guid}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<BillingReceiptDto>> GetById(Guid id)
        {
            var (dto, error, status) = await billingService.GetByIdAsync(id);

            if (status == 400) return BadRequest(error);
            if (status == 404) return NotFound(error);
            if (status == 204) return NoContent();

            if (status.HasValue)
                return StatusCode(status.Value, error);

            if (dto is null)
                return StatusCode(500, "Unexpected null billing receipt.");

            return Ok(dto);
        }

        [HttpGet("user/{username}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<List<BillingReceiptDto>>> GetForUser(string username)
        {
            var (dto, error, status) = await billingService.GetReceiptsForUserAsync(username);

            if (status == 400) return BadRequest(error);
            if (status == 404) return NotFound(error);
            if (status == 204) return NoContent();

            if (status.HasValue)
                return StatusCode(status.Value, error);

            return Ok(dto ?? new List<BillingReceiptDto>());
        }
    }
}
