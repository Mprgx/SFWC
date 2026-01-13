using System.ComponentModel.DataAnnotations;

namespace MobyPark.Models
{
    public class CompanyResponseDto
    {
        public required Guid Id { get; set; }
        public required string CompanyName { get; set; }

        public required string Street { get; set; }
        public required string PostalCode { get; set; }
        public required string City { get; set; }
        public required string Country { get; set; }

        public required string ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? ContactPerson { get; set; }

        public required DateTimeOffset CreatedAt { get; set; }
        public required bool IsActive { get; set; }
    }

    public class CreateCompanyDto
    {
        [Required, StringLength(100)]
        public required string CompanyName { get; set; }

        [Required, StringLength(150)]
        public required string Street { get; set; }

        [Required, StringLength(20)]
        public required string PostalCode { get; set; }

        [Required, StringLength(100)]
        public required string City { get; set; }

        [Required, StringLength(100)]
        public required string Country { get; set; }

        [Required, EmailAddress]
        public required string ContactEmail { get; set; }

        [Phone]
        public string? ContactPhone { get; set; }

        [StringLength(100)]
        public string? ContactPerson { get; set; }
    }

    public class UpdateCompanyDto
    {
        public string? CompanyName { get; set; }

        public string? Street { get; set; }
        public string? PostalCode { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }

        private string? _contactEmail;
        [EmailAddress]
        public string? ContactEmail
        {
            get => _contactEmail;
            set => _contactEmail = string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private string? _contactPhone;
        [Phone]
        public string? ContactPhone
        {
            get => _contactPhone;
            set => _contactPhone = string.IsNullOrWhiteSpace(value) ? null : value;
        }

        public string? ContactPerson { get; set; }
        public bool? IsActive { get; set; }
    }
}
