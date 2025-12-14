using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public interface IParkingLotService
    {
        Task<List<ParkingLotReadDto>> GetAllAsync();
        Task<ParkingLotReadDto?> GetByIdAsync(int lid);
        Task<List<SessionReadDto>> GetSessionsAsync(int lid, string? username, bool isAdmin);
        Task<SessionReadDto?> GetSessionByIdAsync(int lid, Guid sid, string? username, bool isAdmin);
        Task<ParkingLotReadDto> CreateParkingLotAsync(ParkingLotRequestDto parkingLot);
        Task<bool> DeleteParkingLotAsync(int id);
        Task<bool> DeleteParkingLotSessionAsync(int parkingLotId, Guid sessionId);
        Task<ParkingLotReadDto?> UpdateParkingLotAsync(int lid, ParkingLotUpdateDto dto);
    }
}
