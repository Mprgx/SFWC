using System.ComponentModel.DataAnnotations;

namespace MobyPark.Models
{
    public class UpdateProfileDto
    {
        [StringLength(60)]
        public string? Name { get; set; }

        [RegularExpression(@"^$|^[^@\s]+@[^@\s]+\.[^@\s]+$",
            ErrorMessage = "Email is not valid.")]
        public string? Email { get; set; }

        [RegularExpression(@"^$|^\+?[0-9\s\-\(\)\.]{6,}$",
            ErrorMessage = "Phone number is not valid.")]
        public string? PhoneNumber { get; set; }

        [Range(0, 2030)]
        public int? BirthYear { get; set; }

        [RegularExpression(@"^$|^[a-zA-Z0-9_.-]{3,32}$")]
        public string? Username { get; set; }
    }

    public class UpdatePasswordRequestDto
    {
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
    }
}
