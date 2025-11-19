namespace MobyPark.Models
{
    public class PaymentResponseDto
    {
        public string Transaction { get; set; } = "";
        public decimal Amount { get; set; }
        public Guid Initiator { get; set; }
        public bool Completed { get; set; }
        public string Hash { get; set; } = "";
    }
}
