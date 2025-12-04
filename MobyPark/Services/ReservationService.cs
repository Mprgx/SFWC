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

            var loaded = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Vehicle)
                    .ThenInclude(v => v.User)
                .FirstAsync(r => r.Id == reservation.Id);

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
            var reservation = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Vehicle)
                    .ThenInclude(v => v.User)
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (reservation == null)
                return null;

            if (dto.StartTime.HasValue)
            {
                if (dto.StartTime.Value < DateTimeOffset.UtcNow)
                    throw new ValidationException("StartTime cannot be in the past.");

                if (!dto.EndTime.HasValue && reservation.EndTime <= dto.StartTime.Value)
                    throw new ValidationException("StartTime must be before the existing EndTime.");
            }

            if (dto.EndTime.HasValue)
            {
                if (!dto.StartTime.HasValue && dto.EndTime.Value <= reservation.StartTime)
                    throw new ValidationException("EndTime must be after the existing StartTime.");
            }

            if (dto.StartTime.HasValue && dto.EndTime.HasValue)
            {
                if (dto.EndTime.Value <= dto.StartTime.Value)
                    throw new ValidationException("EndTime must be after StartTime.");
            }

            if (dto.ParkingLotId.HasValue)
                reservation.ParkingLotId = dto.ParkingLotId.Value;
            if (dto.StartTime.HasValue)
                reservation.StartTime = dto.StartTime.Value;
            if (dto.EndTime.HasValue)
                reservation.EndTime = dto.EndTime.Value;
            if (dto.UserId.HasValue)
                reservation.UserId = dto.UserId.Value;
            if (!string.IsNullOrEmpty(dto.LicensePlate))
                reservation.LicensePlate = dto.LicensePlate;

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
            _context.SaveChanges();
            return true;
        }
    }
}
