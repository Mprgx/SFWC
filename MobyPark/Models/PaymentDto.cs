using System.ComponentModel.DataAnnotations;

namespace MobyPark.Models
{
    public class PaymentResponseDto
    {
        public string Transaction { get; set; } = "";
        public decimal Amount { get; set; }
        public string Initiator { get; set; }
        public UserReadDto User { get; set; }
        public DateTimeOffset Completed { get; set; }
        public string Hash { get; set; } = "";
    }

    public class PaymentsDto
    {
        [Required]
        public string Transaction { get; set; } = string.Empty;

        [Required]
        public decimal Amount { get; set; } = 0m;
    }
}
