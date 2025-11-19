using System.ComponentModel.DataAnnotations;

namespace MobyPark.Models
{
    public class SessionStartDto
    {
        [Required, Range(1, int.MaxValue)]
        public int VehicleId { get; set; }
        public int ParkingLotId { get; set; }
    }

}
