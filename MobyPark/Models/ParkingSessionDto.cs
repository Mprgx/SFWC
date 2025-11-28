using System.ComponentModel.DataAnnotations;

namespace MobyPark.Models
{
    public class ParkingSessionStartDto
    {
        [Required, Range(1, int.MaxValue)]
        public int VehicleId { get; set; }
        public int ParkingLotId { get; set; }
    }

    public class CancelParkingSessionDto
    {
        public string? Reason { get; set; }
    }

    public class ParkingSessionStopDto
    {
        [Required]
        public string LicensePlate { get; set; } = string.Empty;
    }

}
