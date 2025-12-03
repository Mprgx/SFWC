namespace MobyPark.Models
{
    public class BillingReceiptDto
    {
        public Guid Id { get; set; }
        public string LicensePlate { get; set; } = default!;
        public int ParkingLotId { get; set; }
        public string ParkingLotName { get; set; } = default!;
        public DateTimeOffset Started { get; set; }
        public DateTimeOffset Stopped { get; set; }
        public int DurationMinutes { get; set; }
        public decimal Cost { get; set; }
        public string PaymentStatus { get; set; } = default!;
    }
}
