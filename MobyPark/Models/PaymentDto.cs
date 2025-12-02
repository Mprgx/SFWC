using System.ComponentModel.DataAnnotations;

namespace MobyPark.Models
{
    public class PaymentResponseDto
    {
        public string Transaction { get; set; } = "";
        public decimal Amount { get; set; }
        public string Initiator { get; set; } = "";

        public Guid UserId { get; set; }
        public UserReadDto? User { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? Completed { get; set; }

        public string Hash { get; set; } = "";
        public string T_Data { get; set; } = "";

        public Guid SessionId { get; set; }
        public SessionReadDto? Session { get; set; }

        public int ParkingLotId { get; set; }
        public ParkingLotSummaryDto? ParkingLot { get; set; }
    }

    // Small summary DTO for ParkingLot, only scalar fields
    public class ParkingLotSummaryDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";
        public string Location { get; set; } = "";
        public string Address { get; set; } = "";

        public int Capacity { get; set; }
        public int ReservedSpots { get; set; }

        public double Tariff { get; set; }
        public double DayTariff { get; set; }
    }

    public class PaymentsDto
    {
        [Required]
        public string Transaction { get; set; } = string.Empty;

        [Required]
        public decimal Amount { get; set; } = 0m;
    }
}
