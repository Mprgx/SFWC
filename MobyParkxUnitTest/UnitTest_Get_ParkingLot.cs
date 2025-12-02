/*using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Services;
using Xunit;

namespace MobyParkxUnitTest
{
    public class GetParkingLotServiceTests
    {
        private UserDbContext GetDb()
        {
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new UserDbContext(options);
        }

        private async Task SeedLot(UserDbContext db, int id, string name)
        {
            db.ParkingLots.Add(new ParkingLot { Id = id, Name = name });
            await db.SaveChangesAsync();
        }

        // -------------------------
        // GET ALL
        // -------------------------
        [Fact]
        public async Task GetAllAsync_ReturnsAllLots()
        {
            // Arrange
            var db = GetDb();
            var service = new ParkingLotService(db);

            await SeedLot(db, 1, "Lot A");
            await SeedLot(db, 2, "Lot B");

            // Act
            var result = await service.GetAllAsync();

            // Assert
            Assert.Equal(2, result.Count);
        }

        // -------------------------
        // GET BY ID FOUND
        // -------------------------
        [Fact]
        public async Task GetByIdAsync_ReturnsLot_WhenExists()
        {
            // Arrange
            var db = GetDb();
            var service = new ParkingLotService(db);

            await SeedLot(db, 10, "TestLot");

            // Act
            var result = await service.GetByIdAsync(10);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("TestLot", result.Name);
        }

        // -------------------------
        // GET BY ID NOT FOUND
        // -------------------------
        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
        {
            // Arrange
            var db = GetDb();
            var service = new ParkingLotService(db);

            // Act
            var result = await service.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }
    }
}
*/