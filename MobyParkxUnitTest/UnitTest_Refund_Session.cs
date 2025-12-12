using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;
using MobyPark.Services;

namespace MobyParkxUnitTest
{
    public class RefundServiceTests
    {
        // ------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------
        private static UserDbContext CreateDb(string name)
        {
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(name)
                .Options;

            return new UserDbContext(options);
        }

        private class FakeEncryption : IEncryptionService
        {
            public string? Encrypt(string? plaintext) => plaintext;
            public string? Decrypt(string? ciphertext) => ciphertext;
        }

        private static SessionService CreateService(UserDbContext db)
        {
            return new SessionService(db, new FakeEncryption());
        }

        private static Session CreateBaseSession(Guid userId, DateTimeOffset started, DateTimeOffset? stopped,
            bool isCancelled = true, bool isRefunded = false)
        {
            return new Session
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ParkingLotId = 1,
                LicensePlate = "TEST-123",
                Started = started,
                Stopped = stopped,
                IsCancelled = isCancelled,
                IsRefunded = isRefunded,
                PaymentStatus = "paid"
            };
        }

        // ------------------------------------------------------------
        // 1. Session must exist
        // ------------------------------------------------------------
        [Fact]
        public async Task RequestRefundAsync_ReturnsNull_WhenSessionDoesNotExist()
        {
            using var db = CreateDb(nameof(RequestRefundAsync_ReturnsNull_WhenSessionDoesNotExist));
            var service = CreateService(db);

            var result = await service.RequestRefundAsync(Guid.NewGuid(), Guid.NewGuid(), new RefundRequestDto
            {
                IBAN = "NL91ABNA0417164300"
            });

            Assert.Null(result);
        }

        // ------------------------------------------------------------
        // 2. Session must be cancelled
        // ------------------------------------------------------------
        [Fact]
        public async Task RequestRefundAsync_ReturnsNull_WhenSessionIsNotCancelled()
        {
            using var db = CreateDb(nameof(RequestRefundAsync_ReturnsNull_WhenSessionIsNotCancelled));
            var service = CreateService(db);

            var userId = Guid.NewGuid();
            var session = CreateBaseSession(
                userId,
                DateTimeOffset.UtcNow.AddMinutes(-10),
                DateTimeOffset.UtcNow,
                isCancelled: false,
                isRefunded: false);

            db.Sessions.Add(session);
            await db.SaveChangesAsync();

            var result = await service.RequestRefundAsync(userId, session.Id, new RefundRequestDto
            {
                IBAN = "NL91ABNA0417164300"
            });

            Assert.Null(result);
        }

        // ------------------------------------------------------------
        // 3. Session must be stopped
        // ------------------------------------------------------------
        [Fact]
        public async Task RequestRefundAsync_ReturnsNull_WhenSessionIsNotStopped()
        {
            using var db = CreateDb(nameof(RequestRefundAsync_ReturnsNull_WhenSessionIsNotStopped));
            var service = CreateService(db);

            var userId = Guid.NewGuid();
            var session = CreateBaseSession(
                userId,
                DateTimeOffset.UtcNow.AddMinutes(-10),
                stopped: null,
                isCancelled: true,
                isRefunded: false);

            db.Sessions.Add(session);
            await db.SaveChangesAsync();

            var result = await service.RequestRefundAsync(userId, session.Id, new RefundRequestDto
            {
                IBAN = "NL91ABNA0417164300"
            });

            Assert.Null(result);
        }

        // ------------------------------------------------------------
        // 4. No double refunds
        // ------------------------------------------------------------
        [Fact]
        public async Task RequestRefundAsync_ReturnsNull_WhenSessionAlreadyRefunded()
        {
            using var db = CreateDb(nameof(RequestRefundAsync_ReturnsNull_WhenSessionAlreadyRefunded));
            var service = CreateService(db);

            var userId = Guid.NewGuid();
            var session = CreateBaseSession(
                userId,
                DateTimeOffset.UtcNow.AddMinutes(-20),
                DateTimeOffset.UtcNow,
                isCancelled: true,
                isRefunded: true);

            db.Sessions.Add(session);
            await db.SaveChangesAsync();

            var result = await service.RequestRefundAsync(userId, session.Id, new RefundRequestDto
            {
                IBAN = "NL91ABNA0417164300"
            });

            Assert.Null(result);
        }

        // ------------------------------------------------------------
        // 5. Refund attempts limited to 3
        // ------------------------------------------------------------
        [Fact]
        public async Task RequestRefundAsync_StopsAfterThreeFailedRefundAttempts()
        {
            using var db = CreateDb(nameof(RequestRefundAsync_StopsAfterThreeFailedRefundAttempts));
            var service = CreateService(db);

            var userId = Guid.NewGuid();
            var session = CreateBaseSession(
                userId,
                DateTimeOffset.UtcNow.AddMinutes(-5),
                DateTimeOffset.UtcNow,
                isCancelled: true,
                isRefunded: false);

            db.Sessions.Add(session);
            await db.SaveChangesAsync();

            var dto = new RefundRequestDto { IBAN = "NL91ABNA0417164300" };

            var r1 = await service.RequestRefundAsync(userId, session.Id, dto);
            var r2 = await service.RequestRefundAsync(userId, session.Id, dto);
            var r3 = await service.RequestRefundAsync(userId, session.Id, dto);
            var r4 = await service.RequestRefundAsync(userId, session.Id, dto);

            Assert.Null(r4);
        }

