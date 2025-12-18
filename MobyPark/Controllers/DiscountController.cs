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
        [HttpPost]
        public async Task<ActionResult<DiscountReadDto>> CreateDiscountAsync(DiscountPostDto request)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var dto = await discountService.CreateDiscountAsync(request, userId);

            if (dto.statusCode == 400)
                return BadRequest(new { dto.message });
            if (dto.statusCode == 404)
                return NotFound(new { dto.message });
            if (dto.statusCode == 409)
                return Conflict(new { dto.message });
            else
                return Created(string.Empty, new { dto.Item3 });
        }

        [HttpPost("apply")]
        public async Task<ActionResult> ApplyDiscountAsync(ApplyDiscountDto request)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var response = await discountService.ApplyDiscountAsync(request.DiscountCode, request.Transaction, userId);

            if (response.statusCode == 400)
                return BadRequest(new { response.message });
            if (response.statusCode == 404)
                return NotFound(new { response.message });
            if (response.statusCode == 409)
                return Conflict(new { response.message });
            if (response.statusCode == 422)
                return UnprocessableEntity(new { response.message });
            if (response.statusCode == 403)
                return StatusCode(403, new { response.message });
            if (response.statusCode == 410)
                return StatusCode(410, new { response.message });
            else
                return Ok(response.message);
        }

        private bool TryGetUserId(out Guid id)
        {
            var s = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(s, out id);
        }

    }

}
