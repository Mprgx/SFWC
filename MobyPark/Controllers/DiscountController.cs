using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MobyPark.Constants;
using MobyPark.Models;
using MobyPark.Services;

namespace MobyPark.Controllers
{
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

        private bool TryGetUserId(out Guid id)
        {
            var s = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(s, out id);
        }

    }

}
