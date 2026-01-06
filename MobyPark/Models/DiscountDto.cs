using System.ComponentModel.DataAnnotations;

namespace MobyPark.Models
{
    public class ApplyDiscountDto
    {
        [Required]
        public string Transaction { get; set; } = string.Empty;

        [MaxLength(32)]
        [RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9_-]*$", ErrorMessage = "Invalid discount code format.")]
        public string? DiscountCode { get; set; }
    }

    public class DiscountApplyResultDto
    {
        public bool Applied { get; init; }
        public string Message { get; init; } = string.Empty;
        public string? Code { get; init; }
    }

    public class DiscountPostDto : IValidatableObject
    {
        [Required, MinLength(3), MaxLength(32)]
        [RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9_-]*$", ErrorMessage = "Invalid discount code format.")]
        public string Code { get; set; } = string.Empty;

        [Required]
        public DiscountType Type { get; set; }


        [Required, Range(0.01, double.MaxValue)]
        public decimal Value { get; set; }


        [Required]
        public DateTimeOffset ValidFrom { get; set; }

        [Required]
        public DateTimeOffset ValidUntil { get; set; }

        public TimeSpan? TimeWindowStart { get; set; }
        public TimeSpan? TimeWindowEnd { get; set; }
        public int? MaxUsage { get; set; }
        public List<int> AllowedLocations { get; set; } = [];
        public List<Guid> ValidForUsers { get; set; } = [];
        public List<Guid> ValidForCompanies { get; set; } = [];

        public IEnumerable<ValidationResult> Validate(ValidationContext _)
        {
            if (TimeWindowStart.HasValue != TimeWindowEnd.HasValue)
            {
                yield return new ValidationResult(
                    "TimeWindowStart and TimeWindowEnd must both be set, or both be null.",
                    new[] { nameof(TimeWindowStart), nameof(TimeWindowEnd) });
                yield break;
            }

            var day = TimeSpan.FromDays(1);

            if (TimeWindowStart.HasValue && (TimeWindowStart.Value < TimeSpan.Zero || TimeWindowStart.Value >= day))
            {
                yield return new ValidationResult(
                    "TimeWindowStart must be between 00:00 and 23:59:59.",
                    new[] { nameof(TimeWindowStart) });
            }

            if (TimeWindowEnd.HasValue && (TimeWindowEnd.Value < TimeSpan.Zero || TimeWindowEnd.Value >= day))
            {
                yield return new ValidationResult(
                    "TimeWindowEnd must be between 00:00 and 23:59:59.",
                    new[] { nameof(TimeWindowEnd) });
            }

            if (MaxUsage is not null && MaxUsage < 1)
            {
                yield return new ValidationResult(
                    "MaxUsage must be 1 or higher.",
                    new[] { nameof(MaxUsage) });
            }

            if (ValidFrom >= ValidUntil)
            {
                yield return new ValidationResult(
                    "ValidUntil must be after ValidFrom.",
                    new[] { nameof(ValidFrom), nameof(ValidUntil) });
            }
        }
    }

    public class DiscountReadDto
    {
        public string Code { get; set; } = string.Empty;

        public Guid CreatedBy { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DiscountType Type { get; set; }

        public decimal Value { get; set; }

        public DateTimeOffset ValidFrom { get; set; }
        public DateTimeOffset ValidUntil { get; set; }

        public TimeSpan? TimeWindowStart { get; set; }
        public TimeSpan? TimeWindowEnd { get; set; }

        public int? MaxUsage { get; set; }

        public List<int> AllowedLocations { get; set; } = [];

        public List<Guid> ValidForUsers { get; set; } = [];
        public List<Guid> ValidForCompanies { get; set; } = [];
    }

    public class DiscountCodeAnalyticsReadDto
    {
        // required by AC
        public string Code { get; set; } = string.Empty;          // uppercase
        public DiscountType Type { get; set; }
        public decimal Value { get; set; }

        public DateTimeOffset ValidFrom { get; set; }
        public DateTimeOffset ValidUntil { get; set; }

        public bool IsActive { get; set; }

        public int? MaxUsageCount { get; set; }
        public int? CurrentUsageCount { get; set; }

        // stats
        public int ReservationsUsedCount { get; set; }
        public decimal TotalSavedAmount { get; set; }             // rounded 2 decimals
    }

    public enum DiscountType
    {
        Percentage,
        FixedAmount
    }

    public enum DiscountCodeStatus
    {
        Active,     // default: Active == true AND ValidUntil >= now
        Inactive,   // Active == false
        Expired,    // ValidUntil < now
        All
    }

}
