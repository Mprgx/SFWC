using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;
using MobyPark.Services;
using Xunit;

namespace MobyParkxUnitTest
{
    public class VehicleHistoryServiceTests
    {
        private static UserDbContext CreateDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new UserDbContext(options);
        }

        private class FakeEncryptionService : IEncryptionService
        {
            public string? Encrypt(string? plaintext) => plaintext is null ? null : $"ENC:{plaintext}";

            public string? Decrypt(string? ciphertext)
            {
                if (ciphertext is null) return null;
                const string prefix = "ENC:";
                if (ciphertext.StartsWith(prefix, StringComparison.Ordinal))
                    return ciphertext[prefix.Length..];
                return ciphertext;
            }
        }

        private static VehicleService CreateService(UserDbContext context)
        {
            return new VehicleService(context, new FakeEncryptionService());
        }

        private static User CreateUser()
        {
            return new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Name = "Test User",
                Email = "",
                PhoneNumber = "",
                BirthYear = 2000,
                CreatedAt = DateTimeOffset.UtcNow,
                Role = UserRole.Customer,
                PasswordHash = "hash"
            };
        }

        private static Vehicle CreateVehicle(int id, Guid userId)
        {
            return new Vehicle
            {
                Id = id,
                UserId = userId,
                LicensePlate = "AA-12-BB",
                Make = "Tesla",
                Model = "Model 3",
                Color = "Black",
                Year = 2022
            };
        }

        private static ParkingLot CreateParkingLot(int id = 1)
        {
            return new ParkingLot
            {
                Id = id,
                Name = "Test Lot",
                Location = "Test Location",
                Address = "Test Address",
                Capacity = 50,
                Tariff = 2,
                DayTariff = 15,
                Coordinates = "{\"latitude\":0,\"longitude\":0}"
            };
        }

        private static Session CreateSession(Vehicle vehicle, User user, ParkingLot parkingLot, DateTimeOffset started)
        {
            return new Session
            {
                Id = Guid.NewGuid(),
                VehicleId = vehicle.Id,
                Vehicle = vehicle,
                UserId = user.Id,
                User = user,
                ParkingLotId = parkingLot.Id,
                ParkingLot = parkingLot,
                LicensePlate = vehicle.LicensePlate,
                Started = started,
                PaymentStatus = "unpaid",
                Cost = 10
            };
        }

        [Fact]
        public async Task GetVehicleHistoryAsync_ReturnsHistory_WhenVehicleExists()
        {
            using var context = CreateDbContext(nameof(GetVehicleHistoryAsync_ReturnsHistory_WhenVehicleExists));
            var service = CreateService(context);

            var user = CreateUser();
            var vehicle = CreateVehicle(1, user.Id);
            var parkingLot = CreateParkingLot();

            context.Users.Add(user);
            context.Vehicles.Add(vehicle);
            context.ParkingLots.Add(parkingLot);

            context.Sessions.AddRange(
                CreateSession(vehicle, user, parkingLot, DateTimeOffset.UtcNow.AddHours(-2)),
                CreateSession(vehicle, user, parkingLot, DateTimeOffset.UtcNow.AddHours(-1))
            );

            await context.SaveChangesAsync();

            var (dtos, error, status) = await service.GetVehicleHistoryAsync(user.Id, vehicle.Id);

            Assert.NotNull(dtos);
            Assert.Null(error);
            Assert.Null(status);
            Assert.Equal(2, dtos!.Count);
        }

        [Fact]
        public async Task GetVehicleHistoryAsync_ReturnsOnlySessionsForGivenVehicle()
        {
            using var context = CreateDbContext(nameof(GetVehicleHistoryAsync_ReturnsOnlySessionsForGivenVehicle));
            var service = CreateService(context);

            var user = CreateUser();
            var vehicle1 = CreateVehicle(1, user.Id);
            var vehicle2 = CreateVehicle(2, user.Id);
            var parkingLot = CreateParkingLot();

            context.Users.Add(user);
            context.Vehicles.AddRange(vehicle1, vehicle2);
            context.ParkingLots.Add(parkingLot);

            context.Sessions.AddRange(
                CreateSession(vehicle1, user, parkingLot, DateTimeOffset.UtcNow.AddHours(-2)),
                CreateSession(vehicle2, user, parkingLot, DateTimeOffset.UtcNow.AddHours(-1))
            );

            await context.SaveChangesAsync();

            var (dtos, error, status) = await service.GetVehicleHistoryAsync(user.Id, vehicle1.Id);

            Assert.NotNull(dtos);
            Assert.Null(error);
            Assert.Null(status);
            Assert.Single(dtos!);
            Assert.All(dtos!, d => Assert.Equal(vehicle1.Id, context.Sessions.Single(s => s.Id == d.SessionId).VehicleId));
        }

        [Fact]
        public async Task GetVehicleHistoryAsync_ReturnsSessionsOrderedByStartTime()
        {
            using var context = CreateDbContext(nameof(GetVehicleHistoryAsync_ReturnsSessionsOrderedByStartTime));
            var service = CreateService(context);

            var user = CreateUser();
            var vehicle = CreateVehicle(1, user.Id);
            var parkingLot = CreateParkingLot();

            context.Users.Add(user);
            context.Vehicles.Add(vehicle);
            context.ParkingLots.Add(parkingLot);

            var older = CreateSession(vehicle, user, parkingLot, DateTimeOffset.UtcNow.AddDays(-2));
            var newer = CreateSession(vehicle, user, parkingLot, DateTimeOffset.UtcNow.AddDays(-1));

            context.Sessions.AddRange(newer, older);
            await context.SaveChangesAsync();

            var (dtos, error, status) = await service.GetVehicleHistoryAsync(user.Id, vehicle.Id);

            Assert.NotNull(dtos);
            Assert.Null(error);
            Assert.Null(status);
            Assert.Equal(newer.Started, dtos![0].Started);
            Assert.Equal(older.Started, dtos[1].Started);
        }

        [Fact]
        public async Task GetVehicleHistoryAsync_ReturnsEmptyList_WhenVehicleHasNoSessions()
        {
            using var context = CreateDbContext(nameof(GetVehicleHistoryAsync_ReturnsEmptyList_WhenVehicleHasNoSessions));
            var service = CreateService(context);

            var user = CreateUser();
            var vehicle = CreateVehicle(1, user.Id);

            context.Users.Add(user);
            context.Vehicles.Add(vehicle);
            await context.SaveChangesAsync();

            var (dtos, error, status) = await service.GetVehicleHistoryAsync(user.Id, vehicle.Id);

            Assert.NotNull(dtos);
            Assert.Null(error);
            Assert.Null(status);
            Assert.Empty(dtos!);
        }

        [Fact]
        public async Task GetVehicleHistoryAdminAsync_Returns404_WhenVehicleDoesNotExist()
        {
            using var context = CreateDbContext(nameof(GetVehicleHistoryAdminAsync_Returns404_WhenVehicleDoesNotExist));
            var service = CreateService(context);

            var (dtos, error, status) = await service.GetVehicleHistoryAdminAsync(vehicleId: 999);

            Assert.Null(dtos);
            Assert.Equal("Vehicle not found.", error);
            Assert.Equal(404, status);
        }
    }
}
