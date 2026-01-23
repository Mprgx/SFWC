using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MobyPark.Constants;
using MobyPark.Data;
using MobyPark.Models;
using MobyPark.Services;

namespace MobyPark.Controllers
{
    [ApiController]
    [Route("api/migrate")]
    [Authorize(Roles = Roles.Admin)]
    public class MigrationController(DataMigrationService migrationService, ILogger<MigrationController> logger, UserDbContext context) : ControllerBase
    {
        [HttpGet("payments/start")]
        public IActionResult StartPayments()
        {
            if (context.Payments.Any())
            {
                return UnprocessableEntity("There already is data in the db, unable to migrate.");
            }
            _ = Task.Run(async () =>
            {
                try
                {
                    await migrationService.PrepareDatabaseAsync();
                    await migrationService.MigrateTransactionsAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Migration has failed");
                }
            });

            return Accepted("Migration started.");
        }

        [HttpGet("parking-lots/start")]
        public async Task<IActionResult> StartParkingLots()
        {
            if (await context.ParkingLots.AnyAsync())
            {
                return UnprocessableEntity("There already is data in the db, unable to migrate.");
            }

            await migrationService.MigrateParkingLotsAsync();
            return Ok();
        }
    }
}
