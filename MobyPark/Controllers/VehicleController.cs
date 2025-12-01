using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobyPark.Models;
using MobyPark.Services;
using System.Security.Claims;

namespace MobyPark.Controllers
{
    [Route("api/")]
    [ApiController]
    [Authorize]
    public class VehicleController(IVehicleService vehicleService) : ControllerBase
    {
        [HttpPost("vehicle")]
        public async Task<ActionResult<VehicleReadDto>> CreateVehicle(VehicleReadDto request)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid or missing user ID.");

            var vehicle = await vehicleService.CreateVehicleAsync(userId, request);
            if (vehicle is null) return Conflict("License plate may already exist.");

            return Ok(vehicle);
        }

        [HttpGet("vehicle/{username}")]
        public async Task<ActionResult<List<VehicleReadDto>>> GetVehicleByUser(string username)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out _))
                return Unauthorized("Invalid or missing user ID.");

            var role = User.FindFirstValue(ClaimTypes.Role);
            if (role != "Admin")
                return Forbid("You are not permitted to use this function");

            var vehicles = await vehicleService.GetVehiclesByUsernameAsync(username);

            if (vehicles.Count == 0)
                return NoContent();

            var dtoList = vehicles.Select(v => new VehicleReadDto
            {
                Id = v.Id,
                UserId = v.UserId,
                OwnerInformation = new UserReadDto
                {
                    Id = v.User.Id,
                    Username = v.User.Username,
                    Name = v.User.Name,
                    Email = v.User.Email,
                    PhoneNumber = v.User.PhoneNumber,
                    BirthYear = v.User.BirthYear,
                    Role = v.User.Role,
                    CreatedAt = v.User.CreatedAt
                },
                LicensePlate = v.LicensePlate,
                Make = v.Make,
                Model = v.Model,
                Color = v.Color,
                Year = v.Year,
                CreatedAt = v.CreatedAt
            }).ToList();

            return Ok(dtoList);
        }


    }
}
