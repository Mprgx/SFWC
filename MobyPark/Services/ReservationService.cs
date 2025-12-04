using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public class ReservationService(UserDbContext _context) : IReservationService
    {

        //POST
        public async Task<GetReservationDto> CreateReservation(PostReservationDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Check if parking lot and user exists
                var parkingLot = await _context.ParkingLots.FirstOrDefaultAsync(p => p.Id == dto.ParkingLotId)
                    ?? throw new KeyNotFoundException("Parking lot not found");

                var user = await _context.Users.FindAsync(dto.UserId)
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

                // Books the reservation
                var reservation = new Reservation
                {
                    ParkingLotId = dto.ParkingLotId,
                    UserId = dto.UserId,
                    LicensePlate = dto.LicensePlate,
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
                    LicensePlate = reservation.LicensePlate,
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
                .Include(r => r.User)
                .Include(r => r.Vehicle)
                    .ThenInclude(v => v.User)
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            return reservation is null ? null : new GetReservationDto
            {
                Id = reservation.Id,
                ParkingLotId = reservation.ParkingLotId,
                UserId = reservation.UserId,
                LicensePlate = reservation.LicensePlate,
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
                .FirstOrDefaultAsync(r => r.Vehicle.Id == vehicleId);

            return reservation is null ? null : new GetReservationDto
            {
                Id = reservation.Id,
                ParkingLotId = reservation.ParkingLotId,
                UserId = reservation.UserId,
                LicensePlate = reservation.LicensePlate,
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

            var parkingLot = dto.ParkingLotId.HasValue
                ? await _context.ParkingLots.FindAsync(dto.ParkingLotId.Value)
                    ?? throw new KeyNotFoundException("Parking lot not found")
                : await _context.ParkingLots.FindAsync(reservation.ParkingLotId)
                    ?? throw new KeyNotFoundException("Parking lot not found");

            if (dto.UserId.HasValue)
            {
                var user = await _context.Users.FindAsync(dto.UserId.Value)
                    ?? throw new KeyNotFoundException("User not found");
            }

            var newStart = dto.StartTime ?? reservation.StartTime;
            var newEnd = dto.EndTime ?? reservation.EndTime;

            if (newStart >= newEnd)
                throw new ValidationException("StartTime must be before EndTime.");

            if (newStart < DateTimeOffset.UtcNow)
                throw new ValidationException("StartTime cannot be in the past.");

            if (dto.StartTime.HasValue || dto.EndTime.HasValue || dto.ParkingLotId.HasValue)
            {
                var reservedCount = await _context.Reservations
                    .Where(r => r.ParkingLotId == parkingLot.Id
                            && r.Id != reservation.Id
                            && r.StartTime < newEnd
                            && r.EndTime > newStart)
                    .CountAsync();

                if (reservedCount >= parkingLot.Capacity)
                    throw new ParkingLotFullException("No spots available for the selected time slot.");
            }

            if (dto.ParkingLotId.HasValue) reservation.ParkingLotId = dto.ParkingLotId.Value;
            if (dto.StartTime.HasValue) reservation.StartTime = dto.StartTime.Value;
            if (dto.EndTime.HasValue) reservation.EndTime = dto.EndTime.Value;
            if (dto.UserId.HasValue) reservation.UserId = dto.UserId.Value;
            if (!string.IsNullOrEmpty(dto.LicensePlate)) reservation.LicensePlate = dto.LicensePlate;

            await _context.SaveChangesAsync();

            return new GetReservationDto
            {
                Id = reservation.Id,
                ParkingLotId = reservation.ParkingLotId,
                UserId = reservation.UserId,
                LicensePlate = reservation.LicensePlate,
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
