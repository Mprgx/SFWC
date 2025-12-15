using System.ComponentModel.DataAnnotations;

using Microsoft.EntityFrameworkCore;

using MobyPark.Data;
using MobyPark.EncryptionHelper;
using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public class ReservationService(UserDbContext _context, IEncryptionService encryption) : IReservationService
    {
        //POST
        public async Task<GetReservationDto> CreateReservation(PostReservationDto dto, Guid userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var parkingLot = await _context.ParkingLots
                    .FirstOrDefaultAsync(p => p.Id == dto.ParkingLotId)
                    ?? throw new KeyNotFoundException("Parking lot not found");

                var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
                if (!userExists) throw new KeyNotFoundException("User not found");

                // Resolve vehicle (VehicleId preferred; LicensePlate fallback)
                Vehicle vehicle;

                if (dto.VehicleId.HasValue)
                {
                    vehicle = await _context.Vehicles
                        .FirstOrDefaultAsync(v => v.Id == dto.VehicleId.Value && v.UserId == userId)
                        ?? throw new ValidationException("Vehicle not found for this user.");
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(dto.LicensePlate))
                        throw new ValidationException("VehicleId or LicensePlate is required.");

                    var wanted = LicensePlateProtector.Normalize(dto.LicensePlate);

                    // Only scan this user's vehicles (small set)
                    var userVehicles = await _context.Vehicles
                        .Where(v => v.UserId == userId)
                        .ToListAsync();

                    vehicle = userVehicles.FirstOrDefault(v =>
                        LicensePlateProtector.DecryptNormalized(encryption, v.LicensePlate) == wanted)
                        ?? throw new ValidationException($"Vehicle with license plate {dto.LicensePlate} does not exist for this user.");
                }

                // Availability check (count only active reservations in THIS parking lot)
                var reservedCount = await _context.Reservations
                    .Where(r => r.IsActive)
                    .Where(r => r.ParkingLotId == dto.ParkingLotId)
                    .Where(r => r.StartTime < dto.EndTime && r.EndTime > dto.StartTime)
                    .CountAsync();

                if (reservedCount >= parkingLot.Capacity)
                    throw new ParkingLotFullException("No spots available for the selected time slot.");

                // Overlap check for SAME user in SAME parking lot (recommended)
                var userOverlap = await _context.Reservations
                    .Where(r => r.IsActive)
                    .Where(r => r.UserId == userId)
                    .Where(r => r.ParkingLotId == dto.ParkingLotId)
                    .Where(r => r.StartTime < dto.EndTime && r.EndTime > dto.StartTime)
                    .AnyAsync();

                if (userOverlap)
                    throw new ValidationException("There is already an overlapping reservation for this user at this time.");

                // Create reservation with REQUIRED fields set
                var reservation = new Reservation
                {
                    ParkingLotId = dto.ParkingLotId,
                    UserId = userId,                 //  required
                    VehicleId = vehicle.Id,          //  required
                    LicensePlate = vehicle.LicensePlate, //  store encrypted plate from Vehicle (no double-encrypt)
                    StartTime = dto.StartTime,
                    EndTime = dto.EndTime,
                    IsActive = true
                };

                await _context.Reservations.AddAsync(reservation);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new GetReservationDto
                {
                    Id = reservation.Id,
                    ParkingLotId = reservation.ParkingLotId,
                    UserId = reservation.UserId,
                    VehicleId = reservation.VehicleId, //  return it
                    LicensePlate = LicensePlateProtector.DecryptNormalized(encryption, reservation.LicensePlate),
                    StartTime = reservation.StartTime,
                    EndTime = reservation.EndTime,
                    IsActive = reservation.IsActive
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        //GET
        public async Task<GetReservationDto?> GetById(int reservationId, Guid userId)
        {
            var reservation = await _context.Reservations
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == reservationId && r.UserId == userId);

            return reservation is null ? null : new GetReservationDto
            {
                Id = reservation.Id,
                ParkingLotId = reservation.ParkingLotId,
                UserId = reservation.UserId,
                VehicleId = reservation.VehicleId, //  add this
                LicensePlate = LicensePlateProtector.DecryptNormalized(encryption, reservation.LicensePlate),
                StartTime = reservation.StartTime,
                EndTime = reservation.EndTime,
                IsActive = reservation.IsActive
            };
        }

        //GET
        public async Task<GetReservationDto?> GetByVehicleId(int vehicleId, Guid userId)
        {
            var reservation = await _context.Reservations
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.VehicleId == vehicleId && r.UserId == userId);

            return reservation is null ? null : new GetReservationDto
            {
                Id = reservation.Id,
                ParkingLotId = reservation.ParkingLotId,
                UserId = reservation.UserId,
                VehicleId = reservation.VehicleId, //  add this
                LicensePlate = LicensePlateProtector.DecryptNormalized(encryption, reservation.LicensePlate),
                StartTime = reservation.StartTime,
                EndTime = reservation.EndTime,
                IsActive = reservation.IsActive
            };
        }

        //PUT
        public async Task<GetReservationDto?> UpdateReservation(int reservationId, PutReservationDto dto, Guid userId)
        {
            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.Id == reservationId && r.UserId == userId)
                ?? throw new KeyNotFoundException("Reservation not found");

            var newStart = dto.StartTime ?? reservation.StartTime;
            var newEnd = dto.EndTime ?? reservation.EndTime;

            var parkingLot = await _context.ParkingLots
                .FirstOrDefaultAsync(pl => pl.Id == reservation.ParkingLotId)
                    ?? throw new KeyNotFoundException("Parking lot not found");

            if (newStart >= newEnd)
                throw new ValidationException("StartTime must be before EndTime.");

            if (newStart < DateTimeOffset.UtcNow)
                throw new ValidationException("StartTime cannot be in the past.");

            if (dto.StartTime.HasValue || dto.EndTime.HasValue)
            {
                var reservedCount = await _context.Reservations
                    .Where(r => r.IsActive)
                    .Where(r => r.Id != reservation.Id)
                    .Where(r => r.ParkingLotId == reservation.ParkingLotId) //  important
                    .Where(r => r.StartTime < newEnd && r.EndTime > newStart)
                    .CountAsync();

                if (reservedCount >= parkingLot.Capacity)
                    throw new ParkingLotFullException("No spots available for the selected time slot.");
            }

            if (dto.StartTime.HasValue) reservation.StartTime = dto.StartTime.Value;
            if (dto.EndTime.HasValue) reservation.EndTime = dto.EndTime.Value;
            if (!string.IsNullOrWhiteSpace(dto.LicensePlate))
            {
                var wanted = LicensePlateProtector.Normalize(dto.LicensePlate);

                var userVehicles = await _context.Vehicles
                    .Where(v => v.UserId == reservation.UserId)
                    .ToListAsync();

                var vehicle = userVehicles.FirstOrDefault(v =>
                    LicensePlateProtector.DecryptNormalized(encryption, v.LicensePlate) == wanted);

                if (vehicle is null)
                    throw new ValidationException($"Vehicle with license plate {dto.LicensePlate} does not exist for this user.");

                reservation.VehicleId = vehicle.Id;
                reservation.LicensePlate = vehicle.LicensePlate;
            }

            await _context.SaveChangesAsync();

            return new GetReservationDto
            {
                Id = reservation.Id,
                ParkingLotId = reservation.ParkingLotId,
                UserId = reservation.UserId,
                VehicleId = reservation.VehicleId, //  add this
                LicensePlate = LicensePlateProtector.DecryptNormalized(encryption, reservation.LicensePlate),
                StartTime = reservation.StartTime,
                EndTime = reservation.EndTime,
                IsActive = reservation.IsActive
            };
        }

        //DELETE
        public async Task<bool> DeleteReservation(int reservationId, Guid userId)
        {
            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.Id == reservationId && r.UserId == userId);

            if (reservation is null)
                return false;

            _context.Reservations.Remove(reservation);
            await _context.SaveChangesAsync();
            return true;
        }
    }

    public class ParkingLotFullException : Exception
    {
        public ParkingLotFullException(string message) : base(message) { }
    }

}
