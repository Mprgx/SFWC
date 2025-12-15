//using Microsoft.EntityFrameworkCore;
//using MobyPark.Data;
//using MobyPark.Entities;
//using MobyPark.Services;

//namespace MobyParkxUnitTest
//{
//    public class DeleteVehicleServiceTests
//    {
//        private UserDbContext CreateDbContext()
//        {
//            var options = new DbContextOptionsBuilder<UserDbContext>()
//                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // uniek voor elke test
//                .Options;

//            return new UserDbContext(options);
//        }

//        [Fact]
//        public async Task DeleteVehicleAsync_ShouldDeleteVehicle_WhenOwnedByUser()
//        {
//            // Arrange
//            var context = CreateDbContext();
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

//        [Fact]
//        public async Task DeleteVehicleAsync_ShouldReturnFalse_WhenVehicleDoesNotExist()
//        {
//            // Arrange
//            var context = CreateDbContext();
//            var service = new VehicleService(context);

//            var userId = Guid.NewGuid();

//            // Act
//            var result = await service.DeleteVehicleAsync(userId, 999); // bestaat niet

//            // Assert
//            Assert.False(result);
//        }

//        [Fact]
//        public async Task DeleteVehicleAsync_ShouldReturnFalse_WhenVehicleNotOwnedByUser()
//        {
//            // Arrange
//            var context = CreateDbContext();
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
