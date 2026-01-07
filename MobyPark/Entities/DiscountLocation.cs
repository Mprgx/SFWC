
using System.ComponentModel.DataAnnotations;

namespace MobyPark.Entities
{
    public class DiscountLocation
    {
        [Required, MaxLength(32)]
        public string Code { get; set; } = string.Empty;
        public Discount? Discount { get; set; }

        public int ParkingLotId { get; set; }
        public ParkingLot? ParkingLot { get; set; }
    }
}