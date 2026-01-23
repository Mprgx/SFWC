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

        [Authorize(Roles = Roles.Admin)]
        [HttpPatch("{code}")]
        public async Task<ActionResult<DiscountReadDto>> PatchDiscountAsync([FromRoute] string code,[FromBody] DiscountPatchDto dto)
        {
            var (statusCode, message, updated) = await discountService.UpdateDiscountAsync(code, dto);

            if (statusCode == 200) return Ok(updated);

            if (statusCode == 400) return BadRequest(new { message });

            if (statusCode == 404) return NotFound(new { message });

            if (statusCode == 409) return Conflict(new { message });

            return StatusCode(500, new { message = "Unexpected error." });
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpGet("statistics/all-codes")]
        public async Task<ActionResult<List<DiscountCodeAnalyticsReadDto>>> GetDiscountAllCodesAnalytics([FromQuery] DiscountCodeStatus status = DiscountCodeStatus.Active)
        {
            var (statusCode, message, dto) = await discountService.GetDiscountCodesAllAnalyticsAsync(status);

            if (statusCode == 400) return BadRequest(new { message });
            return Ok(dto ?? new List<DiscountCodeAnalyticsReadDto>());
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpGet("statistics/code/{code}")]
        public async Task<ActionResult<DiscountCodeAnalyticsReadDto>> GetDiscountCodeAnalyticsByCode([FromRoute] string code)
        {
            var (statusCode, message, dto) =
                await discountService.GetDiscountCodeAnalyticsByCodeAsync(code);

            if (statusCode == 200) return Ok(dto);
            if (statusCode == 404) return NotFound(new { message });
            return BadRequest(new { message });
        }


        private bool TryGetUserId(out Guid id)
        {
            var s = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(s, out id);
        }

    }

}
