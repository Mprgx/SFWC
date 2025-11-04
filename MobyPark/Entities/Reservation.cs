using System.ComponentModel.DataAnnotations;

namespace MobyPark.Entities
{
    public class Reservation
    {
        public int ReservationId { get; set; }
        public int ParkingLotId { get; set; }
        public Guid UserId { get; set; }
        public required string LicensePlate { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public bool IsActive { get; set; } = true;
    }
}