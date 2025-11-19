using System.ComponentModel.DataAnnotations;

namespace MobyPark.Entities
{
    public class ParkingLot
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Location { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        public int Capacity { get; set; }
        public int Reserved { get; set; }

        public double Tariff { get; set; }
        public double DayTariff { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string Coordinates { get; set; } = "{}";

        public ICollection<Session> Sessions { get; set; } = new List<Session>();

        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
