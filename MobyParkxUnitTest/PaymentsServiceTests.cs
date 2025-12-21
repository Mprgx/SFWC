using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MobyPark.Constants;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;
using MobyPark.Services;
using Xunit;

namespace MobyParkxUnitTest
{
    public class PaymentServiceTests
    {
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

        private static PaymentService CreateService(UserDbContext db)
        {
            return new PaymentService(db, new FakeEncryption());
        }

        private static JsonElement CreateJsonElement(object obj)
        {
            var json = JsonSerializer.Serialize(obj);
            return JsonDocument.Parse(json).RootElement;
        }

        private static Payment CreateBasePayment(Guid userId, string transactionId, decimal amount,
            string? hash = null, DateTimeOffset? completed = null, Guid? sessionId = null)
        {
            return new Payment
            {
                UserId = userId,
                Initiator = "System",
                ParkingLotId = 1,
                Transaction = transactionId,
                Amount = amount,
                Hash = hash ?? "valid-default-hash",
                Completed = completed,
                Created_At = DateTimeOffset.UtcNow,
                SessionId = sessionId
            };
        }

        private static Session CreateBaseSession(Guid userId, Guid sessionId)
        {
            return new Session
            {
                Id = sessionId,
                UserId = userId,
                ParkingLotId = 1,
                LicensePlate = "TEST-123",
                Started = DateTimeOffset.UtcNow.AddHours(-1),
                Stopped = DateTimeOffset.UtcNow,
                PaymentStatus = "pending",
                Cost = 5.00m
            };
        }

        private static User CreateUser(string username)
        {
            return new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                Email = "test@example.com",
                Name = "Test User",
                PhoneNumber = "1234567890",
                PasswordHash = "hash",
                BirthYear = 1990
            };
        }

        [Fact]
        public async Task CompletePaymentAsync_ReturnsError_WhenInputIsInvalid()
        {
            using var db = CreateDb(nameof(CompletePaymentAsync_ReturnsError_WhenInputIsInvalid));
            var service = CreateService(db);
            var userId = Guid.NewGuid();

            var r1 = await service.CompletePaymentAsync(Guid.Empty, "TX123", new PaymentValidationDto());
            Assert.Null(r1.dto);
            Assert.Equal(400, r1.status);

            var r2 = await service.CompletePaymentAsync(userId, "", new PaymentValidationDto());
            Assert.Null(r2.dto);
            Assert.Equal(400, r2.status);

            var r3 = await service.CompletePaymentAsync(userId, "TX123", null!);
            Assert.Null(r3.dto);
            Assert.Equal(400, r3.status);
        }

        [Fact]
        public async Task CompletePaymentAsync_ReturnsError_WhenValidationDataIsMissing()
        {
            using var db = CreateDb(nameof(CompletePaymentAsync_ReturnsError_WhenValidationDataIsMissing));
            var service = CreateService(db);
            var userId = Guid.NewGuid();
            var r1 = await service.CompletePaymentAsync(userId, "TX123", new PaymentValidationDto
            {
                Validation = "",
                T_Data = CreateJsonElement(new { some = "data" })
            });
            Assert.Equal("Validation field is missing.", r1.error);

            var r2 = await service.CompletePaymentAsync(userId, "TX123", new PaymentValidationDto
            {
                Validation = "valid-hash"
            });
            Assert.Equal("t_data field is missing.", r2.error);
        }

        [Fact]
        public async Task CompletePaymentAsync_ReturnsNotFound_WhenPaymentDoesNotExist()
        {
            using var db = CreateDb(nameof(CompletePaymentAsync_ReturnsNotFound_WhenPaymentDoesNotExist));
            var service = CreateService(db);

            var result = await service.CompletePaymentAsync(Guid.NewGuid(), "NON_EXISTENT", new PaymentValidationDto
            {
                Validation = "hash",
                T_Data = CreateJsonElement(new { id = 1 })
            });

            Assert.Null(result.dto);
            Assert.Equal(404, result.status);
        }

