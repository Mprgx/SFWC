using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

using MobyPark.Data;

namespace MobyPark.Services
{
    public class DataMigrationService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DataMigrationService> _logger;

        public DataMigrationService(IServiceScopeFactory scopeFactory, ILogger<DataMigrationService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task PrepareDatabaseAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<UserDbContext>();

            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Payments_Hash')
                CREATE INDEX IX_Payments_Hash ON Payments (Hash)");

            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Staging_Init')
                CREATE INDEX IX_Staging_Init ON StagingPayments (initiator)");

            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_UserName')
                CREATE INDEX IX_Users_UserName ON dbo.Users (UserName)");
        }

        public async Task MigrateTransactionsAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<UserDbContext>();

            context.Database.SetCommandTimeout(3600);

            int batchSize = 50000;
            int totalProcessed = 0;
            bool rowsLeft = true;

            while (rowsLeft)
            {
                var batchParam = new SqlParameter("@batchSize", batchSize);

                var sql = @"
                    INSERT INTO Payments (
                        [Transaction], Amount, DiscountCode, AmountWithDiscount, 
                        Initiator, UserId, Created_At, Completed, Hash, T_Data, 
                        SessionId, ParkingLotId
                    )
                    SELECT TOP (@batchSize)
                        s.[transaction],
                        ISNULL(TRY_CONVERT(FLOAT, REPLACE(s.amount, ',', '.')) / 100, 0),
                        NULL, 
                        ISNULL(TRY_CONVERT(FLOAT, REPLACE(s.amount, ',', '.')) / 100, 0),
                        s.initiator,
                        u.Id, 
                        TRY_CONVERT(DATETIME, LEFT(s.created_at, 19), 105), 
                        TRY_CONVERT(DATETIME, LEFT(s.completed, 19), 105),
                        s.hash,
                        s.t_data,
                        NULL, 
                        1     
                    FROM StagingPayments s
                    INNER JOIN dbo.Users u ON s.initiator = u.UserName
                    WHERE NOT EXISTS (
                        SELECT 1 FROM Payments p WHERE p.Hash = s.hash
                    )
                    ORDER BY s.hash;";

                try
                {
                    var affectedRows = await context.Database.ExecuteSqlRawAsync(sql, batchParam);

                    if (affectedRows == 0)
                    {
                        rowsLeft = false;
                        _logger.LogInformation("Migration done.");
                    }
                    else
                    {
                        totalProcessed += affectedRows;
                        _logger.LogInformation($"{totalProcessed} rows processed...");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception occurred");
                    throw;
                }
            }
        }

        public async Task MigrateParkingLotsAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<UserDbContext>();

            var sql = @"
                SET IDENTITY_INSERT ParkingLots ON;

                INSERT INTO ParkingLots (
                    Id, Name, Location, Address, Capacity, 
                    Tariff, DayTariff, CreatedAt, Coordinates
                )
                SELECT 
                    s.id,
                    s.name,
                    s.location,
                    s.address,
                    TRY_CONVERT(INT, NULLIF(s.capacity, '')),
                    ISNULL(TRY_CONVERT(FLOAT, REPLACE(s.tariff, ',', '.')), 4),                   
                    ISNULL(TRY_CONVERT(FLOAT, REPLACE(s.daytariff, ',', '.')), 20),
                    COALESCE(TRY_CONVERT(DATETIME, LEFT(s.created_at, 19), 105), GETUTCDATE()),
                    CONCAT('{{""coordinates"": {{""lat"": ', s.lat, ', ""lng"": ', s.lng, '}}}}')
                FROM StagingParkingLots s
                WHERE NOT EXISTS (
                    SELECT 1 FROM ParkingLots p WHERE p.Id = s.id
                );

                SET IDENTITY_INSERT ParkingLots OFF;";

            await context.Database.ExecuteSqlRawAsync(sql);
            _logger.LogInformation("ParkingLots migrated.");
        }
    }
}