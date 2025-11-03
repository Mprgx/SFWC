using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MobyPark.Models
{
    public class CreateReservationDto : IValidatableObject
    {
        [Required]
        public int ParkingLotId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

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
            if (StartTime < DateTime.UtcNow)
                yield return new ValidationResult(
                    "StartTime can not be in the past.",
                    new[] { nameof(StartTime) });
        }
    }
}
