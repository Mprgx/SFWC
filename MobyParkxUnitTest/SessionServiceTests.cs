using Microsoft.EntityFrameworkCore;
using MobyPark.Constants;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;
using MobyPark.Services;
using Xunit;

namespace MobyParkxUnitTest
{
    public class SessionServiceTests
    {
        private static UserDbContext CreateDb(string name)
        {
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new UserDbContext(options);
        }

        private class FakeEncryption : IEncryptionService
        {
            public string? Encrypt(string? plaintext) => plaintext;
            public string? Decrypt(string? ciphertext) => ciphertext;
        }

        private class FakeDiscountService : IDiscountService
        {
            public Task<(int statusCode, string message, DiscountReadDto?)> CreateDiscountAsync(DiscountPostDto dto, Guid userId)
            {
                return Task.FromResult((200, "Discount created successfully.", (DiscountReadDto?)null));
            }

            public Task<(int statusCode, string message)> ApplyDiscountAsync(string? discountCode, string transaction, Guid userId)
            {
                return Task.FromResult((200, "Discount applied successfully."));
            }

            public Task<(int statusCode, string message, decimal? amountWithDiscount, string? normalizedCode)> PreviewDiscountAsync(string? discountCode, Guid userId, int parkingLotId, DateTimeOffset atTime, decimal amount)
            {
                return Task.FromResult((statusCode: 200, message: "Preview successful.", amountWithDiscount: (decimal?)amount, normalizedCode: discountCode));
            }
        }

        private static SessionService CreateService(UserDbContext db)
        {
            return new SessionService(db, new FakeEncryption(), new FakeDiscountService());
        }

        private static Vehicle CreateVehicle(Guid userId, int vehicleId = 1, string plate = "TEST-123")
        {
            return new Vehicle
            {
                Id = vehicleId,
                UserId = userId,
                LicensePlate = plate,
                Make = "Tesla",    // Changed from Brand
                Model = "Model 3",
                Color = "Red",
                Year = 2022
            };
        }

        private static Session CreateSession(Guid userId, int vehicleId, string plate = "TEST-123",
            DateTimeOffset? stopped = null, bool isCancelled = false, bool isRefunded = false)
        {
            return new Session
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                VehicleId = vehicleId,
                ParkingLotId = 1,
                LicensePlate = plate,
                Started = DateTimeOffset.UtcNow.AddHours(-1),
                Stopped = stopped,
                IsCancelled = isCancelled,
                IsRefunded = isRefunded,
                PaymentStatus = stopped == null ? "unpaid" : "awaiting_payment",
                Cost = 0
            };
        }

        private static User CreateUser(Guid userId)
        {
            return new User
            {
                Id = userId,
                Username = "TestUser",
                Name = "Test User",
                Email = "test@example.com",
                PhoneNumber = "1234567890",
                BirthYear = 1990,
                PasswordHash = "hashed_password", // Required field
                Role = UserRole.Customer
            };
        }

        [Fact]
        public async Task StartSessionAsync_ReturnsNotFound_WhenVehicleDoesNotExist()
        {
            using var db = CreateDb(nameof(StartSessionAsync_ReturnsNotFound_WhenVehicleDoesNotExist));
            var service = CreateService(db);

            // VehicleId is int
            var result = await service.StartSessionAsync(Guid.NewGuid(), new SessionStartDto { VehicleId = 999 });

            Assert.Null(result.dto);
            Assert.Equal(404, result.status);
            Assert.Equal("Vehicle not found for this user.", result.error);
        }

        [Fact]
        public async Task StartSessionAsync_ReturnsConflict_WhenActiveSessionExists()
        {
            using var db = CreateDb(nameof(StartSessionAsync_ReturnsConflict_WhenActiveSessionExists));
            var service = CreateService(db);
            var userId = Guid.NewGuid();

            // Create vehicle with ID 10
            var vehicle = CreateVehicle(userId, vehicleId: 10);

            // Active session (stopped is null)
            var session = CreateSession(userId, vehicle.Id, stopped: null);

            db.Vehicles.Add(vehicle);
            db.Sessions.Add(session);
            await db.SaveChangesAsync();

            var result = await service.StartSessionAsync(userId, new SessionStartDto { VehicleId = vehicle.Id, ParkingLotId = 1 });

            Assert.Null(result.dto);
            Assert.Equal(409, result.status);
            Assert.Equal("There is already an active session for this vehicle.", result.error);
        }

