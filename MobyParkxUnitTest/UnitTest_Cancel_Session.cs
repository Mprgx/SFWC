using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;
using MobyPark.Services;

namespace MobyParkxUnitTest
{
    public class CancelSessionServiceTests
    {
        private static UserDbContext CreateDb(string name)
        {
            var opts = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(name)
                .Options;

            return new UserDbContext(opts);
        }

        private class FakeEncryptionService : IEncryptionService
        {
            public string? Encrypt(string? plaintext)
            {
                return plaintext is null ? null : $"ENC:{plaintext}";
            }

            public string? Decrypt(string? ciphertext)
            {
                if (ciphertext is null)
                    return null;

                const string prefix = "ENC:";
                if (ciphertext.StartsWith(prefix, StringComparison.Ordinal))
                    return ciphertext[prefix.Length..];

                return ciphertext;
            }
        }

        private static SessionService CreateService(UserDbContext db)
        {
            return new SessionService(db, new FakeEncryptionService());
        }

        // -------------------------------------------------------------------
        // 1. Returns null when session does not exist
        // -------------------------------------------------------------------
        [Fact]
        public async Task CancelSessionAsync_ReturnsNull_WhenSessionDoesNotExist()
        {
            using var db = CreateDb(nameof(CancelSessionAsync_ReturnsNull_WhenSessionDoesNotExist));
            var service = CreateService(db);

            var result = await service.CancelSessionAsync(Guid.NewGuid(), Guid.NewGuid(), new CancelSessionDto());

            Assert.Null(result);
        }

        // -------------------------------------------------------------------
        // 2. Returns null when already cancelled
        // -------------------------------------------------------------------
        [Fact]
        public async Task CancelSessionAsync_ReturnsNull_WhenSessionAlreadyCancelled()
        {
            using var db = CreateDb(nameof(CancelSessionAsync_ReturnsNull_WhenSessionAlreadyCancelled));
            var service = CreateService(db);

            var s = new Session
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Started = DateTimeOffset.UtcNow.AddMinutes(-5),
                Stopped = DateTimeOffset.UtcNow,
                IsCancelled = true,
                ParkingLotId = 1,
                LicensePlate = "TESTPLATE"
            };

            db.Sessions.Add(s);
            await db.SaveChangesAsync();

            var result = await service.CancelSessionAsync(s.UserId, s.Id, new CancelSessionDto());

            Assert.Null(result);
        }

        // -------------------------------------------------------------------
        // 3. Cancelling automatically sets Stopped when null
        // -------------------------------------------------------------------
        [Fact]
        public async Task CancelSessionAsync_SetsStoppedTimestamp_WhenStoppedIsNull()
        {
            using var db = CreateDb(nameof(CancelSessionAsync_SetsStoppedTimestamp_WhenStoppedIsNull));
            var service = CreateService(db);

            var before = DateTimeOffset.UtcNow.AddMinutes(-20);

            var s = new Session
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Started = before,
                Stopped = null,
                IsCancelled = false,
                ParkingLotId = 1,
                LicensePlate = "TEST"
            };

            db.Sessions.Add(s);
            await db.SaveChangesAsync();

            var result = await service.CancelSessionAsync(s.UserId, s.Id, new CancelSessionDto());

            Assert.NotNull(result);
            Assert.NotNull(result!.Stopped);
            Assert.True(result!.Stopped >= s.Started);
        }

        // -------------------------------------------------------------------
        // 4. Cancel sets IsCancelled + CancelledAt
        // -------------------------------------------------------------------
        [Fact]
        public async Task CancelSessionAsync_MarksSessionCancelledAndSetsCancelledAt()
        {
            using var db = CreateDb(nameof(CancelSessionAsync_MarksSessionCancelledAndSetsCancelledAt));
            var service = CreateService(db);

            var s = new Session
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Started = DateTimeOffset.UtcNow.AddMinutes(-5),
                ParkingLotId = 1,
                LicensePlate = "TEST"
            };

            db.Sessions.Add(s);
            await db.SaveChangesAsync();

            var result = await service.CancelSessionAsync(s.UserId, s.Id, new CancelSessionDto());

            Assert.NotNull(result);
            Assert.True(result!.IsCancelled);
            Assert.NotNull(result!.CancelledAt);
        }

        // -------------------------------------------------------------------
        // 5. Cancel persists DB state
        // -------------------------------------------------------------------
        [Fact]
        public async Task CancelSessionAsync_SavesChangesToDatabase()
        {
            using var db = CreateDb(nameof(CancelSessionAsync_SavesChangesToDatabase));
            var service = CreateService(db);

            var s = new Session
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Started = DateTimeOffset.UtcNow.AddMinutes(-10),
                ParkingLotId = 1,
                LicensePlate = "TEST"
            };

            db.Sessions.Add(s);
            await db.SaveChangesAsync();

            var result = await service.CancelSessionAsync(s.UserId, s.Id, new CancelSessionDto());
            Assert.NotNull(result);

            var dbSession = await db.Sessions.FindAsync(s.Id);

            Assert.True(dbSession!.IsCancelled);
            Assert.NotNull(dbSession.CancelledAt);
        }
    }
}
