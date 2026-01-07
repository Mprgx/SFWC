using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;
using MobyPark.Services;
using Xunit;

namespace MobyParkxUnitTest
{
    public class BillingServiceTests
    {

        private static UserDbContext CreateDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new UserDbContext(options);
        }

        private class FakeEncryptionService : IEncryptionService
        {
            public string? Encrypt(string? plaintext) => plaintext;
            public string? Decrypt(string? ciphertext) => ciphertext;
        }

        private static BillingService CreateService(UserDbContext context)
        {
            return new BillingService(context, new FakeEncryptionService());
        }

        private static ParkingLot CreateParkingLot(int id = 1)
        {
            return new ParkingLot
            {
                Id = id,
                Name = "Test Parking",
                Location = "Test Location",
                Address = "Test Address",
                Capacity = 100,
                Tariff = 2.5,
                DayTariff = 20,
                Coordinates = "{}"
            };
        }

        private static Billing CreateBilling(
            string username,
            string licensePlate,
            ParkingLot parkingLot,
            DateTimeOffset started,
            DateTimeOffset stopped,
            decimal cost)
        {
            return new Billing
            {
                Id = Guid.NewGuid(),
                Username = username,
                LicensePlate = licensePlate,
                ParkingLotId = parkingLot.Id,
                ParkingLot = parkingLot,
                Started = started,
                Stopped = stopped,
                DurationMinutes = (int)(stopped - started).TotalMinutes,
                Cost = cost,
                PaymentStatus = "paid"
            };
        }


        [Fact]
        public async Task GetReceiptsForUserAsync_ReturnsReceipts_WhenUsernameIsValid()
        {
            using var context = CreateDbContext(nameof(GetReceiptsForUserAsync_ReturnsReceipts_WhenUsernameIsValid));
            var service = CreateService(context);

            var parkingLot = CreateParkingLot();
            context.ParkingLots.Add(parkingLot);

            context.Billings.AddRange(
                CreateBilling("soufiane", "AA-123-B", parkingLot,
                    DateTimeOffset.UtcNow.AddHours(-2),
                    DateTimeOffset.UtcNow.AddHours(-1), 10.123m),
                CreateBilling("soufiane", "BB-456-C", parkingLot,
                    DateTimeOffset.UtcNow.AddHours(-1),
                    DateTimeOffset.UtcNow, 5.5m)
            );

            await context.SaveChangesAsync();

            var (dto, error, status) = await service.GetReceiptsForUserAsync("soufiane");

            Assert.Null(error);
            Assert.Null(status);
            Assert.NotNull(dto);
            Assert.Equal(2, dto.Count);
        }

        [Fact]
        public async Task GetReceiptsForUserAsync_ReturnsEmptyList_WhenUserHasNoReceipts()
        {
            using var context = CreateDbContext(nameof(GetReceiptsForUserAsync_ReturnsEmptyList_WhenUserHasNoReceipts));
            var service = CreateService(context);

            var (dto, error, status) = await service.GetReceiptsForUserAsync("soufiane");

            Assert.NotNull(dto);
            Assert.Empty(dto);
            Assert.Null(error);
            Assert.Null(status);
        }

        [Fact]
        public async Task GetReceiptsForUserAsync_ReturnsBadRequest_WhenUsernameIsEmpty()
        {
            using var context = CreateDbContext(nameof(GetReceiptsForUserAsync_ReturnsBadRequest_WhenUsernameIsEmpty));
            var service = CreateService(context);

            var (dto, error, status) = await service.GetReceiptsForUserAsync(" ");

            Assert.Null(dto);
            Assert.Equal(400, status);
            Assert.NotNull(error);
        }

        [Fact]
        public async Task GetAllReceiptsAsync_ReturnsAllReceipts()
        {
            using var context = CreateDbContext(nameof(GetAllReceiptsAsync_ReturnsAllReceipts));
            var service = CreateService(context);

            var parkingLot = CreateParkingLot();
            context.ParkingLots.Add(parkingLot);

            context.Billings.AddRange(
                CreateBilling("user1", "AA-111-A", parkingLot,
                    DateTimeOffset.UtcNow.AddHours(-3),
                    DateTimeOffset.UtcNow.AddHours(-2), 8),
                CreateBilling("user2", "BB-222-B", parkingLot,
                    DateTimeOffset.UtcNow.AddHours(-2),
                    DateTimeOffset.UtcNow.AddHours(-1), 12)
            );

            await context.SaveChangesAsync();

            var (dto, error, status) = await service.GetAllReceiptsAsync();

            Assert.Null(error);
            Assert.Null(status);
            Assert.NotNull(dto);
            Assert.Equal(2, dto.Count);
        }


        [Fact]
        public async Task GetByIdAsync_ReturnsReceipt_WhenBillingExists()
        {
            using var context = CreateDbContext(nameof(GetByIdAsync_ReturnsReceipt_WhenBillingExists));
            var service = CreateService(context);

            var parkingLot = CreateParkingLot();
            context.ParkingLots.Add(parkingLot);

            var billing = CreateBilling(
                "soufiane",
                "AA-123-B",
                parkingLot,
                DateTimeOffset.UtcNow.AddHours(-2),
                DateTimeOffset.UtcNow.AddHours(-1),
                9.999m);

            context.Billings.Add(billing);
            await context.SaveChangesAsync();

            var (dto, error, status) = await service.GetByIdAsync(billing.Id);

            Assert.Null(error);
            Assert.Null(status);
            Assert.NotNull(dto);
            Assert.Equal("AA-123-B", dto.LicensePlate);
            Assert.Equal(Math.Round(9.999m, 2), dto.Cost);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNotFound_WhenBillingDoesNotExist()
        {
            using var context = CreateDbContext(nameof(GetByIdAsync_ReturnsNotFound_WhenBillingDoesNotExist));
            var service = CreateService(context);

            var (dto, error, status) = await service.GetByIdAsync(Guid.NewGuid());

            Assert.Null(dto);
            Assert.Equal(404, status);
            Assert.NotNull(error);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsBadRequest_WhenIdIsEmpty()
        {
            using var context = CreateDbContext(nameof(GetByIdAsync_ReturnsBadRequest_WhenIdIsEmpty));
            var service = CreateService(context);

            var (dto, error, status) = await service.GetByIdAsync(Guid.Empty);

            Assert.Null(dto);
            Assert.Equal(400, status);
            Assert.NotNull(error);
        }
    }
}
