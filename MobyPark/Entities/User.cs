using System.ComponentModel.DataAnnotations;

namespace MobyPark.Entities
{
    public class User
    {
        public Guid Id { get; set; }

        [Required, MaxLength(20)]
        public string Username { get; set; } = string.Empty;

        [Required, MaxLength(70)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string PhoneNumber { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; }
        public int BirthYear { get; set; }
        public UserRole Role { get; set; } = UserRole.Customer;
        public string PasswordHash { get; set; } = string.Empty;

        public string? RefreshToken { get; set; }
        public DateTimeOffset? RefreshTokenExpiryTime { get; set; }

        public ICollection<ParkingSession> ParkingSessions { get; set; } = new List<ParkingSession>();
        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