        // ------------------------------------------------------------
        // 6. Minimum cost and 100% refund for short session
        // ------------------------------------------------------------
        [Fact]
        public async Task RequestRefundAsync_ReturnsFullRefund_ForShortSession()
        {
            using var db = CreateDb(nameof(RequestRefundAsync_ReturnsFullRefund_ForShortSession));
            var service = CreateService(db);

            var userId = Guid.NewGuid();
            var started = DateTimeOffset.UtcNow.AddMinutes(-5);
            var stopped = DateTimeOffset.UtcNow;

            var session = CreateBaseSession(userId, started, stopped, true, false);
            db.Sessions.Add(session);
            await db.SaveChangesAsync();

            var result = await service.RequestRefundAsync(userId, session.Id, new RefundRequestDto
            {
                IBAN = "NL91ABNA0417164300"
            });

            var data = Assert.IsType<RefundResponseDto>(result);

            Assert.InRange(data.DurationMinutes, 4, 6);
            Assert.Equal(0.50m, data.Cost);
            Assert.Equal(100m, data.Percentage);
            Assert.Equal(0.50m, data.Refunded);
        }

        // ------------------------------------------------------------
        // 7. Medium session → 50% refund
        // ------------------------------------------------------------
        [Fact]
        public async Task RequestRefundAsync_ReturnsHalfRefund_ForMediumSession()
        {
            using var db = CreateDb(nameof(RequestRefundAsync_ReturnsHalfRefund_ForMediumSession));
            var service = CreateService(db);

            var userId = Guid.NewGuid();
            var started = DateTimeOffset.UtcNow.AddMinutes(-20);
            var stopped = DateTimeOffset.UtcNow;

            var session = CreateBaseSession(userId, started, stopped, true, false);
            db.Sessions.Add(session);
            await db.SaveChangesAsync();

            var result = await service.RequestRefundAsync(userId, session.Id, new RefundRequestDto
            {
                IBAN = "NL91ABNA0417164300"
            });

            var data = Assert.IsType<RefundResponseDto>(result);

            Assert.InRange(data.DurationMinutes, 19, 21);
            var expectedCost = Math.Round(data.DurationMinutes * 0.05m, 2);
            Assert.Equal(expectedCost, data.Cost);
            Assert.Equal(50m, data.Percentage);
            Assert.Equal(Math.Round(expectedCost * 0.5m, 2), data.Refunded);
        }

        // ------------------------------------------------------------
        // 8. Long session → 0% refund → returns null
        // ------------------------------------------------------------
        [Fact]
        public async Task RequestRefundAsync_ReturnsNull_WhenRefundPercentageIsZero()
        {
            using var db = CreateDb(nameof(RequestRefundAsync_ReturnsNull_WhenRefundPercentageIsZero));
            var service = CreateService(db);

            var userId = Guid.NewGuid();
            var started = DateTimeOffset.UtcNow.AddMinutes(-60);
            var stopped = DateTimeOffset.UtcNow;

            var session = CreateBaseSession(userId, started, stopped, true, false);
            db.Sessions.Add(session);
            await db.SaveChangesAsync();

            var dto = new RefundRequestDto { IBAN = "NL91ABNA0417164300" };

            var result = await service.RequestRefundAsync(userId, session.Id, dto);

            Assert.Null(result);

            var dbSession = await db.Sessions.FindAsync(session.Id);
            Assert.False(dbSession!.IsRefunded);
            Assert.Null(dbSession.RefundDate);
            Assert.Equal("paid", dbSession.PaymentStatus);
        }

        // ------------------------------------------------------------
        // 9. IBAN required
        // ------------------------------------------------------------
        [Fact]
        public async Task RequestRefundAsync_ReturnsNull_WhenIBANIsMissing()
        {
            using var db = CreateDb(nameof(RequestRefundAsync_ReturnsNull_WhenIBANIsMissing));
            var service = CreateService(db);

            var userId = Guid.NewGuid();
            var started = DateTimeOffset.UtcNow.AddMinutes(-8);
            var stopped = DateTimeOffset.UtcNow;

            var session = CreateBaseSession(userId, started, stopped, true, false);
            db.Sessions.Add(session);
            await db.SaveChangesAsync();

            var dto = new RefundRequestDto { IBAN = "   " };

            var result = await service.RequestRefundAsync(userId, session.Id, dto);

            Assert.Null(result);

            var dbSession = await db.Sessions.FindAsync(session.Id);
            Assert.False(dbSession!.IsRefunded);
            Assert.Null(dbSession.RefundDate);
            Assert.Equal("paid", dbSession.PaymentStatus);
        }

        // ------------------------------------------------------------
        // 10. Successful refund updates DB
        // ------------------------------------------------------------
        [Fact]
        public async Task RequestRefundAsync_UpdatesRefundStateInDatabase()
        {
            using var db = CreateDb(nameof(RequestRefundAsync_UpdatesRefundStateInDatabase));
            var service = CreateService(db);

            var userId = Guid.NewGuid();
            var started = DateTimeOffset.UtcNow.AddMinutes(-12);
            var stopped = DateTimeOffset.UtcNow;

            var session = CreateBaseSession(userId, started, stopped, true, false);
            db.Sessions.Add(session);
            await db.SaveChangesAsync();

            var result = await service.RequestRefundAsync(userId, session.Id, new RefundRequestDto
            {
                IBAN = "NL91ABNA0417164300"
            });

            var data = Assert.IsType<RefundResponseDto>(result);

            var dbSession = await db.Sessions.FindAsync(session.Id);
            Assert.True(dbSession!.IsRefunded);
            Assert.NotNull(dbSession.RefundDate);
            Assert.Equal("refunded", dbSession.PaymentStatus);
        }
    }
}
