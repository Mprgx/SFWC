using MobyPark.Models;

namespace MobyPark.Services
{
    public interface ISessionService
    {
        Task<(SessionReadDto? dto, string? error, int? status)> StartSessionAsync(Guid userId, SessionStartDto dto);
        Task<(StopSessionResponseDto? dto, string? error, int? status)> StopSessionByPlateAsync(Guid userId, SessionStopDto dto);
        Task<(StopSessionResponseDto? dto, string? error, int? status)> StopSessionByIdAsync(Guid userId, Guid sessionId);
        Task<(SessionReadDto? dto, string? error, int? status)> GetSessionByIdAsync(Guid userId, Guid sessionId);
        Task<(SessionReadDto? dto, string? error, int? status)> CancelSessionAsync(Guid userId, Guid sessionId, CancelSessionDto dto);
        Task<(List<SessionReadDto>? dto, string? error, int? status)> GetAllForUserAsync(Guid userId, bool onlyActive);
        Task<(bool dto, string? error, int? status)> DeleteSessionAsync(int parkingLotId, Guid sessionId);
        Task<(RefundResponseDto? dto, string? error, int? status)> RequestRefundAsync(Guid userId, Guid sessionId, RefundRequestDto? dto);
    }
}
