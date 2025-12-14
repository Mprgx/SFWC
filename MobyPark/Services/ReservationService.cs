using System.ComponentModel.DataAnnotations;

using Microsoft.EntityFrameworkCore;

using MobyPark.Data;
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
                // Check if parking lot and user exists
                var parkingLot = await _context.ParkingLots.FirstOrDefaultAsync(p => p.Id == dto.ParkingLotId)
                    ?? throw new KeyNotFoundException("Parking lot not found");

                var user = await _context.Users.FindAsync(userId)
                    ?? throw new KeyNotFoundException("User not found");

                // Check date validity
                if (dto.StartTime >= dto.EndTime)
                {
                    throw new ValidationException("Start time must be before end time");
                }

                if (dto.StartTime < DateTimeOffset.UtcNow)
                {
                    throw new ValidationException("Start time cannot be in the past");
                }

                // Check vehicle existence

                var vehicleExistence = await _context.Vehicles.AnyAsync(v => v.LicensePlate == dto.LicensePlate);

                if (!vehicleExistence)
                {
                    throw new ValidationException($"Vehicle with license plate {dto.LicensePlate} does not exist");
                }

                // Check if there is a spot open
                var reservedCount = await _context.Reservations
                    .Where(r => r.ParkingLotId == dto.ParkingLotId
                            && r.StartTime < dto.EndTime
                            && r.EndTime > dto.StartTime)
                    .CountAsync();

                bool isAvailable = reservedCount < parkingLot.Capacity;

                if (!isAvailable)
                {
                    throw new ParkingLotFullException("No spots available for the selected time slot.");
                }

                // Check if there is an overlapping reservation
                var overlappingReservation = await _context.Reservations
                    .Where(r => r.IsActive)
                    .Where(r => r.UserId == userId)
                    .Where(r => r.ParkingLotId == dto.ParkingLotId)
                    .Where(r => r.StartTime <= dto.EndTime && r.EndTime > dto.StartTime)
                    .AnyAsync();

                if (overlappingReservation)
                {
                    throw new ValidationException("There is already an overlapping reservation for this user at this time");
                }

                var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.LicensePlate == dto.LicensePlate);

                // Books the reservation
                var reservation = new Reservation
                {
                    ParkingLotId = dto.ParkingLotId,
                    LicensePlate = encryption.Encrypt(dto.LicensePlate) ?? string.Empty,
                    StartTime = dto.StartTime,
                    EndTime = dto.EndTime,
                };

                await _context.Reservations.AddAsync(reservation);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new GetReservationDto
                {
                    Id = reservation.Id,
                    ParkingLotId = reservation.ParkingLotId,
                    UserId = reservation.UserId,
                    LicensePlate = encryption.Decrypt(reservation.LicensePlate) ?? string.Empty,
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
        public async Task<GetReservationDto?> GetById(int reservationId)
        {
            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            return reservation is null ? null : new GetReservationDto
            {
                Id = reservation.Id,
                ParkingLotId = reservation.ParkingLotId,
                UserId = reservation.UserId,
                LicensePlate = encryption.Decrypt(reservation.LicensePlate) ?? string.Empty,
                StartTime = reservation.StartTime,
                EndTime = reservation.EndTime,
                IsActive = reservation.IsActive
            };
        }

        //GET
        public async Task<GetReservationDto?> GetByVehicleId(int vehicleId)
        {
            var reservation = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Vehicle)
                    .ThenInclude(v => v.User)
                .FirstOrDefaultAsync(r => r.VehicleId == vehicleId);

            return reservation is null ? null : new GetReservationDto
            {
                Id = reservation.Id,
                ParkingLotId = reservation.ParkingLotId,
                UserId = reservation.UserId,
                LicensePlate = encryption.Decrypt(reservation.LicensePlate) ?? string.Empty,
                StartTime = reservation.StartTime,
                EndTime = reservation.EndTime,
                IsActive = reservation.IsActive
            };
        }

        //PUT
        public async Task<GetReservationDto?> UpdateReservation(int reservationId, PutReservationDto dto)
        {
            var reservation = await _context.Reservations.FindAsync(reservationId)
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
                    .Where(r => r.Id != reservation.Id
                            && r.StartTime < newEnd
                            && r.EndTime > newStart)
                    .CountAsync();

                if (reservedCount >= parkingLot.Capacity)
                    throw new ParkingLotFullException("No spots available for the selected time slot.");
            }

            if (dto.StartTime.HasValue) reservation.StartTime = dto.StartTime.Value;
            if (dto.EndTime.HasValue) reservation.EndTime = dto.EndTime.Value;
            if (!string.IsNullOrEmpty(dto.LicensePlate))
                reservation.LicensePlate = encryption.Encrypt(dto.LicensePlate) ?? string.Empty;

            await _context.SaveChangesAsync();

            return new GetReservationDto
            {
                Id = reservation.Id,
                ParkingLotId = reservation.ParkingLotId,
                UserId = reservation.UserId,
                LicensePlate = encryption.Decrypt(reservation.LicensePlate) ?? string.Empty,
                StartTime = reservation.StartTime,
                EndTime = reservation.EndTime,
                IsActive = reservation.IsActive
            };
        }

        //DELETE
        public async Task<bool> DeleteReservation(int reservationId)
        {
            var reservation = await _context.Reservations.FindAsync(reservationId);
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
