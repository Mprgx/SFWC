using System.ComponentModel.DataAnnotations;

namespace MobyPark.Entities
{
    public class Payment
    {
        [Key]
        [MaxLength(12)]
        public required string Transaction { get; set; }

        public decimal Amount { get; set; }

        [MaxLength(64)]
        public required string Initiator { get; set; }

        public Guid UserId { get; set; }
        public User? User { get; set; }

        public DateTimeOffset Created_At { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? Completed { get; set; }

        [MaxLength(64)]
        public required string Hash { get; set; }

        public string? T_Data { get; set; }

        public Guid? SessionId { get; set; }
        public Session? Session { get; set; }

        public int ParkingLotId { get; set; }
        public ParkingLot? ParkingLot { get; set; }
    }
}