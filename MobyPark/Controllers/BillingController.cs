using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MobyPark.Constants;
using MobyPark.Services;

namespace MobyPark.Controllers
{
    [ApiController]
    [Route("api/billing")]
    [Authorize]
    public class BillingController(IBillingService billing) : ControllerBase
    {
        private string Username => User.FindFirst(ClaimTypes.Name)!.Value;
        private bool IsAdmin => User.IsInRole("ADMIN");

        [HttpGet("receipts")]
        public async Task<IActionResult> GetMyReceipts()
        {
            if (IsAdmin)
                return Ok(await billing.GetAllReceiptsAsync());

            return Ok(await billing.GetReceiptsForUserAsync(Username));
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await billing.GetByIdAsync(id);
            return result is null ? NotFound() : Ok(result);
        }
    }
}
