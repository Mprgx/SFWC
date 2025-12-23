
namespace MobyPark.Entities
{
    public class DiscountLocation
    {
        public string Code { get; set; }
        public Discount? Discount { get; set; }

        public int ParkingLotId { get; set; }
        public ParkingLot? ParkingLot { get; set; }
    }
}