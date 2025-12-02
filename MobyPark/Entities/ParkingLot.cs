using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MobyPark.Entities
{
        public class ParkingLot
        {
                [Key]
                public required int Id { get; set; } = default!;
                public required string Name { get; set; } = default!;
                public required string Location { get; set; } = default!;
                public required string Address { get; set; } = default!;
                public required int Capacity { get; set; }
                public int ReservedSpots { get; set; } = 0;
                public required double Tariff { get; set; }
                public required double DayTariff { get; set; }
                public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
                public required string Coordinates { get; set; } = "{}";

                [JsonIgnore] public ICollection<Session> ParkingSessions { get; set; } = new List<Session>();
                [JsonIgnore] public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

        }
}
