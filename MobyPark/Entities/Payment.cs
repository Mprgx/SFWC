using System.ComponentModel.DataAnnotations;

namespace MobyPark.Entities
{
    public class Payment
    {
        [Key]
        public required string Transaction { get; set; }
        public required decimal Amount { get; set; }
        public required string Initiator { get; set; }

        public required Guid UserId { get; set; }
        public User? User { get; set; }

        public DateTimeOffset Created_At { get; set; } = DateTime.UtcNow;
        public DateTimeOffset? Completed { get; set; }
        public required string Hash { get; set; }
        public required string T_Data { get; set; }

        public Guid SessionId { get; set; }
        public Session? Session { get; set; }

        public int ParkingLotId { get; set; }
        public ParkingLot? ParkingLot { get; set; }
    }
}