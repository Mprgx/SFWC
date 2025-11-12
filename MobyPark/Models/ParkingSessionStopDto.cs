using System.ComponentModel.DataAnnotations;
namespace MobyPark.Models
{
    public class ParkingSessionStopDto
    {
        [Required]
        public string LicensePlate { get; set; } = string.Empty;
    }
}