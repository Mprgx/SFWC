using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;
using MobyPark.Services;
using Xunit;

namespace MobyParkxUnitTest
{
    public class RefundServiceTests
    {

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

        public class FakeDiscountService : IDiscountService
        {
            public Task<(int statusCode, string message, DiscountReadDto?)> CreateDiscountAsync(
                DiscountPostDto dto,
                Guid userId)
            {
                // Not needed for refund/session tests
                return Task.FromResult<(int, string, DiscountReadDto?)>(
                    (201, "Discount created (fake)", null)
                );
            }

            public Task<(int statusCode, string message)> ApplyDiscountAsync(
                string? discountCode,
                string transaction,
                Guid userId)
            {
                // Always succeed in tests
                return Task.FromResult((200, "Discount applied (fake)"));
            }

            public Task<(int statusCode, string message, decimal? amountWithDiscount, string? normalizedCode)>
                PreviewDiscountAsync(
                    string? discountCode,
                    Guid userId,
                    int parkingLotId,
                    DateTimeOffset atTime,
                    decimal amount)
            {
                // No discount logic in unit tests
                return Task.FromResult<(int statusCode, string message, decimal? amountWithDiscount, string? normalizedCode)>(
                    (200, "Preview OK (fake)", (decimal?)amount, (string?)discountCode)
                );

            }

            public Task<(int statusCode, string message, List<DiscountCodeAnalyticsReadDto>? dto)>
                GetDiscountCodesAllAnalyticsAsync(DiscountCodeStatus status)
            {
                return Task.FromResult(
                    (200, "Analytics OK (fake)", new List<DiscountCodeAnalyticsReadDto>())
                );
            }

            public Task<(int statusCode, string message, DiscountCodeAnalyticsReadDto?)>
                GetDiscountCodeAnalyticsByCodeAsync(string code)
            {
                return Task.FromResult<(int statusCode, string message, DiscountCodeAnalyticsReadDto? dto)>(
                    (200, "Analytics OK (fake)", null)
                );

            }

            public Task<(int statusCode, string message, DiscountReadDto?)>
    UpdateDiscountAsync(string code, DiscountPatchDto dto)
            {
                return Task.FromResult<(int statusCode, string message, DiscountReadDto?)>(
                    (200, "Discount updated (fake)", null)
                );
            }
        }



        private static SessionService CreateService(UserDbContext context)
        {
            return new SessionService(context, new FakeEncryptionService(), new FakeDiscountService());
        }

        private static Session CreateBaseSession(
            Guid userId,
            DateTimeOffset started,
            DateTimeOffset? stopped,
            bool isCancelled = true,
            bool isRefunded = false)
        {
            return new Session
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                VehicleId = 1,
                ParkingLotId = 1,
                LicensePlate = "TEST-123",
                Started = started,
                Stopped = stopped,
                IsCancelled = isCancelled,
                IsRefunded = isRefunded,
                PaymentStatus = "paid",
                Cost = 20
            };
        }

        private static RefundRequestDto CreateValidRefundRequest()
        {
            return new RefundRequestDto
            {
                IBAN = "NL91ABNA0417164300"
            };
        }

        // --------------------------------------------------------
        // Tests
        // --------------------------------------------------------

        [Fact]
        public async Task RequestRefundAsync_ReturnsNull_WhenSessionDoesNotExist()
        {
            using var context = CreateDbContext(nameof(RequestRefundAsync_ReturnsNull_WhenSessionDoesNotExist));
            var service = CreateService(context);

            var (dto, error, status) = await service.RequestRefundAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                CreateValidRefundRequest()
            );

