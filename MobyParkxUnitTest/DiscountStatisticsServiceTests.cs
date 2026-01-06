using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;
using MobyPark.Services;

namespace MobyParkxUnitTest
{
    public class DiscountStatisticsServiceTests
    {
        // --------------------------------------------------------
        // Helpers
        // --------------------------------------------------------

        private static UserDbContext CreateDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new UserDbContext(options);
        }

        private static DiscountService CreateService(UserDbContext context)
        {
            return new DiscountService(context);
        }

        private static Discount CreateDiscount(
            string code,
            bool isActive,
            DateTimeOffset validFrom,
            DateTimeOffset validUntil,
            int? maxUsage = null,
            int? currentUsage = null,
            DiscountType type = DiscountType.Percentage,
            decimal value = 10m)
        {
            return new Discount
            {
                Code = code,
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTimeOffset.UtcNow,

                Type = type,
                Value = value,

                ValidFrom = validFrom,
                ValidUntil = validUntil,
                Active = isActive,

                MaxUsage = maxUsage,
                CurrentUsage = currentUsage,

                AllowedLocations = new List<DiscountLocation>(),
                ValidForUsers = new List<DiscountUser>(),
                ValidForCompanies = new List<DiscountCompany>(),
            };
        }

        private static Payment CreatePayment(
            string transaction,
            Guid userId,
            string? discountCode,
            decimal amount,
            decimal amountWithDiscount,
            bool completed = true)
        {
            return new Payment
            {
                Transaction = transaction,                 // required
                Initiator = "unit-test",                   // required :contentReference[oaicite:1]{index=1}
                Hash = "hash-unit-test",                   // required :contentReference[oaicite:2]{index=2}

                UserId = userId,

                DiscountCode = discountCode,
                Amount = amount,
                AmountWithDiscount = amountWithDiscount,

                Completed = completed ? DateTimeOffset.UtcNow : null,

                ParkingLotId = 1,
                SessionId = Guid.NewGuid()
            };
        }

        // --------------------------------------------------------
        // Tests - Filtering
        // --------------------------------------------------------

        [Fact]
        public async Task GetDiscountCodesAllAnalyticsAsync_DefaultActive_ReturnsOnlyActiveAndNotExpired()
        {
            using var context = CreateDbContext(nameof(GetDiscountCodesAllAnalyticsAsync_DefaultActive_ReturnsOnlyActiveAndNotExpired));
            var service = CreateService(context);

            var now = DateTimeOffset.UtcNow;

            context.Discounts.Add(CreateDiscount("ACTIVE_OK", true, now.AddDays(-1), now.AddDays(5)));
            context.Discounts.Add(CreateDiscount("ACTIVE_EXPIRED", true, now.AddDays(-10), now.AddDays(-1)));
            context.Discounts.Add(CreateDiscount("INACTIVE_OK", false, now.AddDays(-1), now.AddDays(5)));

            await context.SaveChangesAsync();

            var (statusCode, message, dto) = await service.GetDiscountCodesAllAnalyticsAsync(DiscountCodeStatus.Active);

            Assert.Equal(200, statusCode);
            Assert.NotNull(dto);

            Assert.Single(dto!);
            Assert.Equal("ACTIVE_OK", dto![0].Code);
            Assert.True(dto![0].IsActive);
        }

        [Fact]
        public async Task GetDiscountCodesAllAnalyticsAsync_StatusInactive_ReturnsOnlyInactive()
        {
            using var context = CreateDbContext(nameof(GetDiscountCodesAllAnalyticsAsync_StatusInactive_ReturnsOnlyInactive));
            var service = CreateService(context);

            var now = DateTimeOffset.UtcNow;

            context.Discounts.Add(CreateDiscount("ACTIVE_OK", true, now.AddDays(-1), now.AddDays(5)));
            context.Discounts.Add(CreateDiscount("INACTIVE_OK", false, now.AddDays(-1), now.AddDays(5)));

            await context.SaveChangesAsync();

            var (statusCode, message, dto) = await service.GetDiscountCodesAllAnalyticsAsync(DiscountCodeStatus.Inactive);

            Assert.Equal(200, statusCode);
            Assert.NotNull(dto);

            Assert.Single(dto!);
            Assert.Equal("INACTIVE_OK", dto![0].Code);
            Assert.False(dto![0].IsActive);
        }

