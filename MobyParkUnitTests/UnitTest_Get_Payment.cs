using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Services;
using MobyPark.Models;
using Xunit;

namespace MobyParkUnitTests
{
    public class GetPaymentServiceTests
    {
        private UserDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // uniek voor elke test
                .Options;

            return new UserDbContext(options);
        }

        // --------------------------------------------------------------------
        // TEST 1 — GetPaymentsForUserAsync: geeft alleen payments van ingelogde gebruiker
        // --------------------------------------------------------------------
        [Fact]
        public async Task GetPaymentsForUserAsync_ShouldReturnUserPayments()
        {
            // Arrange
            var context = CreateDbContext();
            var service = new PaymentService(context);

            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();

            context.Users.Add(new User { Id = userId, Username = "john" });
            context.Users.Add(new User { Id = otherUserId, Username = "alice" });

            context.Payments.Add(new Payment
            {
                Id = Guid.NewGuid(),
                Transaction = "T1",
                Amount = 10,
                Initiator = userId,
                Completed = false,
                Hash = ""
            });

            context.Payments.Add(new Payment
            {
                Id = Guid.NewGuid(),
                Transaction = "T2",
                Amount = 20,
                Initiator = otherUserId,
                Completed = true,
                Hash = ""
            });

            await context.SaveChangesAsync();

            // Act
            var result = await service.GetPaymentsForUserAsync(userId);

            // Assert
            Assert.Single(result);
            Assert.Equal("T1", result[0].Transaction);
        }

        // --------------------------------------------------------------------
        // TEST 2 — GetPaymentsForUserAsync: gebruiker heeft geen payments
        // --------------------------------------------------------------------
        [Fact]
        public async Task GetPaymentsForUserAsync_ShouldReturnEmptyList_WhenNoPaymentsExist()
        {
            // Arrange
            var context = CreateDbContext();
            var service = new PaymentService(context);

            var userId = Guid.NewGuid();

            context.Users.Add(new User { Id = userId, Username = "john" });
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetPaymentsForUserAsync(userId);

            // Assert
            Assert.Empty(result);
        }

        // --------------------------------------------------------------------
        // TEST 3 — GetPaymentsForAnyUserAsync: admin haalt payments van andere gebruiker op
        // --------------------------------------------------------------------
        [Fact]
        public async Task GetPaymentsForAnyUserAsync_ShouldReturnPaymentsForGivenUsername()
        {
            // Arrange
            var context = CreateDbContext();
            var service = new PaymentService(context);

            var userId = Guid.NewGuid();

            context.Users.Add(new User { Id = userId, Username = "john" });

            context.Payments.Add(new Payment
            {
                Id = Guid.NewGuid(),
                Transaction = "TX100",
                Amount = 50,
                Initiator = userId,
                Completed = true,
                Hash = "xyz"
            });

            await context.SaveChangesAsync();

            // Act
            var result = await service.GetPaymentsForAnyUserAsync("john");

            // Assert
            Assert.Single(result);
            Assert.Equal("TX100", result[0].Transaction);
        }

        // --------------------------------------------------------------------
        // TEST 4 — GetPaymentsForAnyUserAsync: user bestaat niet → empty list
        // --------------------------------------------------------------------
        [Fact]
        public async Task GetPaymentsForAnyUserAsync_ShouldReturnEmptyList_WhenUserDoesNotExist()
        {
            // Arrange
            var context = CreateDbContext();
            var service = new PaymentService(context);

            // geen users toegevoegd

            // Act
            var result = await service.GetPaymentsForAnyUserAsync("unknown");

            // Assert
            Assert.Empty(result);
        }

        // --------------------------------------------------------------------
        // TEST 5 — GetPaymentsForAnyUserAsync: gebruiker bestaat wel maar geen payments
        // --------------------------------------------------------------------
        [Fact]
        public async Task GetPaymentsForAnyUserAsync_ShouldReturnEmptyList_WhenUserHasNoPayments()
        {
            // Arrange
            var context = CreateDbContext();
            var service = new PaymentService(context);

            context.Users.Add(new User { Id = Guid.NewGuid(), Username = "john" });
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetPaymentsForAnyUserAsync("john");

            // Assert
            Assert.Empty(result);
        }
    }
}
