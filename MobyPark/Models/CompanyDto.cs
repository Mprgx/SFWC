using System.ComponentModel.DataAnnotations;

namespace MobyPark.Models
{
    public class CompanyResponseDto
    {
        public required Guid Id { get; set; }
        public required string CompanyName { get; set; }
        public required int Discount { get; set; }
        public required string Perks { get; set; }
        public required DateTimeOffset CreatedAt { get; set; }
        public required bool IsActive { get; set; }
    }

    public class CreateCompanyDto
    {
        [Required]
        [StringLength(100)]
        public required string CompanyName { get; set; }

        [Range(0, 100)]
        public int Discount { get; set; } = 0;

        public string Perks { get; set; } = "";
    }

    public class UpdateCompanyDto
    {
        [StringLength(100)]
        public string? CompanyName { get; set; }

        [Range(0, 100)]
        public int? Discount { get; set; }

        public string? Perks { get; set; }

        public bool? IsActive { get; set; }
    }
}