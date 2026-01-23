using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MobyPark.Entities
{
    public class DuplicateUser
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [MaxLength(50)]
        public string? LegacyId { get; set; }

        [Required, MaxLength(40)]
        public string Username { get; set; } = string.Empty;

        [Required, MaxLength(70)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string PhoneNumber { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public int? BirthYear { get; set; }

        public UserRole Role { get; set; } = UserRole.Customer;

        [MaxLength(200)]
        public string? LegacyPasswordHash { get; set; }

        [MaxLength(20)]
        public string? LegacyPasswordAlgo { get; set; } = "MD5";

        [Required, MaxLength(50)]
        public string DuplicateReason { get; set; } = "Unknown";

        public DateTimeOffset ImportedAt { get; set; } = DateTimeOffset.UtcNow;

        [Column(TypeName = "nvarchar(max)")]
        public string? OriginalJson { get; set; }

        public Guid? ExistingUserId { get; set; }
        public User? ExistingUser { get; set; }
    }
}
