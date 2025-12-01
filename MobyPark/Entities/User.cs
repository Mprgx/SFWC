using System.ComponentModel.DataAnnotations;

namespace MobyPark.Entities
{
        public class User
        {
                public Guid Id { get; set; } = Guid.NewGuid();

                [Required, MaxLength(20)]
                public string Username { get; set; } = string.Empty;

                [Required, MaxLength(70)]
                public string Name { get; set; } = string.Empty;

                [Required, EmailAddress, MaxLength(256)]
                public string Email { get; set; } = string.Empty;

                [Required, MaxLength(30)]
                public string PhoneNumber { get; set; } = string.Empty;

                public DateTimeOffset CreatedAt { get; set; } = DateTime.UtcNow;
                public required int BirthYear { get; set; }
                public UserRole Role { get; set; } = UserRole.Customer;

                public Guid? CompanyId { get; set; }
                public Company? Company { get; set; }

                public required string PasswordHash { get; set; }
                public string? RefreshToken { get; set; }
                public DateTimeOffset? RefreshTokenExpiryTime { get; set; }

                public ICollection<Session> ParkingSessions { get; set; } = new List<Session>();
                public ICollection<UserVehicle> UserVehicles { get; set; } = new List<UserVehicle>();
                public ICollection<CompanyUser> CompanyUsers { get; set; } = new List<CompanyUser>();
                public ICollection<Payment> Payments { get; set; } = new List<Payment>();
                public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

        }
}
