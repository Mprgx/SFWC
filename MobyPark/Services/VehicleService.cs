using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public class VehicleService(UserDbContext context) : IVehicleService
    {
        //POST /vehicles
        public async Task<VehicleReadDto?> CreateVehicleAsync(Guid userId, VehicleCreateDto request)
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

            return new VehicleReadDto
            {
                Id = vehicle.Id,
                UserId = vehicle.UserId,
                LicensePlate = vehicle.LicensePlate,
                Make = vehicle.Make,
                Model = vehicle.Model,
                Color = vehicle.Color,
                Year = vehicle.Year,
                CreatedAt = vehicle.CreatedAt
            };
        }

        //GET /vehicles
        public async Task<List<Vehicle>> GetVehiclesForUserAsync(Guid userId)
        {
            return await context.Vehicles
                .Where(v => v.UserId == userId)
                .OrderBy(v => v.CreatedAt)
                .ToListAsync();
        }

        //GET /vehicles
        public async Task<List<VehicleReadDto>> GetVehiclesByUsernameAsync(string username)
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user is null)
                return new List<VehicleReadDto>();

            return await context.Vehicles
                .Where(v => v.UserId == user.Id)
                .Select(v => new VehicleReadDto
                {
                    Id = v.Id,
                    UserId = v.UserId,
                    LicensePlate = v.LicensePlate,
                    Make = v.Make,
                    Model = v.Model,
                    Color = v.Color,
                    Year = v.Year,
                    CreatedAt = v.CreatedAt
                })
                .ToListAsync();
        }

        //PUT /vehicles

        //DELETE /vehicles
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
    }
}
