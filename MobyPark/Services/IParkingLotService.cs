using MobyPark.Entities;

namespace MobyPark.Services
{
    public interface IParkingLotService
    {
        Task<List<ParkingLot>> GetAllAsync();
        Task<ParkingLot?> GetByIdAsync(int lid);
        Task<List<Session>> GetSessionsAsync(int lid, string? username, bool isAdmin);
        Task<Session?> GetSessionByIdAsync(int lid, string sid, string? username, bool isAdmin);

        Task<Payment?> StopSessionAsync(string licensePlate, string username, Guid userId, IEncryptionService encryption);
        Task<ParkingLot> CreateParkingLotAsync(ParkingLot parkingLot);
        Task<bool> DeleteParkingLotAsync(int id);
        Task<bool> DeleteParkingLotSessionAsync(int parkingLotId, Guid sessionId);
    }
}
