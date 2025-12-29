using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MobyPark.Constants;
using MobyPark.Models;
using MobyPark.Services;

namespace MobyPark.Controllers
{
    [Authorize]
    [Route("discounts")]
    [ApiController]

    public class DiscountController(IDiscountService discountService) : ControllerBase
    {
        [Authorize(Roles = Roles.Admin)]
        [HttpPost("create")]
        public async Task<ActionResult<DiscountReadDto>> CreateDiscountAsync(DiscountPostDto request)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized("Invalid or missing user ID.");

            var (statusCode, message, discount) = await discountService.CreateDiscountAsync(request, userId);

            if (statusCode == 400) return BadRequest(new { message });
            if (statusCode == 404) return NotFound(new { message });
            if (statusCode == 409) return Conflict(new { message });

            return Created(string.Empty, discount);
        }

        [HttpPost("apply")]
        public async Task<ActionResult> ApplyDiscountAsync(ApplyDiscountDto request)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized("Invalid or missing user ID.");

            var (statusCode, message) =
                 await discountService.ApplyDiscountAsync(request.DiscountCode, request.Transaction, userId);

            if (statusCode == 400) return BadRequest(new { message });
            if (statusCode == 404) return NotFound(new { message });
            if (statusCode == 409) return Conflict(new { message });
            if (statusCode == 422) return UnprocessableEntity(new { message });
            if (statusCode == 403) return StatusCode(403, new { message });
            if (statusCode == 410) return StatusCode(410, new { message });

            return Ok(new { message });
        }

        private bool TryGetUserId(out Guid id)
        {
            var s = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(s, out id);
        }

    }

}
