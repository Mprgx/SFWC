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

        public required string LicensePlate { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public int SpotsReserved { get; set; } = 1;
        public bool IsActive { get; set; } = true;

    }
}