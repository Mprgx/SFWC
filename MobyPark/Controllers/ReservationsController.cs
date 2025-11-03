using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobyPark.Models;
using MobyPark.Services;
using System.Security.Claims;

namespace MobyPark.Controllers
{
    [Route("api/[controller]")]

    [ApiController]
    public class ReservationsController : ControllerBase
    {
        [HttpPost("reservations")]
        public async Task<ActionResult<ReservationDto>> CreateReservation(CreateReservationDto request)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid or missing user ID.");

            var vehicle = await vehicleService.CreateVehicleAsync(userId, request);
            if (vehicle is null) return Conflict("License plate may already exist.");

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
