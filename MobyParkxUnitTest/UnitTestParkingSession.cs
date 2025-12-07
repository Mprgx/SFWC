using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;
using MobyPark.Services;
using Moq;
using Xunit;

namespace MobyParkxUnitTest
{
    public class ParkingLotServiceTests
    {
        private UserDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new UserDbContext(options);
        }

        private ParkingLotService CreateService(UserDbContext db)
        {
            return new ParkingLotService(db);
        }

        private ParkingLot CreateTestParkingLot(int id = 1)
        {
            return new ParkingLot
            {
                Id = id,
                Name = $"Lot{id}",
                Location = $"Location{id}",
                Address = $"Address{id}",
                Capacity = 100,
                Tariff = 2.0,
                DayTariff = 10.0,
                Coordinates = "1.0.0.-0"
            };
        }

        [Fact]
        public async Task CreateParkingLotAsync_Adds_New_Lot_To_Database()
        {
            var db = CreateDbContext();
            var service = CreateService(db);
            var newLot = CreateTestParkingLot(10);
            var result = await service.CreateParkingLotAsync(newLot);

            Assert.NotNull(result);
            var dbLot = await db.ParkingLots.FindAsync(result.Id);
            Assert.NotNull(dbLot);
            Assert.Equal("Lot10", dbLot!.Name);
        }

        [Fact]
        public async Task GetAllAsync_Returns_All_ParkingLots()
        {
            var db = CreateDbContext();
            db.ParkingLots.Add(CreateTestParkingLot(1));
            db.ParkingLots.Add(CreateTestParkingLot(2));
            await db.SaveChangesAsync();

            var service = CreateService(db);

            var result = await service.GetAllAsync();

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, p => p.Name == "Lot1");
            Assert.Contains(result, p => p.Name == "Lot2");
        }

        [Fact]
        public async Task GetAllAsync_Returns_Empty_List_When_None_Exist()
        {
            var service = CreateService(CreateDbContext());
            var result = await service.GetAllAsync();
            Assert.Empty(result);
        }

        [Fact]
        public async Task UpdateParkingLotAsync_Updates_Fields_Only_When_Provided()
        {
            // Arrange
            var db = CreateDbContext();
            var lot = CreateTestParkingLot(1);
            lot.Name = "Old Name";
            lot.Capacity = 50;
            lot.Address = "Old Address";

            db.ParkingLots.Add(lot);
            await db.SaveChangesAsync();

            var service = CreateService(db);

            var updateDto = new ParkingLotUpdateDto
            {
                Name = "New Name",
                Capacity = 200
            };
            var result = await service.UpdateParkingLotAsync(1, updateDto);

            Assert.NotNull(result);
            Assert.Equal("New Name", result!.Name);
            Assert.Equal(200, result.Capacity);
            Assert.Equal("Old Address", result.Address);

            var dbLot = await db.ParkingLots.FindAsync(1);
            Assert.Equal("New Name", dbLot!.Name);
        }

        [Fact]
        public async Task UpdateParkingLotAsync_Returns_Null_When_Lot_Not_Found()
        {
            var service = CreateService(CreateDbContext());
            var updateDto = new ParkingLotUpdateDto { Name = "New Name" };

            var result = await service.UpdateParkingLotAsync(999, updateDto);
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteParkingLotAsync_Removes_Lot_And_Returns_True()
        {
            var db = CreateDbContext();
            var lot = CreateTestParkingLot(1);
            db.ParkingLots.Add(lot);
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var result = await service.DeleteParkingLotAsync(1);

            Assert.True(result);
            var deletedLot = await db.ParkingLots.FindAsync(1);
            Assert.Null(deletedLot);
        }

        [Fact]
        public async Task DeleteParkingLotAsync_Returns_False_When_Lot_Not_Found()
        {
            var service = CreateService(CreateDbContext());
            var result = await service.DeleteParkingLotAsync(999);

            Assert.False(result);
        }

        [Fact]
        public async Task DeleteParkingLotSessionAsync_Removes_Session_And_Returns_True()
        {
            var db = CreateDbContext();
            var sessionId = Guid.NewGuid();
            var session = new Session
            {
                Id = sessionId,
                ParkingLotId = 1,
                UserId = Guid.NewGuid(),
                LicensePlate = "ABC-123"
            };

            db.Sessions.Add(session);
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var result = await service.DeleteParkingLotSessionAsync(1, sessionId);

            Assert.True(result);
            var deletedSession = await db.Sessions.FindAsync(sessionId);
            Assert.Null(deletedSession);
        }

        [Fact]
        public async Task DeleteParkingLotSessionAsync_Returns_False_If_Session_Or_Lot_Mismatch()
        {
            var db = CreateDbContext();
            var sessionId = Guid.NewGuid();
            var session = new Session
            {
                Id = sessionId,
                ParkingLotId = 1
            };

            db.Sessions.Add(session);
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var result = await service.DeleteParkingLotSessionAsync(2, sessionId);

            Assert.False(result);
            Assert.NotNull(await db.Sessions.FindAsync(sessionId));
        }
    }
}