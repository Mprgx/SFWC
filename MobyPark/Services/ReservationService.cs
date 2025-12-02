using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public class ReservationService(UserDbContext _context) : IReservationService
    {
        private static GetReservationDto ToDto(Reservation reservation)
        {
            var creator = reservation.User;
            var vehicle = reservation.Vehicle;
            var vehicleOwner = vehicle?.User;

            var creatorDto = creator is null
                ? null
                : new UserReadDto
                {
                    Id = creator.Id,
                    Username = creator.Username,
                    Name = creator.Name,
                    Email = creator.Email,
                    PhoneNumber = creator.PhoneNumber,
                    BirthYear = creator.BirthYear,
                    Role = creator.Role,
                    CreatedAt = creator.CreatedAt
                };

            var vehicleOwnerDto = vehicleOwner is null
                ? null
                : new UserReadDto
                {
                    Id = vehicleOwner.Id,
                    Username = vehicleOwner.Username,
                    Name = vehicleOwner.Name,
                    Email = vehicleOwner.Email,
                    PhoneNumber = vehicleOwner.PhoneNumber,
                    BirthYear = vehicleOwner.BirthYear,
                    Role = vehicleOwner.Role,
                    CreatedAt = vehicleOwner.CreatedAt
                };

            var vehicleDto = vehicle is null
                ? null
                : new VehicleReadDto
                {
                    Id = vehicle.Id,
                    UserId = vehicle.UserId,
                    OwnerInformation = vehicleOwnerDto,
                    LicensePlate = vehicle.LicensePlate,
                    Make = vehicle.Make,
                    Model = vehicle.Model,
                    Color = vehicle.Color,
                    Year = vehicle.Year,
                    CreatedAt = vehicle.CreatedAt
                };

            return new GetReservationDto
            {
                Id = reservation.Id,
                ParkingLotId = reservation.ParkingLotId,
                UserId = reservation.UserId,
                ReservationCreator = creatorDto,
                LicensePlate = reservation.LicensePlate,
                Vehicle = vehicleDto,
                StartTime = reservation.StartTime,
                EndTime = reservation.EndTime,
                IsActive = reservation.IsActive
            };
        }

        public GetReservationDto? GetById(int reservationId)
        {
            var reservation = _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Vehicle)
                    .ThenInclude(v => v.User)
                .FirstOrDefault(r => r.Id == reservationId);

            return reservation is null ? null : ToDto(reservation);
        }

        public GetReservationDto? GetByVehicleId(int vehicleId)
        {
            var reservation = _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Vehicle)
                    .ThenInclude(v => v.User)
                .FirstOrDefault(r => r.Vehicle.Id == vehicleId);

            return reservation is null ? null : ToDto(reservation);
        }

        public GetReservationDto CreateReservation(PostReservationDto dto)
        {
            var reservation = new Reservation
            {
                ParkingLotId = dto.ParkingLotId,
                UserId = dto.UserId,
                LicensePlate = dto.LicensePlate,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
            };

            _context.Reservations.Add(reservation);
            _context.SaveChanges();

            var loaded = _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Vehicle)
                    .ThenInclude(v => v.User)
                .First(r => r.Id == reservation.Id);

            return ToDto(loaded);
        }

        public bool DeleteReservation(int reservationId)
        {
            var reservation = _context.Reservations.Find(reservationId);
            if (reservation is null)
                return false;

            _context.Reservations.Remove(reservation);
            _context.SaveChanges();
            return true;
        }

        public GetReservationDto? UpdateReservation(int reservationId, PostReservationDto dto)
        {
            var reservation = _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Vehicle)
                    .ThenInclude(v => v.User)
                .FirstOrDefault(r => r.Id == reservationId);

            if (reservation is null)
                return null;

            reservation.ParkingLotId = dto.ParkingLotId;
            reservation.StartTime = dto.StartTime;
            reservation.EndTime = dto.EndTime;
            reservation.UserId = dto.UserId;
            reservation.LicensePlate = dto.LicensePlate;

            _context.SaveChanges();

            return ToDto(reservation);
        }
    }
}
