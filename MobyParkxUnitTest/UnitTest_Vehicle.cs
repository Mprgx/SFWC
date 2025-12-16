//using System;
//using System.Threading.Tasks;
//using Microsoft.EntityFrameworkCore;
//using MobyPark.Data;
//using MobyPark.Entities;
//using MobyPark.Services;
//using MobyPark.Models;
//using Xunit;

//namespace MobyParkxUnitTest
//{
//    public class VehicleServiceTests
//    {
//        private static UserDbContext CreateDbContext(string dbName)
//        {
//            var options = new DbContextOptionsBuilder<UserDbContext>()
//                .UseInMemoryDatabase(databaseName: dbName)
//                .Options;

//            return new UserDbContext(options);
//        }

//        private async Task SeedVehicle(UserDbContext db, Guid userId, string plate, int id)
//        {
//            db.Vehicles.Add(new Vehicle
//            {
//                Id = id,
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

//        // POST 
//        // POSITIEF - VEHICLE WORDT AANGEMAAKT
//        [Fact]
//        public async Task CreateVehicleAsync_ReturnsVehicle_WhenNewForUser()
//        {
//            // Arrange
//            var db = CreateDbContext(nameof(CreateVehicleAsync_ReturnsVehicle_WhenNewForUser));
//            var service = new VehicleService(db);

//            var userId = Guid.NewGuid();
//            var dto = new VehicleCreateDto
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

//        // POST 
//        // DUPLICATE LICENSE PLATE (ZELFDE USER)
//        [Fact]
//        public async Task CreateVehicleAsync_ReturnsNull_WhenLicensePlateAlreadyExistsForUser()
//        {
//            // Arrange
//            var db = CreateDbContext(nameof(CreateVehicleAsync_ReturnsNull_WhenLicensePlateAlreadyExistsForUser));
//            var service = new VehicleService(db);

//            var userId = Guid.NewGuid();
//            await SeedVehicle(db, userId, "AA-123-AA", 9999);

//            var dto = new VehicleCreateDto
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

//        // POST
//        // DUPLICATE LICENSE PLATE MAAR ANDERE USER → TOEGESTAAN
//        [Fact]
//        public async Task CreateVehicleAsync_CreatesVehicle_WhenPlateExistsButDifferentUser()
//        {
//            // Arrange
//            var db = CreateDbContext(nameof(CreateVehicleAsync_CreatesVehicle_WhenPlateExistsButDifferentUser));
//            var service = new VehicleService(db);

//            var user1 = Guid.NewGuid();
//            var user2 = Guid.NewGuid();

//            await SeedVehicle(db, user1, "AA-123-AA", 9999);

//            var dto = new VehicleCreateDto
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

//        // GET
//        // POSITIEF - ALLE VEHICLES VAN USER WORDEN OPGEHAALD
//        [Fact]
//        public async Task GetVehiclesForUserAsync_ReturnsVehicles_WhenUserHasVehicles()
//        {
//            // Arrange
//            var db = CreateDbContext(nameof(GetVehiclesForUserAsync_ReturnsVehicles_WhenUserHasVehicles));
//            var service = new VehicleService(db);

//            var userId = Guid.NewGuid();
//            await SeedVehicle(db, userId, "QQ-111-QQ", 9999);
//            await SeedVehicle(db, userId, "YY-222-YY", 8888);

//            // Act
//            var result = await service.GetVehiclesForUserAsync(userId);

//            // Assert
//            Assert.Equal(2, result.Count);
//            Assert.All(result, v => Assert.Equal(userId, v.UserId));
//        }

//        // GET
//        // NEGATIEF - USER HEEFT GEEN VEHICLES
//        [Fact]
//        public async Task GetVehiclesForUserAsync_ReturnsEmptyList_WhenUserHasNoVehicles()
//        {
//            // Arrange
//            var db = CreateDbContext(nameof(GetVehiclesForUserAsync_ReturnsEmptyList_WhenUserHasNoVehicles));
//            var service = new VehicleService(db);

//            var userId = Guid.NewGuid();

//            // Act
//            var result = await service.GetVehiclesForUserAsync(userId);

//            // Assert
//            Assert.Empty(result);
//        }

//        // PUT
//        // POSITIEF - VEHICLE WORDT GEUPDATE
//        [Fact]
//        public async Task UpdateVehicleAsync_UpdatesVehicle_WhenOwnedByUser()
//        {
//            // Arrange
//            var db = CreateDbContext(nameof(UpdateVehicleAsync_UpdatesVehicle_WhenOwnedByUser));
//            var service = new VehicleService(db);

