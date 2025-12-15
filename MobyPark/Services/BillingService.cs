using Microsoft.EntityFrameworkCore;

using MobyPark.Data;
using MobyPark.EncryptionHelper;
using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public class BillingService(UserDbContext db, IEncryptionService encryption) : IBillingService
    {
        public async Task<(List<BillingReceiptDto>? dto, string? error, int? status)> GetReceiptsForUserAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return (null, "Username is required.", 400);
            }

            var billings = await db.Billings
                .Include(b => b.ParkingLot)
                .Where(b => b.Username == username)
                .OrderByDescending(b => b.Stopped)
                .ToListAsync();

            var receipts = billings
                .Select(ToDto)
                .ToList();

            return (receipts, null, null);
        }

        public async Task<(List<BillingReceiptDto>? dto, string? error, int? status)> GetAllReceiptsAsync()
        {
            var billings = await db.Billings
                .Include(b => b.ParkingLot)
                .OrderByDescending(b => b.Stopped)
                .ToListAsync();

            var receipts = billings
                .Select(ToDto)
                .ToList();

            return (receipts, null, null);
        }

        public async Task<(BillingReceiptDto? dto, string? error, int? status)> GetByIdAsync(Guid billingId)
        {
            if (billingId == Guid.Empty)
            {
                return (null, "Billing ID is required.", 400);
            }

            var billing = await db.Billings
                .Include(b => b.ParkingLot)
                .FirstOrDefaultAsync(b => b.Id == billingId);

            if (billing is null)
            {
                return (null, $"Billing with id '{billingId}' was not found.", 404);
            }

            var dto = ToDto(billing);
            return (dto, null, null);
        }

        private BillingReceiptDto ToDto(Billing billing)
        {
            return new()
            {
                Id = billing.Id,
                LicensePlate = LicensePlateProtector.DecryptNormalized(encryption, billing.LicensePlate),
                ParkingLotId = billing.ParkingLotId,
                ParkingLotName = billing.ParkingLot?.Name ?? string.Empty,
                Started = billing.Started,
                Stopped = billing.Stopped,
                DurationMinutes = billing.DurationMinutes,
                Cost = Math.Round(billing.Cost, 2),
                PaymentStatus = billing.PaymentStatus
            };
        }
    }
}
