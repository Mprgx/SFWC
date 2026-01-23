namespace MobyPark.Models
{
    public class DuplicateUserReadDto
    {
        public Guid Id { get; set; }

        public string? LegacyId { get; set; }

        public string Username { get; set; } = "";
        public string Name { get; set; } = "";

        public string Email { get; set; } = "";
        public string PhoneNumber { get; set; } = "";

        public int? BirthYear { get; set; }

        public string Role { get; set; } = "Customer";

        public DateTimeOffset CreatedAt { get; set; }

        public string DuplicateReason { get; set; } = "Unknown";
        public DateTimeOffset ImportedAt { get; set; }

        public Guid? ExistingUserId { get; set; }
    }

    public sealed class DuplicateUserResolveRequestDto
    {
        public string Username { get; set; } = "";
        public string Name { get; set; } = "";

        public string Email { get; set; } = "";
        public string PhoneNumber { get; set; } = "";

        public int? BirthYear { get; set; }

        public string Role { get; set; } = "Customer";

        public string? NewPassword { get; set; }
    }

    public sealed class DuplicateUserResolveResponseDto
    {
        public string Message { get; set; } = "";
        public Guid CreatedUserId { get; set; }
        public Guid DuplicateUserId { get; set; }

        public string? TempPassword { get; set; }
    }
}