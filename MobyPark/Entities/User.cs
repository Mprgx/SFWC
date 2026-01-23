using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MobyPark.Entities
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [MaxLength(50)]
        public string? LegacyId { get; set; }

        [Required, MaxLength(20)]
        public string Username { get; set; } = string.Empty;

        [Required, MaxLength(70)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string PhoneNumber { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; } = DateTime.UtcNow;

        public required int BirthYear { get; set; }

        public UserRole Role { get; set; } = UserRole.Customer;

        [MaxLength(200)]
        public string? PasswordHash { get; set; }

        [MaxLength(200)]
        public string? LegacyPasswordHash { get; set; }

        [MaxLength(20)]
        public string? LegacyPasswordAlgo { get; set; } // "MD5" for example

        public string? RefreshToken { get; set; }
        public DateTimeOffset? RefreshTokenExpiryTime { get; set; }

        [JsonIgnore] public ICollection<Session> ParkingSessions { get; set; } = new List<Session>();
        [JsonIgnore] public ICollection<UserVehicle> UserVehicles { get; set; } = new List<UserVehicle>();
        [JsonIgnore] public ICollection<CompanyUser> CompanyUsers { get; set; } = new List<CompanyUser>();
        [JsonIgnore] public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        [JsonIgnore] public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
        [JsonIgnore] public ICollection<Discount> Discounts { get; set; } = new List<Discount>();
    }
}