            Assert.Null(dto);
            Assert.Equal("Session not found.", error);
            Assert.Equal(404, status);
        }

        [Fact]
        public async Task RequestRefundAsync_ReturnsNull_WhenIBANIsMissing()
        {
            using var context = CreateDbContext(nameof(RequestRefundAsync_ReturnsNull_WhenIBANIsMissing));
            var service = CreateService(context);

            var userId = Guid.NewGuid();
            var session = CreateBaseSession(
                userId,
                DateTimeOffset.UtcNow.AddMinutes(-10),
                DateTimeOffset.UtcNow
            );

            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var (dto, error, status) = await service.RequestRefundAsync(
                userId,
                session.Id,
                new RefundRequestDto { IBAN = "" }
            );

            Assert.Null(dto);
            Assert.Equal("IBAN is required.", error);
            Assert.Equal(400, status);
        }


        [Fact]
        public async Task RequestRefundAsync_ReturnsNull_WhenSessionIsNotCancelled()
        {
            using var context = CreateDbContext(nameof(RequestRefundAsync_ReturnsNull_WhenSessionIsNotCancelled));
            var service = CreateService(context);

            var userId = Guid.NewGuid();
            var session = CreateBaseSession(
                userId,
                DateTimeOffset.UtcNow.AddHours(-2),
                DateTimeOffset.UtcNow.AddHours(-1),
                isCancelled: false
            );

            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var (dto, error, status) = await service.RequestRefundAsync(
                userId,
                session.Id,
                CreateValidRefundRequest()
            );

            Assert.Null(dto);
            Assert.Equal("Only cancelled sessions can be refunded.", error);
            Assert.Equal(409, status);
        }

        [Fact]
        public async Task RequestRefundAsync_ReturnsNull_WhenSessionIsNotStopped()
        {
            using var context = CreateDbContext(nameof(RequestRefundAsync_ReturnsNull_WhenSessionIsNotStopped));
            var service = CreateService(context);

            var userId = Guid.NewGuid();
            var session = CreateBaseSession(
                userId,
                DateTimeOffset.UtcNow.AddHours(-1),
                null
            );

            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var (dto, error, status) = await service.RequestRefundAsync(
                userId,
                session.Id,
                CreateValidRefundRequest()
            );

            Assert.Null(dto);
            Assert.Equal("Session must be stopped before refund.", error);
            Assert.Equal(409, status);
        }

        [Fact]
        public async Task RequestRefundAsync_ReturnsNull_WhenSessionAlreadyRefunded()
        {
            using var context = CreateDbContext(nameof(RequestRefundAsync_ReturnsNull_WhenSessionAlreadyRefunded));
            var service = CreateService(context);

            var userId = Guid.NewGuid();
            var session = CreateBaseSession(
                userId,
                DateTimeOffset.UtcNow.AddHours(-3),
                DateTimeOffset.UtcNow.AddHours(-2),
                isRefunded: true
            );

            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var (dto, error, status) = await service.RequestRefundAsync(
                userId,
                session.Id,
                CreateValidRefundRequest()
            );

            Assert.Null(dto);
            Assert.Equal("Session has already been refunded.", error);
            Assert.Equal(409, status);
        }

        [Fact]
        public async Task RequestRefundAsync_ReturnsFullRefund_ForShortSession()
        {
            using var context = CreateDbContext(nameof(RequestRefundAsync_ReturnsFullRefund_ForShortSession));
            var service = CreateService(context);

            var userId = Guid.NewGuid();
            var session = CreateBaseSession(
                userId,
                DateTimeOffset.UtcNow.AddMinutes(-10),
                DateTimeOffset.UtcNow
            );

            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var (dto, error, status) = await service.RequestRefundAsync(
                userId,
                session.Id,
                CreateValidRefundRequest()
            );

            Assert.NotNull(dto);
            Assert.Null(error);
            Assert.Null(status);
            Assert.True(dto!.Refunded > 0);
        }

        [Fact]
        public async Task RequestRefundAsync_ReturnsHalfRefund_ForMediumSession()
        {
            using var context = CreateDbContext(nameof(RequestRefundAsync_ReturnsHalfRefund_ForMediumSession));
            var service = CreateService(context);

            var userId = Guid.NewGuid();
            var session = CreateBaseSession(
                userId,
                DateTimeOffset.UtcNow.AddMinutes(-30),
                DateTimeOffset.UtcNow
            );

            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var (dto, error, status) = await service.RequestRefundAsync(
                userId,
                session.Id,
                CreateValidRefundRequest()
            );

            Assert.NotNull(dto);
            Assert.Null(error);
            Assert.Null(status);
            Assert.True(dto!.Refunded > 0);
        }


        [Fact]
        public async Task RequestRefundAsync_ReturnsNull_WhenRefundPercentageIsZero()
        {
            using var context = CreateDbContext(nameof(RequestRefundAsync_ReturnsNull_WhenRefundPercentageIsZero));
            var service = CreateService(context);

            var userId = Guid.NewGuid();
            var session = CreateBaseSession(
                userId,
                DateTimeOffset.UtcNow.AddHours(-10),
                DateTimeOffset.UtcNow
            );

            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var (dto, error, status) = await service.RequestRefundAsync(
                userId,
                session.Id,
                CreateValidRefundRequest()
            );

            Assert.Null(dto);
            Assert.Equal("No refundable amount for this session.", error);
            Assert.Equal(400, status);
        }

        [Fact]
        public async Task RequestRefundAsync_UpdatesRefundStateInDatabase()
        {
            using var context = CreateDbContext(nameof(RequestRefundAsync_UpdatesRefundStateInDatabase));
            var service = CreateService(context);

            var userId = Guid.NewGuid();
            var session = CreateBaseSession(
                userId,
                DateTimeOffset.UtcNow.AddMinutes(-10),
                DateTimeOffset.UtcNow
            );

            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            await service.RequestRefundAsync(
                userId,
                session.Id,
                CreateValidRefundRequest()
            );

            var updated = await context.Sessions.FindAsync(session.Id);
            Assert.True(updated!.IsRefunded);
        }
    }
}
