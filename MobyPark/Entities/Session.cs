using System.ComponentModel.DataAnnotations;

using Microsoft.EntityFrameworkCore;

namespace MobyPark.Entities
{
    public class Session
    {
        public Guid Id { get; set; }

        public int ParkingLotId { get; set; }
        public ParkingLot? ParkingLot { get; set; }

        public Guid UserId { get; set; }
        public User? User { get; set; }

        public int VehicleId { get; set; }
        public Vehicle? Vehicle { get; set; }

        [Required, MaxLength(20)]
        public string LicensePlate { get; set; } = string.Empty;

        public DateTimeOffset Started { get; set; }
        public DateTimeOffset? Stopped { get; set; }

        public int DurationMinutes { get; set; }

        [Precision(10, 2)]
        public decimal Cost { get; set; }

        [Required, MaxLength(20)]
        public string PaymentStatus { get; set; } = "unpaid";

        public bool IsCancelled { get; set; }
        public DateTimeOffset? CancelledAt { get; set; }

        public bool IsRefunded { get; set; }
        public DateTimeOffset? RefundDate { get; set; }
    }
}
