using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobyPark.Entities;
using MobyPark.Models;
using MobyPark.Services;
using System.Security.Claims;

namespace MobyPark.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class VehicleController(IVehicleService vehicleService) : ControllerBase
    {

        // Voegt een nieuw voertuig toe aan de ingelogde gebruiker.
        [HttpPost("vehicle")]
        public async Task<ActionResult<VehicleReadDto>> CreateVehicle(VehicleRequestDto request)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid or missing user ID.");

            // Maak voertuig aan via de service
            var vehicle = await vehicleService.CreateVehicleAsync(userId, request);
            if (vehicle is null)
                return BadRequest("Could not create vehicle. License plate may already exist.");

            // Maak een DTO om terug te sturen
            var dto = new VehicleReadDto(
                vehicle.Id,
                vehicle.UserId,
                vehicle.LicensePlate,
                vehicle.Make,
                vehicle.Model,
                vehicle.Color,
                vehicle.Year,
                vehicle.CreatedAt
            );

            return Ok(dto);
        }
    }
}
