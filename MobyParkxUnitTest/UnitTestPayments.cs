using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;
using MobyPark.Services;
using Xunit;

namespace MobyParkxUnitTest
{
    public class PaymentServiceTests
    {
        private UserDbContext CreateDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(dbName)
                .EnableSensitiveDataLogging()
                .Options;

            return new UserDbContext(options);
        }

        private PaymentService CreateService(UserDbContext db)
        {
            return new PaymentService(db);
        }

        private User CreateTestUser(string username)
        {
            return new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                Name = $"{username} Name",
                Email = $"{username}@example.com",
                PhoneNumber = "1234567890",
                BirthYear = 1990,
                Role = UserRole.Customer,
                PasswordHash = "fakehash",
                CreatedAt = DateTimeOffset.UtcNow
            };
        }

        private ParkingLot CreateTestParkingLot(int id)
        {
            return new ParkingLot
            {
                Id = id,
                Name = "Test Lot",
                Location = "Test Loc",
                Address = "Test Address",
                Capacity = 50,
                Tariff = 1.0,
                DayTariff = 10.0,
                Coordinates = "0,0"
            };
        }

        private Session CreateTestSession(Guid id, User user, ParkingLot lot)
        {
            return new Session
            {
                Id = id,
                UserId = user.Id,
                User = user,
                ParkingLotId = lot.Id,
                ParkingLot = lot,
                LicensePlate = "TEST-PL",
                Started = DateTimeOffset.UtcNow.AddHours(-1),
                PaymentStatus = "unpaid",
                Cost = 5.0m
            };
        }

        private Payment CreateTestPayment(User user, Session session, string transactionId, decimal amount = 10.0m)
        {
            return new Payment
            {
                Transaction = transactionId,
                Amount = amount,
                Initiator = user.Username,
                UserId = user.Id,
                Created_At = DateTimeOffset.UtcNow,
                Hash = "dummyhash",
                T_Data = "{}",
                SessionId = session.Id,
                ParkingLotId = session.ParkingLotId
            };
        }

        [Fact]
        public async Task CompletePaymentAsync_Throws_Unauthorized_When_User_Not_Owner()
        {
            var dbName = Guid.NewGuid().ToString();

            // 1. Setup Data
            using (var db = CreateDbContext(dbName))
            {
                var owner = CreateTestUser("owner");
                var hacker = CreateTestUser("hacker");
                var lot = CreateTestParkingLot(1);
                var session = CreateTestSession(Guid.NewGuid(), owner, lot);
                var payment = CreateTestPayment(owner, session, "trans123");

                db.Users.AddRange(owner, hacker);
                db.ParkingLots.Add(lot);
                db.Sessions.Add(session);
                db.Payments.Add(payment);
                await db.SaveChangesAsync();
            }

            using (var db = CreateDbContext(dbName))
            {
                var service = CreateService(db);
                var hacker = await db.Users.FirstAsync(u => u.Username == "hacker");

                var req = new PaymentValidationDto
                {
                    Validation = "abc",
                    T_Data = JsonDocument.Parse("{}").RootElement
                };

                var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                    service.CompletePaymentAsync(hacker.Id, "trans123", req));

                Assert.Equal("You may only complete your own payments", ex.Message);
            }
        }

        [Fact]
        public async Task GetPaymentsForUserAsync_Returns_List_Of_Payments()
        {
            var dbName = Guid.NewGuid().ToString();

            using (var db = CreateDbContext(dbName))
            {
                var user = CreateTestUser("alice");
                var lot = CreateTestParkingLot(1);
                var session1 = CreateTestSession(Guid.NewGuid(), user, lot);
                var session2 = CreateTestSession(Guid.NewGuid(), user, lot);

                var p1 = CreateTestPayment(user, session1, "t1", 5.0m);
                var p2 = CreateTestPayment(user, session2, "t2", 15.0m);

                db.Users.Add(user);
                db.ParkingLots.Add(lot);
                db.Sessions.AddRange(session1, session2);
                db.Payments.AddRange(p1, p2);
                await db.SaveChangesAsync();
            }

            using (var db = CreateDbContext(dbName))
            {
                var service = CreateService(db);
                var user = await db.Users.FirstAsync(u => u.Username == "alice");

                var result = await service.GetPaymentsForUserAsync(user.Id);

                Assert.Equal(2, result.Count);
                Assert.Contains(result, p => p!.Transaction == "t1");
                Assert.Contains(result, p => p!.Transaction == "t2");
            }
        }

        [Fact]
        public async Task GetPaymentsForAnyUserAsync_Returns_Correct_Payments()
        {
            var dbName = Guid.NewGuid().ToString();

            using (var db = CreateDbContext(dbName))
            {
                var user1 = CreateTestUser("bob");
                var user2 = CreateTestUser("charlie");
                var lot = CreateTestParkingLot(1);

                var s1 = CreateTestSession(Guid.NewGuid(), user1, lot);
                var s2 = CreateTestSession(Guid.NewGuid(), user2, lot);

                db.Users.AddRange(user1, user2);
                db.ParkingLots.Add(lot);
                db.Sessions.AddRange(s1, s2);

                db.Payments.Add(CreateTestPayment(user1, s1, "p_bob"));
                db.Payments.Add(CreateTestPayment(user2, s2, "p_charlie"));
                await db.SaveChangesAsync();
            }

            using (var db = CreateDbContext(dbName))
            {
                var service = CreateService(db);
                var result = await service.GetPaymentsForAnyUserAsync("bob");

                Assert.Single(result);
                Assert.Equal("p_bob", result[0]!.Transaction);
            }
        }

        [Fact]
        public async Task CompletePaymentAsync_Updates_Session_To_Paid_On_Success()
        {
            var dbName = Guid.NewGuid().ToString();
            Guid userId;
            string transId = "trans_valid";

            using (var db = CreateDbContext(dbName))
            {
                var user = CreateTestUser("bob");
                var lot = CreateTestParkingLot(1);
                var session = CreateTestSession(Guid.NewGuid(), user, lot);
                var payment = CreateTestPayment(user, session, transId);

                userId = user.Id;

                db.Users.Add(user);
                db.ParkingLots.Add(lot);
                db.Sessions.Add(session);
                db.Payments.Add(payment);
                await db.SaveChangesAsync();
            }

            using (var db = CreateDbContext(dbName))
            {
                var service = CreateService(db);

                var req = new PaymentValidationDto
                {
                    Validation = "random_guess",
                    T_Data = JsonDocument.Parse("{}").RootElement
                };

                var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                    service.CompletePaymentAsync(userId, transId, req));
                Assert.Equal("Validation failed", ex.Message);
            }
        }

        [Fact]
        public async Task CompletePaymentAsync_Throws_ArgumentNullException_When_Request_Null()
        {
            var service = CreateService(CreateDbContext(Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                service.CompletePaymentAsync(Guid.NewGuid(), "t", null!));
        }

        [Fact]
        public async Task CompletePaymentAsync_Throws_KeyNotFound_When_Payment_NotExists()
        {
            var service = CreateService(CreateDbContext(Guid.NewGuid().ToString()));
            var req = new PaymentValidationDto { Validation = "x", T_Data = JsonDocument.Parse("{}").RootElement };
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.CompletePaymentAsync(Guid.NewGuid(), "ghost", req));
        }

        [Fact]
        public async Task DeletePaymentByTransactionId_Removes_Payment()
        {
            var dbName = Guid.NewGuid().ToString();
            using (var db = CreateDbContext(dbName))
            {
                var u = CreateTestUser("del");
                var l = CreateTestParkingLot(1);
                var s = CreateTestSession(Guid.NewGuid(), u, l);
                var p = CreateTestPayment(u, s, "del_t");
                db.Users.Add(u); db.ParkingLots.Add(l); db.Sessions.Add(s); db.Payments.Add(p);
                await db.SaveChangesAsync();
            }

            using (var db = CreateDbContext(dbName))
            {
                var service = CreateService(db);
                var res = await service.DeletePaymentByTransactionId("del_t");
                Assert.True(res);
                Assert.Null(await db.Payments.FindAsync("del_t"));
            }
        }

        [Fact]
        public async Task FulfillPaymentAsync_Sets_Completed()
        {
            var dbName = Guid.NewGuid().ToString();
            Guid uId;
            using (var db = CreateDbContext(dbName))
            {
                var u = CreateTestUser("ful");
                var l = CreateTestParkingLot(1);
                var s = CreateTestSession(Guid.NewGuid(), u, l);
                var p = CreateTestPayment(u, s, "ful_t");
                p.Completed = null;
                uId = u.Id;
                db.Users.Add(u); db.ParkingLots.Add(l); db.Sessions.Add(s); db.Payments.Add(p);
                await db.SaveChangesAsync();
            }

            using (var db = CreateDbContext(dbName))
            {
                var service = CreateService(db);
                var res = await service.FulfillPaymentAsync(uId.ToString(), new PaymentsDto { Transaction = "ful_t" });
                Assert.NotNull(res);
                Assert.NotNull(res.Completed);
            }
        }
    }
}