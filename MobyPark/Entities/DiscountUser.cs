
using System.ComponentModel.DataAnnotations;

namespace MobyPark.Entities
{
    public class DiscountUser
    {
        [Required, MaxLength(32)]
        public string Code { get; set; } = string.Empty;
        public Discount? Discount { get; set; }

        public Guid UserId { get; set; }
        public User? User { get; set; }
    }
}