using System.ComponentModel.DataAnnotations;

namespace MobyPark.Models
{
    public class SessionStartDto
    {
        [Required, Range(1, int.MaxValue)]
        public int VehicleId { get; set; }
        [Required, Range(1, int.MaxValue)]
        public int ParkingLotId { get; set; }
    }

    public class SessionStopDto
    {
        [Required]
        [RegularExpression(@"^(?:[A-Z]{2}-\d{2}-\d{2}|\d{2}-\d{2}-[A-Z]{2}|\d{2}-[A-Z]{2}-\d{2}|[A-Z]{2}-\d{2}-[A-Z]{2}|[A-Z]{2}-[A-Z]{2}-\d{2}|\d{2}-[A-Z]{2}-[A-Z]{2})$",
        ErrorMessage = "Invalid Dutch license plate format.")]
        public string LicensePlate { get; set; } = string.Empty;

    }

    public class StopSessionResponseDto
    {
        public SessionReadDto Session { get; init; } = default!;
        public PaymentInitiationDto Payment { get; init; } = default!;
    }

    public class CancelSessionDto
    {
        [StringLength(300)]
        public string? Reason { get; set; }

    }

    public class SessionReadDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public int VehicleId { get; set; }
        public int ParkingLotId { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public DateTimeOffset Started { get; set; }
        public DateTimeOffset? Stopped { get; set; }
        public int DurationMinutes { get; set; }
        public decimal Cost { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public bool IsCancelled { get; set; }
        public DateTimeOffset? CancelledAt { get; set; }
        public DateTimeOffset? RefundDate { get; set; }
        public bool IsRefunded { get; set; }
    }
}
