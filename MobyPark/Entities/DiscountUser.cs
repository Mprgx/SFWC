
namespace MobyPark.Entities
{
    public class DiscountUser
    {
        public string Code { get; set; }
        public Discount? Discount { get; set; }

        public Guid UserId { get; set; }
        public User? User { get; set; }
    }
}