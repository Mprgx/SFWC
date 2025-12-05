namespace MobyPark.Models
{
    public class RefundResponseDto
    {
        public Guid SessionId { get; set; }
        public decimal Refunded { get; set; }
        public decimal Percentage { get; set; }
        public int DurationMinutes { get; set; }
        public decimal Cost { get; set; }
        public DateTimeOffset RefundDate { get; set; }
    }
}