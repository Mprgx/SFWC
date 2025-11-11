namespace MobyPark.Models
{
    public record ParkingLotReadDto(
        string Id,
        string Name,
        string Location,
        string Address,
        int Capacity,
        int Reserved,
        double Tariff,
        double DayTariff,
        DateTime CreatedAt,
        double Latitude,
        double Longitude
    );
}
