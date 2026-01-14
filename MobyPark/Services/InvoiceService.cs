using Microsoft.EntityFrameworkCore;

using MobyPark.Constants;
using MobyPark.Data;
using MobyPark.EncryptionHelper;
using MobyPark.Entities;
using MobyPark.Models;
using MobyPark.Services;

using QuestPDF.Fluent;


public class InvoiceService : IInvoiceService
{
    private readonly UserDbContext _context;
    private readonly IEncryptionService _encryption;

    public InvoiceService(UserDbContext context, IEncryptionService encryption)
    {
        _context = context;
        _encryption = encryption;
    }

    public async Task<byte[]?> GenerateMonthlyInvoicePdfAsync(Guid userId, int month, int year)
    {
        Guid? companyId = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.CompanyId)
            .FirstOrDefaultAsync();

        if (!companyId.HasValue)
            return null;

        if (companyId == Guid.Empty)
            return null;

        var billings = await _context.Billings
            .Include(b => b.ParkingLot)
            .Include(b => b.Session)
                .ThenInclude(s => s.Vehicle)
            .Where(b =>
                b.Session.User.CompanyId == companyId.Value &&
                b.Started.Month == month &&
                b.Started.Year == year)
            .ToListAsync();

        if (!billings.Any())
            return null;

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Date = DateTimeOffset.UtcNow,
            Price = billings.Sum(b => b.Cost),
            PaymentStatus = "Pending",
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        var billingInvoices = new List<BillingInvoiceRowDto>();

        foreach (var billing in billings)
        {
            var discountedPrice = await GetDiscountedPriceAsync(
                billing.Cost,
                billing.Session.Reservation?.DiscountCode ?? ""
            );

            billingInvoices.Add(new BillingInvoiceRowDto
            {
                Location = billing.ParkingLot.Location,
                LicensePlate = LicensePlateProtector.DecryptNormalized(
                    _encryption,
                    billing.Session.Vehicle.LicensePlate
                ),
                Started = billing.Started,
                Stopped = billing.Stopped,
                Cost = billing.Cost,
                DiscountCode = billing.Session.Reservation?.DiscountCode,
                DiscountedCost = discountedPrice
            });
        }

        var document = new InvoicePdfDocument(invoice, billingInvoices);
        return document.GeneratePdf();
    }

    private async Task<decimal> GetDiscountedPriceAsync(decimal initialPrice, string discountCode)
    {
        var discount = await _context.Discounts.Where(d => d.Code == discountCode).FirstOrDefaultAsync();

        if (discount is null)
            return initialPrice;

        if (discount.Type == MobyPark.Models.DiscountType.Percentage)
        {
            return initialPrice - (initialPrice * (discount.Value / 100m));
        }
        else return initialPrice - discount.Value;
    }
}

