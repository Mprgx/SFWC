using Microsoft.EntityFrameworkCore;

using MobyPark.Data;
using MobyPark.EncryptionHelper;
using MobyPark.Entities;
using MobyPark.Models;
using MobyPark.Services;

using QuestPDF.Fluent;

using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;

using System.IO.Compression;

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
            UserId = userId,
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
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.Id == invoice.Id);

        if (invoiceReloaded == null)
            return null;

        var document = new InvoicePdfDocument(invoiceReloaded, billingInvoices);
        var pdfBytes = document.GeneratePdf();

        invoiceReloaded.PdfData = pdfBytes;
        await _context.SaveChangesAsync();

        return pdfBytes;
    }

    public async Task<byte[]?> GenerateAllPdfAsync(Guid userId)
    {
        var companyId = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.CompanyId)
            .FirstOrDefaultAsync();

        if (!companyId.HasValue || companyId == Guid.Empty)
            return null;

        var invoices = await _context.Invoices
            .Where(i => i.CompanyId == companyId.Value)
            .OrderBy(i => i.Year)
            .ThenBy(i => i.Month)
            .ToListAsync();

        if (!invoices.Any())
            return null;

        using var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
        {
            foreach (var invoice in invoices)
            {
                if (invoice.PdfData == null || invoice.PdfData.Length == 0)
                    continue;

                var fileName = $"Invoice_{invoice.Id}_{invoice.Year}_{invoice.Month:D2}.pdf";
                var zipEntry = archive.CreateEntry(fileName);
                zipEntry.LastWriteTime = invoice.DateRequested;

                using var entryStream = zipEntry.Open();
                await entryStream.WriteAsync(invoice.PdfData, 0, invoice.PdfData.Length);
            }
        }

        if (zipStream.Length == 0)
            return null;

        return zipStream.ToArray();
    }
}
