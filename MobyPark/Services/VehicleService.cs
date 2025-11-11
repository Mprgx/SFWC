using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public class VehicleService : IVehicleService
    {
        private readonly UserDbContext _context;

        public VehicleService(UserDbContext context)
        {
            _context = context;
        }

        public async Task<Vehicle?> CreateVehicleAsync(Guid userId, VehicleRequestDto request)
        {
            // Check for duplicate license plate for the same user
            bool exists = await _context.Vehicles
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

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            return vehicle;
        }
    }
}
