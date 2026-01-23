using System.ComponentModel.DataAnnotations;

namespace MobyPark.Entities
{
    public class Billing
    {
        [Key]
        public Guid Id { get; set; }

        public int ParkingLotId { get; set; }
        public ParkingLot? ParkingLot { get; set; }

        [Required, MaxLength(128)]
        public string LicensePlate { get; set; } = default!;

        public DateTimeOffset Started { get; set; }
        public DateTimeOffset Stopped { get; set; }

        public string Username { get; set; } = default!;

        public int DurationMinutes { get; set; }

        public decimal Cost { get; set; }

        public string PaymentStatus { get; set; } = default!;

        public Guid SessionId { get; set; }
        public Session? Session { get; set; }
    }
}
