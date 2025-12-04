namespace MobyPark.Models
{
    public class VehicleCreateDto
    {
        public string LicensePlate { get; set; } = string.Empty;
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public int Year { get; set; }
    }

    public class VehicleReadDto
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }

        public string LicensePlate { get; set; } = string.Empty;
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public int Year { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
