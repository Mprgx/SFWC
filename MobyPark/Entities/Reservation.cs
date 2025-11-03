using System.ComponentModel.DataAnnotations;

namespace MobyPark.Entities
{
    public class Reservation
    {
        public int ReservationId { get; set; }
        public int ParkingLotId { get; set; }
        public Guid UserId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsActive { get; set; } = true;
    }
}