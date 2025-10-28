using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MobyPark.Models
{
    public class VehicleRequestDto
    {
        // Auto-increment ID
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // Foreign key naar de gebruiker
        [Required]
        public int UserId { get; set; }

        // License plate van de auto
        [Required]
        [StringLength(20, MinimumLength = 2)]
        [RegularExpression(@"^[A-Z0-9\-]{2,10}$", 
            ErrorMessage = "License plate may only contain letters, numbers, and hyphens.")]
        public string LicensePlate { get; set; } = string.Empty;

        // Merk van de auto
        [Required, StringLength(50)]
        public string Make { get; set; } = string.Empty;

        // Model van de auto
        [Required, StringLength(50)]
        public string Model { get; set; } = string.Empty;

        // Kleur van de auto
        [Required, StringLength(30)]
        public string Color { get; set; } = string.Empty;

        // Bouwjaar
        [Required, Range(1900, 2100)]
        public int Year { get; set; }

        // Tijdstip van aanmaak
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
