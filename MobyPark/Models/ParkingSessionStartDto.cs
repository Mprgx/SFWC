using System.ComponentModel.DataAnnotations;

namespace MobyPark.Models
{
    public class ParkingSessionStartDto
    {
        [Required, Range(1, int.MaxValue)]
        public int VehicleId { get; set; }
    }

}
