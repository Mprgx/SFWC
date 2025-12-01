using System.ComponentModel.DataAnnotations;

namespace MobyPark.Models
{
    public class SessionStartDto
    {
        [Required, Range(1, int.MaxValue)]
        public int VehicleId { get; set; }
        public int ParkingLotId { get; set; }
    }

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

    public class SessionStopDto
    {
        [Required]
        public string LicensePlate { get; set; } = string.Empty;
    }

    public class CancelSessionDto
    {
        public string? Reason { get; set; }

    }

}
