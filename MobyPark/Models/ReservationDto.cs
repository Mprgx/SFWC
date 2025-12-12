using System.ComponentModel.DataAnnotations;

namespace MobyPark.Models
{
    public class PostReservationDto : IValidatableObject
    {
        [Required]
        public int ParkingLotId { get; set; }

        [Required, MaxLength(10)]
        [RegularExpression(@"^[A-Z0-9 -]{1,10}$", ErrorMessage = "Invalid license plate.")]
        public string LicensePlate { get; set; }

        [Required]
        public DateTimeOffset StartTime { get; set; }

        [Required]
        public DateTimeOffset EndTime { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext _)
        {

            // Check if end time is after start time.
            if (EndTime <= StartTime)
            {
                yield return new ValidationResult(
                    "EndTime must be after StartTime.",
                    new[] { nameof(EndTime) });
            }

            // Check if reservation is in the future.
            if (StartTime < DateTimeOffset.UtcNow)
                yield return new ValidationResult(
                    "StartTime can not be in the past.",
                    new[] { nameof(StartTime) });
        }
    }

    public class PutReservationDto : IValidatableObject
    {
        [MaxLength(10)]
        [RegularExpression(@"^[A-Z0-9 -]{1,10}$", ErrorMessage = "Invalid license plate.")]
        public string? LicensePlate { get; set; }
        public DateTimeOffset? StartTime { get; set; }
        public DateTimeOffset? EndTime { get; set; }
        public IEnumerable<ValidationResult> Validate(ValidationContext _)
        {
            // Check if end time is after start time.
            if (StartTime.HasValue && EndTime.HasValue)
            {
                if (EndTime <= StartTime)
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
        }
    }

    public class GetReservationDto
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public int ParkingLotId { get; set; }
        [Required]
        public Guid UserId { get; set; }
        [Required]
        public int VehicleId { get; set; }
        [Required]
        public string LicensePlate { get; set; }
        [Required]
        public DateTimeOffset StartTime { get; set; }
        [Required]
        public DateTimeOffset EndTime { get; set; }
        [Required]
        public bool IsActive { get; set; }
    }
}
