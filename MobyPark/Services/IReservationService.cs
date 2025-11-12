using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public interface IReservationService
    {
        Reservation? GetById(int reservationId);
        Reservation CreateReservation(CreateReservationDto dto);
        void DeleteReservation(Reservation reservation);
        void UpdateReservation(Reservation reservation, CreateReservationDto dto);

    }
}
