using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public interface IVehicleService
    {
        Task<Vehicle?> CreateVehicleAsync(Guid userId, VehicleRequestDto request);
        Task<List<Vehicle>> GetVehiclesByUsernameAsync(string username);
    }

}
