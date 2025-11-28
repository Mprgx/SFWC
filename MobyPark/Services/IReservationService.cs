using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public interface IReservationService
    {
        Reservation? GetById(int reservationId);
        Reservation? GetByVehicleId(int vehicleid);
        GetReservationDto CreateReservation(PostReservationDto dto);
        void DeleteReservation(Reservation reservation);
        void UpdateReservation(Reservation reservation, PostReservationDto dto);

    }
}
