using System.ComponentModel.DataAnnotations;

using MobyPark.Models;

namespace MobyPark.Entities
{
    public class Discount
    {
        [Required, Key]
        public string Code { get; set; }

        [Required]
        public Guid CreatedBy { get; set; }
        public User? User { get; set; }

        [Required]
        public DateTimeOffset CreatedAt { get; set; }

        [Required]
        public DiscountType Type { get; set; }

        [Required]
        public decimal Value { get; set; }

        [Required]
        public DateTimeOffset ValidFrom { get; set; }

        [Required]
        public DateTimeOffset ValidUntil { get; set; }

        public bool Active { get; set; } = true;

        // Optional
        public TimeSpan? TimeWindowStart { get; set; } = null;
        public TimeSpan? TimeWindowEnd { get; set; } = null;
        public int? MaxUsage { get; set; } = null;
        public int? CurrentUsage { get; set; } = null;

        public ICollection<DiscountLocation> AllowedLocations { get; set; } = new List<DiscountLocation>();
        public ICollection<DiscountUser> ValidForUsers { get; set; } = new List<DiscountUser>();
        public ICollection<DiscountCompany> ValidForCompanies { get; set; } = new List<DiscountCompany>();

    }
}
