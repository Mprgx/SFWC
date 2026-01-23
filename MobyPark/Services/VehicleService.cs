using Microsoft.EntityFrameworkCore;

using MobyPark.Data;
using MobyPark.EncryptionHelper;
using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public class VehicleService(UserDbContext context, IEncryptionService encryption) : IVehicleService
    {
        public async Task<(VehicleReadDto? dto, string? error, int? status)> CreateVehicleAsync(Guid userId, VehicleCreateDto request)
        {
            if (userId == Guid.Empty)
                return (null, "UserId is required.", 400);

            if (request is null)
                return (null, "Request body is required.", 400);

            if (string.IsNullOrWhiteSpace(request.LicensePlate))
                return (null, "LicensePlate is required.", 400);

            string normalized;
            try
            {
                normalized = LicensePlateProtector.Normalize(request.LicensePlate);
            }
            catch
            {
                return (null, "Invalid license plate format.", 400);
            }

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
                return (null, "License plate already exists.", 409);

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

            return (MapToReadDto(vehicle), null, null);
        }

        public async Task<(List<VehicleReadDto>? dtos, string? error, int? status)> GetVehiclesForUserAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                return (null, "UserId is required.", 400);

            var vehicles = await context.Vehicles
                .AsNoTracking()
                .Where(v => v.UserId == userId)
                .ToListAsync();

            var dtos = vehicles.Select(MapToReadDto).ToList();

            return (dtos, null, null);
        }

        public async Task<(VehicleReadDto? dto, string? error, int? status)> UpdateVehicleAsync(Guid userId, int vehicleId, VehicleUpdateDto request)
        {
            if (userId == Guid.Empty)
                return (null, "UserId is required.", 400);

            if (request is null)
                return (null, "Request body is required.", 400);

            var hasAnyUpdate =
                !string.IsNullOrWhiteSpace(request.Make) ||
                !string.IsNullOrWhiteSpace(request.Model) ||
                !string.IsNullOrWhiteSpace(request.Color) ||
                request.Year.HasValue;

            if (!hasAnyUpdate)
                return (null, "No fields provided to update.", 400);

            if (request.Year.HasValue && (request.Year.Value < 1900 || request.Year.Value > 2100))
                return (null, "Year must be between 1900 and 2100.", 400);

            var vehicle = await context.Vehicles
                .FirstOrDefaultAsync(v => v.Id == vehicleId && v.UserId == userId);

            if (vehicle is null)
                return (null, "Vehicle not found or not owned by the user.", 404);

            if (!string.IsNullOrWhiteSpace(request.Make))
                vehicle.Make = request.Make.Trim();

            if (!string.IsNullOrWhiteSpace(request.Model))
                vehicle.Model = request.Model.Trim();

            if (!string.IsNullOrWhiteSpace(request.Color))
                vehicle.Color = request.Color.Trim();

            if (request.Year.HasValue)
                vehicle.Year = request.Year.Value;

            await context.SaveChangesAsync();

            return (MapToReadDto(vehicle), null, null);
        }

        public async Task<(string? error, int? status)> DeleteVehicleAsync(Guid userId, int vehicleId)
        {
            if (userId == Guid.Empty)
                return ("UserId is required.", 400);

            var vehicle = await context.Vehicles
                .FirstOrDefaultAsync(v => v.Id == vehicleId && v.UserId == userId);

            if (vehicle is null)
                return ("Vehicle not found or not owned by the user.", 404);

            var hasSessions = await context.Sessions
                .AsNoTracking()
                .AnyAsync(s => s.VehicleId == vehicleId);

            if (hasSessions)
                return ("Vehicle cannot be deleted because it has related sessions.", 409);

            try
            {
                context.Vehicles.Remove(vehicle);
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return ("Could not delete vehicle.", 500);
            }

            return (null, 204);
        }

        public async Task<(List<VehicleReadDto>? dtos, string? error, int? status)> GetVehiclesByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return (null, "Username is required.", 400);

            var user = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user is null)
                return (null, "User not found.", 404);

            var vehicles = await context.Vehicles
                .AsNoTracking()
                .Where(v => v.UserId == user.Id)
                .ToListAsync();

            var dtos = vehicles.Select(MapToReadDto).ToList();
            return (dtos, null, null);
        }

        public async Task<(List<VehicleHistoryDto>? dtos, string? error, int? status)> GetVehicleHistoryAsync(Guid userId, int vehicleId)
        {
            if (userId == Guid.Empty)
                return (null, "UserId is required.", 400);

            // ownership check
            var owned = await context.Vehicles
                .AsNoTracking()
                .AnyAsync(v => v.Id == vehicleId && v.UserId == userId);

            if (!owned)
                return (null, "Vehicle not found or not owned by the user.", 404);

            var history = await context.Sessions
                .AsNoTracking()
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

            return (history, null, null);
        }

        public async Task<(List<VehicleHistoryDto>? dtos, string? error, int? status)> GetVehicleHistoryAdminAsync(int vehicleId)
        {
            var vehicleExists = await context.Vehicles
                .AsNoTracking()
                .AnyAsync(v => v.Id == vehicleId);

            if (!vehicleExists)
                return (null, "Vehicle not found.", 404);

            var history = await context.Sessions
                .AsNoTracking()
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

            return (history, null, null);
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
