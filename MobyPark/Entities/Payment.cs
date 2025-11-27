using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace MobyPark.Entities
{
    public class Payment
    {
        [Key]
        public string Transaction { get; set; }

        public decimal Amount { get; set; }

        public Guid Initiator { get; set; }

        public DateTimeOffset Created_At { get; set; }

        public DateTimeOffset? Completed { get; set; }

        public string Hash { get; set; }

        public string T_Data { get; set; }

        public string Session_Id { get; set; }

        public string Parking_Lot_Id { get; set; }
    }
}
