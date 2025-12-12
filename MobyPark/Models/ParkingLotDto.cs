namespace MobyPark.Models
{
    public class ParkingLotRequestDto
    {
        public ParkingLotRequestDto(string name, string location, string address, int capacity, double tariff, double dayTariff, Dictionary<string, double> coordinates)
        {
            Name = name;
            Location = location;
            Address = address;
            Capacity = capacity;
            Tariff = tariff;
            DayTariff = dayTariff;
            Coordinates = coordinates;
        }

        public string Name { get; set; } = default!;
        public string Location { get; set; } = default!;
        public string Address { get; set; } = default!;
        public int Capacity { get; set; }
        public double Tariff { get; set; }
        public double DayTariff { get; set; }
        public Dictionary<string, double> Coordinates { get; set; }
            = new Dictionary<string, double>
            {
            { "latitude", 0 },
            { "longitude", 0 }
            };
    }

    public class ParkingLotUpdateDto
    {
        public string? Name { get; set; }
        public string? Location { get; set; }
        public string? Address { get; set; }
        public int? Capacity { get; set; }
        public double? Tariff { get; set; }
        public double? DayTariff { get; set; }
        public Dictionary<string, double>? Coordinates { get; set; }
    }

    public class ParkingLotReadDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string Location { get; set; } = default!;
        public string Address { get; set; } = default!;
        public int Capacity { get; set; }
        public int Reserved { get; set; }
        public double Tariff { get; set; }
        public double DayTariff { get; set; }
        public Dictionary<string, double> Coordinates { get; set; }
            = new Dictionary<string, double>
            {
            { "latitude", 0 },
            { "longitude", 0 }
            };
    }


}

