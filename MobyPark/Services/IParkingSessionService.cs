using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public interface IParkingSessionService
    {
        Task<Session?> StartSessionAsync(Guid userId, ParkingSessionStartDto dto);
        Task<Session?> GetSessionByIdAsync(Guid userId, Guid sessionId);
        Task<Session?> StopSessionByIdAsync(Guid userId, bool isAdmin, Guid sessionId);
        Task<Session?> StopSessionByPlateAsync(string username, Guid userId, ParkingSessionStopDto dto);
        Task<Session?> CancelSessionAsync(Guid userId, bool isAdmin, Guid sessionId, CancelParkingSessionDto dto);
        Task<object?> RequestRefundAsync(Guid userId, bool isAdmin, Guid sessionId, RefundRequestDto? dto);
    }
}
