using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public interface ISessionService
    {
        Task<Session?> StartSessionAsync(Guid userId, SessionStartDto dto);
        Task<Session?> GetSessionByIdAsync(Guid userId, Guid sessionId);
        Task<Session?> StopSessionByPlateAsync(string username, Guid userId, SessionStopDto dto);
        Task<Session?> StopSessionByIdAsync(Guid userId, Guid sessionId);
        Task<Session?> CancelSessionAsync(Guid userId, Guid sessionId, CancelSessionDto dto);
        Task<object?> RequestRefundAsync(Guid userId, Guid sessionId, RefundRequestDto? dto);
        Task<List<Session>> GetAllForUserAsync(Guid userId, bool onlyActive);
    }
}