        [Fact]
        public async Task CompletePaymentAsync_UpdatesPaymentAndSession_WhenSuccessful()
        {
            using var db = CreateDb(nameof(CompletePaymentAsync_UpdatesPaymentAndSession_WhenSuccessful));
            var service = CreateService(db);

            var userId = Guid.NewGuid();
            var sessionId = Guid.NewGuid();

            var session = CreateBaseSession(userId, sessionId);
            // FIX: Ensure the payment is linked to the user AND session
            var payment = CreateBasePayment(userId, "TX_SUCCESS", 5.00m, hash: "secure-hash", sessionId: sessionId);

            // Add ParkingLot to satisfy foreign key constraint
            db.ParkingLots.Add(new ParkingLot { Id = 1, Name = "Test Lot", Location = "Test Location", Address = "123 Main St", Capacity = 50, Tariff = 5.0, DayTariff = 25.0, Coordinates = "{}" });
            db.Sessions.Add(session);
            db.Payments.Add(payment);
            await db.SaveChangesAsync();

            var request = new PaymentValidationDto
            {
                Validation = "secure-hash",
                T_Data = CreateJsonElement(new { provider = "mollie" })
            };
            var result = await service.CompletePaymentAsync(userId, "TX_SUCCESS", request);

            Assert.NotNull(result.dto);
            Assert.Null(result.error);
            Assert.Equal(PaymentStatuses.Paid, result.dto.Status);

            var dbPayment = await db.Payments.FindAsync(payment.Transaction);
            Assert.NotNull(dbPayment!.Completed);
            Assert.Contains("mollie", dbPayment.T_Data);

            var dbSession = await db.Sessions.FindAsync(sessionId);
            Assert.Equal(PaymentStatuses.Paid, dbSession!.PaymentStatus);
        }

        [Fact]
        public async Task FulfillPaymentAsync_ReturnsError_WhenAmountMismatch()
        {
            using var db = CreateDb(nameof(FulfillPaymentAsync_ReturnsError_WhenAmountMismatch));
            var service = CreateService(db);

            var userId = Guid.NewGuid();
            var payment = CreateBasePayment(userId, "TX_AMOUNT", 10.00m);

            // Add ParkingLot to satisfy foreign key constraint
            db.ParkingLots.Add(new ParkingLot { Id = 1, Name = "Test Lot", Location = "Test Location", Address = "123 Main St", Capacity = 50, Tariff = 5.0, DayTariff = 25.0, Coordinates = "{}" });
            db.Payments.Add(payment);
            await db.SaveChangesAsync();

            var request = new PaymentsDto
            {
                Transaction = "TX_AMOUNT",
                Amount = 5.00m // Mismatch
            };
            var result = await service.FulfillPaymentAsync(userId, request);

            Assert.Null(result.dto);
            Assert.Equal(409, result.status);
            Assert.Equal("Amount mismatch.", result.error);
        }

        [Fact]
        public async Task FulfillPaymentAsync_ReturnsSuccess_WhenValid()
        {
            using var db = CreateDb(nameof(FulfillPaymentAsync_ReturnsSuccess_WhenValid));
            var service = CreateService(db);

            var userId = Guid.NewGuid();
            var payment = CreateBasePayment(userId, "TX_OK", 10.00m);

            // Add ParkingLot to satisfy foreign key constraint
            db.ParkingLots.Add(new ParkingLot { Id = 1, Name = "Test Lot", Location = "Test Location", Address = "123 Main St", Capacity = 50, Tariff = 5.0, DayTariff = 25.0, Coordinates = "{}" });
            db.Payments.Add(payment);
            await db.SaveChangesAsync();

            var request = new PaymentsDto
            {
                Transaction = "TX_OK",
                Amount = 10.00m
            };

            var result = await service.FulfillPaymentAsync(userId, request);

            Assert.NotNull(result.dto);
            Assert.Equal(PaymentStatuses.Paid, result.dto.Status);

            var dbPayment = await db.Payments.FindAsync(payment.Transaction);
            Assert.NotNull(dbPayment!.Completed);
        }