        [Fact]
        public async Task StartSessionAsync_CreatesSession_WhenValid()
        {
            using var db = CreateDb(nameof(StartSessionAsync_CreatesSession_WhenValid));
            var service = CreateService(db);
            var userId = Guid.NewGuid();
            var vehicle = CreateVehicle(userId, vehicleId: 5);

            db.Vehicles.Add(vehicle);
            await db.SaveChangesAsync();

            var result = await service.StartSessionAsync(userId, new SessionStartDto { VehicleId = vehicle.Id, ParkingLotId = 1 });

            Assert.NotNull(result.dto);
            Assert.Null(result.error);
            Assert.Equal("unpaid", result.dto.PaymentStatus);

            var inDb = await db.Sessions.FirstOrDefaultAsync(s => s.Id == result.dto.Id);
            Assert.NotNull(inDb);
            Assert.Null(inDb.Stopped);
        }

        [Fact]
        public async Task StopSessionByPlateAsync_ReturnsError_WhenPlateInvalid()
        {
            using var db = CreateDb(nameof(StopSessionByPlateAsync_ReturnsError_WhenPlateInvalid));
            var service = CreateService(db);

            // Invalid format (no dashes)
            var result = await service.StopSessionByPlateAsync(Guid.NewGuid(), new SessionStopDto { LicensePlate = "ABC1234" });

            Assert.Null(result.dto);
            Assert.Equal(400, result.status);
            Assert.Equal("License plate format is invalid.", result.error);
        }

        [Fact]
        public async Task StopSessionByPlateAsync_ReturnsNotFound_WhenNoActiveSession()
        {
            using var db = CreateDb(nameof(StopSessionByPlateAsync_ReturnsNotFound_WhenNoActiveSession));
            var service = CreateService(db);
            var userId = Guid.NewGuid();

            // Format is valid, but no session exists
            var result = await service.StopSessionByPlateAsync(userId, new SessionStopDto { LicensePlate = "AA-01-BB" });

            Assert.Null(result.dto);
            Assert.Equal(404, result.status);
        }

        [Fact]
        public async Task StopSessionByPlateAsync_StopsSessionAndCreatesPayment_WhenFound()
        {
            using var db = CreateDb(nameof(StopSessionByPlateAsync_StopsSessionAndCreatesPayment_WhenFound));
            var service = CreateService(db);
            var userId = Guid.NewGuid();
            var plate = "XX-99-YY";

            var user = CreateUser(userId);
            db.Users.Add(user);

            var vehicle = CreateVehicle(userId, 20, plate);
            var session = CreateSession(userId, vehicle.Id, plate, stopped: null);

            db.Vehicles.Add(vehicle);
            db.Sessions.Add(session);
            await db.SaveChangesAsync();

            var result = await service.StopSessionByPlateAsync(userId, new SessionStopDto { LicensePlate = plate });

            Assert.NotNull(result.dto);
            Assert.NotNull(result.dto.Payment);

            var dbSession = await db.Sessions.FindAsync(session.Id);
            Assert.NotNull(dbSession!.Stopped);
            Assert.NotEqual(0, dbSession.Cost);
        }

        [Fact]
        public async Task StopSessionByIdAsync_ReturnsConflict_WhenAlreadyStopped()
        {
            using var db = CreateDb(nameof(StopSessionByIdAsync_ReturnsConflict_WhenAlreadyStopped));
            var service = CreateService(db);
            var userId = Guid.NewGuid();

            var user = CreateUser(userId);
            db.Users.Add(user);

            var session = CreateSession(userId, 1, stopped: DateTimeOffset.UtcNow);
            db.Sessions.Add(session);
            await db.SaveChangesAsync();

            var result = await service.StopSessionByIdAsync(userId, session.Id);
            Assert.Null(result.dto);
            Assert.Equal(409, result.status);
            Assert.Equal("Session is already stopped.", result.error);
        }

