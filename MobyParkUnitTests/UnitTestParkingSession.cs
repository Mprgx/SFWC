using System;
using System.Collections.Generic;
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

    [Fact]
    public async Task GetByIdAsync_Returns_Lot_IfExists()
    {
        var db = CreateDbContext();
        var lot = new ParkingLot { Name = "Main" };
        db.ParkingLots.Add(lot);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetByIdAsync(lot.Id);

        Assert.NotNull(result);
        Assert.Equal("Main", result!.Name);
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
        var session = new Session
        {
            ParkingLotId = 1,
            Id = Guid.NewGuid(),
            User = new User { Username = "bob" }
        };

        db.ParkingLots.Add(new ParkingLot { Id = 1 });
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
        var session = new Session
        {
            ParkingLotId = 1,
            Id = Guid.NewGuid(),
            User = new User { Username = "alice" }
        };

        db.ParkingLots.Add(new ParkingLot { Id = 1 });
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
        var user = new User { Id = Guid.NewGuid(), Username = "bob" };

        db.Users.Add(user);
        db.ParkingLots.Add(new ParkingLot { Id = 1 });

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
        var payment = await service.StopSessionAsync("ABC123", user.Id, encryption.Object);

        Assert.NotNull(payment);
        Assert.True(payment!.Amount > 0);
    }

    [Fact]
    public async Task StopSessionAsync_ReturnsNull_When_User_Is_Not_Owner()
    {
        var db = CreateDbContext();
        var user1 = new User { Id = Guid.NewGuid() };
        var user2 = new User { Id = Guid.NewGuid() };

        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(e => e.Decrypt(It.IsAny<string>())).Returns("PLATE");

        db.Sessions.Add(new Session
        {
            UserId = user1.Id,
            LicensePlate = "encrypted",
            Started = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.StopSessionAsync("PLATE", user2.Id, encryption.Object);

        Assert.Null(result);
    }

    [Fact]
    public async Task StopSessionAsync_ReturnsNull_When_Plate_Not_Found()
    {
        var db = CreateDbContext();

        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(e => e.Decrypt(It.IsAny<string>())).Returns("OTHER");

        var service = CreateService(db);
        var result = await service.StopSessionAsync("TARGET", Guid.NewGuid(), encryption.Object);

        Assert.Null(result);
    }
}
