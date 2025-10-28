using System;

namespace MobyPark.Models
{
    public record VehicleReadDto(
        int Id,
        Guid UserId,
        string LicensePlate,
        string Make,
        string Model,
        string Color,
        int Year,
        DateTime CreatedAt
    );
}
