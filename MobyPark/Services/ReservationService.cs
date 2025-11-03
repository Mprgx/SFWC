using MobyPark.Entities;
using MobyPark.Data;
using MobyPark.Models;

namespace MobyPark.Services
{
    public class ReservationService
    {
        private readonly UserDbContext _context;

        public ReservationService(UserDbContext context)
        {
            _context = context;
        }

        public Reservation? GetById(int reservationId)
        {
            return _context.Reservations.FirstOrDefault(r => r.ReservationId == reservationId);
        }

        public Reservation CreateReservation(CreateReservationDto dto)
        {
            var reservation = new Reservation
            {
                ParkingLotId = dto.ParkingLotId,
                UserId = dto.UserId,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
            };

            _context.Reservations.Add(reservation);
            _context.SaveChanges();
            return reservation;
        }

        public void DeleteReservation(Reservation reservation)
        {
            _context.Reservations.Remove(reservation);
            _context.SaveChanges();
        }

        public void UpdateReservation(Reservation reservation, CreateReservationDto dto)
        {
            reservation.ParkingLotId = dto.ParkingLotId;
            reservation.StartTime = dto.StartTime;
            reservation.EndTime = dto.EndTime;
            reservation.UserId = dto.UserId;

            _context.SaveChanges();
        }
    }
}
