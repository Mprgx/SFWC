using Microsoft.EntityFrameworkCore;

using MobyPark.Data;
using MobyPark.EncryptionHelper;
using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public class VehicleService(UserDbContext context, IEncryptionService encryption) : IVehicleService
    {
        public async Task<VehicleReadDto?> CreateVehicleAsync(Guid userId, VehicleCreateDto request)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            var normalized = LicensePlateProtector.Normalize(request.LicensePlate);

            var existingEncryptedPlates = await context.Vehicles
                .Where(v => v.UserId == userId)
                .Select(v => v.LicensePlate)
                .ToListAsync();

            bool exists = existingEncryptedPlates.Any(p =>
                string.Equals(
                    LicensePlateProtector.DecryptNormalized(encryption, p),
                    normalized,
                    StringComparison.Ordinal));

            if (exists)
                return null;

            var vehicle = new Vehicle
            {
                UserId = userId,
                LicensePlate = LicensePlateProtector.EncryptNormalized(encryption, normalized),
                Make = request.Make,
                Model = request.Model,
                Color = request.Color,
                Year = request.Year,
                CreatedAt = DateTimeOffset.UtcNow
            };

            context.Vehicles.Add(vehicle);
            await context.SaveChangesAsync();

            return MapToReadDto(vehicle);
        }

        public async Task<List<VehicleReadDto>> GetVehiclesForUserAsync(Guid userId)
        {
            var vehicles = await context.Vehicles
                .Where(v => v.UserId == userId)
                .OrderBy(v => v.CreatedAt)
                .ToListAsync();

            return vehicles.Select(MapToReadDto).ToList();
        }

        public async Task<List<VehicleReadDto>> GetVehiclesByUsernameAsync(string username)
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user is null)
                return new List<VehicleReadDto>();

            var vehicles = await context.Vehicles
                .Where(v => v.UserId == user.Id)
                .OrderBy(v => v.CreatedAt)
                .ToListAsync();

            return vehicles.Select(MapToReadDto).ToList();
        }

        public async Task<VehicleReadDto?> UpdateVehicleAsync(Guid userId, int vehicleId, VehicleUpdateDto request)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            var vehicle = await context.Vehicles
                .FirstOrDefaultAsync(v => v.Id == vehicleId && v.UserId == userId);

            if (vehicle is null)
                return null;

            if (!string.IsNullOrWhiteSpace(request.Make))
                vehicle.Make = request.Make.Trim();

            if (!string.IsNullOrWhiteSpace(request.Model))
                vehicle.Model = request.Model.Trim();

            if (!string.IsNullOrWhiteSpace(request.Color))
                vehicle.Color = request.Color.Trim();

            if (request.Year.HasValue)
                vehicle.Year = request.Year.Value;

            await context.SaveChangesAsync();

            return MapToReadDto(vehicle);
        }

        public async Task<List<VehicleHistoryDto>> GetVehicleHistoryAsync(int vehicleId)
        {
            var vehicleExists = await context.Vehicles
                .AnyAsync(v => v.Id == vehicleId);

            if (!vehicleExists)
                throw new KeyNotFoundException("Vehicle not found");

            return await context.Sessions
                .Where(s => s.VehicleId == vehicleId)
                .Include(s => s.ParkingLot)
                .OrderByDescending(s => s.Started)
                .Select(s => new VehicleHistoryDto
                {
                    SessionId = s.Id,
                    ParkingLotId = s.ParkingLotId,
                    ParkingLotName = s.ParkingLot!.Name,
                    Started = s.Started,
                    Stopped = s.Stopped,
                    Cost = s.Cost
                })
                .ToListAsync();
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

        private VehicleReadDto MapToReadDto(Vehicle v) => new()
        {
            Id = v.Id,
            UserId = v.UserId,
            LicensePlate = LicensePlateProtector.DecryptNormalized(encryption, v.LicensePlate),
            Make = v.Make,
            Model = v.Model,
            Color = v.Color,
            Year = v.Year,
            CreatedAt = v.CreatedAt
        };
    }
}
