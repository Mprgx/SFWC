using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace MobyPark.Entities
{
    public class Payment
    {
        public int Id { get; set; }

        [Key]
        public string Transaction { get; set; }

        public decimal Amount { get; set; }

        public Guid Initiator { get; set; }

        public DateTimeOffset Created_At { get; set; }

        public DateTimeOffset? Completed { get; set; }

        public string Hash { get; set; }

        public string T_Data { get; set; }

        public string SessionId { get; set; }

        public string ParkingLotId { get; set; }
    }
}
