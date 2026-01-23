public class VehicleHistoryDto
{
    public Guid SessionId { get; set; }
    public int ParkingLotId { get; set; }
    public string ParkingLotName { get; set; } = string.Empty;
    public DateTimeOffset Started { get; set; }
    public DateTimeOffset? Stopped { get; set; }
    public decimal Cost { get; set; }
}
