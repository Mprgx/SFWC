using System.ComponentModel.DataAnnotations;

namespace MobyPark.Models
{
    public class PostReservationDto : IValidatableObject
    {
        [Required]
        public int ParkingLotId { get; set; }

        public int? VehicleId { get; set; }

        [MaxLength(10)]
        [RegularExpression(@"^[A-Z0-9 -]{1,10}$", ErrorMessage = "Invalid license plate.")]
        public string? LicensePlate { get; set; }

        [Required]
        public DateTimeOffset StartTime { get; set; }

        [Required]
        public DateTimeOffset EndTime { get; set; }

        [MaxLength(32)]
        [RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9_-]*$", ErrorMessage = "Invalid discount code format.")]
        public string? DiscountCode { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext _)
        {
            if (StartTime.Offset != TimeSpan.Zero)
                yield return new ValidationResult("StartTime must be UTC (offset +00:00).", new[] { nameof(StartTime) });

            if (EndTime.Offset != TimeSpan.Zero)
                yield return new ValidationResult("EndTime must be UTC (offset +00:00).", new[] { nameof(EndTime) });

            if (EndTime <= StartTime)
                yield return new ValidationResult("EndTime must be after StartTime.", new[] { nameof(EndTime) });

            if (StartTime < DateTimeOffset.UtcNow)
                yield return new ValidationResult("StartTime can not be in the past.", new[] { nameof(StartTime) });

            if (!VehicleId.HasValue && string.IsNullOrWhiteSpace(LicensePlate))
                yield return new ValidationResult("VehicleId or LicensePlate is required.", new[] { nameof(VehicleId), nameof(LicensePlate) });
        }
    }

    public class PutReservationDto : IValidatableObject
    {
        [MaxLength(10)]
        [RegularExpression(@"^[A-Z0-9 -]{1,10}$", ErrorMessage = "Invalid license plate.")]
        public string? LicensePlate { get; set; }

        public DateTimeOffset? StartTime { get; set; }
        public DateTimeOffset? EndTime { get; set; }

        [MaxLength(32)]
        [RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9_-]*$", ErrorMessage = "Invalid discount code format.")]
        public string? DiscountCode { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext _)
        {
            // Check if end time is after start time.
            if (StartTime.HasValue && EndTime.HasValue)
            {
                if (EndTime.Value <= StartTime.Value)
                {
                    yield return new ValidationResult(
                        "EndTime must be after StartTime.",
                        new[] { nameof(EndTime) });
                }
            }

            // Check if reservation is in the future.
            if (StartTime.HasValue && StartTime < DateTimeOffset.UtcNow)
                yield return new ValidationResult(
                    "StartTime can not be in the past.",
                    new[] { nameof(StartTime) });

            if (StartTime.HasValue && StartTime.Value.Offset != TimeSpan.Zero)
                yield return new ValidationResult("StartTime must be UTC (offset +00:00).", new[] { nameof(StartTime) });

            if (EndTime.HasValue && EndTime.Value.Offset != TimeSpan.Zero)
                yield return new ValidationResult("EndTime must be UTC (offset +00:00).", new[] { nameof(EndTime) });
        }
    }

    public class GetReservationDto
    {
        public int Id { get; set; }
        public int ParkingLotId { get; set; }
        public Guid UserId { get; set; }
        public int VehicleId { get; set; }
        public string? LicensePlate { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public bool IsActive { get; set; }
        public string? DiscountCode { get; set; }
        public decimal? EstimatedCost { get; set; }
        public decimal? EstimatedCostWithDiscount { get; set; }
    }
}
