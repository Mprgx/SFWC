using System.ComponentModel.DataAnnotations;

namespace MobyPark.Models
{
    public class RegisterRequestDto
    {
        [Required, MinLength(3), MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, Phone, MaxLength(30)]
        public string PhoneNumber { get; set; }

        [Required, Range(1900, 2100)]
        public int BirthYear { get; set; }
    }
}
