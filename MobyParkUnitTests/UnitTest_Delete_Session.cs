using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Services;
using Xunit;

namespace MobyParkUnitTests
{
    public class DeleteSessionServiceTests
    {
        private UserDbContext GetDb()
        {
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new UserDbContext(options);
        }

        private async Task SeedSession(UserDbContext db, int lotId, Guid sessionId, Guid userId)
        {
            db.Sessions.Add(new Session
            {
                Id = sessionId,
                ParkingLotId = lotId,
                UserId = userId,
                LicensePlate = "AA-11-AA",
                Started = DateTimeOffset.UtcNow
            });

            await db.SaveChangesAsync();
        }

        // -------------------------
        // POSITIVE CASE
        // -------------------------
        [Fact]
        public async Task DeleteSession_ReturnsTrue_WhenSessionExists()
        {
            // Arrange
            var db = GetDb();
            var service = new SessionService(db);

            var lotId = 9999;
            var sessionId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            await SeedSession(db, lotId, sessionId, userId);

            // Act
            var result = await service.DeleteSessionAsync(lotId, sessionId);

            // Assert
            Assert.True(result);

            var removed = await db.Sessions.FindAsync(sessionId);
            Assert.Null(removed);
        }

        // -------------------------
        // SESSION DOES NOT EXIST
        // -------------------------
        [Fact]
        public async Task DeleteSession_ReturnsFalse_WhenSessionDoesNotExist()
        {
            // Arrange
            var db = GetDb();
            var service = new SessionService(db);

            var lotId = 9999;
            var sessionId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            // Act
            var result = await service.DeleteSessionAsync(lotId, sessionId);

            // Assert
            Assert.False(result);
        }

        // -------------------------
        // SESSION EXISTS BUT ANOTHER LOT
        // -------------------------
        [Fact]
        public async Task DeleteSession_ReturnsFalse_WhenSessionBelongsToOtherParkingLot()
        {
            // Arrange
            var db = GetDb();
            var service = new SessionService(db);

            var correctLot = 9999;
            var wrongLot = 9000;

            var sessionId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            await SeedSession(db, correctLot, sessionId, userId);

            // Act
            var result = await service.DeleteSessionAsync(wrongLot, sessionId);

            // Assert
            Assert.False(result);

            // Ensure session is still there
            Assert.NotNull(await db.Sessions.FindAsync(sessionId));
        }

        // -------------------------
        // NON-ADMIN USER (IF CHECK IN SERVICE)
        // -------------------------
        [Fact]
        public async Task DeleteSession_ReturnsFalse_WhenNotAuthorizedUser()
        {
            // Arrange
            var db = GetDb();
            var service = new SessionService(db);

            var lotId = 9999;
            var sessionId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();

            await SeedSession(db, lotId, sessionId, ownerId);

            // Act
            var result = await service.DeleteSessionAsync(lotId, sessionId);

            // Assert
            Assert.False(result);
        }
    }
}
