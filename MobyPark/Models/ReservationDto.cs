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
        public required string LicensePlate { get; set; }
        [Required]
        public DateTimeOffset StartTime { get; set; }
        [Required]
        public DateTimeOffset EndTime { get; set; }
        [Required]
        public bool IsActive { get; set; }
    }
}
