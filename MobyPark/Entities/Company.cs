using System.Text.Json.Serialization;

namespace MobyPark.Entities
{
    public class Company
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public required string CompanyName { get; set; }
        public int Discount { get; set; }
        public string Perks { get; set; } = "";
        public DateTimeOffset CreatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        [JsonIgnore] public ICollection<CompanyUser> CompanyUsers { get; set; } = new List<CompanyUser>();
        [JsonIgnore] public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();


    }
}