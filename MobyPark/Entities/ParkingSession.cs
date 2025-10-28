public class ParkingSession
{
    public Guid Id { get; set; }
    public string LicensePlate { get; set; }
    public DateTimeOffset Started { get; set; }
    public DateTimeOffset? Stopped { get; set; }
    public string User { get; set; }
    public int DurationMinutes { get; set; }
    public double Cost { get; set; }
    public string PaymentStatus { get; set; }
    public int ParkingLotId { get; set; }
}
