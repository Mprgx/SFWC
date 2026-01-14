using Microsoft.EntityFrameworkCore;

using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

public class InvoicePdfDocument : IDocument
{
    private readonly Invoice _invoice;
    private readonly IReadOnlyList<BillingInvoiceRowDto> _rows;

    public InvoicePdfDocument(Invoice invoice, IReadOnlyList<BillingInvoiceRowDto> rows)
    {
        _invoice = invoice;
        _rows = rows;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(10));

            page.Content().Column(column =>
            {
                Header(column);
                BillingTable(column);
                Footer(column);
            });
        });
    }

    private void Header(ColumnDescriptor column)
    {
        column.Item().Text("INVOICE")
            .FontSize(20)
            .Bold()
            .FontColor(Colors.Blue.Medium);

        column.Item().PaddingTop(5).Text($"Company: {_invoice.Company?.CompanyName ?? "None"}");
        column.Item().Text($"Invoice ID: {_invoice.Id}");
        column.Item().Text($"Invoice date: {_invoice.Date:dd-MM-yyyy}");

        column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
    }

    private void BillingTable(ColumnDescriptor column)
    {
        column.Item().PaddingTop(10).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3);   // Location
                columns.RelativeColumn(2);   // Plate
                columns.RelativeColumn(2.5f);// Start
                columns.RelativeColumn(2.5f);// Stop
                columns.RelativeColumn(1.8f);// Before
                columns.RelativeColumn(2);   // Code
                columns.RelativeColumn(1.8f);// After
            });

            table.Header(header =>
            {
                header.Cell().Element(CellStyle).Text("Location");
                header.Cell().Element(CellStyle).Text("Plate");
                header.Cell().Element(CellStyle).Text("Start");
                header.Cell().Element(CellStyle).Text("Stop");
                header.Cell().Element(CellStyle).AlignRight().Text("Before");
                header.Cell().Element(CellStyle).Text("Code");
                header.Cell().Element(CellStyle).AlignRight().Text("After");

                static IContainer CellStyle(IContainer container)
                {
                    return container.DefaultTextStyle(x => x.Bold())
                                    .PaddingRight(5)
                                    .PaddingVertical(5)
                                    .BorderBottom(1)
                                    .BorderColor(Colors.Black);
                }
            });

            foreach (var row in _rows)
            {
                table.Cell().Element(RowStyle).Text(row.Location);
                table.Cell().Element(RowStyle).Text(row.LicensePlate);
                table.Cell().Element(RowStyle).Text(row.Started.ToString("dd-MM HH:mm"));
                table.Cell().Element(RowStyle).Text(row.Stopped.ToString("dd-MM HH:mm"));
                table.Cell().Element(RowStyle).AlignRight().Text($"€{row.Cost:F2}");
                table.Cell().Element(RowStyle).Text(row.DiscountCode ?? "-");
                table.Cell().Element(RowStyle).AlignRight().Text($"€{row.DiscountedCost:F2}").Bold();

                static IContainer RowStyle(IContainer container)
                {
                    return container.PaddingRight(5)
                                    .BorderBottom(1)
                                    .BorderColor(Colors.Grey.Lighten3)
                                    .PaddingVertical(5);
                }
            }

            var totalBefore = _rows.Sum(r => r.Cost);
            var totalAfter = _rows.Sum(r => r.DiscountedCost);
            var totalDiscount = totalBefore - totalAfter;

            table.Cell().ColumnSpan(4).Element(FooterStyle).AlignRight().Text("Total:").Bold();
            table.Cell().Element(FooterStyle).AlignRight().Text($"€ {totalBefore:F2}").Bold();
            table.Cell().Element(FooterStyle).AlignRight().Text($"- € {totalDiscount:F2}").FontColor(Colors.Red.Medium);
            table.Cell().Element(FooterStyle).AlignRight().Text($"€ {totalAfter:F2}").Bold();

            static IContainer FooterStyle(IContainer container)
            {
                return container.PaddingTop(10).PaddingRight(5).BorderTop(1).BorderColor(Colors.Black).PaddingVertical(5);
            }

        });
    }

    private void Footer(ColumnDescriptor column)
    {
        column.Item().PaddingTop(20).Column(footer =>
        {
            footer.Item().Text($"Payment status: {_invoice.PaymentStatus}").Bold();
        });
    }
}
