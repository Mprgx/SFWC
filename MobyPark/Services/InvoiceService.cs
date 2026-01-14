using Microsoft.EntityFrameworkCore;

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
        var companyId = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.CompanyId)
            .FirstOrDefaultAsync();

        if (!companyId.HasValue || companyId == Guid.Empty)
            return null;

        var payments = await _context.Payments
            .Include(p => p.Session)
                .ThenInclude(s => s.Vehicle)
            .Include(p => p.Session)
                .ThenInclude(s => s.Reservation)
            .Include(p => p.ParkingLot)
            .Where(p =>
                p.Session.User.CompanyId == companyId.Value &&
                p.Session.Started.Month == month &&
                p.Session.Started.Year == year &&
                p.Session.Stopped != null)
            .ToListAsync();

        if (!payments.Any())
            return null;

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            DateRequested = DateTimeOffset.UtcNow,
            Price = payments.Sum(p => p.AmountWithDiscount),
            PaymentStatus = "Pending",
            Month = month,
            Year = year
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        var billingInvoices = payments.Select(p => new BillingInvoiceRowDto
        {
            Location = p.ParkingLot.Location,
            Name = p.ParkingLot.Name,
            LicensePlate = LicensePlateProtector.DecryptNormalized(_encryption, p.Session.Vehicle.LicensePlate),
            Started = p.Session.Started,
            Stopped = p.Session.Stopped.Value,
            Cost = p.Amount,
            DiscountCode = p.DiscountCode,
            DiscountedCost = p.AmountWithDiscount
        }).ToList();

        var invoiceReloaded = await _context.Invoices
            .Include(i => i.Company)
            .FirstOrDefaultAsync(i => i.Id == invoice.Id);

        var document = new InvoicePdfDocument(invoice, billingInvoices);
        return document.GeneratePdf();
    }
}
