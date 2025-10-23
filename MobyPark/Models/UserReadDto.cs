using MobyPark.Entities;

namespace MobyPark.Models
{
    public record UserReadDto(Guid Id, string Username, string Name, string Email, string PhoneNumber, int BirthYear, UserRole Role, DateTimeOffset CreatedAt);
}
