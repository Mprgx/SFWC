using MobyPark.Models;

namespace MobyPark.Services
{
    public interface IVehicleService
    {
        Task<(VehicleReadDto? dto, string? error, int? status)> CreateVehicleAsync(Guid userId, VehicleCreateDto request);
        Task<(List<VehicleReadDto>? dtos, string? error, int? status)> GetVehiclesForUserAsync(Guid userId);       
        Task<(VehicleReadDto? dto, string? error, int? status)> UpdateVehicleAsync(Guid userId, int vehicleId, VehicleUpdateDto request);
        Task<(string? error, int? status)> DeleteVehicleAsync(Guid userId, int vehicleId);
        Task<(List<VehicleReadDto>? dtos, string? error, int? status)> GetVehiclesByUsernameAsync(string username);
        Task<(List<VehicleHistoryDto>? dtos, string? error, int? status)> GetVehicleHistoryAsync(Guid userId, int vehicleId);
        Task<(List<VehicleHistoryDto>? dtos, string? error, int? status)> GetVehicleHistoryAdminAsync(int vehicleId);
    }


}