//            var userId = Guid.NewGuid();
//            await SeedVehicle(db, userId, "AA-123-AA", 9999);

//            var updateDto = new VehicleUpdateDto
//            {
//                Make = "UpdatedMake",
//                Model = "UpdatedModel",
//                Color = "Black",
//                Year = 2024
//            };

//            // Act
//            var result = await service.UpdateVehicleAsync(userId, 9999, updateDto);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal("UpdatedMake", result.Make);
//            Assert.Equal("UpdatedModel", result.Model);
//            Assert.Equal("Black", result.Color);
//            Assert.Equal(2024, result.Year);
//        }

//        // PUT
//        // NEGATIEF - VEHICLE BESTAAT NIET OF NIET VAN USER
//        [Fact]
//        public async Task UpdateVehicleAsync_ReturnsNull_WhenVehicleNotOwned()
//        {
//            // Arrange
//            var db = CreateDbContext(nameof(UpdateVehicleAsync_ReturnsNull_WhenVehicleNotOwned));
//            var service = new VehicleService(db);

//            var ownerId = Guid.NewGuid();
//            var otherUserId = Guid.NewGuid();

//            await SeedVehicle(db, ownerId, "AA-123-AA", 9999);

//            var updateDto = new VehicleUpdateDto
//            {
//                Make = "NewMake"
//            };

//            // Act
//            var result = await service.UpdateVehicleAsync(otherUserId, 9999, updateDto);

//            // Assert
//            Assert.Null(result);
//        }

//        // DELETE
//        // POSITIEF - VEHICLE WORDT VERWIJDERD
//        [Fact]
//        public async Task DeleteVehicleAsync_ShouldDeleteVehicle_WhenOwnedByUser()
//        {
//            // Arrange
//            var context = CreateDbContext(nameof(DeleteVehicleAsync_ShouldDeleteVehicle_WhenOwnedByUser));
//            var service = new VehicleService(context);

//            var userId = Guid.NewGuid();
//            var vehicle = new Vehicle
//            {
//                Id = 1,
//                UserId = userId,
//                LicensePlate = "ABC123",
//                Make = "Test",
//                Model = "Car",
//                Color = "Blue",
//                Year = 2020
//            };

//            context.Vehicles.Add(vehicle);
//            await context.SaveChangesAsync();

//            // Act
//            var result = await service.DeleteVehicleAsync(userId, 1);

//            // Assert
//            Assert.True(result);
//            Assert.Empty(context.Vehicles);
//        }

//        // DELETE
//        // NEGATIEF - VEHICLE BESTAAT NIET
//        [Fact]
//        public async Task DeleteVehicleAsync_ShouldReturnFalse_WhenVehicleDoesNotExist()
//        {
//            // Arrange
//            var context = CreateDbContext(nameof(DeleteVehicleAsync_ShouldReturnFalse_WhenVehicleDoesNotExist));
//            var service = new VehicleService(context);

//            var userId = Guid.NewGuid();

//            // Act
//            var result = await service.DeleteVehicleAsync(userId, 999); // bestaat niet

//            // Assert
//            Assert.False(result);
//        }

//        // DELETE
//        // NEGATIEF - VEHICLE BEHOORT NIET TOT DE GEBRUIKER
//        [Fact]
//        public async Task DeleteVehicleAsync_ShouldReturnFalse_WhenVehicleNotOwnedByUser()
//        {
//            // Arrange
//            var context = CreateDbContext(nameof(DeleteVehicleAsync_ShouldReturnFalse_WhenVehicleNotOwnedByUser));
//            var service = new VehicleService(context);

//            var ownerId = Guid.NewGuid();
//            var otherUserId = Guid.NewGuid(); // de gebruiker die probeert te verwijderen

//            var vehicle = new Vehicle
//            {
//                Id = 1,
//                UserId = ownerId,
//                LicensePlate = "XYZ999",
//                Make = "Brand",
//                Model = "Model",
//                Color = "Red",
//                Year = 2021
//            };

//            context.Vehicles.Add(vehicle);
//            await context.SaveChangesAsync();

//            // Act
//            var result = await service.DeleteVehicleAsync(otherUserId, 1);

//            // Assert
//            Assert.False(result);
//            Assert.Single(context.Vehicles); // nog steeds aanwezig
//        }
//    }
//}
