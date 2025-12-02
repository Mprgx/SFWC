using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public interface IVehicleService
    {
        Task<VehicleReadDto?> CreateVehicleAsync(Guid userId, VehicleCreateDto request);
        Task<bool> DeleteVehicleAsync(Guid userId, int vehicleId);
        Task<List<VehicleReadDto>> GetVehiclesByUsernameAsync(string username);
    }


}
