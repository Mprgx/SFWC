using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public interface IReservationService
    {
        GetReservationDto CreateReservation(PostReservationDto dto);
        GetReservationDto? GetById(int reservationId);
        GetReservationDto? GetByVehicleId(int vehicleId);
        GetReservationDto? UpdateReservation(int reservationId, PostReservationDto dto);
        bool DeleteReservation(int reservationId);

    }
}
