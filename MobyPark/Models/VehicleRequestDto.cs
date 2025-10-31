using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MobyPark.Models
{
    public class VehicleRequestDto
    {
        [Required]
        [StringLength(20, MinimumLength = 2)]
        [RegularExpression(@"^[A-Z0-9\-]{2,10}$", 
            ErrorMessage = "License plate may only contain letters, numbers, and hyphens.")]
        public string LicensePlate { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Make { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Model { get; set; } = string.Empty;

        [Required, StringLength(30)]
        public string Color { get; set; } = string.Empty;

        [Required, Range(1900, 2100)]
        public int Year { get; set; }
    }
}
