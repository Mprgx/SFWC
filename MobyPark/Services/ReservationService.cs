using MobyPark.Entities;
using MobyPark.Data;
using MobyPark.Models;

namespace MobyPark.Services
{
    public class ReservationService(UserDbContext context) : IReservationService
    {
        public Reservation? GetById(int reservationId)
        {
            return context.Reservations.FirstOrDefault(r => r.ReservationId == reservationId);
        }

        public Reservation CreateReservation(CreateReservationDto dto)
        {
            var reservation = new Reservation
            {
                ParkingLotId = dto.ParkingLotId,
                UserId = dto.UserId,
                LicensePlate = dto.LicensePlate,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
            };

            context.Reservations.Add(reservation);
            context.SaveChanges();
            return reservation;
        }

        public void DeleteReservation(Reservation reservation)
        {
            context.Reservations.Remove(reservation);
            context.SaveChanges();
        }

        public void UpdateReservation(Reservation reservation, CreateReservationDto dto)
        {
            reservation.ParkingLotId = dto.ParkingLotId;
            reservation.StartTime = dto.StartTime;
            reservation.EndTime = dto.EndTime;
            reservation.UserId = dto.UserId;

            context.SaveChanges();
        }
    }
}
