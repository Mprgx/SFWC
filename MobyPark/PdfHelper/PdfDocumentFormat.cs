using MobyPark.Entities;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

public class InvoicePdfDocument : IDocument
{
    private readonly Invoice _invoice;
    private readonly IReadOnlyList<Billing> _billings;

    public InvoicePdfDocument(Invoice invoice, IReadOnlyList<Billing> billings)
    {
        _invoice = invoice;
        _billings = billings;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(12));

            page.Content().Column(column =>
            {
                Header(column);
                BillingTable(column);
                Totals(column);
                Footer(column);
            });
        });
    }

    private void Header(ColumnDescriptor column)
    {
        column.Item().Text("INVOICE")
            .FontSize(22)
            .Bold();

        column.Item().Text($"Company: {_invoice.Company.CompanyName}");
        column.Item().Text($"Invoice ID: {_invoice.Id}");
        column.Item().Text($"Invoice date: {_invoice.Date:dd-MM-yyyy}");

        column.Item().PaddingVertical(10).LineHorizontal(1);
    }

    private void BillingTable(ColumnDescriptor column)
    {
        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
                columns.RelativeColumn(1);
            });

            table.Header(header =>
            {
                header.Cell().Text("Location").Bold();
                header.Cell().Text("Plate").Bold();
                header.Cell().Text("Start").Bold();
                header.Cell().Text("Stop").Bold();
                header.Cell().AlignRight().Text("Price").Bold();
            });

            foreach (var billing in _billings)
            {
                table.Cell().Text(billing.ParkingLot.Location);
                table.Cell().Text(billing.Session.Vehicle.LicensePlate);
                table.Cell().Text(billing.Started.ToString("dd-MM HH:mm"));
                table.Cell().Text(billing.Stopped.ToString("dd-MM HH:mm"));
                table.Cell().AlignRight().Text($"€ {billing.Cost:F2}");
            }
        });
    }

    private void Totals(ColumnDescriptor column)
    {
        var total = _billings.Sum(b => b.Cost);

        column.Item().PaddingTop(10).AlignRight().Column(c =>
        {
            c.Item().Text($"Subtotal: € {total:F2}");
            c.Item().Text($"VAT (21%): € {total * 0.21m:F2}");
            c.Item().Text($"TOTAL: € {total * 1.21m:F2}")
                .FontSize(14)
                .Bold();
        });

        column.Item().PaddingVertical(10).LineHorizontal(1);
    }

    private void Footer(ColumnDescriptor column)
    {
        column.Item().Text("Payment status: " + _invoice.PaymentStatus);
        column.Item().Text("Payment method: " + _invoice.PaymentMethod);
    }
}
