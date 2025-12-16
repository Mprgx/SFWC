using MobyPark.Models;

namespace MobyPark.Services
{
    public interface IReservationService
    {
        Task<GetReservationDto> CreateReservation(PostReservationDto dto, Guid userId);
        Task<GetReservationDto?> GetById(int reservationId, Guid userId);
        Task<GetReservationDto?> GetByVehicleId(int vehicleId, Guid userId);
        Task<GetReservationDto?> UpdateReservation(int reservationId, PutReservationDto dto, Guid userId);
        Task<bool> DeleteReservation(int reservationId, Guid userId);

    }
}
