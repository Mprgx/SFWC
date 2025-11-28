using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MobyPark.Entities
{
    [Index(nameof(UserId), nameof(LicensePlate), IsUnique = true)]
    public class Vehicle
    {
        public int Id { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        [Required, MaxLength(20)]
        public string LicensePlate { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Make { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Model { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string Color { get; set; } = string.Empty;

        [Range(1900, 2100)]
        public int Year { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<UserVehicle> UserVehicles { get; set; } = new List<UserVehicle>();

    }
}