        [Fact]
        public async Task StopSessionByIdAsync_ReturnsConflict_WhenCancelled()
        {
            using var db = CreateDb(nameof(StopSessionByIdAsync_ReturnsConflict_WhenCancelled));
            var service = CreateService(db);
            var userId = Guid.NewGuid();

            var user = CreateUser(userId);
            db.Users.Add(user);
            var session = CreateSession(userId, 1, stopped: null, isCancelled: true);
            db.Sessions.Add(session);
            await db.SaveChangesAsync();

            var result = await service.StopSessionByIdAsync(userId, session.Id);

            Assert.Null(result.dto);
            Assert.Equal(409, result.status);
            Assert.Equal("Cancelled sessions cannot be stopped.", result.error);
        }

        [Fact]
        public async Task CancelSessionAsync_ReturnsSuccess_AndMarksStopped()
        {
            using var db = CreateDb(nameof(CancelSessionAsync_ReturnsSuccess_AndMarksStopped));
            var service = CreateService(db);
            var userId = Guid.NewGuid();

            var session = CreateSession(userId, 1, stopped: null); // Active
            db.Sessions.Add(session);
            await db.SaveChangesAsync();

            var result = await service.CancelSessionAsync(userId, session.Id, new CancelSessionDto());

            Assert.NotNull(result.dto);
            Assert.True(result.dto.IsCancelled);

            var dbSession = await db.Sessions.FindAsync(session.Id);
            Assert.True(dbSession!.IsCancelled);
            Assert.NotNull(dbSession.Stopped); // Should stop the session upon cancellation
        }

        [Fact]
        public async Task CancelSessionAsync_ReturnsConflict_WhenAlreadyCancelled()
        {
            using var db = CreateDb(nameof(CancelSessionAsync_ReturnsConflict_WhenAlreadyCancelled));
            var service = CreateService(db);
            var userId = Guid.NewGuid();

            var session = CreateSession(userId, 1, stopped: DateTimeOffset.UtcNow, isCancelled: true);
            db.Sessions.Add(session);
            await db.SaveChangesAsync();

            var result = await service.CancelSessionAsync(userId, session.Id, new CancelSessionDto());

            Assert.Null(result.dto);
            Assert.Equal(409, result.status);
            Assert.Equal("Session is already cancelled.", result.error);
        }

        [Fact]
        public async Task CancelSessionAsync_ReturnsNotFound_WhenSessionDoesNotExist()
        {
            using var db = CreateDb(nameof(CancelSessionAsync_ReturnsNotFound_WhenSessionDoesNotExist));
            var service = CreateService(db);

            var result = await service.CancelSessionAsync(Guid.NewGuid(), Guid.NewGuid(), new CancelSessionDto());

            Assert.Null(result.dto);
            Assert.Equal(404, result.status);
            Assert.Equal("Session not found.", result.error);
        }

        [Fact]
        public async Task DeleteSessionAsync_Deletes_WhenValid()
        {
            using var db = CreateDb(nameof(DeleteSessionAsync_Deletes_WhenValid));
            var service = CreateService(db);
            var userId = Guid.NewGuid();

            db.ParkingLots.Add(new ParkingLot
            {
                Id = 1,
                Name = "Test Lot",
                Location = "Test City",
                Address = "Test Street 1",
                Capacity = 100,
                Tariff = 2.00,
                DayTariff = 15.00,
                Coordinates = "{}"
            });

            var session = CreateSession(userId, 1);
            session.ParkingLotId = 1;
            db.Sessions.Add(session);
            await db.SaveChangesAsync();

            var result = await service.DeleteSessionAsync(1, session.Id);

            Assert.True(result.dto);
            Assert.Null(await db.Sessions.FindAsync(session.Id));
        }

