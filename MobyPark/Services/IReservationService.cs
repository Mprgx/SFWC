using MobyPark.Models;

namespace MobyPark.Services
{
    public interface IReservationService
    {
        Task<GetReservationDto> CreateReservation(PostReservationDto dto);
        Task<GetReservationDto?> GetById(int reservationId);
        Task<GetReservationDto?> GetByVehicleId(int vehicleId);
        Task<GetReservationDto?> UpdateReservation(int reservationId, PutReservationDto dto);
        Task<bool> DeleteReservation(int reservationId);

    }
}
