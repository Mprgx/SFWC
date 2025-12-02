using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public interface IReservationService
    {
        GetReservationDto? GetById(int reservationId);
        GetReservationDto? GetByVehicleId(int vehicleId);
        GetReservationDto CreateReservation(PostReservationDto dto);
        bool DeleteReservation(int reservationId);
        GetReservationDto? UpdateReservation(int reservationId, PostReservationDto dto);

    }
}