        [Fact]
        public async Task GetPaymentsForUserAsync_ReturnsOnlyUserPayments()
        {
            using var db = CreateDb(nameof(GetPaymentsForUserAsync_ReturnsOnlyUserPayments));
            var service = CreateService(db);

            var user1 = Guid.NewGuid();
            var user2 = Guid.NewGuid();

            // Add ParkingLot to satisfy foreign key constraint
            db.ParkingLots.Add(new ParkingLot { Id = 1, Name = "Test Lot", Location = "Test Location", Address = "123 Main St", Capacity = 50, Tariff = 5.0, DayTariff = 25.0, Coordinates = "{}" });
            db.Payments.Add(CreateBasePayment(user1, "TX_1", 10m));
            db.Payments.Add(CreateBasePayment(user1, "TX_2", 15m));
            db.Payments.Add(CreateBasePayment(user2, "TX_3", 20m));
            await db.SaveChangesAsync();
            var result = await service.GetPaymentsForUserAsync(user1);

            Assert.NotNull(result.dto);
            Assert.Equal(2, result.dto.Count);
            Assert.DoesNotContain(result.dto, p => p.Transaction == "TX_3");
        }

        [Fact]
        public async Task GetPaymentsForAnyUserAsync_ReturnsNotFound_WhenUserDoesNotExist()
        {
            using var db = CreateDb(nameof(GetPaymentsForAnyUserAsync_ReturnsNotFound_WhenUserDoesNotExist));
            var service = CreateService(db);

            var result = await service.GetPaymentsForAnyUserAsync("ghost_user");

            Assert.Null(result.dto);
            Assert.Equal(404, result.status);
        }

        [Fact]
        public async Task GetPaymentsForAnyUserAsync_ReturnsPayments_WhenUserExists()
        {
            using var db = CreateDb(nameof(GetPaymentsForAnyUserAsync_ReturnsPayments_WhenUserExists));
            var service = CreateService(db);

            var user = CreateUser("john_doe");

            // Add ParkingLot to satisfy foreign key constraint
            db.ParkingLots.Add(new ParkingLot { Id = 1, Name = "Test Lot", Location = "Test Location", Address = "123 Main St", Capacity = 50, Tariff = 5.0, DayTariff = 25.0, Coordinates = "{}" });
            db.Users.Add(user);
            db.Payments.Add(CreateBasePayment(user.Id, "TX_JOHN", 50m));
            await db.SaveChangesAsync();

            // Act
            var result = await service.GetPaymentsForAnyUserAsync("john_doe");

            Assert.NotNull(result.dto);
            Assert.Single(result.dto);
            Assert.Equal("TX_JOHN", result.dto[0].Transaction);
        }

        [Fact]
        public async Task DeletePayment_RemovesPayment_WhenFound()
        {
            using var db = CreateDb(nameof(DeletePayment_RemovesPayment_WhenFound));
            var service = CreateService(db);

            var payment = CreateBasePayment(Guid.NewGuid(), "TX_DEL", 100m);
            db.Payments.Add(payment);
            await db.SaveChangesAsync();

            var result = await service.DeletePaymentByTransactionId("TX_DEL");

            Assert.True(result.dto);

            var dbPayment = await db.Payments.FirstOrDefaultAsync(p => p.Transaction == "TX_DEL");
            Assert.Null(dbPayment);
        }

        [Fact]
        public async Task DeletePayment_ReturnsFalse_WhenNotFound()
        {
            using var db = CreateDb(nameof(DeletePayment_ReturnsFalse_WhenNotFound));
            var service = CreateService(db);

            var result = await service.DeletePaymentByTransactionId("TX_MISSING");

            Assert.False(result.dto);
            Assert.Equal(404, result.status);
        }

