using System.ComponentModel.DataAnnotations;

namespace MobyPark.Entities
{
    public class Reservation
    {
        public int ReservationId { get; set; }

        public int ParkingLotId { get; set; }
        public Guid UserId { get; set; }

        [Required]
        public string LicensePlate { get; set; } = string.Empty;

        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }

        public bool IsActive { get; set; } = true;

        public User? User { get; set; }
        public ParkingLot? ParkingLot { get; set; }
    }
}