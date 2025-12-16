using MobyPark.Models;

namespace MobyPark.Services
{
    public interface IVehicleService
    {
        Task<VehicleReadDto?> CreateVehicleAsync(Guid userId, VehicleCreateDto request);
        Task<List<VehicleReadDto>> GetVehiclesForUserAsync(Guid userId);
        Task<List<VehicleReadDto>> GetVehiclesByUsernameAsync(string username);
        Task<VehicleReadDto?> UpdateVehicleAsync(Guid userId, int vehicleId, VehicleUpdateDto request);
        Task<bool> DeleteVehicleAsync(Guid userId, int vehicleId);
        Task<List<VehicleHistoryDto>> GetVehicleHistoryAsync(int vehicleId);

    }


}