        [Fact]
        public async Task DeletePayment_ReturnsError_WhenTransactionIdIsEmpty()
        {
            using var db = CreateDb(nameof(DeletePayment_ReturnsError_WhenTransactionIdIsEmpty));
            var service = CreateService(db);

            var result = await service.DeletePaymentByTransactionId("");

            Assert.False(result.dto);
            Assert.Equal(400, result.status);
            Assert.Equal("Transaction ID is required.", result.error);
        }

        [Fact]
        public async Task DeletePayment_RemovesOnlyTargetPayment_WhenMultipleExist()
        {
            using var db = CreateDb(nameof(DeletePayment_RemovesOnlyTargetPayment_WhenMultipleExist));
            var service = CreateService(db);

            var userId = Guid.NewGuid();
            db.ParkingLots.Add(new ParkingLot { Id = 1, Name = "Test Lot", Location = "Test Location", Address = "123 Main St", Capacity = 50, Tariff = 5.0, DayTariff = 25.0, Coordinates = "{}" });
            db.Payments.Add(CreateBasePayment(userId, "TX_KEEP_1", 10m));
            db.Payments.Add(CreateBasePayment(userId, "TX_DELETE", 20m));
            db.Payments.Add(CreateBasePayment(userId, "TX_KEEP_2", 15m));
            await db.SaveChangesAsync();

            var result = await service.DeletePaymentByTransactionId("TX_DELETE");

            Assert.True(result.dto);
            var remainingPayments = await db.Payments.ToListAsync();
            Assert.Equal(2, remainingPayments.Count);
            Assert.DoesNotContain(remainingPayments, p => p.Transaction == "TX_DELETE");
        }

        [Fact]
        public async Task FulfillPaymentAsync_ReturnsError_WhenAmountIsNegativeOrZero()
        {
            using var db = CreateDb(nameof(FulfillPaymentAsync_ReturnsError_WhenAmountIsNegativeOrZero));
            var service = CreateService(db);

            var request1 = new PaymentsDto { Transaction = "TX1", Amount = 0 };
            var request2 = new PaymentsDto { Transaction = "TX2", Amount = -5.00m };

            var result1 = await service.FulfillPaymentAsync(Guid.NewGuid(), request1);
            var result2 = await service.FulfillPaymentAsync(Guid.NewGuid(), request2);

            Assert.Equal(400, result1.status);
            Assert.Equal(400, result2.status);
            Assert.Equal("Amount must be a positive number.", result1.error);
            Assert.Equal("Amount must be a positive number.", result2.error);
        }

        [Fact]
        public async Task FulfillPaymentAsync_UpdatesSessionPaymentStatus_WhenSuccessful()
        {
            using var db = CreateDb(nameof(FulfillPaymentAsync_UpdatesSessionPaymentStatus_WhenSuccessful));
            var service = CreateService(db);

            var userId = Guid.NewGuid();
            var sessionId = Guid.NewGuid();
            var session = CreateBaseSession(userId, sessionId);
            var payment = CreateBasePayment(userId, "TX_SESSION", 5.00m, sessionId: sessionId);

            db.ParkingLots.Add(new ParkingLot { Id = 1, Name = "Test Lot", Location = "Test Location", Address = "123 Main St", Capacity = 50, Tariff = 5.0, DayTariff = 25.0, Coordinates = "{}" });
            db.Sessions.Add(session);
            db.Payments.Add(payment);
            await db.SaveChangesAsync();

            var request = new PaymentsDto { Transaction = "TX_SESSION", Amount = 5.00m };
            var result = await service.FulfillPaymentAsync(userId, request);

            Assert.NotNull(result.dto);
            var updatedSession = await db.Sessions.FindAsync(sessionId);
            Assert.Equal(PaymentStatuses.Paid, updatedSession!.PaymentStatus);
        }
    }
}