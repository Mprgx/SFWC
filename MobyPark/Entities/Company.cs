using System.Text.Json.Serialization;

namespace MobyPark.Entities
{
    public class Company
    {
        // Identiteit
        public Guid Id { get; set; } = Guid.NewGuid();
        public required string CompanyName { get; set; }

        // Adresgegevens
        public required string Street { get; set; }
        public required string PostalCode { get; set; }
        public required string City { get; set; }
        public required string Country { get; set; }

        public required string ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? ContactPerson { get; set; }

        public string? KvKNumber { get; set; }
        public string? VatNumber { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public bool IsActive { get; set; } = true;

        [JsonIgnore]
        public ICollection<CompanyUser> CompanyUsers { get; set; } = new List<CompanyUser>();

        [JsonIgnore]
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

        [JsonIgnore]
        public ICollection<DiscountCompany> DiscountCompanies { get; set; } = new List<DiscountCompany>();
    }
}
