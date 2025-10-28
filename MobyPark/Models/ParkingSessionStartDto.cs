using System.ComponentModel.DataAnnotations;

namespace MobyPark.Dtos
{
    public class ParkingSessionStartDto
    {
        [Required]
        [RegularExpression(@"^[A-Z0-9\-]{4,10}$", ErrorMessage = "License plate must be 4–10 characters (A-Z, 0-9, - only).")]
        public string LicensePlate { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Parking lot ID must be a positive number.")]
        public int ParkingLotId { get; set; }
    }

}
