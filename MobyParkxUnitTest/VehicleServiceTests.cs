using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Services;
using MobyPark.Models;
using Xunit;

namespace MobyParkxUnitTest
{
    public class VehicleServiceTests
    {
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

        private static UserDbContext CreateDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new UserDbContext(options);
        }

        private static async Task SeedVehicle(UserDbContext db, Guid userId, string plate, int id)
        {
            db.Vehicles.Add(new Vehicle
            {
                Id = id,
                UserId = userId,
                LicensePlate = plate,
                Make = "Test",
                Model = "TestModel",
                Color = "Red",
                Year = 2020,
                CreatedAt = DateTimeOffset.UtcNow
            });

            await db.SaveChangesAsync();
        }

        [Fact]
        public async Task CreateVehicleAsync_ReturnsVehicle_WhenNewForUser()
        {
            using var db = CreateDbContext(nameof(CreateVehicleAsync_ReturnsVehicle_WhenNewForUser));
            var service = new VehicleService(db, new FakeEncryptionService());

            var userId = Guid.NewGuid();
            var dto = new VehicleCreateDto
            {
                LicensePlate = "AA-123-AA",
                Make = "BMW",
                Model = "320i",
                Color = "Black",
                Year = 2021
            };

            var (created, error, status) = await service.CreateVehicleAsync(userId, dto);

            Assert.NotNull(created);
            Assert.Null(error);
            Assert.Null(status);
            Assert.Equal(userId, created!.UserId);
            Assert.Equal("ENC:AA-123-AA", created.LicensePlate);

            var saved = await db.Vehicles.FirstAsync();
            Assert.Equal("ENC:AA-123-AA", saved.LicensePlate);
        }

        [Fact]
        public async Task CreateVehicleAsync_Returns409_WhenLicensePlateAlreadyExistsForUser()
        {
            using var db = CreateDbContext(nameof(CreateVehicleAsync_Returns409_WhenLicensePlateAlreadyExistsForUser));
            var service = new VehicleService(db, new FakeEncryptionService());

            var userId = Guid.NewGuid();
            await SeedVehicle(db, userId, "AA-123-AA", 9999);

            var dto = new VehicleCreateDto
            {
                LicensePlate = "AA-123-AA",
                Make = "BMW",
                Model = "320i",
                Color = "Black",
                Year = 2021
            };

            var (created, error, status) = await service.CreateVehicleAsync(userId, dto);

            Assert.Null(created);
            Assert.Equal("License plate already exists.", error);
            Assert.Equal(409, status);
        }

        [Fact]
        public async Task CreateVehicleAsync_CreatesVehicle_WhenPlateExistsButDifferentUser()
        {
            using var db = CreateDbContext(nameof(CreateVehicleAsync_CreatesVehicle_WhenPlateExistsButDifferentUser));
            var service = new VehicleService(db, new FakeEncryptionService());

            var user1 = Guid.NewGuid();
            var user2 = Guid.NewGuid();

            await SeedVehicle(db, user1, "AA-123-AA", 9999);

            var dto = new VehicleCreateDto
            {
                LicensePlate = "AA-123-AA",
                Make = "Audi",
                Model = "A3",
                Color = "Blue",
                Year = 2022
            };

            var (created, error, status) = await service.CreateVehicleAsync(user2, dto);

            Assert.NotNull(created);
            Assert.Null(error);
            Assert.Null(status);
            Assert.Equal(user2, created!.UserId);

            var count = await db.Vehicles.CountAsync();
            Assert.Equal(2, count);
        }

        [Fact]
        public async Task GetVehiclesForUserAsync_ReturnsVehicles_WhenUserHasVehicles()
        {
            using var db = CreateDbContext(nameof(GetVehiclesForUserAsync_ReturnsVehicles_WhenUserHasVehicles));
            var service = new VehicleService(db, new FakeEncryptionService());

            var userId = Guid.NewGuid();
            await SeedVehicle(db, userId, "QQ-111-QQ", 9999);
            await SeedVehicle(db, userId, "YY-222-YY", 8888);

            var (dtos, error, status) = await service.GetVehiclesForUserAsync(userId);

            Assert.NotNull(dtos);
            Assert.Null(error);
            Assert.Null(status);
            Assert.Equal(2, dtos!.Count);
        }

