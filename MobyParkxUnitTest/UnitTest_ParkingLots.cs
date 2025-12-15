//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.EntityFrameworkCore;
//using MobyPark.Data;
//using MobyPark.Entities;
//using MobyPark.Models;
//using MobyPark.Services;
//using Xunit;

//namespace MobyParkxUnitTest
//{
//    public class ParkingLotServiceTests
//    {
//        private static UserDbContext CreateDbContext(string name)
//        {
//            var options = new DbContextOptionsBuilder<UserDbContext>()
//                .UseInMemoryDatabase(name)
//                .Options;

//            return new UserDbContext(options);
//        }

//        private async Task SeedParkingLot(UserDbContext db, int id, string name = "TestLot")
//        {
//            db.ParkingLots.Add(new ParkingLot
//            {
//                Id = id,
//                Name = name,
//                Address = "TestStreet",
//                Location = "TestLocation",
//                Tariff = 2,
//                DayTariff = 10,
//                Capacity = 100,
//                ReservedSpots = 10,
//                Coordinates = "{}"
//            });

//            await db.SaveChangesAsync();
//        }

//        private async Task SeedSession(UserDbContext db, int lotId, Guid sessionId, string username)
//        {
//            var user = new User
//            {
//                Id = Guid.NewGuid(),
//                Username = username,
//                Name = "Test User",
//                Email = "test.test@testuser.com",
//                PhoneNumber = "0612345678",
//                BirthYear = 1990,
//                Role = UserRole.Customer,
//                PasswordHash = "hash",
//                CreatedAt = DateTimeOffset.UtcNow
//            };

//            var vehicle = new Vehicle
//            {
//                Id = 100,
//                UserId = user.Id,
//                LicensePlate = "TEST-111"
//            };

//            var session = new Session
//            {
//                Id = sessionId,
//                ParkingLotId = lotId,
//                UserId = user.Id,
//                VehicleId = vehicle.Id,
//            };

//            db.Users.Add(user);
//            db.Vehicles.Add(vehicle);
//            db.Sessions.Add(session);

//            await db.SaveChangesAsync();
//        }

//        // -------------------------------------------------------------
//        // POST - CREATE
//        // -------------------------------------------------------------
//        [Fact]
//        public async Task CreateParkingLotAsync_ShouldCreateParkingLot()
//        {
//            var db = CreateDbContext(nameof(CreateParkingLotAsync_ShouldCreateParkingLot));
//            var service = new ParkingLotService(db);

//            var newLot = new ParkingLot
//            {
//                Id = 9999,
//                Name = "NewLot",
//                Address = "Street",
//                Location = "City",
//                Capacity = 200,
//                Tariff = 3.5,
//                DayTariff = 15.0,
//                Coordinates = "{}"
//            };

//            var result = await service.CreateParkingLotAsync(newLot);

//            Assert.NotNull(result);
//            Assert.Equal("NewLot", result.Name);

//            Assert.Equal(1, db.ParkingLots.Count());
//        }

//        // -------------------------------------------------------------
//        // GET - ALL
//        // -------------------------------------------------------------
//        [Fact]
//        public async Task GetAllAsync_ShouldReturnAllParkingLots()
//        {
//            var db = CreateDbContext(nameof(GetAllAsync_ShouldReturnAllParkingLots));
//            var service = new ParkingLotService(db);

//            await SeedParkingLot(db, 1);
//            await SeedParkingLot(db, 2);

//            var result = await service.GetAllAsync();

//            Assert.Equal(2, result.Count);
//        }

//        // -------------------------------------------------------------
//        // GET - BY ID
//        // -------------------------------------------------------------
//        [Fact]
//        public async Task GetByIdAsync_ShouldReturnParkingLot_WhenExists()
//        {
//            var db = CreateDbContext(nameof(GetByIdAsync_ShouldReturnParkingLot_WhenExists));
//            var service = new ParkingLotService(db);

//            await SeedParkingLot(db, 10);

//            var result = await service.GetByIdAsync(10);

//            Assert.NotNull(result);
//            Assert.Equal(10, result.Id);
//        }

//        [Fact]
//        public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
//        {
//            var db = CreateDbContext(nameof(GetByIdAsync_ShouldReturnNull_WhenNotExists));
//            var service = new ParkingLotService(db);

//            var result = await service.GetByIdAsync(999);

//            Assert.Null(result);
//        }

//        // -------------------------------------------------------------
//        // GET - SESSIONS
//        // -------------------------------------------------------------
//        [Fact]
//        public async Task GetSessionsAsync_ShouldReturnAllSessions_ForAdmin()
//        {
//            var db = CreateDbContext(nameof(GetSessionsAsync_ShouldReturnAllSessions_ForAdmin));
//            var service = new ParkingLotService(db);

//            await SeedParkingLot(db, 1);
//            await SeedSession(db, 1, Guid.NewGuid(), "john");
//            await SeedSession(db, 1, Guid.NewGuid(), "anna");

//            var sessions = await service.GetSessionsAsync(1, null, true);

//            Assert.Equal(2, sessions.Count);
//        }

//        [Fact]
//        public async Task GetSessionsAsync_ShouldReturnUserSessions_WhenNotAdmin()
//        {
//            var db = CreateDbContext(nameof(GetSessionsAsync_ShouldReturnUserSessions_WhenNotAdmin));
//            var service = new ParkingLotService(db);

//            await SeedParkingLot(db, 1);

