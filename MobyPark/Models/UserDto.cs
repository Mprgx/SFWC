using System.ComponentModel.DataAnnotations;
using MobyPark.Entities;

namespace MobyPark.Models
{
    public class UserPostDto
    {
        public required string Username { get; set; }
        public required string Name { get; set; }

        [EmailAddress]
        public required string Email { get; set; }
        public required string PhoneNumber { get; set; }
        public required int BirthYear { get; set; }
    }

    public class UserGetDto
    {
        public required Guid Id { get; set; }
        public required string Username { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string PhoneNumber { get; set; }
        public required int BirthYear { get; set; }
        public required UserRole Role { get; set; }
        public required DateTimeOffset CreatedAt { get; set; }
    }
}
