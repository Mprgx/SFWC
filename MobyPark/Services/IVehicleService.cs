using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public interface IVehicleService
    {
        Task<VehicleReadDto?> CreateVehicleAsync(Guid userId, VehiclePostDto request);
        Task<bool> DeleteVehicleAsync(Guid userId, int vehicleId);
        Task<List<Vehicle>> GetVehiclesForUserAsync(Guid userId);
        Task<List<VehicleReadDto>> GetVehiclesByUsernameAsync(string username);
    }


}
