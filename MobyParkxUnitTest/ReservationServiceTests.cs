using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;
using MobyPark.Services;
using System.ComponentModel.DataAnnotations;

namespace MobyParkxUnitTest
{
    public class FakeEncryptionService : IEncryptionService
    {
        private const string Prefix = "ENC::";
        private const string Suffix = "::PADPADPADPADPADPADPADPADPADPADPADPADPADPADPADPADPADPADPADPADPAD";

        public string? Encrypt(string? plaintext)
            => plaintext is null ? null : $"{Prefix}{plaintext}{Suffix}";

        public string? Decrypt(string? ciphertext)
        {
            if (ciphertext is null) return null;
            if (!ciphertext.StartsWith(Prefix, StringComparison.Ordinal)) return ciphertext;

            var body = ciphertext.Substring(Prefix.Length);
            var end = body.IndexOf("::", StringComparison.Ordinal);
            return end < 0 ? body : body.Substring(0, end);
        }
    }

    public class CreateReservationTest
    {
        private static UserDbContext CreateDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new UserDbContext(options);
        }

        private static ReservationService CreateService(UserDbContext context)
        {
            var encryption = new FakeEncryptionService();
            var discount = new DiscountService(context);

            return new ReservationService(
                context,
                encryption,
                discount
            );
        }

