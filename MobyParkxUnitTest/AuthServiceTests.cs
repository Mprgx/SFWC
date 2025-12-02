using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;
using MobyPark.Services;
using Xunit;

namespace MobyParkxUnitTest
{
    public class AuthServiceTests
    {

        private static UserDbContext CreateDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new UserDbContext(options);
        }

        private static IConfiguration CreateConfiguration()
        {
            var settings = new Dictionary<string, string?>
            {
                ["AppSettings:Token"] = "super-secret-test-key-super-secret-test-key-123456",
                ["AppSettings:Issuer"] = "test-issuer",
                ["AppSettings:Audience"] = "test-audience"
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();
        }

        private class FakeEncryptionService : IEncryptionService
        {
            public string? Encrypt(string? plaintext)
            {
                return plaintext is null ? null : $"ENC:{plaintext}";
            }

            public string? Decrypt(string? ciphertext)
            {
                if (ciphertext is null)
                    return null;

                const string prefix = "ENC:";
                if (ciphertext.StartsWith(prefix, StringComparison.Ordinal))
                    return ciphertext[prefix.Length..];

                return ciphertext;
            }
        }

        private static AuthService CreateService(UserDbContext context, IEncryptionService? encryption = null)
        {
            var config = CreateConfiguration();
            return new AuthService(context, config, encryption ?? new FakeEncryptionService());
        }

        [Fact]
        public async Task RegisterAsync_CreatesUser_WhenDataIsValid()
        {
            using var context = CreateDbContext(nameof(RegisterAsync_CreatesUser_WhenDataIsValid));
            var service = CreateService(context);

            var request = new RegisterRequestDto
            {
                Username = "  TestUser  ",
                Password = "StrongP@ssword1!",
                Name = "  Sharad  ",
                Email = "SharadRandjitsing@gmail.com",
                PhoneNumber = "0639659824",
                BirthYear = 2002
            };

            var result = await service.RegisterAsync(request);

            Assert.NotNull(result);
            Assert.Equal("testuser", result.Username);
            Assert.Equal("Sharad", result.Name);
            Assert.Equal("sharadrandjitsing@gmail.com", result.Email);
            Assert.Equal("0639659824", result.PhoneNumber);
            Assert.Equal(2002, result.BirthYear);

            var userInDb = await context.Users.SingleAsync();
            Assert.Equal("testuser", userInDb.Username);
            Assert.Equal("Sharad", userInDb.Name);
            Assert.False(string.IsNullOrWhiteSpace(userInDb.PasswordHash));
        }

        [Fact]
        public async Task RegisterAsync_ReturnsNull_WhenUsernameAlreadyExists()
        {
            using var context = CreateDbContext(nameof(RegisterAsync_ReturnsNull_WhenUsernameAlreadyExists));
            var service = CreateService(context);

            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "existinguser",
                Name = "Existing User",
                Email = string.Empty,
                PhoneNumber = string.Empty,
                BirthYear = 2000,
                CreatedAt = DateTimeOffset.UtcNow,
                Role = UserRole.Customer,
                PasswordHash = "hash"
            };
            context.Users.Add(existingUser);
            await context.SaveChangesAsync();

            var request = new RegisterRequestDto
            {
                Username = " ExistingUser ",
                Password = "StrongP@ssword1!",
                Name = " New User",
                Email = "newuser@gmail.com",
                PhoneNumber = "0611111111",
                BirthYear = 2001
            };

            var result = await service.RegisterAsync(request);

            Assert.Null(result);
            Assert.Equal(1, await context.Users.CountAsync());
        }

        [Fact]
        public async Task RegisterAsync_ReturnsNull_WhenEmailAlreadyTaken()
        {
            using var context = CreateDbContext(nameof(RegisterAsync_ReturnsNull_WhenEmailAlreadyTaken));
            var encryption = new FakeEncryptionService();
            var config = CreateConfiguration();
            var service = new AuthService(context, config, encryption);

            var existingEmailPlain = "duplicateemail@gmail.com";

            var existingUseer = new User
            {
                Id = Guid.NewGuid(),
                Username = "user1",
                Name = "User One",
                Email = encryption.Encrypt(existingEmailPlain)!,
                PhoneNumber = encryption.Encrypt("0600000000")!,
                BirthYear = 1995,
                CreatedAt = DateTimeOffset.UtcNow,
                Role = UserRole.Customer,
                PasswordHash = "hash"
            };
            context.Users.Add(existingUseer);
            await context.SaveChangesAsync();

            var request = new RegisterRequestDto
            {
                Username = "user2",
                Password = "StronmgP@ssword1!",
                Name = "User Two",
                Email = "  DUPLICATEemail@gmail.com  ",
                PhoneNumber = "0611111111",
                BirthYear = 1998
            };

            var result = await service.RegisterAsync(request);

            Assert.Null(result);
            Assert.Equal(1, await context.Users.CountAsync());
        }
    }
}
