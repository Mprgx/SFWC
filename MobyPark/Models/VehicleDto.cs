using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MobyPark.Models
{
    public class VehicleReadDto
    {
        public required int Id { get; set; }
        public required Guid UserId { get; set; }
        public required UserReadDto OwnerInformation { get; set; }
        public required string LicensePlate { get; set; }
        public required string Make { get; set; } = string.Empty;
        public required string Model { get; set; } = string.Empty;
        public required string Color { get; set; } = string.Empty;
        public required int Year { get; set; }
        public required DateTimeOffset CreatedAt { get; set; }
    }

    public class VehiclePostDto
    {
        [StringLength(20, MinimumLength = 2)]
        [RegularExpression(@"^[A-Z0-9\-]{2,10}$",
            ErrorMessage = "License plate may only contain letters, numbers, and hyphens.")]
        public required string LicensePlate { get; set; }

        [StringLength(50)]
        public required string Make { get; set; } = string.Empty;

        [StringLength(50)]
        public required string Model { get; set; } = string.Empty;

        [StringLength(30)]
        public required string Color { get; set; } = string.Empty;

        [Range(1900, 2100)]
        public required int Year { get; set; }

    }
}