        [Fact]
        public async Task GetDiscountCodesAllAnalyticsAsync_StatusExpired_ReturnsOnlyExpired()
        {
            using var context = CreateDbContext(nameof(GetDiscountCodesAllAnalyticsAsync_StatusExpired_ReturnsOnlyExpired));
            var service = CreateService(context);

            var now = DateTimeOffset.UtcNow;

            context.Discounts.Add(CreateDiscount("EXPIRED_OK", true, now.AddDays(-10), now.AddDays(-1)));
            context.Discounts.Add(CreateDiscount("ACTIVE_OK", true, now.AddDays(-1), now.AddDays(5)));

            await context.SaveChangesAsync();

            var (statusCode, message, dto) = await service.GetDiscountCodesAllAnalyticsAsync(DiscountCodeStatus.Expired);

            Assert.Equal(200, statusCode);
            Assert.NotNull(dto);

            Assert.Single(dto!);
            Assert.Equal("EXPIRED_OK", dto![0].Code);
        }

        [Fact]
        public async Task GetDiscountCodesAllAnalyticsAsync_StatusAll_ReturnsAll()
        {
            using var context = CreateDbContext(nameof(GetDiscountCodesAllAnalyticsAsync_StatusAll_ReturnsAll));
            var service = CreateService(context);

            var now = DateTimeOffset.UtcNow;

            context.Discounts.Add(CreateDiscount("ACTIVE_OK", true, now.AddDays(-1), now.AddDays(5)));
            context.Discounts.Add(CreateDiscount("INACTIVE_OK", false, now.AddDays(-1), now.AddDays(5)));
            context.Discounts.Add(CreateDiscount("EXPIRED_OK", true, now.AddDays(-10), now.AddDays(-1)));

            await context.SaveChangesAsync();

            var (statusCode, message, dto) = await service.GetDiscountCodesAllAnalyticsAsync(DiscountCodeStatus.All);

            Assert.Equal(200, statusCode);
            Assert.NotNull(dto);
            Assert.Equal(3, dto!.Count);
        }

        [Fact]
        public async Task GetDiscountCodesAllAnalyticsAsync_NoCodes_ReturnsEmptyList()
        {
            using var context = CreateDbContext(nameof(GetDiscountCodesAllAnalyticsAsync_NoCodes_ReturnsEmptyList));
            var service = CreateService(context);

            var (statusCode, message, dto) = await service.GetDiscountCodesAllAnalyticsAsync(DiscountCodeStatus.Active);

            Assert.Equal(200, statusCode);
            Assert.NotNull(dto);
            Assert.Empty(dto!);
        }

        // --------------------------------------------------------
        // Tests - Response fields + Statistics
        // --------------------------------------------------------

        [Fact]
        public async Task GetDiscountCodesAllAnalyticsAsync_ReturnsRequiredFields_PerCode()
        {
            using var context = CreateDbContext(nameof(GetDiscountCodesAllAnalyticsAsync_ReturnsRequiredFields_PerCode));
            var service = CreateService(context);

            var now = DateTimeOffset.UtcNow;

            context.Discounts.Add(CreateDiscount(
                code: "SAVE10",
                isActive: true,
                validFrom: now.AddDays(-1),
                validUntil: now.AddDays(5),
                maxUsage: 100,
                currentUsage: 2,
                type: DiscountType.Percentage,
                value: 10m));

            await context.SaveChangesAsync();

            var (statusCode, message, dto) = await service.GetDiscountCodesAllAnalyticsAsync(DiscountCodeStatus.Active);

            Assert.Equal(200, statusCode);
            Assert.NotNull(dto);
            Assert.Single(dto!);

            var row = dto![0];

            Assert.Equal("SAVE10", row.Code);
            Assert.Equal(DiscountType.Percentage, row.Type);
            Assert.Equal(10m, row.Value);

            Assert.True(row.ValidFrom < row.ValidUntil);
            Assert.True(row.IsActive);

            Assert.Equal(100, row.MaxUsageCount);
            Assert.Equal(2, row.CurrentUsageCount);

            Assert.True(row.ReservationsUsedCount >= 0);
            Assert.True(row.TotalSavedAmount >= 0);
        }

