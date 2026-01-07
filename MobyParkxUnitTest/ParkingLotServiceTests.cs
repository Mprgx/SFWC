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
//using random = System.Random;

//namespace MobyParkxUnitTest
//{
//    public class ParkingLotServiceTests
//    {
//        private class FakeEncryptionService : IEncryptionService
//        {
//            public string? Encrypt(string? plaintext)
//            {
//                return plaintext is null ? null : $"ENC:{plaintext}";
//            }

//            public string? Decrypt(string? ciphertext)
//            {
//                if (ciphertext is null)
//                    return null;

//                const string prefix = "ENC:";
//                if (ciphertext.StartsWith(prefix, StringComparison.Ordinal))
//                    return ciphertext[prefix.Length..];

//                return ciphertext;
//            }
//        }

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
//                Id = new Random().Next(),
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
//            var service = new ParkingLotService(db, encryption: new FakeEncryptionService());

//            var newLot = new ParkingLotRequestDto
//            {
//                Name = "NewLot",
//                Address = "Street",
//                Location = "City",
//                Capacity = 200,
//                Tariff = 3.5,
//                DayTariff = 15.0,
//                Coordinates = new Dictionary<string, double>
//                {
//                    { "lat", 0 },
//                    { "lng", 0 }
//                }
//            };

//            var result = await service.CreateAsync(newLot, true);

//            Assert.NotNull(result);
//            Assert.Equal("NewLot", result.Item1.Name);

//            Assert.Equal(1, db.ParkingLots.Count());
//        }

//        // -------------------------------------------------------------
//        // GET - ALL
//        // -------------------------------------------------------------
//        [Fact]
//        public async Task GetAllAsync_ShouldReturnAllParkingLots()
//        {
//            var db = CreateDbContext(nameof(GetAllAsync_ShouldReturnAllParkingLots));
//            var service = new ParkingLotService(db, encryption: new FakeEncryptionService());

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
//            var service = new ParkingLotService(db, encryption: new FakeEncryptionService());

//            await SeedParkingLot(db, 10);

//            var result = await service.GetByIdAsync(10);

//            Assert.NotNull(result);
//            Assert.Equal(10, result.Id);
//        }

//        [Fact]
//        public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
//        {
//            var db = CreateDbContext(nameof(GetByIdAsync_ShouldReturnNull_WhenNotExists));
//            var service = new ParkingLotService(db, encryption: new FakeEncryptionService());

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
//            var service = new ParkingLotService(db, encryption: new FakeEncryptionService());

//            await SeedParkingLot(db, 1);
//            await SeedSession(db, 1, Guid.NewGuid(), "john");
//            await SeedSession(db, 1, Guid.NewGuid(), "anna");

//            var sessions = await service.GetSessionsAsync(1, null, true);

//            Assert.Equal(2, sessions.Item1?.Count ?? 0);
//        }

//        [Fact]
//        public async Task GetSessionsAsync_ShouldReturnUserSessions_WhenNotAdmin()
//        {
//            var db = CreateDbContext(nameof(GetSessionsAsync_ShouldReturnUserSessions_WhenNotAdmin));
//            var service = new ParkingLotService(db, encryption: new FakeEncryptionService());

//            await SeedParkingLot(db, 1);

//            await SeedSession(db, 1, Guid.NewGuid(), "john");
//            await SeedSession(db, 1, Guid.NewGuid(), "anna");

//            var sessions = await service.GetSessionsAsync(1, "john", false);

//            Assert.Single(sessions.Item1);
//        }

//        [Fact]
//        public async Task GetSessionsAsync_ShouldReturnEmpty_WhenLotNotFound()
//        {
//            var db = CreateDbContext(nameof(GetSessionsAsync_ShouldReturnEmpty_WhenLotNotFound));
//            var service = new ParkingLotService(db, encryption: new FakeEncryptionService());

//            var result = await service.GetSessionsAsync(999, "john", false);

//            Assert.Null(result.Item1);
//        }

//        // -------------------------------------------------------------
//        // GET SESSION BY ID
//        // -------------------------------------------------------------
//        [Fact]
//        public async Task GetSessionByIdAsync_ShouldReturnSession_ForAdmin()
//        {
//            var db = CreateDbContext(nameof(GetSessionByIdAsync_ShouldReturnSession_ForAdmin));
//            var service = new ParkingLotService(db, encryption: new FakeEncryptionService());

//            var sid = Guid.NewGuid();

//            await SeedParkingLot(db, 1);
//            await SeedSession(db, 1, sid, "john");

//            var result = await service.GetSessionByIdAsync(1, sid, null, true);

