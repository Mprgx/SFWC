//using System;
//using System.Threading.Tasks;
//using Microsoft.EntityFrameworkCore;
//using MobyPark.Data;
//using MobyPark.Entities;
//using MobyPark.Services;
//using MobyPark.Models;
//using Xunit;

//namespace MobyParkUnitTests
//{
//    public class PostVehicleServiceTests
//    {
//        private UserDbContext GetDb()
//        {
//            var options = new DbContextOptionsBuilder<UserDbContext>()
//                .UseInMemoryDatabase(Guid.NewGuid().ToString())
//                .Options;

//            return new UserDbContext(options);
//        }

//        private async Task SeedVehicle(UserDbContext db, Guid userId, string plate)
//        {
//            db.Vehicles.Add(new Vehicle
//            {
//                Id = 9999,
//                UserId = userId,
//                LicensePlate = plate,
//                Make = "Test",
//                Model = "TestModel",
//                Color = "Red",
//                Year = 2020,
//                CreatedAt = DateTimeOffset.UtcNow
//            });

//            await db.SaveChangesAsync();
//        }

//        // -------------------------
//        // POSITIEF - VEHICLE WORDT AANGEMAAKT
//        // -------------------------
//        [Fact]
//        public async Task CreateVehicleAsync_ReturnsVehicle_WhenNewForUser()
//        {
//            // Arrange
//            var db = GetDb();
//            var service = new VehicleService(db);

//            var userId = Guid.NewGuid();
//            var dto = new VehicleRequestDto
//            {
//                LicensePlate = "AA-123-AA",
//                Make = "BMW",
//                Model = "320i",
//                Color = "Black",
//                Year = 2021
//            };

//            // Act
//            var result = await service.CreateVehicleAsync(userId, dto);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal(dto.LicensePlate, result.LicensePlate);
//            Assert.Equal(userId, result.UserId);

//            var saved = await db.Vehicles.FirstAsync();
//            Assert.Equal(dto.LicensePlate, saved.LicensePlate);
//        }

//        // -------------------------
//        // DUPLICATE LICENSE PLATE (ZELFDE USER)
//        // -------------------------
//        [Fact]
//        public async Task CreateVehicleAsync_ReturnsNull_WhenLicensePlateAlreadyExistsForUser()
//        {
//            // Arrange
//            var db = GetDb();
//            var service = new VehicleService(db);

//            var userId = Guid.NewGuid();
//            await SeedVehicle(db, userId, "AA-123-AA");

//            var dto = new VehicleRequestDto
//            {
//                LicensePlate = "AA-123-AA",
//                Make = "BMW",
//                Model = "320i",
//                Color = "Black",
//                Year = 2021
//            };

//            // Act
//            var result = await service.CreateVehicleAsync(userId, dto);

//            // Assert
//            Assert.Null(result);
//        }

//        // -------------------------
//        // DUPLICATE LICENSE PLATE MAAR ANDERE USER → TOEGESTAAN
//        // -------------------------
//        [Fact]
//        public async Task CreateVehicleAsync_CreatesVehicle_WhenPlateExistsButDifferentUser()
//        {
//            // Arrange
//            var db = GetDb();
//            var service = new VehicleService(db);

//            var user1 = Guid.NewGuid();
//            var user2 = Guid.NewGuid();

//            await SeedVehicle(db, user1, "AA-123-AA");

//            var dto = new VehicleRequestDto
//            {
//                LicensePlate = "AA-123-AA",
//                Make = "Audi",
//                Model = "A3",
//                Color = "Blue",
//                Year = 2022
//            };

//            // Act
//            var result = await service.CreateVehicleAsync(user2, dto);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal(user2, result.UserId);
//        }
//    }
//}
