using MobyPark.Entities;

namespace MobyPark.Services
{
    public interface IParkingLotService
    {
        Task<List<ParkingLot>> GetAllAsync();
        Task<ParkingLot?> GetByIdAsync(int lid);
        Task<List<Session>> GetSessionsAsync(int lid, string? username, bool isAdmin);
        Task<Session?> GetSessionByIdAsync(int lid, string sid, string? username, bool isAdmin);

        Task<Payment?> StopSessionAsync(string licensePlate, Guid userId, IEncryptionService encryption);
    }
}
