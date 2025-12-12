using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;
using MobyPark.Services;

namespace MobyParkxUnitTest
{
    public class ProfileServiceTests
    {
        private static UserDbContext CreateDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new UserDbContext(options);
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

        private static ProfileService CreateService(UserDbContext context, IEncryptionService encryption = null)
        {
            return new ProfileService(context, encryption ?? new FakeEncryptionService());
        }

        private static User CreateUser(Guid id, string username, string emailPlain, string phonePlain, int birthYear = 2000)
        {
            var encryption = new FakeEncryptionService();
            return new User
            {
                Id = id,
                Username = username,
                Name = "Test User",
                Email = string.IsNullOrEmpty(emailPlain) ? string.Empty : encryption.Encrypt(emailPlain)!,
                PhoneNumber = string.IsNullOrEmpty(phonePlain) ? string.Empty : encryption.Encrypt(phonePlain)!,
                BirthYear = birthYear,
                Role = UserRole.Customer,
                CreatedAt = DateTimeOffset.UtcNow,
                PasswordHash = "hash",
                RefreshToken = null,
                RefreshTokenExpiryTime = null
            };
        }

        [Fact]
        public async Task GetProfileAsync_ReturnsDecryptedUser_WhenUserExists()
        {
            using var context = CreateDbContext(nameof(GetProfileAsync_ReturnsDecryptedUser_WhenUserExists));
            var service = CreateService(context);

            var userId = Guid.NewGuid();
            var user = CreateUser(userId, "testuser", "test@example.com", "0639659824", 2002);
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var result = await service.GetProfileAsync(userId);

            Assert.NotNull(result);
            Assert.Equal(userId, result!.Id);
            Assert.Equal("testuser", result.Username);
            Assert.Equal("Test User", result.Name);
            Assert.Equal("test@example.com", result.Email);
            Assert.Equal("0639659824", result.PhoneNumber);
            Assert.Equal(2002, result.BirthYear);
        }

