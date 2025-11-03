using System.ComponentModel.DataAnnotations;

namespace MobyPark.Models
{
    public class ReservationDto
    {
        [Required]
        public int ReservationId { get; set; }
        [Required]
        public int ParkingLotId { get; set; }
        [Required]
        public Guid UserId { get; set; }
        [Required]
        public DateTime StartTime { get; set; }
        [Required]
        public DateTime EndTime { get; set; }
        [Required]
        public bool IsActive { get; set; }
    }
}
