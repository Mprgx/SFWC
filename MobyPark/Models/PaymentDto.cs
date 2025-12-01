using System.ComponentModel.DataAnnotations;
using MobyPark.Models;
using MobyPark.Entities;

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
        public ParkingLot? ParkingLot { get; set; } = null;
    }

    public class PaymentsDto
    {
        [Required]
        public string Transaction { get; set; } = string.Empty;

        [Required]
        public decimal Amount { get; set; } = 0m;
    }
}
