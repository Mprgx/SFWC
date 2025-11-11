using MobyPark.Entities;

namespace MobyPark.Services
{
    public interface IParkingLotService
    {
        Task<List<ParkingLot>> GetAllAsync();
        Task<ParkingLot?> GetByIdAsync(string lid);
        Task<List<ParkingSession>> GetSessionsAsync(string lid, string? username, bool isAdmin);
        Task<ParkingSession?> GetSessionByIdAsync(string lid, string sid, string? username, bool isAdmin);
    }
}
