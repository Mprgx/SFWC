using Microsoft.EntityFrameworkCore;
using MobyPark.Entities;
using System.ComponentModel.DataAnnotations;

namespace MobyPark.Entities
{
    [Index(nameof(UserId), nameof(Started))]
    public class ParkingSession
    {
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    [Required, MaxLength(20)]
    public string LicensePlate { get; set; } = string.Empty;

    public DateTimeOffset Started { get; set; }
    public DateTimeOffset? Stopped { get; set; }

    public int DurationMinutes { get; set; }

    [Precision(10, 2)]
    public decimal Cost { get; set; }

    [Required, MaxLength(20)]
    public string PaymentStatus { get; set; } = "unpaid";
    }
}
