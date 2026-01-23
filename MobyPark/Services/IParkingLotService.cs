using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public interface IParkingLotService
    {
        Task<(ParkingLotReadDto? dto, string? error, int? status)> CreateAsync(ParkingLotRequestDto dto, bool isAdmin);
        Task<List<ParkingLotReadDto>> GetAllAsync();
        Task<ParkingLotReadDto?> GetByIdAsync(int lid);

        Task<(ParkingLotReadDto? dto, string? error, int? status)> UpdateAsync(int lid, ParkingLotUpdateDto dto, bool isAdmin);
        Task<(bool deleted, string? error, int? status)> DeleteAsync(int lid, bool isAdmin);

        Task<(List<SessionReadDto>? dto, string? error, int? status)> GetSessionsAsync(int lid, string? username, bool isAdmin);

        Task<(SessionReadDto? dto, string? error, int? status)> GetSessionByIdAsync(int lid, Guid sid, string? username, bool isAdmin);

        Task<(bool deleted, string? error, int? status)> DeleteSessionAsync(int lid, Guid sid, bool isAdmin);
    }
}
