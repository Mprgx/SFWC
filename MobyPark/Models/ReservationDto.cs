using System.ComponentModel.DataAnnotations;

namespace MobyPark.Models
{
    public class GetReservationDto
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public int ParkingLotId { get; set; }
        [Required]
        public Guid UserId { get; set; }
        [Required]
        public required UserReadDto ReservationCreator { get; set; }
        [Required]
        public required string LicensePlate { get; set; }
        [Required]
        public required VehicleReadDto Vehicle { get; set; }
        [Required]
        public DateTimeOffset StartTime { get; set; }
        [Required]
        public DateTimeOffset EndTime { get; set; }
        [Required]
        public bool IsActive { get; set; }
    }

    public class PostReservationDto : IValidatableObject
    {
        [Required]
        public int ParkingLotId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required, MaxLength(10)]
        [RegularExpression(@"^[A-Z0-9 -]{1,10}$", ErrorMessage = "Invalid license plate.")]
        public required string LicensePlate { get; set; }

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
}
