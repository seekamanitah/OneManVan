using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using OneManVan.Shared.Models;

namespace OneManVan.Web.Services.Pdf;

public interface IInvoicePdfGenerator
{
    byte[] GenerateInvoicePdf(Invoice invoice);
}

public class InvoicePdfGenerator : IInvoicePdfGenerator
{
    public InvoicePdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateInvoicePdf(Invoice invoice)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(40);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Black));

                page.Header().Element(ComposeHeader);
                page.Content().Element(c => ComposeContent(c, invoice));
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Page ");
                    t.CurrentPageNumber();
                    t.Span(" of ");
                    t.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("INVOICE")
                    .FontSize(28)
                    .Bold()
                    .FontColor(Colors.Blue.Medium);

                column.Item().PaddingTop(10).Text("OneManVan")
                    .FontSize(16)
                    .SemiBold();

                column.Item().Text("Professional Service Management")
                    .FontSize(10)
                    .FontColor(Colors.Grey.Darken2);
            });

            row.ConstantItem(150).AlignRight().Column(column =>
            {
                column.Item().Text($"Date: {DateTime.Now:MM/dd/yyyy}")
                    .FontSize(10);
            });
        });
    }

    private void ComposeContent(IContainer container, Invoice invoice)
    {
        container.PaddingVertical(20).Column(column =>
        {
            column.Spacing(15);

            // Invoice Details Section
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Bill To:").Bold();
                    col.Item().Text(invoice.Customer?.Name ?? "N/A");
                    if (!string.IsNullOrEmpty(invoice.Customer?.HomeAddress))
                        col.Item().Text(invoice.Customer.HomeAddress);
                    if (!string.IsNullOrEmpty(invoice.Customer?.Phone))
                        col.Item().Text($"Phone: {invoice.Customer.Phone}");
                    if (!string.IsNullOrEmpty(invoice.Customer?.Email))
                        col.Item().Text($"Email: {invoice.Customer.Email}");
                });

                row.ConstantItem(200).Column(col =>
                {
                    col.Item().Text($"Invoice #: {invoice.InvoiceNumber}").Bold();
                    col.Item().Text($"Date: {invoice.InvoiceDate:MM/dd/yyyy}");
                    col.Item().Text($"Due Date: {invoice.DueDate:MM/dd/yyyy}");
                    col.Item().Text($"Status: {invoice.Status}");
                });
            });

            // Line Items Table
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(4); // Description
                    columns.RelativeColumn(1); // Qty
                    columns.RelativeColumn(1.5f); // Unit Price
                    columns.RelativeColumn(1.5f); // Total
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Element(HeaderStyle).Text("Description");
                    header.Cell().Element(HeaderStyle).AlignRight().Text("Qty");
                    header.Cell().Element(HeaderStyle).AlignRight().Text("Unit Price");
                    header.Cell().Element(HeaderStyle).AlignRight().Text("Total");
                });

                // Line Items
                foreach (var item in invoice.LineItems ?? new List<InvoiceLineItem>())
                {
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                        .PaddingVertical(8).Text(item.Description ?? "");
                    
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                        .PaddingVertical(8).AlignRight().Text(item.Quantity.ToString("N2"));
                    
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                        .PaddingVertical(8).AlignRight().Text($"${item.UnitPrice:N2}");
                    
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                        .PaddingVertical(8).AlignRight().Text($"${item.Total:N2}").Bold();
                }
            });

            // Totals Section
            column.Item().AlignRight().Column(totals =>
            {
                totals.Item().Row(row =>
                {
                    row.ConstantItem(120).Text("Subtotal:");
                    row.ConstantItem(100).AlignRight().Text($"${invoice.SubTotal:N2}");
                });

                totals.Item().Row(row =>
                {
                    row.ConstantItem(120).Text($"Tax ({invoice.TaxRate:N2}%):");
                    row.ConstantItem(100).AlignRight().Text($"${invoice.TaxAmount:N2}");
                });

                totals.Item().PaddingTop(5).Row(row =>
                {
                    row.ConstantItem(120).Text("TOTAL:").Bold().FontSize(14);
                    row.ConstantItem(100).AlignRight().Text($"${invoice.Total:N2}")
                        .Bold().FontSize(14).FontColor(Colors.Blue.Medium);
                });
            });

            // Notes
            if (!string.IsNullOrEmpty(invoice.Notes))
            {
                column.Item().PaddingTop(20).Column(notes =>
                {
                    notes.Item().Text("Notes:").Bold();
                    notes.Item().Text(invoice.Notes);
                });
            }

            // Footer Message
            column.Item().PaddingTop(30).AlignCenter().Text("Thank you for your business!")
                .FontSize(12).Italic().FontColor(Colors.Grey.Darken1);
        });
    }

    private IContainer HeaderStyle(IContainer container)
    {
        return container
            .BorderBottom(2)
            .BorderColor(Colors.Blue.Medium)
            .PaddingVertical(8)
            .Background(Colors.Blue.Lighten4);
    }
}
