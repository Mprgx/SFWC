using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;
using MobyPark.Services;

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
                ["AppSettings:Token"] = "v4JQk0W6J3y1mO0eY9q3t2mJgZ3mLh3kq0m4hQH5y1Z3wqgcdv1cJj8mJrjZJcJr6hJ0Yp9E9Z3k2uE3pQ==",
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

        [Fact]
        public async Task LoginAsync_ReturnsTokens_WhenCredentialsAreValid()
        {
            using var context = CreateDbContext(nameof(LoginAsync_ReturnsTokens_WhenCredentialsAreValid));
            var service = CreateService(context);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Name = "Test User",
                Email = string.Empty,
                PhoneNumber = string.Empty,
                BirthYear = 2002,
                CreatedAt = DateTimeOffset.UtcNow,
                Role = UserRole.Customer,
                PasswordHash = ""
            };

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("StrongP@ssword1!");

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var request = new LoginRequestDto
            {
                Username = "  TestUser  ",
                Password = "StrongP@ssword1!"
            };

            var tokens = await service.LoginAsync(request);

            Assert.NotNull(tokens);
            Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
            Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));

            var userInDb = await context.Users.SingleAsync();
            Assert.False(string.IsNullOrEmpty(userInDb.RefreshToken));
            Assert.NotNull(userInDb.RefreshTokenExpiryTime);
            Assert.True(userInDb.RefreshTokenExpiryTime > DateTimeOffset.UtcNow);
        }

        [Fact]
        public async Task LoginAsync_ReturnsNull_WhenUserDoesNotExist()
        {
            using var context = CreateDbContext(nameof(LoginAsync_ReturnsNull_WhenUserDoesNotExist));
            var service = CreateService(context);

            var request = new LoginRequestDto
            {
                Username = "TestUser",
                Password = "StrongP@ssword1!"
            };

            var tokens = await service.LoginAsync(request);

            Assert.Null(tokens);
        }

        [Fact]
        public async Task LoginAsync_ReturnsNull_WhenPasswordIsWrong()
        {
            using var context = CreateDbContext(nameof(LoginAsync_ReturnsNull_WhenPasswordIsWrong));
            var service = CreateService(context);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Name = "Test User",
                Email = string.Empty,
                PhoneNumber = string.Empty,
                BirthYear = 2002,
                CreatedAt = DateTimeOffset.UtcNow,
                Role = UserRole.Customer,
                PasswordHash = ""
            };

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectP@ssword1!");

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var request = new LoginRequestDto
            {
                Username = "  TestUser  ",
                Password = "WrongP@ssword1!"
            };

            var tokens = await service.LoginAsync(request);

            Assert.Null(tokens);
        }

        [Fact]
        public async Task RefreshTokensAsync_ReturnsNewTokens_WhenRefreshTokenIsValid()
        {
            using var context = CreateDbContext(nameof(RefreshTokensAsync_ReturnsNewTokens_WhenRefreshTokenIsValid));
            var service = CreateService(context);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Name = "Test User",
                Email = string.Empty,
                PhoneNumber = string.Empty,
                BirthYear = 2002,
                CreatedAt = DateTimeOffset.UtcNow,
                Role = UserRole.Customer,
                PasswordHash = ""
            };

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("StrongP@ssword1!");

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var loginRequest = new LoginRequestDto
            {
                Username = "testuser",
                Password = "StrongP@ssword1!"
            };

            var login = await service.LoginAsync(loginRequest);
            Assert.NotNull(login);

            var request = new RefreshTokenRequestDto
            {
                UserId = user.Id,
                RefreshToken = login.RefreshToken
            };

            var refreshed = await service.RefreshTokensAsync(request);

            Assert.NotNull(refreshed);
            Assert.False(string.IsNullOrWhiteSpace(refreshed.AccessToken));
            Assert.False(string.IsNullOrWhiteSpace(refreshed.RefreshToken));
        }

        [Fact]
        public async Task RefreshTokensAsync_ReturnsNull_WhenRefreshTokenExpired()
        {
            using var context = CreateDbContext(nameof(RefreshTokensAsync_ReturnsNull_WhenRefreshTokenExpired));
            var encryption = new FakeEncryptionService();
            var config = CreateConfiguration();
            var service = new AuthService(context, config, encryption);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Name = "Test User",
                Email = string.Empty,
                PhoneNumber = string.Empty,
                BirthYear = 2002,
                CreatedAt = DateTimeOffset.UtcNow,
                Role = UserRole.Customer,
                PasswordHash = "hash"
            };

            var plainRefreshToken = "expired-token";
            user.RefreshToken = encryption.Encrypt(plainRefreshToken);
            user.RefreshTokenExpiryTime = DateTimeOffset.UtcNow.AddDays(-1);

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var request = new RefreshTokenRequestDto
            {
                UserId = user.Id,
                RefreshToken = plainRefreshToken
            };

            var refreshed = await service.RefreshTokensAsync(request);

            Assert.Null(refreshed);
        }

        [Fact]
        public async Task RefreshTokensAsync_ReturnsNull_WhenRefreshTokenDoesNotMatch()
        {
            using var context = CreateDbContext(nameof(RefreshTokensAsync_ReturnsNull_WhenRefreshTokenDoesNotMatch));
            var encryption = new FakeEncryptionService();
            var config = CreateConfiguration();
            var service = new AuthService(context, config, encryption);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Name = "Test User",
                Email = string.Empty,
                PhoneNumber = string.Empty,
                BirthYear = 2002,
                CreatedAt = DateTimeOffset.UtcNow,
                Role = UserRole.Customer,
                PasswordHash = "hash"
            };

            user.RefreshToken = encryption.Encrypt("correct-token");
            user.RefreshTokenExpiryTime = DateTimeOffset.UtcNow.AddDays(1);

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var request = new RefreshTokenRequestDto
            {
                UserId = user.Id,
                RefreshToken = "wrong-token"
            };

            var refreshed = await service.RefreshTokensAsync(request);

            Assert.Null(refreshed);
        }

        [Fact]
        public async Task LogoutAsync_ReturnsFalse_WhenUserNotFound()
        {
            using var context = CreateDbContext(nameof(LogoutAsync_ReturnsFalse_WhenUserNotFound));
            var service = CreateService(context);

            var nonExistingUserId = Guid.NewGuid();

            var result = await service.LogoutAsync(nonExistingUserId);

            Assert.False(result);
        }

        [Fact]
        public async Task LogoutAsync_ClearsRefreshToken_WhenUserExists()
        {
            using var context = CreateDbContext(nameof(LogoutAsync_ClearsRefreshToken_WhenUserExists));
            var encryption = new FakeEncryptionService();
            var config = CreateConfiguration();
            var service = new AuthService(context, config, encryption);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Name = "Test User",
                Email = string.Empty,
                PhoneNumber = string.Empty,
                BirthYear = 2002,
                CreatedAt = DateTimeOffset.UtcNow,
                Role = UserRole.Customer,
                PasswordHash = "hash"
            };

            user.RefreshToken = encryption.Encrypt("any-token")!;
            user.RefreshTokenExpiryTime = DateTimeOffset.UtcNow.AddDays(2);

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var result = await service.LogoutAsync(user.Id);

            Assert.True(result);

            var updatedUser = await context.Users.FindAsync(user.Id);
            Assert.NotNull(updatedUser);
            Assert.Null(updatedUser!.RefreshToken);
            Assert.Null(updatedUser.RefreshTokenExpiryTime);
        }
    }
}
