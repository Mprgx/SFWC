
using System.ComponentModel.DataAnnotations;

namespace MobyPark.Entities
{
    public class DiscountCompany
    {
        [Required, MaxLength(32)]
        public string Code { get; set; } = string.Empty;
        public Discount? Discount { get; set; }

        public Guid CompanyId { get; set; }
        public Company? Company { get; set; }
    }
}