using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobyPark.Entities;
using MobyPark.Models;
using MobyPark.Services;
using System.Security.Claims;

namespace MobyPark.Controllers
{
    [ApiController]
    [Authorize]
    public class VehicleController(IVehicleService vehicleService) : ControllerBase
    {
        [HttpPost("vehicle")]
        public async Task<ActionResult<VehicleReadDto>> CreateVehicle([FromBody] VehiclePostDto request)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid or missing user ID.");

            var vehicle = await vehicleService.CreateVehicleAsync(userId, request);
            if (vehicle is null)
                return Conflict("License plate may already exist for this user.");

            return Ok(vehicle);
        }

        [HttpDelete("vehicle/{id:int}")]
        public async Task<IActionResult> DeleteVehicle(int id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid or missing user ID.");

            var deleted = await vehicleService.DeleteVehicleAsync(userId, id);

            if (!deleted)
                return NotFound("Vehicle not found or not owned by the user.");

            return Ok(new { status = "Deleted" });
        }

        [HttpGet("vehicles")]
        public async Task<ActionResult<List<Vehicle>>> GetMyVehicles()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid or missing user ID.");

            var vehicles = await vehicleService.GetVehiclesForUserAsync(userId);
            return Ok(vehicles);
        }

        [HttpGet("vehicle/{username}")]
        public async Task<ActionResult<List<VehicleReadDto>>> GetVehicleByUser(string username)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out _))
                return Unauthorized("Invalid or missing user ID.");

            var role = User.FindFirstValue(ClaimTypes.Role);
            if (role != "Admin")
                return Forbid("You are not permitted to use this function.");

            var vehicles = await vehicleService.GetVehiclesByUsernameAsync(username);

            if (vehicles.Count == 0)
                return NoContent();

            return Ok(vehicles);
        }
    }
}