        [Fact]
        public async Task DeleteSessionAsync_ReturnsError_WhenParkingLotIdIsInvalid()
        {
            using var db = CreateDb(nameof(DeleteSessionAsync_ReturnsError_WhenParkingLotIdIsInvalid));
            var service = CreateService(db);

            var result = await service.DeleteSessionAsync(-1, Guid.NewGuid());

            Assert.False(result.dto);
            Assert.Equal(400, result.status);
            Assert.Equal("Parking lot ID must be a positive integer.", result.error);
        }

        [Fact]
        public async Task DeleteSessionAsync_ReturnsNotFound_WhenParkingLotDoesNotExist()
        {
            using var db = CreateDb(nameof(DeleteSessionAsync_ReturnsNotFound_WhenParkingLotDoesNotExist));
            var service = CreateService(db);

            var result = await service.DeleteSessionAsync(999, Guid.NewGuid());

            Assert.False(result.dto);
            Assert.Equal(404, result.status);
            Assert.Equal("Parking lot not found.", result.error);
        }

        [Fact]
        public async Task RequestRefundAsync_ReturnsNull_WhenSessionIsNotCancelled()
        {
            using var db = CreateDb(nameof(RequestRefundAsync_ReturnsNull_WhenSessionIsNotCancelled));
            var service = CreateService(db);
            var userId = Guid.NewGuid();

            var session = CreateSession(userId, 1, stopped: DateTimeOffset.UtcNow, isCancelled: false);
            db.Sessions.Add(session);
            await db.SaveChangesAsync();

            var result = await service.RequestRefundAsync(userId, session.Id, new RefundRequestDto { IBAN = "NL01BANK0000" });

            Assert.Null(result.dto);
            Assert.Equal(409, result.status);
            Assert.Equal("Only cancelled sessions can be refunded.", result.error);
        }

        [Fact]
        public async Task RequestRefundAsync_ReturnsFullRefund_ForShortSession()
        {
            using var db = CreateDb(nameof(RequestRefundAsync_ReturnsFullRefund_ForShortSession));
            var service = CreateService(db);
            var userId = Guid.NewGuid();

            var start = DateTimeOffset.UtcNow.AddMinutes(-5);
            var stop = DateTimeOffset.UtcNow;

            var session = new Session
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                VehicleId = 1,
                ParkingLotId = 1,
                Started = start,
                Stopped = stop,
                IsCancelled = true,
                IsRefunded = false,
                PaymentStatus = "paid"
            };

            db.Sessions.Add(session);
            await db.SaveChangesAsync();

            var result = await service.RequestRefundAsync(userId, session.Id, new RefundRequestDto { IBAN = "NL01" });

            Assert.NotNull(result.dto);
            Assert.Equal(100m, result.dto.Percentage);
            Assert.Equal(0.50m, result.dto.Refunded);
        }

        [Fact]
        public async Task RequestRefundAsync_EnforcesMaxRefundAttempts()
        {
            using var db = CreateDb(nameof(RequestRefundAsync_EnforcesMaxRefundAttempts));
            var service = CreateService(db);

            var userId = Guid.NewGuid();
            for (int i = 0; i < 4; i++)
            {
                db.Sessions.Add(new Session
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    VehicleId = 1,
                    ParkingLotId = 1,
                    Started = DateTimeOffset.UtcNow.AddMinutes(-5),
                    Stopped = DateTimeOffset.UtcNow,
                    IsCancelled = true,
                    IsRefunded = false,
                    PaymentStatus = "paid"
                });
            }
            await db.SaveChangesAsync();

            var sessions = await db.Sessions.Where(s => s.UserId == userId).ToListAsync();
            var dto = new RefundRequestDto { IBAN = "NL01" };

            await service.RequestRefundAsync(userId, sessions[0].Id, dto);
            await service.RequestRefundAsync(userId, sessions[1].Id, dto);
            await service.RequestRefundAsync(userId, sessions[2].Id, dto);

            var result = await service.RequestRefundAsync(userId, sessions[3].Id, dto);

            Assert.Null(result.dto);
            Assert.Equal(429, result.status);
            Assert.Equal("Maximum number of refund attempts reached.", result.error);
        }
    }
}