        [Fact]
        public async Task GetProfileAsync_ReturnsNull_WhenUserNotFound()
        {
            using var context = CreateDbContext(nameof(GetProfileAsync_ReturnsNull_WhenUserNotFound));
            var service = CreateService(context);

            var result = await service.GetProfileAsync(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateProfileAsync_ChangesUsername_WhenAvailable()
        {
            using var context = CreateDbContext(nameof(UpdateProfileAsync_ChangesUsername_WhenAvailable));
            var service = CreateService(context);
            var userId = Guid.NewGuid();
            var user = CreateUser(userId, "olduser", "old@example.com", "0612345678");
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var dto = new UpdateProfileDto
            {
                Username = "  NewUser  ",
                Email = null,
                Name = null,
                PhoneNumber = null,
                BirthYear = null
            };

            var (result, error, status) = await service.UpdateProfileAsync(userId, dto);

            Assert.Null(error);
            Assert.Null(status);
            Assert.NotNull(result);
            Assert.Equal("newuser", result!.Username);

            var userInDb = await context.Users.SingleAsync();
            Assert.Equal("newuser", userInDb.Username);
        }

        [Fact]
        public async Task UpdateProfileAsync_Returns409_WhenUsernameTaken()
        {
            using var context = CreateDbContext(nameof(UpdateProfileAsync_Returns409_WhenUsernameTaken));
            var service = CreateService(context);

            var user1 = CreateUser(Guid.NewGuid(), "user1", "u1@example.com", "0611111111");
            var user2 = CreateUser(Guid.NewGuid(), "user2", "u2@example.com", "0622222222");
            context.Users.AddRange(user1, user2);
            await context.SaveChangesAsync();

            var dto = new UpdateProfileDto
            {
                Username = "User2",
                Email = null,
                Name = null,
                PhoneNumber = null,
                BirthYear = null
            };

            var (result, error, status) = await service.UpdateProfileAsync(user1.Id, dto);

            Assert.Null(result);
            Assert.Equal("Username already in use.", error);
            Assert.Equal(409, status);
        }

        [Fact]
        public async Task UpdateProfileAsync_ChangesEmail_WhenAvailable()
        {
            using var context = CreateDbContext(nameof(UpdateProfileAsync_ChangesEmail_WhenAvailable));
            var encryption = new FakeEncryptionService();
            var service = new ProfileService(context, encryption);

            var userId = Guid.NewGuid();
            var user = CreateUser(userId, "testuser", "old@example.com", "0612345678");
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var dto = new UpdateProfileDto
            {
                Username = null,
                Email = "NewEmail@example.com",
                Name = null,
                PhoneNumber = null,
                BirthYear = null
            };

            var (result, error, status) = await service.UpdateProfileAsync(userId, dto);

            Assert.Null(error);
            Assert.Null(status);
            Assert.NotNull(result);
            Assert.Equal("newemail@example.com", result!.Email);

            var userInDb = await context.Users.SingleAsync();
            Assert.Equal(encryption.Encrypt("newemail@example.com"), userInDb.Email);
        }

        [Fact]
        public async Task UpdateProfileAsync_Returns409_WhenEmailTaken()
        {
            using var context = CreateDbContext(nameof(UpdateProfileAsync_Returns409_WhenEmailTaken));
            var encryption = new FakeEncryptionService();
            var service = new ProfileService(context, encryption);

            var user1 = CreateUser(Guid.NewGuid(), "user1", "user1@example.com", "0611111111");
            var user2 = CreateUser(Guid.NewGuid(), "user2", "taken@example.com", "0622222222");
            context.Users.AddRange(user1, user2);
            await context.SaveChangesAsync();

            var dto = new UpdateProfileDto
            {
                Username = null,
                Email = "  TAKEN@example.com  ",
                Name = null,
                PhoneNumber = null,
                BirthYear = null
            };

            var (result, error, status) = await service.UpdateProfileAsync(user1.Id, dto);

            Assert.Null(result);
            Assert.Equal("Email already in use.", error);
            Assert.Equal(409, status);
        }

        [Fact]
        public async Task UpdateProfileAsync_UpdatedBirthYear_WhenInRange()
        {
            using var context = CreateDbContext(nameof(UpdateProfileAsync_UpdatedBirthYear_WhenInRange));
            var service = CreateService(context);

            var userId = Guid.NewGuid();
            var user = CreateUser(userId, "testuser", "test@example.com", "0612345678", 2000);
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var dto = new UpdateProfileDto
            {
                Username = null,
                Email = null,
                Name = null,
                PhoneNumber = null,
                BirthYear = 2003
            };

            var (result, error, status) = await service.UpdateProfileAsync(userId, dto);

            Assert.Null(error);
            Assert.Null(status);
            Assert.NotNull(result);
            Assert.Equal(2003, result!.BirthYear);

            var userInDb = await context.Users.SingleAsync();
            Assert.Equal(2003, userInDb.BirthYear);
        }

        [Fact]
        public async Task UpdateProfileAsync_ChangesName_WhenDifferent()
        {
            using var context = CreateDbContext(nameof(UpdateProfileAsync_ChangesName_WhenDifferent));
            var service = CreateService(context);

            var userId = Guid.NewGuid();
            var user = CreateUser(userId, "testuser", "test@example.com", "0612345678");
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var dto = new UpdateProfileDto
            {
                Username = null,
                Email = null,
                Name = "New Name",
                PhoneNumber = null,
                BirthYear = null
            };

            var (result, error, status) = await service.UpdateProfileAsync(userId, dto);

            Assert.Null(error);
            Assert.Null(status);
            Assert.NotNull(result);
            Assert.Equal("New Name", result!.Name);

            var userInDb = await context.Users.SingleAsync();
            Assert.Equal("New Name", userInDb.Name);
        }

        [Fact]
        public async Task UpdateProfileAsync_DoesNotChangeName_WhenSameOrWhitespace()
        {
            using var context = CreateDbContext(nameof(UpdateProfileAsync_DoesNotChangeName_WhenSameOrWhitespace));
            var service = CreateService(context);

            var userId = Guid.NewGuid();
            var user = CreateUser(userId, "testuser", "test@example.com", "0612345678");
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var dto = new UpdateProfileDto
            {
                Username = null,
                Email = null,
                Name = "  Existing Name  ",
                PhoneNumber = null,
                BirthYear = null
            };

            var (result, error, status) = await service.UpdateProfileAsync(userId, dto);

            Assert.Null(error);
            Assert.Null(status);
            Assert.NotNull(result);
            Assert.Equal("Existing Name", result!.Name);

            var userInDb = await context.Users.SingleAsync();
            Assert.Equal("Existing Name", userInDb.Name);
        }

        [Fact]
        public async Task UpdateProfileAsync_ChangesPhoneNumber_WhenDifferent()
        {
            using var context = CreateDbContext(nameof(UpdateProfileAsync_ChangesPhoneNumber_WhenDifferent));
            var encryption = new FakeEncryptionService();
            var service = new ProfileService(context, encryption);

            var userId = Guid.NewGuid();
            var user = CreateUser(userId, "testuser", "test@example.com", "0612345678");
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var dto = new UpdateProfileDto
            {
                Username = null,
                Email = null,
                Name = null,
                PhoneNumber = "  0699999999  ",
                BirthYear = null
            };

            var (result, error, status) = await service.UpdateProfileAsync(userId, dto);

            Assert.Null(error);
            Assert.Null(status);
            Assert.NotNull(result);
            Assert.Equal("0699999999", result!.PhoneNumber);

            var userInDb = await context.Users.SingleAsync();
            Assert.Equal(encryption.Encrypt("0699999999"), userInDb.PhoneNumber);
        }

        [Fact]
        public async Task UpdateProfileAsync_DoesNotChangePhoneNumber_WhenSame()
        {
            using var context = CreateDbContext(nameof(UpdateProfileAsync_DoesNotChangePhoneNumber_WhenSame));
            var encryption = new FakeEncryptionService();
            var service = new ProfileService(context, encryption);

            var userId = Guid.NewGuid();
            var user = CreateUser(userId, "testuser", "test@example.com", "0612345678");
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var dto = new UpdateProfileDto
            {
                Username = null,
                Email = null,
                Name = null,
                PhoneNumber = "0612345678",
                BirthYear = null
            };

            var (result, error, status) = await service.UpdateProfileAsync(userId, dto);

            Assert.Null(error);
            Assert.Null(status);
            Assert.NotNull(result);
            Assert.Equal("0612345678", result!.PhoneNumber);

            var userInDb = await context.Users.SingleAsync();
            Assert.Equal(encryption.Encrypt("0612345678"), userInDb.PhoneNumber);
        }

        [Fact]
        public async Task UpdateProfileAsync_Returns4040_WhenUserNotFound()
        {
            using var context = CreateDbContext(nameof(UpdateProfileAsync_Returns4040_WhenUserNotFound));
            var service = CreateService(context);

            var dto = new UpdateProfileDto
            {
                Username = "newuser",
                Email = "new@example.com",
                Name = "New Name",
                PhoneNumber = "0600000000",
                BirthYear = 2001
            };

            var (result, error, status) = await service.UpdateProfileAsync(Guid.NewGuid(), dto);

            Assert.Null(result);
            Assert.Null(error);
            Assert.Equal(404, status);
        }
    }
}
