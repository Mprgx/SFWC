using System.ComponentModel.DataAnnotations;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.Metadata;

using MobyPark.Data;
using MobyPark.EncryptionHelper;
using MobyPark.Entities;
using MobyPark.Models;
using MobyPark.Services;

namespace MobyParkxUnitTest
{
    public class ReservationServiceDiscountTests
    {
        private sealed class TestModelCacheKeyFactory : IModelCacheKeyFactory
        {
            public object Create(DbContext context, bool designTime)
                => (context.GetType(), context.Database.ProviderName, designTime);

            public object Create(DbContext context)
                => Create(context, designTime: false);
        }

        private sealed class TestUserDbContext : UserDbContext
        {
            public TestUserDbContext(DbContextOptions<UserDbContext> options) : base(options) { }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                var dtoConverter = new ValueConverter<DateTimeOffset, string>(
                    v => v.ToUniversalTime().ToString("O"),
                    v => DateTimeOffset.Parse(v).ToUniversalTime());

                var dtoNullableConverter = new ValueConverter<DateTimeOffset?, string?>(
                    v => v.HasValue ? v.Value.ToUniversalTime().ToString("O") : null,
                    v => v == null ? null : DateTimeOffset.Parse(v).ToUniversalTime());

                foreach (var entity in modelBuilder.Model.GetEntityTypes())
                {
                    foreach (var prop in entity.GetProperties())
                    {
                        var storeType = prop.GetColumnType();
                        if (!string.IsNullOrWhiteSpace(storeType))
                        {
                            var st = storeType!.ToLowerInvariant().Replace(" ", "");
                            if (st == "nvarchar(max)" || st == "varchar(max)") prop.SetColumnType("TEXT");
                            else if (st == "varbinary(max)") prop.SetColumnType("BLOB");
                        }

                        if (prop.ClrType == typeof(DateTimeOffset))
                        {
                            prop.SetValueConverter(dtoConverter);
                            prop.SetColumnType("TEXT");
                        }
                        else if (prop.ClrType == typeof(DateTimeOffset?))
                        {
                            prop.SetValueConverter(dtoNullableConverter);
                            prop.SetColumnType("TEXT");
                        }
                    }
                }
            }
        }

