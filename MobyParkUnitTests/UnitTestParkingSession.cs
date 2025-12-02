using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Services;
using Moq;
using Xunit;

public class ParkingLotServiceTests
{
    private UserDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<UserDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new UserDbContext(options);
    }

    private ParkingLotService CreateService(UserDbContext db)
    {
        return new ParkingLotService(db);
    }

    // Helper to create a fully initialized User
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

    // Helper to create a fully initialized ParkingLot
    private ParkingLot CreateTestParkingLot(int id = 1)
    {
        return new ParkingLot
        {
            Id = id,
            Name = $"Lot{id}",
            Location = $"Location{id}",
            Address = $"Address{id}",
            Capacity = 100,
            Tariff = 2.0,
            DayTariff = 10.0,
            Coordinates = "1.0.0.-0"
        };
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Lot_IfExists()
    {
        var db = CreateDbContext();
        var lot = CreateTestParkingLot(1);
        db.ParkingLots.Add(lot);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetByIdAsync(lot.Id);

        Assert.NotNull(result);
        Assert.Equal(lot.Name, result!.Name);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Null_IfNotExists()
    {
        var service = CreateService(CreateDbContext());
        var result = await service.GetByIdAsync(999);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetSessionsAsync_Returns_Empty_If_ParkingLot_NotFound()
    {
        var service = CreateService(CreateDbContext());
        var result = await service.GetSessionsAsync(777, "bob", false);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetSessionByIdAsync_Returns_Session_For_Admin()
    {
        var db = CreateDbContext();
        var user = CreateTestUser("bob");
        var session = new Session
        {
            ParkingLotId = 1,
            Id = Guid.NewGuid(),
            User = user,
            UserId = user.Id
        };

        db.ParkingLots.Add(CreateTestParkingLot(1));
        db.Sessions.Add(session);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetSessionByIdAsync(1, session.Id.ToString(), null, true);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetSessionByIdAsync_Returns_Null_When_NotOwner()
    {
        var db = CreateDbContext();
        var sessionUser = CreateTestUser("alice");
        var session = new Session
        {
            ParkingLotId = 1,
            Id = Guid.NewGuid(),
            User = sessionUser,
            UserId = sessionUser.Id
        };

        db.ParkingLots.Add(CreateTestParkingLot(1));
        db.Sessions.Add(session);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetSessionByIdAsync(1, session.Id.ToString(), "bob", false);

        Assert.Null(result);
    }

    [Fact]
    public async Task StopSessionAsync_Stops_Session_And_Creates_Payment()
    {
        var db = CreateDbContext();
        var user = CreateTestUser("bob");

        db.Users.Add(user);
        db.ParkingLots.Add(CreateTestParkingLot(1));

        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(e => e.Decrypt(It.IsAny<string>())).Returns("ABC123");

        db.Sessions.Add(new Session
        {
            UserId = user.Id,
            User = user,
            ParkingLotId = 1,
            LicensePlate = "encrypted",
            Started = DateTimeOffset.UtcNow.AddHours(-2)
        });

        await db.SaveChangesAsync();

        var service = CreateService(db);
        var payment = await service.StopSessionAsync("ABC123", user.Username, user.Id, encryption.Object);

        Assert.NotNull(payment);
        Assert.True(payment!.Amount > 0);
        Assert.Equal("bob", payment.Initiator);
        Assert.NotNull(payment.Session);
        Assert.NotNull(payment.User);
    }

    [Fact]
    public async Task StopSessionAsync_ReturnsNull_When_User_Is_Not_Owner()
    {
        var db = CreateDbContext();
        var user1 = CreateTestUser("alice");
        var user2 = CreateTestUser("bob");

        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(e => e.Decrypt(It.IsAny<string>())).Returns("PLATE");

        db.Sessions.Add(new Session
        {
            UserId = user1.Id,
            User = user1,
            LicensePlate = "encrypted",
            Started = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.StopSessionAsync("PLATE", user2.Username, user2.Id, encryption.Object);

        Assert.Null(result);
    }

    [Fact]
    public async Task StopSessionAsync_ReturnsNull_When_Plate_Not_Found()
    {
        var db = CreateDbContext();

        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(e => e.Decrypt(It.IsAny<string>())).Returns("OTHER");

        var service = CreateService(db);
        var result = await service.StopSessionAsync("TARGET", "anyone", Guid.NewGuid(), encryption.Object);

        Assert.Null(result);
    }
}