//            Assert.NotNull(result.Item1);
//            Assert.Equal(sid, result.Item1.Id);
//        }

//        [Fact]
//        public async Task GetSessionByIdAsync_ShouldReturnNull_WhenNotOwner()
//        {
//            var db = CreateDbContext(nameof(GetSessionByIdAsync_ShouldReturnNull_WhenNotOwner));
//            var service = new ParkingLotService(db, encryption: new FakeEncryptionService());

//            var sid = Guid.NewGuid();

//            await SeedParkingLot(db, 1);
//            await SeedSession(db, 1, sid, "john");

//            var result = await service.GetSessionByIdAsync(1, sid, "anna", false);

//            Assert.Null(result.Item1);
//        }

//        // -------------------------------------------------------------
//        // UPDATE
//        // -------------------------------------------------------------
//        [Fact]
//        public async Task UpdateParkingLotAsync_ShouldUpdateLot_WhenExists()
//        {
//            var db = CreateDbContext(nameof(UpdateParkingLotAsync_ShouldUpdateLot_WhenExists));
//            var service = new ParkingLotService(db, encryption: new FakeEncryptionService());

//            await SeedParkingLot(db, 1);

//            var dto = new ParkingLotUpdateDto
//            {
//                Name = "Updated",
//                Tariff = 5,
//                Capacity = 300
//            };

//            var result = await service.UpdateAsync(1, dto, true);

//            Assert.NotNull(result);
//            Assert.Equal("Updated", result.Item1.Name);
//            Assert.Equal(5, result.Item1.Tariff);
//            Assert.Equal(300, result.Item1.Capacity);
//        }

//        [Fact]
//        public async Task UpdateParkingLotAsync_ShouldReturnNull_WhenNotExists()
//        {
//            var db = CreateDbContext(nameof(UpdateParkingLotAsync_ShouldReturnNull_WhenNotExists));
//            var service = new ParkingLotService(db, encryption: new FakeEncryptionService());

//            var dto = new ParkingLotUpdateDto
//            {
//                Name = "Updated"
//            };

//            var result = await service.UpdateAsync(999, dto, true);

//            Assert.Null(result.Item1);
//            Assert.Equal(404, result.Item3);
//        }

//        // -------------------------------------------------------------
//        // DELETE LOT
//        // -------------------------------------------------------------
//        [Fact]
//        public async Task DeleteParkingLotAsync_ShouldDelete_WhenExists()
//        {
//            var db = CreateDbContext(nameof(DeleteParkingLotAsync_ShouldDelete_WhenExists));
//            var service = new ParkingLotService(db, encryption: new FakeEncryptionService());

//            await SeedParkingLot(db, 1);

//            var deleted = await service.DeleteAsync(1, true);

//            Assert.True(deleted.Item1);
//            Assert.Empty(db.ParkingLots);
//        }

//        [Fact]
//        public async Task DeleteParkingLotAsync_ShouldReturnFalse_WhenNotExists()
//        {
//            var db = CreateDbContext(nameof(DeleteParkingLotAsync_ShouldReturnFalse_WhenNotExists));
//            var service = new ParkingLotService(db, encryption: new FakeEncryptionService());

//            var deleted = await service.DeleteAsync(99, true);

//            Assert.False(deleted.Item1);
//        }

//        // -------------------------------------------------------------
//        // DELETE SESSION
//        // -------------------------------------------------------------
//        [Fact]
//        public async Task DeleteParkingLotSessionAsync_ShouldDeleteSession_WhenExists()
//        {
//            var db = CreateDbContext(nameof(DeleteParkingLotSessionAsync_ShouldDeleteSession_WhenExists));
//            var service = new ParkingLotService(db, encryption: new FakeEncryptionService());

//            var sid = Guid.NewGuid();

//            await SeedParkingLot(db, 1);
//            await SeedSession(db, 1, sid, "john");

//            var ok = await service.DeleteSessionAsync(1, sid, true);

//            Assert.True(ok.Item1);
//            Assert.Empty(db.Sessions);
//        }

//        [Fact]
//        public async Task DeleteParkingLotSessionAsync_ShouldReturnFalse_WhenNotExists()
//        {
//            var db = CreateDbContext(nameof(DeleteParkingLotSessionAsync_ShouldReturnFalse_WhenNotExists));
//            var service = new ParkingLotService(db, encryption: new FakeEncryptionService());

//            var ok = await service.DeleteSessionAsync(1, Guid.NewGuid(), true);

//            Assert.False(ok.Item1);
//        }
//    }
//}
