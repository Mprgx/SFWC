using System.ComponentModel.DataAnnotations;

namespace MobyPark.Entities
{
    public class ParkingLot
    {
        [Key]
        [Required]
        public int Id { get; set; } = default!;

        [Required]
        public string Name { get; set; } = default!;

        public string Location { get; set; } = default!;
        public string Address { get; set; } = default!;
        public int Capacity { get; set; }
        public int Reserved { get; set; }
        public double Tariff { get; set; }
        public double DayTariff { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string Coordinates { get; set; } = "{}";
        public ICollection<Session> ParkingSessions { get; set; } = new List<Session>();
    }
}
