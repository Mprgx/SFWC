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
    public class PostVehicleServiceTests
    {
        private static UserDbContext CreateDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new UserDbContext(options);
        }

        private async Task SeedVehicle(UserDbContext db, Guid userId, string plate)
        {
            db.Vehicles.Add(new Vehicle
            {
                Id = 9999,
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

        // POST 
        // POSITIEF - VEHICLE WORDT AANGEMAAKT
        [Fact]
        public async Task CreateVehicleAsync_ReturnsVehicle_WhenNewForUser()
        {
            // Arrange
            var db = CreateDbContext();
            var service = new VehicleService(db);

            var userId = Guid.NewGuid();
            var dto = new VehicleCreateDto
            {
                LicensePlate = "AA-123-AA",
                Make = "BMW",
                Model = "320i",
                Color = "Black",
                Year = 2021
            };

            // Act
            var result = await service.CreateVehicleAsync(userId, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.LicensePlate, result.LicensePlate);
            Assert.Equal(userId, result.UserId);

            var saved = await db.Vehicles.FirstAsync();
            Assert.Equal(dto.LicensePlate, saved.LicensePlate);
        }

        // POST 
        // DUPLICATE LICENSE PLATE (ZELFDE USER)
        [Fact]
        public async Task CreateVehicleAsync_ReturnsNull_WhenLicensePlateAlreadyExistsForUser()
        {
            // Arrange
            var db = CreateDbContext();
            var service = new VehicleService(db);

            var userId = Guid.NewGuid();
            await SeedVehicle(db, userId, "AA-123-AA");

            var dto = new VehicleRequestDto
            {
                LicensePlate = "AA-123-AA",
                Make = "BMW",
                Model = "320i",
                Color = "Black",
                Year = 2021
            };

            // Act
            var result = await service.CreateVehicleAsync(userId, dto);

            // Assert
            Assert.Null(result);
        }

        // POST
        // DUPLICATE LICENSE PLATE MAAR ANDERE USER → TOEGESTAAN
        [Fact]
        public async Task CreateVehicleAsync_CreatesVehicle_WhenPlateExistsButDifferentUser()
        {
            // Arrange
            var db = CreateDbContext();
            var service = new VehicleService(db);

            var user1 = Guid.NewGuid();
            var user2 = Guid.NewGuid();

            await SeedVehicle(db, user1, "AA-123-AA");

            var dto = new VehicleRequestDto
            {
                LicensePlate = "AA-123-AA",
                Make = "Audi",
                Model = "A3",
                Color = "Blue",
                Year = 2022
            };

            // Act
            var result = await service.CreateVehicleAsync(user2, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user2, result.UserId);
        }

        // DELETE
        // POSITIEF - VEHICLE WORDT VERWIJDERD
        [Fact]
        public async Task DeleteVehicleAsync_ShouldDeleteVehicle_WhenOwnedByUser()
        {
            // Arrange
            var context = CreateDbContext();
            var service = new VehicleService(context);

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

            // Act
            var result = await service.DeleteVehicleAsync(userId, 1);

            // Assert
            Assert.True(result);
            Assert.Empty(context.Vehicles);
        }

        // DELETE
        // NEGATIEF - VEHICLE BESTAAT NIET
        [Fact]
        public async Task DeleteVehicleAsync_ShouldReturnFalse_WhenVehicleDoesNotExist()
        {
            // Arrange
            var context = CreateDbContext();
            var service = new VehicleService(context);

            var userId = Guid.NewGuid();

            // Act
            var result = await service.DeleteVehicleAsync(userId, 999); // bestaat niet

            // Assert
            Assert.False(result);
        }

        // DELETE
        // NEGATIEF - VEHICLE BEHOORT NIET TOT DE GEBRUIKER
        [Fact]
        public async Task DeleteVehicleAsync_ShouldReturnFalse_WhenVehicleNotOwnedByUser()
        {
            // Arrange
            var context = CreateDbContext();
            var service = new VehicleService(context);

            var ownerId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid(); // de gebruiker die probeert te verwijderen

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

            // Act
            var result = await service.DeleteVehicleAsync(otherUserId, 1);

            // Assert
            Assert.False(result);
            Assert.Single(context.Vehicles); // nog steeds aanwezig
        }
    }
}
