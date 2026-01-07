using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MobyPark.Constants;
using MobyPark.Models;
using MobyPark.Services;

namespace MobyPark.Controllers
{
    [ApiController]
    [Authorize]
    public class VehicleController(IVehicleService vehicleService) : ControllerBase
    {
        private bool TryGetUserId(out Guid id)
        {
            id = Guid.Empty;
            var s = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return !string.IsNullOrWhiteSpace(s) && Guid.TryParse(s, out id);
        }

        [HttpPost("vehicle")]
        public async Task<ActionResult<VehicleReadDto>> CreateVehicle(VehicleCreateDto request)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized("Invalid or missing user ID.");

            var (dto, error, status) = await vehicleService.CreateVehicleAsync(userId, request);

            if (status == 400) return BadRequest(error);
            if (status == 404) return NotFound(error);
            if (status == 204) return NoContent();

            if (status.HasValue)
                return StatusCode(status.Value, error);

            if (dto is null)
                return StatusCode(500, "Unexpected null vehicle.");

            return Ok(dto);
        }

        [HttpGet("vehicles")]
        public async Task<ActionResult<List<VehicleReadDto>>> GetVehicles()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized("Invalid or missing user ID.");

            var (dtos, error, status) = await vehicleService.GetVehiclesForUserAsync(userId);

            if (status == 400) return BadRequest(error);
            if (status == 404) return NotFound(error);
            if (status == 204) return NoContent();
            if (status.HasValue) return StatusCode(status.Value, error);

            if (dtos is null) return StatusCode(500, "Unexpected null vehicle list.");
            return Ok(dtos);
        }

        [HttpPut("vehicle/{id:int}")]
        public async Task<ActionResult<VehicleReadDto>> UpdateVehicle(int id, VehicleUpdateDto request)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized("Invalid or missing user ID.");

            var (dto, error, status) = await vehicleService.UpdateVehicleAsync(userId, id, request);

            if (status == 400) return BadRequest(error);
            if (status == 404) return NotFound(error);
            if (status == 204) return NoContent();
            if (status.HasValue) return StatusCode(status.Value, error);

            if (dto is null) return StatusCode(500, "Unexpected null vehicle.");
            return Ok(dto);
        }

        [HttpDelete("vehicle/{id:int}")]
        public async Task<IActionResult> DeleteVehicle(int id)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized("Invalid or missing user ID.");

            var (error, status) = await vehicleService.DeleteVehicleAsync(userId, id);

            if (status == 400) return BadRequest(error);
            if (status == 404) return NotFound(error);
            if (status == 204) return NoContent();

            if (status.HasValue)
                return StatusCode(status.Value, error);

            return StatusCode(500, "Unexpected delete result.");
        }

        [HttpGet("vehicle/{vehicleId:int}/history")]
        public async Task<ActionResult<List<VehicleHistoryDto>>> GetMyVehicleHistory(int vehicleId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized("Invalid or missing user ID.");

            var (dtos, error, status) = await vehicleService.GetVehicleHistoryAsync(userId, vehicleId);

            if (status == 400) return BadRequest(error);
            if (status == 404) return NotFound(error);
            if (status == 204) return NoContent();
            if (status.HasValue) return StatusCode(status.Value, error);

            if (dtos is null) return StatusCode(500, "Unexpected null history list.");
            return Ok(dtos);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpGet("vehicles/user/{username}")]
        public async Task<ActionResult<List<VehicleReadDto>>> GetVehiclesByUsername(string username)
        {
            var (dtos, error, status) = await vehicleService.GetVehiclesByUsernameAsync(username);

            if (status == 400) return BadRequest(error);
            if (status == 404) return NotFound(error);
            if (status == 204) return NoContent();
            if (status.HasValue) return StatusCode(status.Value, error);

            if (dtos is null) return StatusCode(500, "Unexpected null vehicle list.");
            return Ok(dtos);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpGet("vehicles/{vehicleId:int}/history")]
        public async Task<ActionResult<List<VehicleHistoryDto>>> GetVehicleHistoryAdmin(int vehicleId)
        {
            var (dtos, error, status) = await vehicleService.GetVehicleHistoryAdminAsync(vehicleId);

            if (status == 400) return BadRequest(error);
            if (status == 404) return NotFound(error);
            if (status == 204) return NoContent();
            if (status.HasValue) return StatusCode(status.Value, error);

            if (dtos is null) return StatusCode(500, "Unexpected null history list.");
            return Ok(dtos);
        }
    }
}