        [Fact]
        public async Task GetVehiclesForUserAsync_ReturnsEmptyList_WhenUserHasNoVehicles()
        {
            using var db = CreateDbContext(nameof(GetVehiclesForUserAsync_ReturnsEmptyList_WhenUserHasNoVehicles));
            var service = new VehicleService(db, new FakeEncryptionService());

            var userId = Guid.NewGuid();

            var (dtos, error, status) = await service.GetVehiclesForUserAsync(userId);

            Assert.NotNull(dtos);
            Assert.Null(error);
            Assert.Null(status);
            Assert.Empty(dtos!);
        }

        [Fact]
        public async Task UpdateVehicleAsync_UpdatesVehicle_WhenOwnedByUser()
        {
            using var db = CreateDbContext(nameof(UpdateVehicleAsync_UpdatesVehicle_WhenOwnedByUser));
            var service = new VehicleService(db, new FakeEncryptionService());

            var userId = Guid.NewGuid();
            await SeedVehicle(db, userId, "AA-123-AA", 9999);

            var updateDto = new VehicleUpdateDto
            {
                Make = "UpdatedMake",
                Model = "UpdatedModel",
                Color = "Black",
                Year = 2024
            };

            var (updated, error, status) = await service.UpdateVehicleAsync(userId, 9999, updateDto);

            Assert.NotNull(updated);
            Assert.Null(error);
            Assert.Null(status);
            Assert.Equal("UpdatedMake", updated!.Make);
            Assert.Equal("UpdatedModel", updated.Model);
            Assert.Equal("Black", updated.Color);
            Assert.Equal(2024, updated.Year);
        }

        [Fact]
        public async Task UpdateVehicleAsync_Returns404_WhenVehicleNotOwned()
        {
            using var db = CreateDbContext(nameof(UpdateVehicleAsync_Returns404_WhenVehicleNotOwned));
            var service = new VehicleService(db, new FakeEncryptionService());

            var ownerId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();

            await SeedVehicle(db, ownerId, "AA-123-AA", 9999);

            var updateDto = new VehicleUpdateDto
            {
                Make = "NewMake"
            };

            var (updated, error, status) = await service.UpdateVehicleAsync(otherUserId, 9999, updateDto);

            Assert.Null(updated);
            Assert.Equal("Vehicle not found or not owned by the user.", error);
            Assert.Equal(404, status);
        }

        [Fact]
        public async Task DeleteVehicleAsync_DeletesVehicle_WhenOwnedByUser()
        {
            using var context = CreateDbContext(nameof(DeleteVehicleAsync_DeletesVehicle_WhenOwnedByUser));
            var service = new VehicleService(context, new FakeEncryptionService());

            var userId = Guid.NewGuid();
            var vehicle = new Vehicle
            {
                Id = 1,
                UserId = userId,
                LicensePlate = "ABC123",
                Make = "Test",
                Model = "Car",
                Color = "Blue",
                Year = 2020
            };

            context.Vehicles.Add(vehicle);
            await context.SaveChangesAsync();

            var (error, status) = await service.DeleteVehicleAsync(userId, 1);

            Assert.Null(error);
            Assert.Equal(204, status);
            Assert.Empty(context.Vehicles);
        }

        [Fact]
        public async Task DeleteVehicleAsync_Returns404_WhenVehicleDoesNotExist()
        {
            using var context = CreateDbContext(nameof(DeleteVehicleAsync_Returns404_WhenVehicleDoesNotExist));
            var service = new VehicleService(context, new FakeEncryptionService());

            var userId = Guid.NewGuid();

            var (error, status) = await service.DeleteVehicleAsync(userId, 999);

            Assert.Equal("Vehicle not found or not owned by the user.", error);
            Assert.Equal(404, status);
        }

        [Fact]
        public async Task DeleteVehicleAsync_Returns404_WhenVehicleNotOwnedByUser()
        {
            using var context = CreateDbContext(nameof(DeleteVehicleAsync_Returns404_WhenVehicleNotOwnedByUser));
            var service = new VehicleService(context, new FakeEncryptionService());

            var ownerId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();

            var vehicle = new Vehicle
            {
                Id = 1,
                UserId = ownerId,
                LicensePlate = "XYZ999",
                Make = "Brand",
                Model = "Model",
                Color = "Red",
                Year = 2021
            };

            context.Vehicles.Add(vehicle);
            await context.SaveChangesAsync();

            var (error, status) = await service.DeleteVehicleAsync(otherUserId, 1);

            Assert.Equal("Vehicle not found or not owned by the user.", error);
            Assert.Equal(404, status);
            Assert.Single(context.Vehicles);
        }
    }
}