        [Fact]
        public async Task CreateReservation_ShouldCreateSuccessfully()
        {
            // Arrange
            var context = CreateDbContext(Guid.NewGuid().ToString());
            var encryption = new FakeEncryptionService();
            var creationDate = DateTime.UtcNow;

            context.ParkingLots.Add(new ParkingLot
            {
                Id = 1,
                Name = "Testlot",
                Location = "Rotterdam",
                Address = "Coolsingel 2",
                Capacity = 5,
                Tariff = 2.0,
                DayTariff = 10.0,
                Coordinates = "{latitude: 51.9239450536725, longitude: 4.47866126718686}",
                CreatedAt = creationDate
            });

            var userId = Guid.NewGuid();

            context.Users.Add(new User
            {
                Id = userId,
                Username = "TestUser",
                Name = "TestName",
                Email = "123@hr.nl",
                PhoneNumber = "0612345678",
                BirthYear = 2000,
                CreatedAt = creationDate,
                PasswordHash = "dummy-password-hash",
                RefreshToken = null
            });

            context.Vehicles.Add(new Vehicle
            {
                Id = 1,
                UserId = userId,
                LicensePlate = "ABC123",
                Make = "Ford",
                Model = "Focus",
                Color = "Gray",
                Year = 2005,
                CreatedAt = creationDate
            });

            await context.SaveChangesAsync();

            var service = CreateService(context);

            var dto = new PostReservationDto
            {
                ParkingLotId = 1,
                LicensePlate = "ABC123",
                StartTime = DateTimeOffset.UtcNow.AddHours(1),
                EndTime = DateTimeOffset.UtcNow.AddHours(2)
            };

            // Act
            var result = await service.CreateReservation(dto, userId);

            // Assert
            var savedReservation = await context.Reservations.FirstAsync();

            Assert.NotNull(result);
            Assert.Equal(1, result.ParkingLotId);
            Assert.Equal(userId, result.UserId);
            Assert.Equal("ABC123", result.LicensePlate);
            Assert.Equal(dto.StartTime, result.StartTime);
            Assert.Equal(dto.EndTime, result.EndTime);
            Assert.True(result.IsActive);
            Assert.Equal(1, await context.Reservations.CountAsync());
            Assert.Equal("ABC123", savedReservation.LicensePlate);
            Assert.Equal(userId, savedReservation.UserId);
            Assert.Equal(1, savedReservation.ParkingLotId);
            Assert.True(result.StartTime > DateTimeOffset.UtcNow);
            Assert.Equal(savedReservation.Id, result.Id);
        }
        [Fact]
        public async Task CreateReservation_ShouldThrow_WhenParkingLotDoesNotExist()
        {
            var context = CreateDbContext(Guid.NewGuid().ToString());

            var creationDate = DateTime.UtcNow;
            var userId = Guid.NewGuid();

            context.Users.Add(new User
            {
                Id = userId,
                Username = "TestUser",
                Name = "TestName",
                Email = "123@hr.nl",
                PhoneNumber = "0612345678",
                BirthYear = 2000,
                CreatedAt = creationDate,
                PasswordHash = "dummy-password-hash",
                RefreshToken = null
            });

            context.Vehicles.Add(new Vehicle
            {
                Id = 1,
                UserId = userId,
                LicensePlate = "ABC123",
                Make = "Ford",
                Model = "Focus",
                Color = "Gray",
                Year = 2005,
                CreatedAt = creationDate
            });

            await context.SaveChangesAsync();

            var service = CreateService(context);

            var dto = new PostReservationDto
            {
                ParkingLotId = 999,
                LicensePlate = "ABC123",
                StartTime = DateTimeOffset.UtcNow.AddHours(1),
                EndTime = DateTimeOffset.UtcNow.AddHours(2)
            };

            // Act and assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.CreateReservation(dto, userId)
            );

        }
        [Fact]
        public async Task CreateReservation_ShouldThrow_WhenUserDoesNotExist()
        {
            // Arrange
            var context = CreateDbContext(Guid.NewGuid().ToString());

            var creationDate = DateTime.UtcNow;

            context.ParkingLots.Add(new ParkingLot
            {
                Id = 1,
                Name = "Testlot",
                Location = "Rotterdam",
                Address = "Coolsingel 2",
                Capacity = 5,
                Tariff = 2.0,
                DayTariff = 10.0,
                Coordinates = "{latitude: 51.9239450536725, longitude: 4.47866126718686}",
                CreatedAt = creationDate
            });

            context.Vehicles.Add(new Vehicle
            {
                Id = 1,
                UserId = Guid.NewGuid(),
                LicensePlate = "ABC123",
                Make = "Ford",
                Model = "Focus",
                Color = "Gray",
                Year = 2005,
                CreatedAt = creationDate
            });

            await context.SaveChangesAsync();

            var service = CreateService(context);

            var dto = new PostReservationDto
            {
                ParkingLotId = 1,
                LicensePlate = "ABC123",
                StartTime = DateTimeOffset.UtcNow.AddHours(1),
                EndTime = DateTimeOffset.UtcNow.AddHours(2)
            };

            // Act and assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.CreateReservation(dto, Guid.NewGuid())
            );
        }
        [Fact]
        public async Task CreateReservation_ShouldThrow_WhenStartTimeIsAfterEndTime()
        {
            // Arrange
            var context = CreateDbContext(Guid.NewGuid().ToString());

            var creationDate = DateTime.UtcNow;

            context.ParkingLots.Add(new ParkingLot
            {
                Id = 1,
                Name = "Testlot",
                Location = "Rotterdam",
                Address = "Coolsingel 2",
                Capacity = 5,
                Tariff = 2.0,
                DayTariff = 10.0,
                Coordinates = "{latitude: 51.9239450536725, longitude: 4.47866126718686}",
                CreatedAt = creationDate
            });

            var userId = Guid.NewGuid();

            context.Users.Add(new User
            {
                Id = userId,
                Username = "TestUser",
                Name = "TestName",
                Email = "123@hr.nl",
                PhoneNumber = "0612345678",
                BirthYear = 2000,
                CreatedAt = creationDate,
                PasswordHash = "dummy-password-hash",
                RefreshToken = null
            });

            context.Vehicles.Add(new Vehicle
            {
                Id = 1,
                UserId = userId,
                LicensePlate = "ABC123",
                Make = "Ford",
                Model = "Focus",
                Color = "Gray",
                Year = 2005,
                CreatedAt = creationDate
            });

            await context.SaveChangesAsync();

            var service = CreateService(context);

            var dto = new PostReservationDto
            {
                ParkingLotId = 1,
                LicensePlate = "ABC123",
                StartTime = DateTimeOffset.UtcNow.AddHours(1),
                EndTime = DateTimeOffset.UtcNow
            };

            // Act and assert
            await Assert.ThrowsAsync<ValidationException>(() =>
                service.CreateReservation(dto, userId)
            );
        }
        [Fact]
        public async Task CreateReservation_ShouldThrow_WhenStartTimeIsInThePast()
        {
            // Arrange
            var context = CreateDbContext(Guid.NewGuid().ToString());

            var creationDate = DateTime.UtcNow;

            context.ParkingLots.Add(new ParkingLot
            {
                Id = 1,
                Name = "Testlot",
                Location = "Rotterdam",
                Address = "Coolsingel 2",
                Capacity = 5,
                Tariff = 2.0,
                DayTariff = 10.0,
                Coordinates = "{latitude: 51.9239450536725, longitude: 4.47866126718686}",
                CreatedAt = creationDate
            });

            var userId = Guid.NewGuid();

            context.Users.Add(new User
            {
                Id = userId,
                Username = "TestUser",
                Name = "TestName",
                Email = "123@hr.nl",
                PhoneNumber = "0612345678",
                BirthYear = 2000,
                CreatedAt = creationDate,
                PasswordHash = "dummy-password-hash",
                RefreshToken = null
            });

            context.Vehicles.Add(new Vehicle
            {
                Id = 1,
                UserId = userId,
                LicensePlate = "ABC123",
                Make = "Ford",
                Model = "Focus",
                Color = "Gray",
                Year = 2005,
                CreatedAt = creationDate
            });

            await context.SaveChangesAsync();

            var service = CreateService(context);

            var dto = new PostReservationDto
            {
                ParkingLotId = 1,
                LicensePlate = "ABC123",
                StartTime = DateTimeOffset.UtcNow.AddHours(-1),
                EndTime = DateTimeOffset.UtcNow.AddHours(1)
            };

            // Act and assert
            await Assert.ThrowsAsync<ValidationException>(() =>
                service.CreateReservation(dto, userId)
            );
        }
        [Fact]
        public async Task CreateReservation_ShouldThrow_WhenParkingLotIsFull()
        {
            // Arrange
            var context = CreateDbContext(Guid.NewGuid().ToString());

            var creationDate = DateTime.UtcNow;

            context.ParkingLots.Add(new ParkingLot
            {
                Id = 1,
                Name = "Testlot",
                Location = "Rotterdam",
                Address = "Coolsingel 2",
                Capacity = 5,
                Tariff = 2.0,
                DayTariff = 10.0,
                Coordinates = "{latitude: 51.9239450536725, longitude: 4.47866126718686}",
                CreatedAt = creationDate
            });

            var userId = Guid.NewGuid();

            context.Users.Add(new User
            {
                Id = userId,
                Username = "TestUser",
                Name = "TestName",
                Email = "123@hr.nl",
                PhoneNumber = "0612345678",
                BirthYear = 2000,
                CreatedAt = creationDate,
                PasswordHash = "dummy-password-hash",
                RefreshToken = null
            });

            context.Vehicles.Add(new Vehicle
            {
                Id = 1,
                UserId = userId,
                LicensePlate = "PQR123",
                Make = "Ford",
                Model = "Focus",
                Color = "Gray",
                Year = 2005,
                CreatedAt = creationDate
            });

            await context.SaveChangesAsync();

            var service = CreateService(context);

            var startTime = DateTimeOffset.UtcNow.AddHours(1);
            var endTime = DateTimeOffset.UtcNow.AddHours(2);

            List<string> plates = new() { "ABC123", "DEF123", "GHI123", "JKL123", "MNO123" };
            foreach (string plate in plates)
            {
                var tempUserId = Guid.NewGuid();

                context.Users.Add(new User
                {
                    Id = tempUserId,
                    Username = Guid.NewGuid().ToString(),
                    Name = "TempTestName",
                    Email = $"{plate}@hr.nl",
                    PhoneNumber = $"{plate[3]}612345678",
                    BirthYear = 2000,
                    CreatedAt = creationDate,
                    PasswordHash = "dummy-password-hash",
                    RefreshToken = null
                });

                context.Vehicles.Add(new Vehicle
                {
                    UserId = tempUserId,
                    LicensePlate = plate,
                    Make = "Ford",
                    Model = "Focus",
                    Color = "Gray",
                    Year = 2005,
                    CreatedAt = creationDate
                });

                await context.SaveChangesAsync();

                var existingDto = new PostReservationDto
                {
                    ParkingLotId = 1,
                    LicensePlate = plate,
                    StartTime = startTime,
                    EndTime = endTime
                };

                await service.CreateReservation(existingDto, tempUserId);
            }

            var dto = new PostReservationDto
            {
                ParkingLotId = 1,
                LicensePlate = "PQR123",
                StartTime = startTime,
                EndTime = endTime
            };

            // Act and assert
            await Assert.ThrowsAsync<ParkingLotFullException>(() =>
                service.CreateReservation(dto, userId)
            );
        }
        [Fact]
        public async Task CreateReservation_ShouldThrow_WhenTimesOverlapAndLotIsFull()
        {
            // Arrange
            var context = CreateDbContext(Guid.NewGuid().ToString());

            var creationDate = DateTime.UtcNow;

            context.ParkingLots.Add(new ParkingLot
            {
                Id = 1,
                Name = "Testlot",
                Location = "Rotterdam",
                Address = "Coolsingel 2",
                Capacity = 5,
                Tariff = 2.0,
                DayTariff = 10.0,
                Coordinates = "{latitude: 51.9239450536725, longitude: 4.47866126718686}",
                CreatedAt = creationDate
            });

            var userId = Guid.NewGuid();

            context.Users.Add(new User
            {
                Id = userId,
                Username = "TestUser",
                Name = "TestName",
                Email = "123@hr.nl",
                PhoneNumber = "0612345678",
                BirthYear = 2000,
                CreatedAt = creationDate,
                PasswordHash = "dummy-password-hash",
                RefreshToken = null
            });

            context.Vehicles.Add(new Vehicle
            {
                Id = 1,
                UserId = userId,
                LicensePlate = "PQR123",
                Make = "Ford",
                Model = "Focus",
                Color = "Gray",
                Year = 2005,
                CreatedAt = creationDate
            });

            await context.SaveChangesAsync();

            var service = CreateService(context);

            var startTime = DateTimeOffset.UtcNow.AddHours(1);
            var endTime = DateTimeOffset.UtcNow.AddHours(2);

            List<string> plates = new() { "ABC1", "ABC2", "ABC3", "ABC4", "ABC5" };
            foreach (var plate in plates)
            {
                var tempUserId = Guid.NewGuid();

                context.Users.Add(new User
                {
                    Id = tempUserId,
                    Username = Guid.NewGuid().ToString(),
                    Name = "TempTestName",
                    Email = $"{plate}@hr.nl",
                    PhoneNumber = $"{plate[3]}612345678",
                    BirthYear = 2000,
                    CreatedAt = creationDate,
                    PasswordHash = "dummy-password-hash",
                    RefreshToken = null
                });

                context.Vehicles.Add(new Vehicle
                {
                    UserId = tempUserId,
                    LicensePlate = plate,
                    Make = "Ford",
                    Model = "Focus",
                    Color = "Gray",
                    Year = 2005,
                    CreatedAt = creationDate
                });

                await context.SaveChangesAsync();

                await service.CreateReservation(new PostReservationDto
                {
                    ParkingLotId = 1,
                    LicensePlate = plate,
                    StartTime = startTime,
                    EndTime = endTime
                },
                tempUserId);
            }

            var dto = new PostReservationDto
            {
                ParkingLotId = 1,
                LicensePlate = "PQR123",
                StartTime = startTime.AddMinutes(60),
                EndTime = endTime.AddMinutes(90)
            };

            await Assert.ThrowsAsync<ParkingLotFullException>(() =>
                service.CreateReservation(dto, userId)
            );

        }
    }

    public class GetReservationTest
    {
        private static UserDbContext CreateDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new UserDbContext(options);
        }
        private static ReservationService CreateService(UserDbContext context)
        {
            var encryption = new FakeEncryptionService();
            var discount = new DiscountService(context);

            return new ReservationService(
                context,
                encryption,
                discount
            );
        }

        [Fact]
        public async Task GetReservationById_ShouldGetSuccessfully()
        {
            // Arrange
            var context = CreateDbContext(Guid.NewGuid().ToString());

            var creationDate = DateTime.UtcNow;

            var userId = Guid.NewGuid();

            context.ParkingLots.Add(new ParkingLot
            {
                Id = 1,
                Name = "Testlot",
                Location = "Rotterdam",
                Address = "Coolsingel 2",
                Capacity = 5,
                Tariff = 2.0,
                DayTariff = 10.0,
                Coordinates = "{latitude: 51.9239450536725, longitude: 4.47866126718686}",
                CreatedAt = creationDate
            });

            context.Users.Add(new User
            {
                Id = userId,
                Username = "TestUser",
                Name = "TestName",
                Email = "123@hr.nl",
                PhoneNumber = "0612345678",
                BirthYear = 2000,
                CreatedAt = creationDate,
                PasswordHash = "dummy-password-hash",
                RefreshToken = null
            });

            context.Vehicles.Add(new Vehicle
            {
                Id = 1,
                UserId = userId,
                LicensePlate = "ABC123",
                Make = "Ford",
                Model = "Focus",
                Color = "Gray",
                Year = 2005,
                CreatedAt = creationDate
            });

            await context.SaveChangesAsync();

            var startTime = DateTimeOffset.UtcNow.AddHours(1);
            var endTime = DateTimeOffset.UtcNow.AddHours(2);

            context.Reservations.Add(new Reservation
            {
                Id = 1,
                ParkingLotId = 1,
                UserId = userId,
                VehicleId = 1,
                LicensePlate = "ABC123",
                StartTime = startTime,
                EndTime = endTime,
            });

            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            var result = await service.GetById(1, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal(1, result.ParkingLotId);
            Assert.Equal(userId, result.UserId);
            Assert.Equal("ABC123", result.LicensePlate);
            Assert.Equal(startTime, result.StartTime);
            Assert.Equal(endTime, result.EndTime);
            Assert.True(result.IsActive);
        }

        [Fact]
        public async Task GetReservationById_ShouldReturnNull_WhenNotFound()
        {
            // Arrange
            var context = CreateDbContext(Guid.NewGuid().ToString());

            var creationDate = DateTime.UtcNow;

            var userId = Guid.NewGuid();

            context.ParkingLots.Add(new ParkingLot
            {
                Id = 1,
                Name = "Testlot",
                Location = "Rotterdam",
                Address = "Coolsingel 2",
                Capacity = 5,
                Tariff = 2.0,
                DayTariff = 10.0,
                Coordinates = "{latitude: 51.9239450536725, longitude: 4.47866126718686}",
                CreatedAt = creationDate
            });

            context.Users.Add(new User
            {
                Id = userId,
                Username = "TestUser",
                Name = "TestName",
                Email = "123@hr.nl",
                PhoneNumber = "0612345678",
                BirthYear = 2000,
                CreatedAt = creationDate,
                PasswordHash = "dummy-password-hash",
                RefreshToken = null
            });

            context.Vehicles.Add(new Vehicle
            {
                Id = 1,
                UserId = userId,
                LicensePlate = "ABC123",
                Make = "Ford",
                Model = "Focus",
                Color = "Gray",
                Year = 2005,
                CreatedAt = creationDate
            });

            await context.SaveChangesAsync();

            var startTime = DateTimeOffset.UtcNow.AddHours(1);
            var endTime = DateTimeOffset.UtcNow.AddHours(2);

            context.Reservations.Add(new Reservation
            {
                Id = 1,
                ParkingLotId = 1,
                UserId = userId,
                VehicleId = 1,
                LicensePlate = "ABC123",
                StartTime = startTime,
                EndTime = endTime,
            });

            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            var result = await service.GetById(999, userId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetReservationByVehicleId_ShouldGetSuccessfully()
        {
            // Arrange
            var context = CreateDbContext(Guid.NewGuid().ToString());

            var creationDate = DateTime.UtcNow;

            var userId = Guid.NewGuid();

            context.ParkingLots.Add(new ParkingLot
            {
                Id = 1,
                Name = "Testlot",
                Location = "Rotterdam",
                Address = "Coolsingel 2",
                Capacity = 5,
                Tariff = 2.0,
                DayTariff = 10.0,
                Coordinates = "{latitude: 51.9239450536725, longitude: 4.47866126718686}",
                CreatedAt = creationDate
            });

            context.Users.Add(new User
            {
                Id = userId,
                Username = "TestUser",
                Name = "TestName",
                Email = "123@hr.nl",
                PhoneNumber = "0612345678",
                BirthYear = 2000,
                CreatedAt = creationDate,
                PasswordHash = "dummy-password-hash",
                RefreshToken = null
            });

            context.Vehicles.Add(new Vehicle
            {
                Id = 1,
                UserId = userId,
                LicensePlate = "ABC123",
                Make = "Ford",
                Model = "Focus",
                Color = "Gray",
                Year = 2005,
                CreatedAt = creationDate
            });

            await context.SaveChangesAsync();

            var startTime = DateTimeOffset.UtcNow.AddHours(1);
            var endTime = DateTimeOffset.UtcNow.AddHours(2);

            context.Reservations.Add(new Reservation
            {
                Id = 1,
                ParkingLotId = 1,
                UserId = userId,
                VehicleId = 1,
                LicensePlate = "ABC123",
                StartTime = startTime,
                EndTime = endTime,
            });

            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            var result = await service.GetByVehicleId(1, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal(1, result.ParkingLotId);
            Assert.Equal(userId, result.UserId);
            Assert.Equal("ABC123", result.LicensePlate);
            Assert.Equal(startTime, result.StartTime);
            Assert.Equal(endTime, result.EndTime);
            Assert.True(result.IsActive);
        }

        [Fact]
        public async Task GetReservationByVehicleId_ShouldReturnNull_WhenNotFound()
        {
            // Arrange
            var context = CreateDbContext(Guid.NewGuid().ToString());

            var creationDate = DateTime.UtcNow;

            var userId = Guid.NewGuid();

            context.ParkingLots.Add(new ParkingLot
            {
                Id = 1,
                Name = "Testlot",
                Location = "Rotterdam",
                Address = "Coolsingel 2",
                Capacity = 5,
                Tariff = 2.0,
                DayTariff = 10.0,
                Coordinates = "{latitude: 51.9239450536725, longitude: 4.47866126718686}",
                CreatedAt = creationDate
            });

            context.Users.Add(new User
            {
                Id = userId,
                Username = "TestUser",
                Name = "TestName",
                Email = "123@hr.nl",
                PhoneNumber = "0612345678",
                BirthYear = 2000,
                CreatedAt = creationDate,
                PasswordHash = "dummy-password-hash",
                RefreshToken = null
            });

            context.Vehicles.Add(new Vehicle
            {
                Id = 1,
                UserId = userId,
                LicensePlate = "ABC123",
                Make = "Ford",
                Model = "Focus",
                Color = "Gray",
                Year = 2005,
                CreatedAt = creationDate
            });

            await context.SaveChangesAsync();

            var startTime = DateTimeOffset.UtcNow.AddHours(1);
            var endTime = DateTimeOffset.UtcNow.AddHours(2);

            context.Reservations.Add(new Reservation
            {
                Id = 1,
                ParkingLotId = 1,
                UserId = userId,
                VehicleId = 1,
                LicensePlate = "ABC123",
                StartTime = startTime,
                EndTime = endTime,
            });

            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            var result = await service.GetByVehicleId(999, userId);

            // Assert
            Assert.Null(result);
        }
    }

    public class PutReservationTest
    {
        private static UserDbContext CreateDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new UserDbContext(options);
        }
        private static ReservationService CreateService(UserDbContext context)
        {
            var encryption = new FakeEncryptionService();
            var discount = new DiscountService(context);

            return new ReservationService(
                context,
                encryption,
                discount
            );
        }

        [Fact]
        public async Task PutReservation_ShouldPartialPutLicensePlateSuccessfully()
        {
            // Arrange
            var context = CreateDbContext(Guid.NewGuid().ToString());

            var creationDate = DateTime.UtcNow;

            var userId = Guid.NewGuid();

            context.ParkingLots.Add(new ParkingLot
            {
                Id = 1,
                Name = "Testlot",
                Location = "Rotterdam",
                Address = "Coolsingel 2",
                Capacity = 5,
                Tariff = 2.0,
                DayTariff = 10.0,
                Coordinates = "{latitude: 52.08205549550792, longitude:  4.292821879337678}",
                CreatedAt = creationDate
            });

            context.Users.Add(new User
            {
                Id = userId,
                Username = "TestUser",
                Name = "TestName",
                Email = "123@hr.nl",
                PhoneNumber = "0612345678",
                BirthYear = 2000,
                CreatedAt = creationDate,
                PasswordHash = "dummy-password-hash",
                RefreshToken = null
            });

            context.Vehicles.Add(new Vehicle
            {
                Id = 1,
                UserId = userId,
                LicensePlate = "ABC123",
                Make = "Ford",
                Model = "Focus",
                Color = "Gray",
                Year = 2005,
                CreatedAt = creationDate
            });

            context.Vehicles.Add(new Vehicle
            {
                Id = 2,
                UserId = userId,
                LicensePlate = "KLM123",
                Make = "Ford",
                Model = "Focus",
                Color = "Gray",
                Year = 2005,
                CreatedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();

            var startTime = DateTimeOffset.UtcNow.AddHours(1);
            var endTime = DateTimeOffset.UtcNow.AddHours(2);

            context.Reservations.Add(new Reservation
            {
                Id = 1,
                ParkingLotId = 1,
                UserId = userId,
                VehicleId = 1,
                LicensePlate = "ABC123",
                StartTime = startTime,
                EndTime = endTime,
            });

            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            var result = await service.UpdateReservation(1, new PutReservationDto
            {
                LicensePlate = "KLM123"
            },
            userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal(1, result.ParkingLotId);
            Assert.Equal(userId, result.UserId);
            Assert.Equal("KLM123", result.LicensePlate);
            Assert.Equal(startTime, result.StartTime);
            Assert.Equal(endTime, result.EndTime);
            Assert.True(result.IsActive);
        }

        [Fact]
        public async Task PutReservation_ShouldPartialPutStartTimeSuccessfully()
        {
            // Arrange
            var context = CreateDbContext(Guid.NewGuid().ToString());

            var creationDate = DateTime.UtcNow;

            var userId = Guid.NewGuid();

            context.ParkingLots.Add(new ParkingLot
            {
                Id = 1,
                Name = "Testlot",
                Location = "Rotterdam",
                Address = "Coolsingel 2",
                Capacity = 5,
                Tariff = 2.0,
                DayTariff = 10.0,
                Coordinates = "{latitude: 52.08205549550792, longitude:  4.292821879337678}",
                CreatedAt = creationDate
            });

            context.Users.Add(new User
            {
                Id = userId,
                Username = "TestUser",
                Name = "TestName",
                Email = "123@hr.nl",
                PhoneNumber = "0612345678",
                BirthYear = 2000,
                CreatedAt = creationDate,
                PasswordHash = "dummy-password-hash",
                RefreshToken = null
            });

            context.Vehicles.Add(new Vehicle
            {
                Id = 1,
                UserId = userId,
                LicensePlate = "ABC123",
                Make = "Ford",
                Model = "Focus",
                Color = "Gray",
                Year = 2005,
                CreatedAt = creationDate
            });

            await context.SaveChangesAsync();

            var startTime = DateTimeOffset.UtcNow.AddHours(1);
            var endTime = DateTimeOffset.UtcNow.AddHours(4);

            context.Reservations.Add(new Reservation
            {
                Id = 1,
                ParkingLotId = 1,
                UserId = userId,
                VehicleId = 1,
                LicensePlate = "ABC123",
                StartTime = startTime,
                EndTime = endTime,
            });

            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            var result = await service.UpdateReservation(1, new PutReservationDto
            {
                StartTime = startTime.AddHours(1)
            },
            userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal(1, result.ParkingLotId);
            Assert.Equal(userId, result.UserId);
            Assert.Equal("ABC123", result.LicensePlate);
            Assert.Equal(startTime.AddHours(1), result.StartTime);
            Assert.Equal(endTime, result.EndTime);
            Assert.True(result.IsActive);
        }

        [Fact]
        public async Task PutReservation_ShouldPartialPutEndTimeSuccessfully()
        {
            // Arrange
            var context = CreateDbContext(Guid.NewGuid().ToString());

            var creationDate = DateTime.UtcNow;

            var userId = Guid.NewGuid();

            context.ParkingLots.Add(new ParkingLot
            {
                Id = 1,
                Name = "Testlot",
                Location = "Rotterdam",
                Address = "Coolsingel 2",
                Capacity = 5,
                Tariff = 2.0,
                DayTariff = 10.0,
                Coordinates = "{latitude: 52.08205549550792, longitude:  4.292821879337678}",
                CreatedAt = creationDate
            });

            context.Users.Add(new User
            {
                Id = userId,
                Username = "TestUser",
                Name = "TestName",
                Email = "123@hr.nl",
                PhoneNumber = "0612345678",
                BirthYear = 2000,
                CreatedAt = creationDate,
                PasswordHash = "dummy-password-hash",
                RefreshToken = null
            });

            context.Vehicles.Add(new Vehicle
            {
                Id = 1,
                UserId = userId,
                LicensePlate = "ABC123",
                Make = "Ford",
                Model = "Focus",
                Color = "Gray",
                Year = 2005,
                CreatedAt = creationDate
            });

            await context.SaveChangesAsync();

            var startTime = DateTimeOffset.UtcNow.AddHours(1);
            var endTime = DateTimeOffset.UtcNow.AddHours(2);

            context.Reservations.Add(new Reservation
            {
                Id = 1,
                ParkingLotId = 1,
                UserId = userId,
                VehicleId = 1,
                LicensePlate = "ABC123",
                StartTime = startTime,
                EndTime = endTime,
            });

            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            var result = await service.UpdateReservation(1, new PutReservationDto
            {
                EndTime = endTime.AddHours(1)
            },
            userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal(1, result.ParkingLotId);
            Assert.Equal(userId, result.UserId);
            Assert.Equal("ABC123", result.LicensePlate);
            Assert.Equal(startTime, result.StartTime);
            Assert.Equal(endTime.AddHours(1), result.EndTime);
            Assert.True(result.IsActive);

        }

        [Fact]
        public async Task PutReservation_ShouldThrow_WhenStartTimeIsInThePast()
        {
            // Arrange
            var context = CreateDbContext(Guid.NewGuid().ToString());

            var creationDate = DateTime.UtcNow;

            var userId = Guid.NewGuid();

            context.ParkingLots.Add(new ParkingLot
            {
                Id = 1,
                Name = "Testlot",
                Location = "Rotterdam",
                Address = "Coolsingel 2",
                Capacity = 5,
                Tariff = 2.0,
                DayTariff = 10.0,
                Coordinates = "{latitude: 52.08205549550792, longitude:  4.292821879337678}",
                CreatedAt = creationDate
            });

            context.Users.Add(new User
            {
                Id = userId,
                Username = "TestUser",
                Name = "TestName",
                Email = "123@hr.nl",
                PhoneNumber = "0612345678",
                BirthYear = 2000,
                CreatedAt = creationDate,
                PasswordHash = "dummy-password-hash",
                RefreshToken = null
            });

            context.Vehicles.Add(new Vehicle
            {
                Id = 1,
                UserId = userId,
                LicensePlate = "ABC123",
                Make = "Ford",
                Model = "Focus",
                Color = "Gray",
                Year = 2005,
                CreatedAt = creationDate
            });

            await context.SaveChangesAsync();

            var startTime = DateTimeOffset.UtcNow.AddHours(1);
            var endTime = DateTimeOffset.UtcNow.AddHours(2);

            context.Reservations.Add(new Reservation
            {
                Id = 1,
                ParkingLotId = 1,
                UserId = userId,
                VehicleId = 1,
                LicensePlate = "ABC123",
                StartTime = startTime,
                EndTime = endTime,
            });

            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act and assert
            await Assert.ThrowsAsync<ValidationException>(() =>
                service.UpdateReservation(1, new PutReservationDto
                {
                    StartTime = startTime.AddHours(-4)
                },
                userId)
            );
        }

        [Fact]
        public async Task PutReservation_ShouldThrow_WhenEndTimeIsBeforeStartTime()
        {
            // Arrange
            var context = CreateDbContext(Guid.NewGuid().ToString());
            var creationDate = DateTime.UtcNow;
            var userId = Guid.NewGuid();

            context.ParkingLots.Add(new ParkingLot
            {
                Id = 1,
                Name = "Testlot",
                Location = "Rotterdam",
                Address = "Coolsingel 2",
                Capacity = 5,
                Tariff = 2.0,
                DayTariff = 10.0,
                Coordinates = "{latitude: 52.08205549550792, longitude:  4.292821879337678}",
                CreatedAt = creationDate
            });

            context.Users.Add(new User
            {
                Id = userId,
                Username = "TestUser",
                Name = "TestName",
                Email = "123@hr.nl",
                PhoneNumber = "0612345678",
                BirthYear = 2000,
                CreatedAt = creationDate,
                PasswordHash = "dummy-password-hash",
                RefreshToken = null
            });

            context.Vehicles.Add(new Vehicle
            {
                Id = 1,
                UserId = userId,
                LicensePlate = "ABC123",
                Make = "Ford",
                Model = "Focus",
                Color = "Gray",
                Year = 2005,
                CreatedAt = creationDate
            });

            await context.SaveChangesAsync();

            var startTime = DateTimeOffset.UtcNow.AddHours(1);
            var endTime = startTime.AddHours(2);

            context.Reservations.Add(new Reservation
            {
                Id = 1,
                ParkingLotId = 1,
                UserId = userId,
                VehicleId = 1,
                LicensePlate = "ABC123",
                StartTime = startTime,
                EndTime = endTime,
            });

            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act & Assert
            var dto1 = new PutReservationDto
            {
                StartTime = startTime,
                EndTime = startTime.AddMinutes(-10)
            };

            await Assert.ThrowsAsync<ValidationException>(() => service.UpdateReservation(1, dto1, userId));

            var dto2 = new PutReservationDto
            {
                StartTime = endTime.AddMinutes(10),
                EndTime = endTime
            };

            await Assert.ThrowsAsync<ValidationException>(() => service.UpdateReservation(1, dto2, userId));
        }

        [Fact]
        public async Task PutReservation_ShouldThrow_WhenTimesOverlapAndLotIsFull()
        {
            // Arrange
            var context = CreateDbContext(Guid.NewGuid().ToString());
            var creationDate = DateTime.UtcNow;

            context.ParkingLots.Add(new ParkingLot
            {
                Id = 1,
                Name = "Testlot",
                Location = "Rotterdam",
                Address = "Coolsingel 2",
                Capacity = 5,
                Tariff = 2.0,
                DayTariff = 10.0,
                Coordinates = "{latitude: 51.9239450536725, longitude: 4.47866126718686}",
                CreatedAt = creationDate
            });

            var userId = Guid.NewGuid();
            context.Users.Add(new User
            {
                Id = userId,
                Username = "TestUser",
                Name = "TestName",
                Email = "123@hr.nl",
                PhoneNumber = "0612345678",
                BirthYear = 2000,
                CreatedAt = creationDate,
                PasswordHash = "dummy-password-hash",
                RefreshToken = null
            });

            context.Vehicles.Add(new Vehicle
            {
                UserId = userId,
                LicensePlate = "First12",
                Make = "Ford",
                Model = "Focus",
                Color = "Gray",
                Year = 2005,
                CreatedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();
            var service = CreateService(context);

            var startTime = DateTimeOffset.UtcNow.AddHours(1);
            var endTime = DateTimeOffset.UtcNow.AddHours(2);

            List<string> plates = new() { "ABC1", "ABC2", "ABC3", "ABC4", "ABC5" };
            foreach (var plate in plates)
            {
                var tempUserId = Guid.NewGuid();

                context.Users.Add(new User
                {
                    Id = tempUserId,
                    Username = Guid.NewGuid().ToString(),
                    Name = "TestName",
                    Email = $"{plate}@hr.nl",
                    PhoneNumber = $"{plate[3]}612345678",
                    BirthYear = 2000,
                    CreatedAt = creationDate,
                    PasswordHash = "dummy-password-hash",
                    RefreshToken = null
                });

                context.Vehicles.Add(new Vehicle
                {
                    UserId = tempUserId,
                    LicensePlate = plate,
                    Make = "Ford",
                    Model = "Focus",
                    Color = "Gray",
                    Year = 2005,
                    CreatedAt = DateTime.UtcNow
                });

                await context.SaveChangesAsync();

                await service.CreateReservation(new PostReservationDto
                {
                    ParkingLotId = 1,
                    LicensePlate = plate,
                    StartTime = startTime,
                    EndTime = endTime
                }, tempUserId);
            }

            var firstDto = await service.CreateReservation(new PostReservationDto
            {
                ParkingLotId = 1,
                LicensePlate = "First12",
                StartTime = startTime.AddHours(10),
                EndTime = endTime.AddHours(10)
            }, userId);

            // Act & Assert
            var dto = new PutReservationDto
            {
                StartTime = startTime,
                EndTime = endTime
            };

            await Assert.ThrowsAsync<ParkingLotFullException>(() =>
                service.UpdateReservation(firstDto.Id, dto, userId)
            );
        }

    }

    public class DeleteReservationTest
    {
        private static UserDbContext CreateDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new UserDbContext(options);
        }
        private static ReservationService CreateService(UserDbContext context)
        {
            var encryption = new FakeEncryptionService();
            var discount = new DiscountService(context);

            return new ReservationService(
                context,
                encryption,
                discount
            );
        }

        [Fact]
        public async Task DeleteReservation_ShouldDeleteSuccessfully()
        {
            // Arrange
            var context = CreateDbContext(Guid.NewGuid().ToString());
            var service = CreateService(context);

            var creationDate = DateTime.UtcNow;

            context.ParkingLots.Add(new ParkingLot
            {
                Id = 1,
                Name = "Testlot",
                Location = "Rotterdam",
                Address = "Coolsingel 2",
                Capacity = 5,
                Tariff = 2.0,
                DayTariff = 10.0,
                Coordinates = "{latitude: 51.9239450536725, longitude: 4.47866126718686}",
                CreatedAt = creationDate
            });

            var userId = Guid.NewGuid();

            context.Users.Add(new User
            {
                Id = userId,
                Username = "TestUser",
                Name = "TestName",
                Email = "123@hr.nl",
                PhoneNumber = "0612345678",
                BirthYear = 2000,
                CreatedAt = creationDate,
                PasswordHash = "dummy-password-hash",
                RefreshToken = null
            });

            context.Vehicles.Add(new Vehicle
            {
                Id = 1,
                UserId = userId,
                LicensePlate = "ABC123",
                Make = "Ford",
                Model = "Focus",
                Color = "Gray",
                Year = 2005,
                CreatedAt = creationDate
            });

            context.Reservations.Add(new Reservation
            {
                Id = 1,
                ParkingLotId = 1,
                UserId = userId,
                VehicleId = 1,
                LicensePlate = "ABC123",
                StartTime = DateTimeOffset.UtcNow,
                EndTime = DateTimeOffset.UtcNow.AddHours(1)
            });

            await context.SaveChangesAsync();

            // Act
            var result = await service.DeleteReservation(1, userId);

            // Assert
            Assert.True(result);
            Assert.Empty(await context.Reservations.ToListAsync());
        }

        [Fact]
        public async Task DeleteReservation_ShouldReturnFalse_WhenNotFound()
        {
            // Arrange
            var context = CreateDbContext(Guid.NewGuid().ToString());
            var service = CreateService(context);

            // Act
            var result = await service.DeleteReservation(999, Guid.NewGuid());

            // Assert
            Assert.False(result);
        }

    }
}

