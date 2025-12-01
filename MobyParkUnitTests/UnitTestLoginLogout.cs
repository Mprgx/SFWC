using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using MobyPark.Controllers;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;
using MobyPark.Services;

public class AuthTests
{
    private UserDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<UserDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new UserDbContext(options);
    }

    private IConfiguration CreateConfig()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"AppSettings:Token", "THIS_IS_A_TEST_SECRET_KEY_FOR_UNIT_TESTS"},
                {"AppSettings:Issuer", "test"},
                {"AppSettings:Audience", "test"}
            })
            .Build();
    }

    private Mock<IEncryptionService> CreateEncryption()
    {
        var mock = new Mock<IEncryptionService>();
        mock.Setup(x => x.Encrypt(It.IsAny<string>())).Returns<string>(x => $"ENC:{x}");
        mock.Setup(x => x.Decrypt(It.IsAny<string>())).Returns<string>(x => x.Replace("ENC:", ""));
        return mock;
    }

    [Fact]
    public async Task RegisterAsync_Creates_User()
    {
        var db = CreateDb();
        var service = new AuthService(db, CreateConfig(), CreateEncryption().Object);

        var result = await service.RegisterAsync(new RegisterRequestDto
        {
            Username = "john",
            Name = "John",
            Password = "123456",
            Email = "john@test.com"
        });

        Assert.NotNull(result);
        Assert.Equal("john", result.Username);
        Assert.Single(db.Users);
    }

    [Fact]
    public async Task RegisterAsync_Duplicate_Username_Returns_Null()
    {
        var db = CreateDb();
        var service = new AuthService(db, CreateConfig(), CreateEncryption().Object);

        await service.RegisterAsync(new RegisterRequestDto
        {
            Username = "john",
            Name = "John",
            Password = "pw"
        });

        var result = await service.RegisterAsync(new RegisterRequestDto
        {
            Username = "john",
            Name = "Dup",
            Password = "pw"
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task RegisterAsync_Duplicate_Email_Returns_Null()
    {
        var db = CreateDb();
        var enc = CreateEncryption();
        var service = new AuthService(db, CreateConfig(), enc.Object);

        await service.RegisterAsync(new RegisterRequestDto
        {
            Username = "a",
            Name = "A",
            Password = "x",
            Email = "mail@test.com"
        });

        var result = await service.RegisterAsync(new RegisterRequestDto
        {
            Username = "b",
            Name = "B",
            Password = "x",
            Email = "mail@test.com"
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_Valid_Password_Returns_Token()
    {
        var db = CreateDb();
        var service = new AuthService(db, CreateConfig(), CreateEncryption().Object);

        await service.RegisterAsync(new RegisterRequestDto
        {
            Username = "bob",
            Name = "Bob",
            Password = "pw"
        });

        var result = await service.LoginAsync(new LoginRequestDto
        {
            Username = "bob",
            Password = "pw"
        });

        Assert.NotNull(result);
        Assert.NotEmpty(result.AccessToken);
        Assert.NotEmpty(result.RefreshToken);
    }

    [Fact]
    public async Task LogoutAsync_Clears_Refresh_Token()
    {
        var db = CreateDb();
        var service = new AuthService(db, CreateConfig(), CreateEncryption().Object);

        var dto = await service.RegisterAsync(new RegisterRequestDto
        {
            Username = "tom",
            Name = "Tom",
            Password = "pw"
        });

        var login = await service.LoginAsync(new LoginRequestDto { Username = "tom", Password = "pw" });

        var user = await db.Users.FirstAsync();
        Assert.NotNull(user.RefreshToken);

        await service.LogoutAsync(user.Id);

        user = await db.Users.FirstAsync();
        Assert.Null(user.RefreshToken);
    }


    [Fact]
    public async Task Register_Invalid_BirthYear_Returns_BadRequest()
    {
        var mock = new Mock<IAuthService>();
        var controller = new AuthController(mock.Object);

        var result = await controller.Register(new RegisterRequestDto
        {
            Username = "x",
            Name = "x",
            Password = "x",
            BirthYear = 1800
        });

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("BirthYear must be 0 (unset) or between 1900 and 2030.", bad.Value);
    }

    [Fact]
    public async Task Logout_Missing_Claim_Returns_Unauthorized()
    {
        var mock = new Mock<IAuthService>();
        var controller = new AuthController(mock.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await controller.Logout();

        Assert.IsType<UnauthorizedResult>(result);
    }
}
