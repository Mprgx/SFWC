using System.ComponentModel.DataAnnotations;

namespace MobyPark.Entities
{
    public class ParkingLot
    {
        [Key]
        [Required]
        public string Id { get; set; } = default!;

        [Required]
        public string Name { get; set; } = default!;

        public string Location { get; set; } = default!;
        public string Address { get; set; } = default!;
        public int Capacity { get; set; }
        public int Reserved { get; set; }
        public double Tariff { get; set; }
        public double DayTariff { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
