//using System;
//using System.Threading.Tasks;
//using Microsoft.EntityFrameworkCore;
//using MobyPark.Data;
//using MobyPark.Entities;
//using MobyPark.Services;
//using Xunit;

//namespace MobyParkxUnitTest
//{
//    public class GetSessionServiceTests
//    {
//        private UserDbContext GetDb()
//        {
//            var options = new DbContextOptionsBuilder<UserDbContext>()
//                .UseInMemoryDatabase(Guid.NewGuid().ToString())
//                .Options;

//            return new UserDbContext(options);
//        }

//        private async Task SeedSession(UserDbContext db, Guid sessionId, Guid userId)
//        {
//            db.Sessions.Add(new Session
//            {
//                Id = sessionId,
//                UserId = userId,
//                ParkingLotId = 1234,
//                LicensePlate = "AA-11-AA",
//                Started = DateTimeOffset.UtcNow
//            });

//            await db.SaveChangesAsync();
//        }

//        // -------------------------
//        // POSITIEF - SESSION BESTAAT
//        // -------------------------
//        [Fact]
//        public async Task GetSessionByIdAsync_ReturnsSession_WhenSessionExistsForUser()
//        {
//            // Arrange
//            var db = GetDb();
//            var service = new SessionService(db);

//            var userId = Guid.NewGuid();
//            var sessionId = Guid.NewGuid();

//            await SeedSession(db, sessionId, userId);

//            // Act
//            var result = await service.GetSessionByIdAsync(userId, sessionId);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal(sessionId, result.Id);
//            Assert.Equal(userId, result.UserId);
//        }

//        // -------------------------
//        // SESSION BESTAAT NIET
//        // -------------------------
//        [Fact]
//        public async Task GetSessionByIdAsync_ReturnsNull_WhenSessionDoesNotExist()
//        {
//            // Arrange
//            var db = GetDb();
//            var service = new SessionService(db);

//            var userId = Guid.NewGuid();
//            var sessionId = Guid.NewGuid();

//            // Act
//            var result = await service.GetSessionByIdAsync(userId, sessionId);

//            // Assert
//            Assert.Null(result);
//        }

//        // -------------------------
//        // SESSION BESTAAT MAAR ANDERE USER
//        // -------------------------
//        [Fact]
//        public async Task GetSessionByIdAsync_ReturnsNull_WhenSessionBelongsToOtherUser()
//        {
//            // Arrange
//            var db = GetDb();
//            var service = new SessionService(db);

//            var correctUserId = Guid.NewGuid();
//            var otherUserId = Guid.NewGuid();
//            var sessionId = Guid.NewGuid();

//            await SeedSession(db, sessionId, correctUserId);

//            // Act
//            var result = await service.GetSessionByIdAsync(otherUserId, sessionId);

//            // Assert
//            Assert.Null(result);
//        }
//    }
//}

//namespace MobyParkUnitTests
//{
//    public class ParkingLotServiceGetSessionsTests
//    {
//        private UserDbContext GetDb()
//        {
//            var options = new DbContextOptionsBuilder<UserDbContext>()
//                .UseInMemoryDatabase(Guid.NewGuid().ToString())
//                .Options;

//            return new UserDbContext(options);
//        }

//        private async Task SeedLot(UserDbContext db, int id)
//        {
//            db.ParkingLots.Add(new ParkingLot { Id = id, Name = $"Lot{id}" });
//            await db.SaveChangesAsync();
//        }

//        private async Task SeedSession(UserDbContext db, int lotId, string username)
//        {
//            var user = new User { Id = Guid.NewGuid(), Username = username };
//            var vehicle = new Vehicle { Id = 9999, UserId = user.Id, LicensePlate = "AA-11-AA" };

//            var session = new Session
//            {
//                Id = Guid.NewGuid(),
//                ParkingLotId = lotId,
//                UserId = user.Id,
//                User = user,
//                Vehicle = vehicle,
//                Started = DateTimeOffset.UtcNow
//            };

//            db.Users.Add(user);
//            db.Vehicles.Add(vehicle);
//            db.Sessions.Add(session);

//            await db.SaveChangesAsync();
//        }

//        // -------------------------
//        // LOT BESTAAT NIET
//        // -------------------------
//        [Fact]
//        public async Task GetSessionsAsync_ReturnsEmptyList_WhenLotDoesNotExist()
//        {
//            // Arrange
//            var db = GetDb();
//            var service = new ParkingLotService(db);

//            // Act
//            var result = await service.GetSessionsAsync(999, null, false);

//            // Assert
//            Assert.Empty(result);
//        }

//        // -------------------------
//        // ADMIN → ALLE SESSIES
//        // -------------------------
//        [Fact]
//        public async Task GetSessionsAsync_ReturnsAll_WhenAdmin()
//        {
//            // Arrange
//            var db = GetDb();
//            var service = new ParkingLotService(db);

