namespace MobyPark.Models
{
    public class ReceiptDto
    {
        public string Transaction { get; set; } = default!;
        public decimal Amount { get; set; }     // Rounded to 2 decimals
        public string ParkingLotName { get; set; } = default!;
        public DateTimeOffset CompletedAt { get; set; }
    }
}
