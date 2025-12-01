using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public class VehicleService(UserDbContext context) : IVehicleService
    {
        public async Task<VehicleReadDto?> CreateVehicleAsync(Guid userId, VehicleReadDto request)
        {
            // Check for duplicate license plate for the same user
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

            var vehicleDto = new VehicleReadDto
            {
                Id = vehicle.Id,
                UserId = vehicle.UserId,
                OwnerInformation = new UserReadDto
                {
                    Id = vehicle.User.Id,
                    Username = vehicle.User.Username,
                    Name = vehicle.User.Name,
                    Email = vehicle.User.Email,
                    PhoneNumber = vehicle.User.PhoneNumber,
                    BirthYear = vehicle.User.BirthYear,
                    Role = vehicle.User.Role,
                    CreatedAt = vehicle.User.CreatedAt
                },
                LicensePlate = vehicle.LicensePlate,
                Make = vehicle.Make,
                Model = vehicle.Model,
                Color = vehicle.Color,
                Year = vehicle.Year,
                CreatedAt = vehicle.CreatedAt
            };

            return vehicleDto;
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
        
        public async Task<List<Vehicle>> GetVehiclesByUsernameAsync(string username)
        {
            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user is null)
                return new List<Vehicle>();

            return await context.Vehicles
                .Where(v => v.UserId == user.Id)
                .ToListAsync();
        }

    }
}
