using System.ComponentModel.DataAnnotations;

namespace MobyPark.Models
{
    public class RegisterRequestDto
    {
        [Required, MinLength(3), MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required, MinLength(8)]
        [RegularExpression(@"^(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
            ErrorMessage = "Password must be at least 8 characters and include a number and a special character.")]
        public string Password { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [EmailAddress, MaxLength(256)]
        public string? Email { get; set; }
        
        [Phone, MaxLength(30)]
        public string? PhoneNumber { get; set; }

        public int? BirthYear { get; set; }
    }
}