        [Fact]
        public async Task GetDiscountCodesAllAnalyticsAsync_UsesPaymentsToComputeCountAndTotalSavedAmount_RoundedTo2Decimals()
        {
            using var context = CreateDbContext(nameof(GetDiscountCodesAllAnalyticsAsync_UsesPaymentsToComputeCountAndTotalSavedAmount_RoundedTo2Decimals));
            var service = CreateService(context);

            var now = DateTimeOffset.UtcNow;
            var userId = Guid.NewGuid();

            context.Discounts.Add(CreateDiscount("SAVE10", true, now.AddDays(-1), now.AddDays(5)));

            context.Payments.Add(CreatePayment("T00000000001", userId, "SAVE10", 10.00m, 8.00m, completed: true));

            context.Payments.Add(CreatePayment("T00000000002", userId, "SAVE10", 7.00m, 6.11m, completed: true));

            context.Payments.Add(CreatePayment("T00000000003", userId, "OTHER", 10.00m, 10.00m, completed: true));

            context.Payments.Add(CreatePayment("T00000000004", userId, null, 10.00m, 10.00m, completed: true));

            await context.SaveChangesAsync();

            var (statusCode, message, dto) = await service.GetDiscountCodesAllAnalyticsAsync(DiscountCodeStatus.Active);

            Assert.Equal(200, statusCode);
            Assert.NotNull(dto);
            Assert.Single(dto!);

            var row = dto![0];

            Assert.Equal("SAVE10", row.Code);

            Assert.Equal(2, row.ReservationsUsedCount);

            Assert.Equal(2.89m, row.TotalSavedAmount);
        }

        [Fact]
        public async Task GetDiscountCodeAnalyticsByCodeAsync_Returns404_WhenCodeDoesNotExist()
        {
            using var context = CreateDbContext(nameof(GetDiscountCodeAnalyticsByCodeAsync_Returns404_WhenCodeDoesNotExist));
            var service = CreateService(context);

            var (statusCode, message, dto) = await service.GetDiscountCodeAnalyticsByCodeAsync("NOT_FOUND");

            Assert.Equal(404, statusCode);
            Assert.Null(dto);
        }

        [Fact]
        public async Task GetDiscountCodeAnalyticsByCodeAsync_ReturnsStats_ForSpecificCode()
        {
            using var context = CreateDbContext(nameof(GetDiscountCodeAnalyticsByCodeAsync_ReturnsStats_ForSpecificCode));
            var service = CreateService(context);

            var now = DateTimeOffset.UtcNow;
            var userId = Guid.NewGuid();

            context.Discounts.Add(CreateDiscount("SAVE10", true, now.AddDays(-1), now.AddDays(5)));
            context.Discounts.Add(CreateDiscount("SAVE20", true, now.AddDays(-1), now.AddDays(5)));

            context.Payments.Add(CreatePayment("T00000000011", userId, "SAVE10", 10m, 8m));
            context.Payments.Add(CreatePayment("T00000000012", userId, "SAVE10", 20m, 15m));
            context.Payments.Add(CreatePayment("T00000000013", userId, "SAVE20", 10m, 9m));

            await context.SaveChangesAsync();

            var (statusCode, message, dto) = await service.GetDiscountCodeAnalyticsByCodeAsync("save10"); // lower-case on purpose

            Assert.Equal(200, statusCode);
            Assert.NotNull(dto);

            Assert.Equal("SAVE10", dto!.Code);
            Assert.Equal(2, dto.ReservationsUsedCount);

            Assert.Equal(7.00m, dto.TotalSavedAmount);
        }
    }
}
