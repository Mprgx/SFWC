using System.ComponentModel.DataAnnotations;

using MobyPark.Entities;

namespace MobyPark.Models
{
    public class DiscountPostDto
    {
        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        public DiscountType Type { get; set; }

        [Required]
        public decimal Value { get; set; }

        [Required]
        public DateTimeOffset ValidFrom { get; set; }

        [Required]
        public DateTimeOffset ValidUntil { get; set; }

        public List<int>? allowedLocations { get; set; } = null;
        public TimeSpan? TimeWindowStart { get; set; } = null;
        public TimeSpan? TimeWindowEnd { get; set; } = null;
        public int? MaxUsage { get; set; } = null;
        public List<Guid>? ValidForUsers { get; set; } = null;
        public List<Guid>? ValidForCompanies { get; set; } = null;
    }

    public class DiscountReadDto
    {
        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        public Guid CreatedBy { get; set; }

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

        public List<int>? allowedLocations { get; set; } = null;
        public TimeSpan? TimeWindowStart { get; set; } = null;
        public TimeSpan? TimeWindowEnd { get; set; } = null;
        public int? MaxUsage { get; set; } = null;
        public List<Guid>? ValidForUsers { get; set; } = null;
        public List<Guid>? ValidForCompanies { get; set; } = null;
    }

    public enum DiscountType
    {
        Percentage,
        FixedAmount
    }

}
