namespace MobyPark.Models
{
    public record SessionReadDto(
        Guid Id,
        Guid UserId,
        int VehicleId,
        int ParkingLotId,
        string LicensePlate,
        DateTimeOffset Started,
        DateTimeOffset? Stopped,
        int DurationMinutes,
        decimal Cost,
        string PaymentStatus,
        bool IsCancelled,
        DateTimeOffset? CancelledAt,
        bool IsRefunded,
        DateTimeOffset? RefundDate
    );
}