//            await SeedLot(db, 1);
//            await SeedSession(db, 1, "alice");
//            await SeedSession(db, 1, "bob");

//            // Act
//            var result = await service.GetSessionsAsync(1, null, true);

//            // Assert
//            Assert.Equal(2, result.Count);
//        }

//        // -------------------------
//        // NIET-ADMIN → ALLEEN EIGEN
//        // -------------------------
//        [Fact]
//        public async Task GetSessionsAsync_ReturnsOnlyOwnSessions_WhenNotAdmin()
//        {
//            // Arrange
//            var db = GetDb();
//            var service = new ParkingLotService(db);

//            await SeedLot(db, 1);
//            await SeedSession(db, 1, "alice");
//            await SeedSession(db, 1, "bob");

//            // Act
//            var result = await service.GetSessionsAsync(1, "bob", false);

//            // Assert
//            Assert.Single(result);
//            Assert.Equal("bob", result[0].User.Username);
//        }

//        // -------------------------
//        // NIET-ADMIN → ANDERE USER → LEGE LIJST
//        // -------------------------
//        [Fact]
//        public async Task GetSessionsAsync_ReturnsEmpty_WhenNotAdminAndWrongUser()
//        {
//            // Arrange
//            var db = GetDb();
//            var service = new ParkingLotService(db);

//            await SeedLot(db, 1);
//            await SeedSession(db, 1, "alice");

//            // Act
//            var result = await service.GetSessionsAsync(1, "bob", false);

//            // Assert
//            Assert.Empty(result);
//        }
//    }
//}


//namespace MobyParkUnitTests
//{
//    public class ParkingLotServiceGetSessionByIdTests
//    {
//        private UserDbContext GetDb()
//        {
//            var options = new DbContextOptionsBuilder<UserDbContext>()
//                .UseInMemoryDatabase(Guid.NewGuid().ToString())
//                .Options;

//            return new UserDbContext(options);
//        }

//        private async Task<Session> SeedSession(UserDbContext db, int lotId, string username)
//        {
//            var user = new User { Id = Guid.NewGuid(), Username = username };
//            var vehicle = new Vehicle { Id = 9999, UserId = user.Id, LicensePlate = "AA-11-AA" };

//            var session = new Session
//            {
//                Id = Guid.NewGuid(),
//                ParkingLotId = lotId,
//                UserId = user.Id,
//                User = user,
//                Vehicle = vehicle,
//                Started = DateTimeOffset.UtcNow
//            };

//            db.Users.Add(user);
//            db.Vehicles.Add(vehicle);
//            db.Sessions.Add(session);

//            await db.SaveChangesAsync();
//            return session;
//        }

//        // -------------------------
//        // NIET GEVONDEN
//        // -------------------------
//        [Fact]
//        public async Task GetSessionByIdAsync_ReturnsNull_WhenSessionNotFound()
//        {
//            // Arrange
//            var db = GetDb();
//            var service = new ParkingLotService(db);

//            // Act
//            var result = await service.GetSessionByIdAsync(1, Guid.NewGuid().ToString(), "alice", false);

//            // Assert
//            Assert.Null(result);
//        }

//        // -------------------------
//        // ADMIN → ALTIJD TOEGANG
//        // -------------------------
//        [Fact]
//        public async Task GetSessionByIdAsync_ReturnsSession_WhenAdmin()
//        {
//            // Arrange
//            var db = GetDb();
//            var service = new ParkingLotService(db);

//            var session = await SeedSession(db, 1, "alice");

//            // Act
//            var result = await service.GetSessionByIdAsync(1, session.Id.ToString(), null, true);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal(session.Id, result.Id);
//        }

//        // -------------------------
//        // EIGEN SESSIE
//        // -------------------------
//        [Fact]
//        public async Task GetSessionByIdAsync_ReturnsSession_WhenUserOwnsSession()
//        {
//            // Arrange
//            var db = GetDb();
//            var service = new ParkingLotService(db);

//            var session = await SeedSession(db, 1, "alice");

//            // Act
//            var result = await service.GetSessionByIdAsync(1, session.Id.ToString(), "alice", false);

//            // Assert
//            Assert.NotNull(result);
//        }

//        // -------------------------
//        // NIET-ADMIN & ANDERE USER
//        // -------------------------
//        [Fact]
//        public async Task GetSessionByIdAsync_ReturnsNull_WhenNotAdminAndNotOwner()
//        {
//            // Arrange
//            var db = GetDb();
//            var service = new ParkingLotService(db);

//            var session = await SeedSession(db, 1, "alice");

//            // Act
//            var result = await service.GetSessionByIdAsync(1, session.Id.ToString(), "bob", false);

//            // Assert
//            Assert.Null(result);
//        }
//    }
//}
