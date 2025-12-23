
namespace MobyPark.Entities
{
    public class DiscountCompany
    {
        public string Code { get; set; }
        public Discount? Discount { get; set; }

        public Guid CompanyId { get; set; }
        public Company? Company { get; set; }
    }
}