        private static UserDbContext CreateDbContext(string dbName, out SqliteConnection connection)
        {
            connection = new SqliteConnection("Filename=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseSqlite(connection)
                .ReplaceService<IModelCacheKeyFactory, TestModelCacheKeyFactory>()
                .EnableSensitiveDataLogging()
                .Options;

            var context = new TestUserDbContext(options);
            context.Database.EnsureCreated();

            return context;
        }

        private class FakeEncryptionService : IEncryptionService
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

        private static ReservationService CreateService(UserDbContext context, IEncryptionService? encryption = null)
        {
            var enc = encryption ?? new FakeEncryptionService();
            var discountService = new DiscountService(context);
            return new ReservationService(context, enc, discountService);
        }

        private static async Task<(Guid userId, int vehicleId, int lotId)> SeedBaseAsync(
            UserDbContext context,
            IEncryptionService encryption,
            int lotId = 1,
            double tariff = 5.0,
            int capacity = 100,
            string licensePlatePlain = "12-AB-34")
        {
            var userId = Guid.NewGuid();

            context.Users.Add(new User
            {
                Id = userId,
                Username = "testuser",
                Name = "Test User",
                Email = string.Empty,
                PhoneNumber = string.Empty,
                BirthYear = 2000,
                CreatedAt = DateTimeOffset.UtcNow,
                Role = UserRole.Customer,
                PasswordHash = "hash"
            });

            context.ParkingLots.Add(new ParkingLot
            {
                Id = lotId,
                Name = "Test Lot",
                Location = "Test City",
                Address = "Test Street 1",
                Capacity = capacity,
                Tariff = tariff,
                DayTariff = tariff,
                Coordinates = "{}"
            });

            var vehicle = new Vehicle
            {
                UserId = userId,
                LicensePlate = LicensePlateProtector.Encrypt(encryption, licensePlatePlain),
                Make = "Toyota",
                Model = "Yaris",
                Color = "Black",
                Year = 2018
            };

            context.Vehicles.Add(vehicle);

            await context.SaveChangesAsync();
            return (userId, vehicle.Id, lotId);
        }

        private static Company NewCompany(Guid id)
        {
            return new Company
            {
                Id = id,
                CompanyName = "Test Company",
                Street = "Test Street 1",
                PostalCode = "1234AB",
                City = "Rotterdam",
                Country = "NL",
                ContactEmail = "company@test.local",
                ContactPhone = "0612345678",
                ContactPerson = "Tester",
                KvKNumber = "12345678",
                VatNumber = "NL123456789B01",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }

        private static CompanyUser NewCompanyUser(Guid userId, Guid companyId, bool isOrganisationAdmin = false)
        {
            return new CompanyUser
            {
                UserId = userId,
                CompanyId = companyId,
                IsOrganisationAdmin = isOrganisationAdmin
            };
        }

        private static Discount NewDiscount(
            string code,
            Guid createdBy,
            DiscountType type,
            decimal value,
            DateTimeOffset validFrom,
            DateTimeOffset validUntil,
            bool active = true,
            int? maxUsage = null,
            int? currentUsage = null,
            TimeSpan? windowStart = null,
            TimeSpan? windowEnd = null,
            IEnumerable<int>? allowedLots = null,
            IEnumerable<Guid>? validUsers = null,
            IEnumerable<Guid>? validCompanies = null)
        {
            var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();

            var d = new Discount
            {
                Code = normalized,
                CreatedBy = createdBy,
                CreatedAt = DateTimeOffset.UtcNow,
                Type = type,
                Value = value,
                ValidFrom = validFrom,
                ValidUntil = validUntil,
                Active = active,
                MaxUsage = maxUsage,
                CurrentUsage = currentUsage,
                TimeWindowStart = windowStart,
                TimeWindowEnd = windowEnd
            };

            if (allowedLots is not null)
            {
                foreach (var id in allowedLots.Distinct())
                    d.AllowedLocations.Add(new DiscountLocation { Code = normalized, ParkingLotId = id });
            }

            if (validUsers is not null)
            {
                foreach (var uid in validUsers.Distinct())
                    d.ValidForUsers.Add(new DiscountUser { Code = normalized, UserId = uid });
            }

            if (validCompanies is not null)
            {
                foreach (var cid in validCompanies.Distinct())
                    d.ValidForCompanies.Add(new DiscountCompany { Code = normalized, CompanyId = cid });
            }

            return d;
        }

        private static PostReservationDto NewReservationDto(
            int lotId,
            int vehicleId,
            DateTimeOffset startUtc,
            DateTimeOffset endUtc,
            string? discountCode)
        {
            return new PostReservationDto
            {
                ParkingLotId = lotId,
                VehicleId = vehicleId,
                StartTime = startUtc,
                EndTime = endUtc,
                DiscountCode = discountCode
            };
        }

        [Fact]
        public async Task CreateReservation_AppliesPercentageDiscount_WhenDiscountIsValid()
        {
            using var context = CreateDbContext(nameof(CreateReservation_AppliesPercentageDiscount_WhenDiscountIsValid), out var connection);
            using var _ = connection;

            var encryption = new FakeEncryptionService();
            var (userId, vehicleId, lotId) = await SeedBaseAsync(context, encryption, tariff: 5.0);

            context.Discounts.Add(NewDiscount(
                code: "save10",
                createdBy: userId,
                type: DiscountType.Percentage,
                value: 10m,
                validFrom: DateTimeOffset.UtcNow.AddDays(-1),
                validUntil: DateTimeOffset.UtcNow.AddDays(5)));

            await context.SaveChangesAsync();

            var service = CreateService(context, encryption);

            var start = DateTimeOffset.UtcNow.AddHours(2);
            var end = start.AddMinutes(90);

            var dto = NewReservationDto(lotId, vehicleId, start, end, discountCode: "  SaVe10  ");
            var result = await service.CreateReservation(dto, userId);

            Assert.NotNull(result);
            Assert.Equal("SAVE10", result.DiscountCode);
            Assert.Equal(10m, result.EstimatedCost);
            Assert.Equal(9m, result.EstimatedCostWithDiscount);

            var stored = await context.Reservations.SingleAsync();
            Assert.Equal("SAVE10", stored.DiscountCode);
        }

        [Fact]
        public async Task CreateReservation_AppliesFixedAmountDiscount_AndClampsToZero()
        {
            using var context = CreateDbContext(nameof(CreateReservation_AppliesFixedAmountDiscount_AndClampsToZero), out var connection);
            using var _ = connection;

            var encryption = new FakeEncryptionService();
            var (userId, vehicleId, lotId) = await SeedBaseAsync(context, encryption, tariff: 5.0);

            context.Discounts.Add(NewDiscount(
                code: "BIGOFF",
                createdBy: userId,
                type: DiscountType.FixedAmount,
                value: 50m,
                validFrom: DateTimeOffset.UtcNow.AddDays(-1),
                validUntil: DateTimeOffset.UtcNow.AddDays(5)));

            await context.SaveChangesAsync();

            var service = CreateService(context, encryption);

            var start = DateTimeOffset.UtcNow.AddHours(2);
            var end = start.AddMinutes(1);

            var dto = NewReservationDto(lotId, vehicleId, start, end, discountCode: "BIGOFF");
            var result = await service.CreateReservation(dto, userId);

            Assert.Equal(5m, result.EstimatedCost);
            Assert.Equal(0m, result.EstimatedCostWithDiscount);
            Assert.Equal("BIGOFF", result.DiscountCode);
        }

        [Fact]
        public async Task CreateReservation_DoesNotApplyDiscount_WhenDiscountCodeIsNullOrWhitespace()
        {
            using var context = CreateDbContext(nameof(CreateReservation_DoesNotApplyDiscount_WhenDiscountCodeIsNullOrWhitespace), out var connection);
            using var _ = connection;

            var encryption = new FakeEncryptionService();
            var (userId, vehicleId, lotId) = await SeedBaseAsync(context, encryption, tariff: 5.0);

            var service = CreateService(context, encryption);

            var start = DateTimeOffset.UtcNow.AddHours(2);
            var end = start.AddHours(2);

            var dto = NewReservationDto(lotId, vehicleId, start, end, discountCode: "   ");
            var result = await service.CreateReservation(dto, userId);

            Assert.Null(result.DiscountCode);
            Assert.Equal(10m, result.EstimatedCost);
            Assert.Equal(10m, result.EstimatedCostWithDiscount);

            var stored = await context.Reservations.SingleAsync();
            Assert.Null(stored.DiscountCode);
        }

        [Fact]
        public async Task CreateReservation_AppliesDiscount_WhenUserIsMemberOfAuthorizedCompany()
        {
            using var context = CreateDbContext(nameof(CreateReservation_AppliesDiscount_WhenUserIsMemberOfAuthorizedCompany), out var connection);
            using var _ = connection;

            var encryption = new FakeEncryptionService();
            var (userId, vehicleId, lotId) = await SeedBaseAsync(context, encryption, tariff: 10.0);

            var companyId = Guid.NewGuid();
            context.Companies.Add(NewCompany(companyId));
            context.CompanyUsers.Add(NewCompanyUser(userId, companyId));

            context.Discounts.Add(NewDiscount(
                code: "B2B",
                createdBy: userId,
                type: DiscountType.FixedAmount,
                value: 3m,
                validFrom: DateTimeOffset.UtcNow.AddDays(-1),
                validUntil: DateTimeOffset.UtcNow.AddDays(10),
                active: true,
                validCompanies: new[] { companyId }));

            await context.SaveChangesAsync();

            var service = CreateService(context, encryption);

            var start = DateTimeOffset.UtcNow.AddHours(2);
            var end = start.AddMinutes(10);

            var dto = NewReservationDto(lotId, vehicleId, start, end, discountCode: "B2B");
            var result = await service.CreateReservation(dto, userId);

            Assert.Equal("B2B", result.DiscountCode);
            Assert.Equal(10m, result.EstimatedCost);
            Assert.Equal(7m, result.EstimatedCostWithDiscount);
        }

        [Fact]
        public async Task CreateReservation_AppliesDiscount_WhenTimeWindowCrossesMidnight_AndTimeIsInside()
        {
            using var context = CreateDbContext(nameof(CreateReservation_AppliesDiscount_WhenTimeWindowCrossesMidnight_AndTimeIsInside), out var connection);
            using var _ = connection;

            var encryption = new FakeEncryptionService();
            var (userId, vehicleId, lotId) = await SeedBaseAsync(context, encryption, tariff: 10.0);

            context.Discounts.Add(NewDiscount(
                code: "NIGHT",
                createdBy: userId,
                type: DiscountType.FixedAmount,
                value: 3m,
                validFrom: DateTimeOffset.UtcNow.AddDays(-1),
                validUntil: DateTimeOffset.UtcNow.AddDays(10),
                active: true,
                windowStart: new TimeSpan(22, 0, 0),
                windowEnd: new TimeSpan(2, 0, 0)));

            await context.SaveChangesAsync();

            var service = CreateService(context, encryption);

            var day = DateTimeOffset.UtcNow.Date.AddDays(1);
            var start = new DateTimeOffset(day.Year, day.Month, day.Day, 23, 0, 0, TimeSpan.Zero);
            var end = start.AddMinutes(10);

            var dto = NewReservationDto(lotId, vehicleId, start, end, discountCode: "NIGHT");
            var result = await service.CreateReservation(dto, userId);

            Assert.Equal(10m, result.EstimatedCost);
            Assert.Equal(7m, result.EstimatedCostWithDiscount);
            Assert.Equal("NIGHT", result.DiscountCode);
        }

        [Fact]
        public async Task CreateReservation_ThrowsKeyNotFoundException_WhenDiscountDoesNotExist_AndDoesNotCreateReservation()
        {
            using var context = CreateDbContext(nameof(CreateReservation_ThrowsKeyNotFoundException_WhenDiscountDoesNotExist_AndDoesNotCreateReservation), out var connection);
            using var _ = connection;

            var encryption = new FakeEncryptionService();
            var (userId, vehicleId, lotId) = await SeedBaseAsync(context, encryption);

            var service = CreateService(context, encryption);

            var start = DateTimeOffset.UtcNow.AddHours(2);
            var end = start.AddHours(1);

            var dto = NewReservationDto(lotId, vehicleId, start, end, discountCode: "NOPE");

            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateReservation(dto, userId));
            Assert.Equal("Discount code NOPE not found.", ex.Message);

            Assert.Equal(0, await context.Reservations.CountAsync());
        }

        [Fact]
        public async Task CreateReservation_ThrowsValidationException_WhenDiscountIsDeactivated_AndDoesNotCreateReservation()
        {
            using var context = CreateDbContext(nameof(CreateReservation_ThrowsValidationException_WhenDiscountIsDeactivated_AndDoesNotCreateReservation), out var connection);
            using var _ = connection;

            var encryption = new FakeEncryptionService();
            var (userId, vehicleId, lotId) = await SeedBaseAsync(context, encryption);

            context.Discounts.Add(NewDiscount(
                code: "OFF",
                createdBy: userId,
                type: DiscountType.FixedAmount,
                value: 2m,
                validFrom: DateTimeOffset.UtcNow.AddDays(-1),
                validUntil: DateTimeOffset.UtcNow.AddDays(5),
                active: false));

            await context.SaveChangesAsync();

            var service = CreateService(context, encryption);

            var start = DateTimeOffset.UtcNow.AddHours(2);
            var end = start.AddHours(1);
            var dto = NewReservationDto(lotId, vehicleId, start, end, discountCode: "OFF");

            var ex = await Assert.ThrowsAsync<ValidationException>(() => service.CreateReservation(dto, userId));
            Assert.Equal("This discount code is deactivated.", ex.Message);

            Assert.Equal(0, await context.Reservations.CountAsync());
        }

        [Fact]
        public async Task CreateReservation_ThrowsValidationException_WhenDiscountExpired_AndDoesNotCreateReservation()
        {
            using var context = CreateDbContext(nameof(CreateReservation_ThrowsValidationException_WhenDiscountExpired_AndDoesNotCreateReservation), out var connection);
            using var _ = connection;

            var encryption = new FakeEncryptionService();
            var (userId, vehicleId, lotId) = await SeedBaseAsync(context, encryption);

            context.Discounts.Add(NewDiscount(
                code: "EXPIRED",
                createdBy: userId,
                type: DiscountType.Percentage,
                value: 10m,
                validFrom: DateTimeOffset.UtcNow.AddDays(-10),
                validUntil: DateTimeOffset.UtcNow.AddDays(-1),
                active: true));

            await context.SaveChangesAsync();

            var service = CreateService(context, encryption);

            var start = DateTimeOffset.UtcNow.AddHours(2);
            var end = start.AddHours(1);
            var dto = NewReservationDto(lotId, vehicleId, start, end, discountCode: "EXPIRED");

            var ex = await Assert.ThrowsAsync<ValidationException>(() => service.CreateReservation(dto, userId));
            Assert.Equal("This discount has expired.", ex.Message);

            Assert.Equal(0, await context.Reservations.CountAsync());
        }

        [Fact]
        public async Task CreateReservation_ThrowsValidationException_WhenDiscountNotYetActive_AndDoesNotCreateReservation()
        {
            using var context = CreateDbContext(nameof(CreateReservation_ThrowsValidationException_WhenDiscountNotYetActive_AndDoesNotCreateReservation), out var connection);
            using var _ = connection;

            var encryption = new FakeEncryptionService();
            var (userId, vehicleId, lotId) = await SeedBaseAsync(context, encryption);

            context.Discounts.Add(NewDiscount(
                code: "FUTURE",
                createdBy: userId,
                type: DiscountType.FixedAmount,
                value: 2m,
                validFrom: DateTimeOffset.UtcNow.AddDays(2),
                validUntil: DateTimeOffset.UtcNow.AddDays(10),
                active: true));

            await context.SaveChangesAsync();

            var service = CreateService(context, encryption);

            var start = DateTimeOffset.UtcNow.AddHours(2);
            var end = start.AddHours(1);
            var dto = NewReservationDto(lotId, vehicleId, start, end, discountCode: "FUTURE");

            var ex = await Assert.ThrowsAsync<ValidationException>(() => service.CreateReservation(dto, userId));
            Assert.Equal("This discount is not yet active.", ex.Message);

            Assert.Equal(0, await context.Reservations.CountAsync());
        }

        [Fact]
        public async Task CreateReservation_ThrowsValidationException_WhenMaxUsageReached_AndDoesNotCreateReservation()
        {
            using var context = CreateDbContext(nameof(CreateReservation_ThrowsValidationException_WhenMaxUsageReached_AndDoesNotCreateReservation), out var connection);
            using var _ = connection;

            var encryption = new FakeEncryptionService();
            var (userId, vehicleId, lotId) = await SeedBaseAsync(context, encryption);

            context.Discounts.Add(NewDiscount(
                code: "LIMITED",
                createdBy: userId,
                type: DiscountType.FixedAmount,
                value: 1m,
                validFrom: DateTimeOffset.UtcNow.AddDays(-1),
                validUntil: DateTimeOffset.UtcNow.AddDays(10),
                active: true,
                maxUsage: 5,
                currentUsage: 5));

            await context.SaveChangesAsync();

            var service = CreateService(context, encryption);

            var start = DateTimeOffset.UtcNow.AddHours(2);
            var end = start.AddHours(1);
            var dto = NewReservationDto(lotId, vehicleId, start, end, discountCode: "LIMITED");

            var ex = await Assert.ThrowsAsync<ValidationException>(() => service.CreateReservation(dto, userId));
            Assert.Equal("Discount code LIMITED has reached its maximum usage.", ex.Message);

            Assert.Equal(0, await context.Reservations.CountAsync());
        }

        [Fact]
        public async Task CreateReservation_ThrowsValidationException_WhenNotValidForUser_AndDoesNotCreateReservation()
        {
            using var context = CreateDbContext(nameof(CreateReservation_ThrowsValidationException_WhenNotValidForUser_AndDoesNotCreateReservation), out var connection);
            using var _ = connection;

            var encryption = new FakeEncryptionService();
            var (userId, vehicleId, lotId) = await SeedBaseAsync(context, encryption);

            var otherUser = Guid.NewGuid();
            context.Users.Add(new User
            {
                Id = otherUser,
                Username = "other",
                Name = "Other",
                Email = "",
                PhoneNumber = "",
                BirthYear = 1995,
                CreatedAt = DateTimeOffset.UtcNow,
                Role = UserRole.Customer,
                PasswordHash = "hash"
            });

            context.Discounts.Add(NewDiscount(
                code: "VIP",
                createdBy: userId,
                type: DiscountType.Percentage,
                value: 20m,
                validFrom: DateTimeOffset.UtcNow.AddDays(-1),
                validUntil: DateTimeOffset.UtcNow.AddDays(10),
                active: true,
                validUsers: new[] { otherUser }));

            await context.SaveChangesAsync();

            var service = CreateService(context, encryption);

            var start = DateTimeOffset.UtcNow.AddHours(2);
            var end = start.AddHours(1);
            var dto = NewReservationDto(lotId, vehicleId, start, end, discountCode: "VIP");

            var ex = await Assert.ThrowsAsync<ValidationException>(() => service.CreateReservation(dto, userId));
            Assert.Equal("This discount code is not valid for your account.", ex.Message);

            Assert.Equal(0, await context.Reservations.CountAsync());
        }

        [Fact]
        public async Task CreateReservation_ThrowsValidationException_WhenNotValidForCompanyMembership_AndDoesNotCreateReservation()
        {
            using var context = CreateDbContext(nameof(CreateReservation_ThrowsValidationException_WhenNotValidForCompanyMembership_AndDoesNotCreateReservation), out var connection);
            using var _ = connection;

            var encryption = new FakeEncryptionService();
            var (userId, vehicleId, lotId) = await SeedBaseAsync(context, encryption);

            var companyId = Guid.NewGuid();
            context.Companies.Add(NewCompany(companyId));

            context.Discounts.Add(NewDiscount(
                code: "B2B",
                createdBy: userId,
                type: DiscountType.FixedAmount,
                value: 2m,
                validFrom: DateTimeOffset.UtcNow.AddDays(-1),
                validUntil: DateTimeOffset.UtcNow.AddDays(10),
                active: true,
                validCompanies: new[] { companyId }));

            await context.SaveChangesAsync();

            var service = CreateService(context, encryption);

            var start = DateTimeOffset.UtcNow.AddHours(2);
            var end = start.AddHours(1);
            var dto = NewReservationDto(lotId, vehicleId, start, end, discountCode: "B2B");

            var ex = await Assert.ThrowsAsync<ValidationException>(() => service.CreateReservation(dto, userId));
            Assert.Equal("This discount code is only valid for specific companies you are not a part of.", ex.Message);

            Assert.Equal(0, await context.Reservations.CountAsync());
        }

        [Fact]
        public async Task CreateReservation_ThrowsValidationException_WhenNotValidForParkingLot_AndDoesNotCreateReservation()
        {
            using var context = CreateDbContext(nameof(CreateReservation_ThrowsValidationException_WhenNotValidForParkingLot_AndDoesNotCreateReservation), out var connection);
            using var _ = connection;

            var encryption = new FakeEncryptionService();
            var (userId, vehicleId, lotId) = await SeedBaseAsync(context, encryption, lotId: 1);

            context.ParkingLots.Add(new ParkingLot
            {
                Id = 2,
                Name = "Other Lot",
                Location = "Other City",
                Address = "Other Street 2",
                Capacity = 100,
                Tariff = 5.0,
                DayTariff = 5.0,
                Coordinates = "{}"
            });

            context.Discounts.Add(NewDiscount(
                code: "LOT2",
                createdBy: userId,
                type: DiscountType.FixedAmount,
                value: 2m,
                validFrom: DateTimeOffset.UtcNow.AddDays(-1),
                validUntil: DateTimeOffset.UtcNow.AddDays(10),
                active: true,
                allowedLots: new[] { 2 }));

            await context.SaveChangesAsync();

            var service = CreateService(context, encryption);

            var start = DateTimeOffset.UtcNow.AddHours(2);
            var end = start.AddHours(1);
            var dto = NewReservationDto(lotId, vehicleId, start, end, discountCode: "LOT2");

            var ex = await Assert.ThrowsAsync<ValidationException>(() => service.CreateReservation(dto, userId));
            Assert.Equal("This discount is not valid for this parking lot.", ex.Message);

            Assert.Equal(0, await context.Reservations.CountAsync());
        }

        [Fact]
        public async Task CreateReservation_ThrowsValidationException_WhenOutsideTimeWindow_AndDoesNotCreateReservation()
        {
            using var context = CreateDbContext(nameof(CreateReservation_ThrowsValidationException_WhenOutsideTimeWindow_AndDoesNotCreateReservation), out var connection);
            using var _ = connection;

            var encryption = new FakeEncryptionService();
            var (userId, vehicleId, lotId) = await SeedBaseAsync(context, encryption);

            context.Discounts.Add(NewDiscount(
                code: "MORNING",
                createdBy: userId,
                type: DiscountType.Percentage,
                value: 20m,
                validFrom: DateTimeOffset.UtcNow.AddDays(-1),
                validUntil: DateTimeOffset.UtcNow.AddDays(10),
                active: true,
                windowStart: new TimeSpan(8, 0, 0),
                windowEnd: new TimeSpan(10, 0, 0)));

            await context.SaveChangesAsync();

            var service = CreateService(context, encryption);

            var day = DateTimeOffset.UtcNow.Date.AddDays(1);
            var start = new DateTimeOffset(day.Year, day.Month, day.Day, 12, 0, 0, TimeSpan.Zero);
            var end = start.AddMinutes(10);

            var dto = NewReservationDto(lotId, vehicleId, start, end, discountCode: "MORNING");

            var ex = await Assert.ThrowsAsync<ValidationException>(() => service.CreateReservation(dto, userId));
            Assert.Equal("This discount is only valid between 08:00 and 10:00 UTC.", ex.Message);

            Assert.Equal(0, await context.Reservations.CountAsync());
        }

        [Fact]
        public async Task CreateReservation_ThrowsValidationException_WhenStartOrEndNotUtc()
        {
            using var context = CreateDbContext(nameof(CreateReservation_ThrowsValidationException_WhenStartOrEndNotUtc), out var connection);
            using var _ = connection;

            var encryption = new FakeEncryptionService();
            var (userId, vehicleId, lotId) = await SeedBaseAsync(context, encryption);

            var service = CreateService(context, encryption);

            var start = new DateTimeOffset(2026, 1, 17, 10, 0, 0, TimeSpan.FromHours(1));
            var end = start.AddHours(1);

            var dto = NewReservationDto(lotId, vehicleId, start, end, discountCode: null);

            var ex = await Assert.ThrowsAsync<ValidationException>(() => service.CreateReservation(dto, userId));
            Assert.Equal("StartTime and EndTime must be UTC (offset +00:00).", ex.Message);

            Assert.Equal(0, await context.Reservations.CountAsync());
        }
    }
}
