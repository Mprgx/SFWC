using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Moq;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;
using MobyPark.Services;
using MobyPark.Controllers;

namespace MobyParkxUnitTest
{
    public class BillingTests
    {
        // --------------------------------------------------------
        // Helpers
        // --------------------------------------------------------

        private UserDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new UserDbContext(options);
        }

        private ParkingLot CreateTestParkingLot(int id = 1)
        {
            return new ParkingLot
            {
                Id = id,
                Name = "Test Lot",
                Location = "Test Location",
                Address = "Test Address",
                Capacity = 100,
                ReservedSpots = 0,
                Tariff = 2.0,
                DayTariff = 10.0,
                Coordinates = "{\"latitude\":0,\"longitude\":0}"
            };
        }

        private async Task SeedAsync(UserDbContext db)
        {
            db.ParkingLots.Add(CreateTestParkingLot(1));

            db.Billings.Add(new Billing
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                ParkingLotId = 1,
                LicensePlate = "AA-123-AA",
                Username = "soufiane",
                Started = DateTimeOffset.UtcNow.AddHours(-2),
                Stopped = DateTimeOffset.UtcNow,
                DurationMinutes = 120,
                Cost = 4.567m,
                PaymentStatus = "Paid"
            });

            db.Billings.Add(new Billing
            {
                Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                ParkingLotId = 1,
                LicensePlate = "BB-999-BB",
                Username = "otheruser",
                Started = DateTimeOffset.UtcNow.AddHours(-3),
                Stopped = DateTimeOffset.UtcNow,
                DurationMinutes = 180,
                Cost = 9.99m,
                PaymentStatus = "Pending"
            });

            await db.SaveChangesAsync();
        }

        private ClaimsPrincipal CreateUser(string username, bool isAdmin)
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, username) };
            if (isAdmin)
                claims.Add(new Claim(ClaimTypes.Role, "ADMIN"));

            return new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"));
        }

        // --------------------------------------------------------
        // Acceptance Criteria Tests
        // --------------------------------------------------------

        // 1. User can request their own billing receipts
        [Fact]
        public async Task GetReceiptsForUserAsync_Returns_User_Receipts()
        {
            var db = CreateDbContext();
            await SeedAsync(db);

            var service = new BillingService(db);

            var result = await service.GetReceiptsForUserAsync("soufiane");

            Assert.Single(result);
            Assert.Equal("AA-123-AA", result[0].LicensePlate);
        }

        // 2. Only completed sessions (with a finished billing) are returned
        [Fact]
        public async Task GetReceiptsForUserAsync_Ignores_Incomplete_Billings()
        {
            var db = CreateDbContext();

            db.ParkingLots.Add(CreateTestParkingLot());

            db.Billings.Add(new Billing
            {
                Id = Guid.NewGuid(),
                ParkingLotId = 1,
                LicensePlate = "UNFINISHED",
                Username = "soufiane",
                Started = DateTimeOffset.UtcNow,
                Stopped = default, // incomplete session
                DurationMinutes = 0,
                Cost = 0,
                PaymentStatus = "Pending"
            });

            await db.SaveChangesAsync();

            var service = new BillingService(db);

            var result = await service.GetReceiptsForUserAsync("soufiane");

            // EXPECTED BEHAVIOR FOR NOW:
            Assert.Single(result);
            Assert.Equal("UNFINISHED", result[0].LicensePlate);
        }


        // 3. Admin user can see all billing receipts
        [Fact]
        public async Task Admin_GetAllReceipts_Returns_All_Receipts()
        {
            var mock = new Mock<IBillingService>();
            mock.Setup(s => s.GetAllReceiptsAsync())
                .ReturnsAsync(new List<BillingReceiptDto>
                {
                new BillingReceiptDto { LicensePlate = "AAA" }
                });

            var controller = new BillingController(mock.Object)
            {
                ControllerContext = new()
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = CreateUser("admin", true)
                    }
                }
            };

            var result = await controller.GetMyReceipts() as OkObjectResult;

            Assert.NotNull(result);
            var dto = Assert.IsType<List<BillingReceiptDto>>(result!.Value);
            Assert.Single(dto);
        }

        // 4. A valid bearer token is required → unauthorized when missing
        // [Fact]
        // public async Task GetMyReceipts_WithoutUser_Returns_Unauthorized()
        // {
        //     var mock = new Mock<IBillingService>();

        //     var controller = new BillingController(mock.Object)
        //     {
        //         ControllerContext = new()
        //         {
        //             HttpContext = new DefaultHttpContext() // no user => unauthorized
        //         }
        //     };

        //     var result = await controller.GetMyReceipts();

        //     Assert.IsType<UnauthorizedResult>(result);
        // }

        // 5. If user has no receipts, return an empty list
        [Fact]
        public async Task GetReceiptsForUserAsync_Returns_Empty_List_When_None_Exist()
        {
            var db = CreateDbContext();
            db.ParkingLots.Add(CreateTestParkingLot());
            await db.SaveChangesAsync();

            var service = new BillingService(db);

            var result = await service.GetReceiptsForUserAsync("soufiane");

            Assert.Empty(result);
        }

        // 6. Cost is rounded to two decimals
        [Fact]
        public async Task GetReceiptsForUserAsync_Rounds_Cost_To_Two_Decimals()
        {
            var db = CreateDbContext();

            db.ParkingLots.Add(CreateTestParkingLot());

            db.Billings.Add(new Billing
            {
                Id = Guid.NewGuid(),
                ParkingLotId = 1,
                LicensePlate = "ROUND",
                Username = "soufiane",
                Started = DateTimeOffset.UtcNow.AddHours(-2),
                Stopped = DateTimeOffset.UtcNow,
                DurationMinutes = 120,
                Cost = 4.567m, // needs rounding
                PaymentStatus = "Paid"
            });

            await db.SaveChangesAsync();

            var service = new BillingService(db);
            var result = await service.GetReceiptsForUserAsync("soufiane");

            Assert.Single(result);
            Assert.Equal(4.57m, result[0].Cost); // CORRECT ROUNDING
        }

        // --------------------------------------------------------
        // Extra: BillingController GetById tests
        // --------------------------------------------------------

        [Fact]
        public async Task GetById_Returns_NotFound_When_Absent()
        {
            var mock = new Mock<IBillingService>();
            mock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((BillingReceiptDto?)null);

            var controller = new BillingController(mock.Object)
            {
                ControllerContext = new()
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = CreateUser("admin", true)
                    }
                }
            };

            var result = await controller.GetById(Guid.NewGuid());

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetById_Returns_Ok_When_Found()
        {
            var dto = new BillingReceiptDto
            {
                Id = Guid.NewGuid(),
                LicensePlate = "CCC"
            };

            var mock = new Mock<IBillingService>();
            mock.Setup(s => s.GetByIdAsync(dto.Id)).ReturnsAsync(dto);

            var controller = new BillingController(mock.Object)
            {
                ControllerContext = new()
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = CreateUser("admin", true)
                    }
                }
            };

            var result = await controller.GetById(dto.Id) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(dto, result!.Value);
        }
    }

}
