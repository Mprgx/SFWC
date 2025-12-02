using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Models;

namespace MobyPark.Services
{
    public class BillingService(UserDbContext db) : IBillingService
    {
        public async Task<List<BillingReceiptDto>> GetReceiptsForUserAsync(string username)
        {
            return await db.Billings
                .Include(b => b.ParkingLot)
                .Where(b => b.Username == username)
                .Select(b => new BillingReceiptDto
                {
                    Id = b.Id,
                    LicensePlate = b.LicensePlate,
                    ParkingLotId = b.ParkingLotId,
                    ParkingLotName = b.ParkingLot!.Name,
                    Started = b.Started,
                    Stopped = b.Stopped,
                    DurationMinutes = b.DurationMinutes,
                    Cost = Math.Round(b.Cost, 2),
                    PaymentStatus = b.PaymentStatus
                })
                .ToListAsync();
        }

        public async Task<List<BillingReceiptDto>> GetAllReceiptsAsync()
        {
            return await db.Billings
                .Include(b => b.ParkingLot)
                .Select(b => new BillingReceiptDto
                {
                    Id = b.Id,
                    LicensePlate = b.LicensePlate,
                    ParkingLotId = b.ParkingLotId,
                    ParkingLotName = b.ParkingLot!.Name,
                    Started = b.Started,
                    Stopped = b.Stopped,
                    DurationMinutes = b.DurationMinutes,
                    Cost = Math.Round(b.Cost, 2),
                    PaymentStatus = b.PaymentStatus
                })
                .ToListAsync();
        }

        public async Task<BillingReceiptDto?> GetByIdAsync(Guid billingId)
        {
            return await db.Billings
                .Include(b => b.ParkingLot)
                .Where(b => b.Id == billingId)
                .Select(b => new BillingReceiptDto
                {
                    Id = b.Id,
                    LicensePlate = b.LicensePlate,
                    ParkingLotId = b.ParkingLotId,
                    ParkingLotName = b.ParkingLot!.Name,
                    Started = b.Started,
                    Stopped = b.Stopped,
                    DurationMinutes = b.DurationMinutes,
                    Cost = Math.Round(b.Cost, 2),
                    PaymentStatus = b.PaymentStatus
                })
                .FirstOrDefaultAsync();
        }
    }
}
