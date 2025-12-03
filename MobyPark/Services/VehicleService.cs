using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public class VehicleService(UserDbContext context) : IVehicleService
    {
        public async Task<VehicleReadDto?> CreateVehicleAsync(Guid userId, VehiclePostDto request)
        {
            bool exists = await context.Vehicles
                .AnyAsync(v => v.LicensePlate == request.LicensePlate && v.UserId == userId);

            if (exists)
                return null;

            var vehicle = new Vehicle
            {
                UserId = userId,
                LicensePlate = request.LicensePlate,
                Make = request.Make,
                Model = request.Model,
                Color = request.Color,
                Year = request.Year,
                CreatedAt = DateTimeOffset.UtcNow
            };

            context.Vehicles.Add(vehicle);
            await context.SaveChangesAsync();

            var user = await context.Users.FindAsync(userId);
            if (user is null)
                throw new InvalidOperationException("User not found for this vehicle.");

            vehicle.User = user;

            return ToVehicleReadDto(vehicle);
        }

        public async Task<bool> DeleteVehicleAsync(Guid userId, int vehicleId)
        {
            var vehicle = await context.Vehicles
                .FirstOrDefaultAsync(v => v.Id == vehicleId && v.UserId == userId);

            if (vehicle is null)
                return false;

            context.Vehicles.Remove(vehicle);
            await context.SaveChangesAsync();

            return true;
        }

        public async Task<List<Vehicle>> GetVehiclesForUserAsync(Guid userId)
        {
            return await context.Vehicles
                .Where(v => v.UserId == userId)
                .OrderBy(v => v.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<VehicleReadDto>> GetVehiclesByUsernameAsync(string username)
        {
            var vehicles = await context.Vehicles
                .Include(v => v.User)
                .Where(v => v.User.Username == username)
                .OrderBy(v => v.CreatedAt)
                .ToListAsync();

            if (vehicles.Count == 0)
                return new List<VehicleReadDto>();

            return vehicles
                .Select(ToVehicleReadDto)
                .ToList();
        }

        private static VehicleReadDto ToVehicleReadDto(Vehicle v)
        {
            if (v.User is null)
                throw new InvalidOperationException("Vehicle.User must be loaded to map to VehicleReadDto.");

            return new VehicleReadDto
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
            };
        }
    }
}
