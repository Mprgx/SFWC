using Microsoft.EntityFrameworkCore;

using MobyPark.Constants;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Services;

using QuestPDF.Fluent;


public class InvoiceService : IInvoiceService
{
    private readonly UserDbContext _context;

    public InvoiceService(UserDbContext context)
    {
        _context = context;
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
            PaymentMethod = "Invoice"
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        var document = new InvoicePdfDocument(invoice, billings);
        return document.GeneratePdf();
    }
}

