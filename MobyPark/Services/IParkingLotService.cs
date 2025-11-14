using MobyPark.Entities;

namespace MobyPark.Services
{
    public interface IParkingLotService
    {
        Task<List<ParkingLot>> GetAllAsync();
        Task<ParkingLot?> GetByIdAsync(string lid);
        Task<List<Session>> GetSessionsAsync(string lid, string? username, bool isAdmin);
        Task<Session?> GetSessionByIdAsync(string lid, string sid, string? username, bool isAdmin);
    }
}
