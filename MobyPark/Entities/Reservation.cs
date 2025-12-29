using System.ComponentModel.DataAnnotations;

namespace MobyPark.Entities
{
    public class Reservation
    {
        public int Id { get; set; }

        public int ParkingLotId { get; set; }
        public ParkingLot? ParkingLot { get; set; }

        public Guid? CompanyId { get; set; }
        public Company? Company { get; set; }

        public Guid UserId { get; set; }
        public User? User { get; set; }

        public int VehicleId { get; set; }
        public Vehicle? Vehicle { get; set; }

        [Required, MaxLength(128)]
        public required string LicensePlate { get; set; }

        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }

        [MaxLength(32)]
        public string? DiscountCode { get; set; }
        public Discount? Discount { get; set; }

        public bool IsActive { get; set; } = true;


    }
}