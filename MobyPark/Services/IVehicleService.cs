using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public interface IVehicleService
    {
        Task<VehicleReadDto?> CreateVehicleAsync(Guid userId, VehicleReadDto request);
        Task<List<Vehicle>> GetVehiclesByUsernameAsync(string username);
    }

}
