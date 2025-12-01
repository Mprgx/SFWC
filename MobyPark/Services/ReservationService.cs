using MobyPark.Entities;
using MobyPark.Data;
using MobyPark.Models;

namespace MobyPark.Services
{
    public class ReservationService(UserDbContext _context) : IReservationService
    {
        public Reservation? GetById(int reservationId)
        {
            return _context.Reservations.FirstOrDefault(r => r.Id == reservationId);
        }

        public Reservation? GetByVehicleId(int vehicleid)
        {
            return _context.Reservations.FirstOrDefault(r => r.Vehicle.Id == vehicleid);
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

            var reservationDto = new GetReservationDto
            {
                Id = reservation.Id,
                ParkingLotId = reservation.ParkingLotId,
                UserId = reservation.UserId,
                ReservationCreator = new UserReadDto
                {
                    Id = reservation.User.Id,
                    Username = reservation.User.Username,
                    Name = reservation.User.Name,
                    Email = reservation.User.Email,
                    PhoneNumber = reservation.User.PhoneNumber,
                    BirthYear = reservation.User.BirthYear,
                    Role = reservation.User.Role,
                    CreatedAt = reservation.User.CreatedAt
                },
                LicensePlate = reservation.LicensePlate,
                Vehicle = new VehicleReadDto
                {
                    Id = reservation.Vehicle.Id,
                    UserId = reservation.Vehicle.UserId,
                    OwnerInformation = new UserReadDto
                    {
                        Id = reservation.Vehicle.UserId,
                        Username = reservation.Vehicle.User.Username,
                        Name = reservation.Vehicle.User.Name,
                        Email = reservation.Vehicle.User.Email,
                        PhoneNumber = reservation.Vehicle.User.PhoneNumber,
                        BirthYear = reservation.Vehicle.User.BirthYear,
                        Role = reservation.Vehicle.User.Role,
                        CreatedAt = reservation.Vehicle.User.CreatedAt
                    },
                    LicensePlate = reservation.Vehicle.LicensePlate,
                    Make = reservation.Vehicle.Make,
                    Model = reservation.Vehicle.Model,
                    Color = reservation.Vehicle.Color,
                    Year = reservation.Vehicle.Year,
                    CreatedAt = reservation.Vehicle.CreatedAt
                },
                StartTime = reservation.StartTime,
                EndTime = reservation.EndTime,
                IsActive = reservation.IsActive
            };

            return reservationDto;
        }

        public void DeleteReservation(Reservation reservation)
        {
            _context.Reservations.Remove(reservation);
            _context.SaveChanges();
        }

        public void UpdateReservation(Reservation reservation, PostReservationDto dto)
        {
            reservation.ParkingLotId = dto.ParkingLotId;
            reservation.StartTime = dto.StartTime;
            reservation.EndTime = dto.EndTime;
            reservation.UserId = dto.UserId;

            _context.SaveChanges();
        }
    }
}
