using Microsoft.EntityFrameworkCore;
using MobyPark.Entities;
using System.ComponentModel.DataAnnotations;

public class ParkingSession
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public int ParkingLotId { get; set; }
    public ParkingLot? ParkingLot { get; set; }

    [Required, MaxLength(12)]
    public string LicensePlate { get; set; } = string.Empty;

    public DateTimeOffset Started { get; set; }
    public DateTimeOffset? Stopped { get; set; }

    public int DurationMinutes { get; set; }

    [Precision(10, 2)]
    public double Cost { get; set; }

    [Required, MaxLength(20)]
    public string PaymentStatus { get; set; } = "unpaid";
    
}
