using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;
using MobyPark.Services;
using Xunit;

namespace MobyParkxUnitTest
{
    public class CancelSessionServiceTests
    {
        // --------------------------------------------------------
        // Helpers
        // --------------------------------------------------------

        private static UserDbContext CreateDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new UserDbContext(options);
        }

        private class FakeEncryptionService : IEncryptionService
        {
            public string? Encrypt(string? plaintext) => plaintext;
            public string? Decrypt(string? ciphertext) => ciphertext;
        }


        private static SessionService CreateService(UserDbContext context)
        {
            return new SessionService(context, new FakeEncryptionService());
        }


        private static Session CreateSession(Guid userId)
        {
            return new Session
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                VehicleId = 1,
                ParkingLotId = 1,
                LicensePlate = "AA-123-B",
                Started = DateTimeOffset.UtcNow.AddHours(1),
                PaymentStatus = "unpaid",
                IsCancelled = false
            };
        }

        // --------------------------------------------------------
        // Tests
        // --------------------------------------------------------

        [Fact]
        public async Task CancelSessionAsync_ReturnsNotFound_WhenSessionDoesNotExist()
        {
            using var context = CreateDbContext(nameof(CancelSessionAsync_ReturnsNotFound_WhenSessionDoesNotExist));
            var service = CreateService(context);

            var result = await service.CancelSessionAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new CancelSessionDto()
            );

            Assert.Null(result.dto);
            Assert.Equal(404, result.status);
            Assert.NotNull(result.error);
        }

        [Fact]
        public async Task CancelSessionAsync_ReturnsConflict_WhenSessionAlreadyCancelled()
        {
            using var context = CreateDbContext(nameof(CancelSessionAsync_ReturnsConflict_WhenSessionAlreadyCancelled));
            var service = CreateService(context);

            var userId = Guid.NewGuid();
            var session = CreateSession(userId);
            session.IsCancelled = true;

            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var result = await service.CancelSessionAsync(
                userId,
                session.Id,
                new CancelSessionDto()
            );

            Assert.Null(result.dto);
            Assert.Equal(409, result.status);
            Assert.NotNull(result.error);
        }

        [Fact]
        public async Task CancelSessionAsync_SetsIsCancelledAndCancelledAt_WhenSuccessful()
        {
            using var context = CreateDbContext(nameof(CancelSessionAsync_SetsIsCancelledAndCancelledAt_WhenSuccessful));
            var service = CreateService(context);

            var userId = Guid.NewGuid();
            var session = CreateSession(userId);

            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var result = await service.CancelSessionAsync(
                userId,
                session.Id,
                new CancelSessionDto()
            );

            Assert.NotNull(result.dto);
            Assert.Null(result.error);
            Assert.Null(result.status);

            var updated = await context.Sessions.FindAsync(session.Id);
            Assert.True(updated!.IsCancelled);
            Assert.NotNull(updated.CancelledAt);
        }

        [Fact]
        public async Task CancelSessionAsync_PersistsChangesInDatabase()
        {
            using var context = CreateDbContext(nameof(CancelSessionAsync_PersistsChangesInDatabase));
            var service = CreateService(context);

            var userId = Guid.NewGuid();
            var session = CreateSession(userId);

            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            await service.CancelSessionAsync(
                userId,
                session.Id,
                new CancelSessionDto()
            );

            using var verifyContext = CreateDbContext(nameof(CancelSessionAsync_PersistsChangesInDatabase));
            var persisted = await verifyContext.Sessions.FindAsync(session.Id);

            Assert.NotNull(persisted);
            Assert.True(persisted!.IsCancelled);
        }

        [Fact]
        public async Task CancelSessionAsync_ReturnsSessionReadDto_OnSuccess()
        {
            using var context = CreateDbContext(nameof(CancelSessionAsync_ReturnsSessionReadDto_OnSuccess));
            var service = CreateService(context);

            var userId = Guid.NewGuid();
            var session = CreateSession(userId);

            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var result = await service.CancelSessionAsync(
                userId,
                session.Id,
                new CancelSessionDto()
            );

            Assert.NotNull(result.dto);
            Assert.Equal(session.Id, result.dto!.Id);
            Assert.True(result.dto.IsCancelled);
        }
    }
}