//            await SeedSession(db, 1, Guid.NewGuid(), "john");
//            await SeedSession(db, 1, Guid.NewGuid(), "anna");

//            var sessions = await service.GetSessionsAsync(1, "john", false);

//            Assert.Single(sessions);
//            Assert.Equal("john", sessions.First().User.Username);
//        }

//        [Fact]
//        public async Task GetSessionsAsync_ShouldReturnEmpty_WhenLotNotFound()
//        {
//            var db = CreateDbContext(nameof(GetSessionsAsync_ShouldReturnEmpty_WhenLotNotFound));
//            var service = new ParkingLotService(db);

//            var result = await service.GetSessionsAsync(999, "john", false);

//            Assert.Empty(result);
//        }

//        // -------------------------------------------------------------
//        // GET SESSION BY ID
//        // -------------------------------------------------------------
//        [Fact]
//        public async Task GetSessionByIdAsync_ShouldReturnSession_ForAdmin()
//        {
//            var db = CreateDbContext(nameof(GetSessionByIdAsync_ShouldReturnSession_ForAdmin));
//            var service = new ParkingLotService(db);

//            var sid = Guid.NewGuid();

//            await SeedParkingLot(db, 1);
//            await SeedSession(db, 1, sid, "john");

//            var result = await service.GetSessionByIdAsync(1, sid.ToString(), null, true);

//            Assert.NotNull(result);
//            Assert.Equal(sid, result.Id);
//        }

//        [Fact]
//        public async Task GetSessionByIdAsync_ShouldReturnNull_WhenNotOwner()
//        {
//            var db = CreateDbContext(nameof(GetSessionByIdAsync_ShouldReturnNull_WhenNotOwner));
//            var service = new ParkingLotService(db);

//            var sid = Guid.NewGuid();

//            await SeedParkingLot(db, 1);
//            await SeedSession(db, 1, sid, "john");

//            var result = await service.GetSessionByIdAsync(1, sid.ToString(), "anna", false);

//            Assert.Null(result);
//        }

//        // -------------------------------------------------------------
//        // UPDATE
//        // -------------------------------------------------------------
//        [Fact]
//        public async Task UpdateParkingLotAsync_ShouldUpdateLot_WhenExists()
//        {
//            var db = CreateDbContext(nameof(UpdateParkingLotAsync_ShouldUpdateLot_WhenExists));
//            var service = new ParkingLotService(db);

//            await SeedParkingLot(db, 1);

//            var dto = new ParkingLotUpdateDto
//            {
//                Name = "Updated",
//                Tariff = 5,
//                Capacity = 300
//            };

//            var result = await service.UpdateParkingLotAsync(1, dto);

//            Assert.NotNull(result);
//            Assert.Equal("Updated", result.Name);
//            Assert.Equal(5, result.Tariff);
//            Assert.Equal(300, result.Capacity);
//        }

//        [Fact]
//        public async Task UpdateParkingLotAsync_ShouldReturnNull_WhenNotExists()
//        {
//            var db = CreateDbContext(nameof(UpdateParkingLotAsync_ShouldReturnNull_WhenNotExists));
//            var service = new ParkingLotService(db);

//            var dto = new ParkingLotUpdateDto
//            {
//                Name = "Updated"
//            };

//            var result = await service.UpdateParkingLotAsync(999, dto);

//            Assert.Null(result);
//        }

//        // -------------------------------------------------------------
//        // DELETE LOT
//        // -------------------------------------------------------------
//        [Fact]
//        public async Task DeleteParkingLotAsync_ShouldDelete_WhenExists()
//        {
//            var db = CreateDbContext(nameof(DeleteParkingLotAsync_ShouldDelete_WhenExists));
//            var service = new ParkingLotService(db);

//            await SeedParkingLot(db, 1);

//            var deleted = await service.DeleteParkingLotAsync(1);

//            Assert.True(deleted);
//            Assert.Empty(db.ParkingLots);
//        }

//        [Fact]
//        public async Task DeleteParkingLotAsync_ShouldReturnFalse_WhenNotExists()
//        {
//            var db = CreateDbContext(nameof(DeleteParkingLotAsync_ShouldReturnFalse_WhenNotExists));
//            var service = new ParkingLotService(db);

//            var deleted = await service.DeleteParkingLotAsync(99);

//            Assert.False(deleted);
//        }

//        // -------------------------------------------------------------
//        // DELETE SESSION
//        // -------------------------------------------------------------
//        [Fact]
//        public async Task DeleteParkingLotSessionAsync_ShouldDeleteSession_WhenExists()
//        {
//            var db = CreateDbContext(nameof(DeleteParkingLotSessionAsync_ShouldDeleteSession_WhenExists));
//            var service = new ParkingLotService(db);

//            var sid = Guid.NewGuid();

//            await SeedParkingLot(db, 1);
//            await SeedSession(db, 1, sid, "john");

//            var ok = await service.DeleteParkingLotSessionAsync(1, sid);

//            Assert.True(ok);
//            Assert.Empty(db.Sessions);
//        }

//        [Fact]
//        public async Task DeleteParkingLotSessionAsync_ShouldReturnFalse_WhenNotExists()
//        {
//            var db = CreateDbContext(nameof(DeleteParkingLotSessionAsync_ShouldReturnFalse_WhenNotExists));
//            var service = new ParkingLotService(db);

//            var ok = await service.DeleteParkingLotSessionAsync(1, Guid.NewGuid());

//            Assert.False(ok);
//        }
//    }
//}
