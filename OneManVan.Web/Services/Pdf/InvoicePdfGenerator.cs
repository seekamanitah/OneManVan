using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using OneManVan.Shared.Models;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Services;

namespace OneManVan.Web.Services.Pdf;

public interface IInvoicePdfGenerator
{
    byte[] GenerateInvoicePdf(Invoice invoice);
    byte[] GenerateInvoicePdf(Invoice invoice, CompanySettings? companySettings);
    byte[] GenerateInvoicePdf(Invoice invoice, CompanySettings? companySettings, PdfOptions? options);
}

public class InvoicePdfGenerator : IInvoicePdfGenerator
{
    private readonly IDbContextFactory<OneManVanDbContext> _contextFactory;
    private readonly ISettingsStorage _settingsStorage;
    private CompanySettings? _companySettings;
    private PdfOptions _pdfOptions = new();

    public InvoicePdfGenerator(
        IDbContextFactory<OneManVanDbContext> contextFactory,
        ISettingsStorage settingsStorage)
    {
        _contextFactory = contextFactory;
        _settingsStorage = settingsStorage;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateInvoicePdf(Invoice invoice)
    {
        // Load company settings
        using var context = _contextFactory.CreateDbContext();
        _companySettings = context.CompanySettings.FirstOrDefault();
        _pdfOptions = new PdfOptions();
        
        return GenerateInvoicePdfInternal(invoice);
    }

    public byte[] GenerateInvoicePdf(Invoice invoice, CompanySettings? companySettings)
    {
        _companySettings = companySettings;
        _pdfOptions = new PdfOptions();
        return GenerateInvoicePdfInternal(invoice);
    }

    public byte[] GenerateInvoicePdf(Invoice invoice, CompanySettings? companySettings, PdfOptions? options)
    {
        _companySettings = companySettings;
        _pdfOptions = options ?? new PdfOptions();
        return GenerateInvoicePdfInternal(invoice);
    }

    private byte[] GenerateInvoicePdfInternal(Invoice invoice)
    {
        var document = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(40);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Black));

                page.Header().Element(ComposeHeader);
                page.Content().Element(c => ComposeContent(c, invoice));
                page.Footer().Element(ComposeFooter);
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

                column.Item().PaddingTop(10).Text(_companySettings?.CompanyName ?? "OneManVan")
                    .FontSize(16)
                    .SemiBold();

                if (!string.IsNullOrEmpty(_companySettings?.Tagline))
                {
                    column.Item().Text(_companySettings.Tagline)
                        .FontSize(10)
                        .FontColor(Colors.Grey.Darken2);
                }
                
                if (!string.IsNullOrEmpty(_companySettings?.FullAddress))
                {
                    column.Item().PaddingTop(5).Text(_companySettings.FullAddress)
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken1);
                }
                
                if (!string.IsNullOrEmpty(_companySettings?.Phone))
                {
                    column.Item().Text($"Phone: {_companySettings.Phone}")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken1);
                }
                
                if (!string.IsNullOrEmpty(_companySettings?.Email))
                {
                    column.Item().Text($"Email: {_companySettings.Email}")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken1);
                }
            });

            // Logo on right side
            row.ConstantItem(150).AlignRight().Column(column =>
            {
                if (!string.IsNullOrEmpty(_companySettings?.LogoBase64))
                {
                    try
                    {
                        var logoBytes = Convert.FromBase64String(_companySettings.LogoBase64);
                        column.Item().Image(logoBytes).FitWidth();
                    }
                    catch
                    {
                        // Skip logo if invalid
                    }
                }
                column.Item().PaddingTop(10).Text($"Date: {DateTime.Now:MM/dd/yyyy}")
                    .FontSize(10);
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(column =>
        {
            if (!string.IsNullOrEmpty(_companySettings?.DocumentFooter))
            {
                column.Item().AlignCenter().Text(_companySettings.DocumentFooter)
                    .FontSize(9)
                    .Italic()
                    .FontColor(Colors.Grey.Darken1);
            }
            
            column.Item().PaddingTop(5).AlignCenter().Text(t =>
            {
                t.Span("Page ");
                t.CurrentPageNumber();
                t.Span(" of ");
                t.TotalPages();
            });
        });
    }

    private void ComposeContent(IContainer container, Invoice invoice)
    {
        container.PaddingVertical(20).Column(column =>
        {
            column.Spacing(15);

            // Invoice Details Section
            if (_pdfOptions.IncludeCustomerInfo)
            {
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
                        if (_pdfOptions.IncludePaymentTerms)
                        {
                            col.Item().Text($"Due Date: {invoice.DueDate:MM/dd/yyyy}");
                        }
                        col.Item().Text($"Status: {invoice.Status}");
                    });
                });
            }

            // Flat Rate Mode - show only total
            if (_pdfOptions.UseFlatRate)
            {
                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Text(invoice.Notes ?? "Services Rendered").FontSize(12);
                    row.ConstantItem(120).AlignRight().Text($"${invoice.Total:N2}").Bold().FontSize(14);
                });
            }
            else
            {
                // Get privacy settings
                var hideEmployeeRates = _settingsStorage.GetBool("HideEmployeeRatesOnInvoice", false) || !_pdfOptions.ShowLaborBreakdown;
                var hideEmployeeBreakdown = _settingsStorage.GetBool("HideEmployeeBreakdownOnInvoice", false) || !_pdfOptions.ShowLaborBreakdown;

                // Separate labor and non-labor items
                var laborItems = invoice.LineItems?.Where(i => i.Source == "Labor").ToList() ?? new List<InvoiceLineItem>();
                var nonLaborItems = invoice.LineItems?.Where(i => i.Source != "Labor").ToList() ?? new List<InvoiceLineItem>();

                // Non-Labor Items Table (Parts & Materials)
                if (nonLaborItems.Any())
                {
                    column.Item().Text("Parts & Materials").Bold().FontSize(12);
                    column.Item().Table(table =>
                    {
                        if (_pdfOptions.IncludePrices)
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4); // Description
                                columns.RelativeColumn(1); // Qty
                                columns.RelativeColumn(1.5f); // Unit Price
                                columns.RelativeColumn(1.5f); // Total
                            });
                        }
                        else
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(5); // Description
                                columns.RelativeColumn(1); // Qty
                            });
                        }

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderStyle).Text("Description");
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Qty");
                            if (_pdfOptions.IncludePrices)
                            {
                                header.Cell().Element(HeaderStyle).AlignRight().Text("Unit Price");
                                header.Cell().Element(HeaderStyle).AlignRight().Text("Total");
                            }
                        });

                        // Line Items
                        foreach (var item in nonLaborItems)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                .PaddingVertical(8).Text(item.Description ?? "");
                            
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                .PaddingVertical(8).AlignRight().Text(item.Quantity.ToString("N2"));
                            
                            if (_pdfOptions.IncludePrices)
                            {
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .PaddingVertical(8).AlignRight().Text($"${item.UnitPrice:N2}");
                                
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .PaddingVertical(8).AlignRight().Text($"${item.Total:N2}").Bold();
                            }
                        }
                    });
                }

                // Labor Items - Respect Privacy Settings
                if (laborItems.Any())
                {
                    column.Item().PaddingTop(10).Text("Labor & Time").Bold().FontSize(12);
                    
                    if (hideEmployeeBreakdown)
                    {
                        // Show only total labor cost
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Labor Services");
                            if (_pdfOptions.IncludePrices)
                            {
                                row.ConstantItem(100).AlignRight().Text($"${laborItems.Sum(i => i.Total):N2}").Bold();
                            }
                        });
                    }
                    else
                    {
                        // Show detailed labor breakdown
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4); // Description
                                columns.RelativeColumn(1); // Hours
                                if (!hideEmployeeRates && _pdfOptions.IncludePrices)
                                    columns.RelativeColumn(1.5f); // Rate (only if not hidden)
                                if (_pdfOptions.IncludePrices)
                                    columns.RelativeColumn(1.5f); // Total
                            });

                            // Header
                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderStyle).Text("Description");
                                header.Cell().Element(HeaderStyle).AlignRight().Text("Hours");
                                if (!hideEmployeeRates && _pdfOptions.IncludePrices)
                                    header.Cell().Element(HeaderStyle).AlignRight().Text("Rate/Hour");
                                if (_pdfOptions.IncludePrices)
                                    header.Cell().Element(HeaderStyle).AlignRight().Text("Total");
                            });

                            // Labor Line Items
                            foreach (var item in laborItems)
                            {
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .PaddingVertical(8).Text(item.Description ?? "");
                                
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .PaddingVertical(8).AlignRight().Text(item.Quantity.ToString("N2"));
                                
                                if (!hideEmployeeRates && _pdfOptions.IncludePrices)
                                {
                                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                        .PaddingVertical(8).AlignRight().Text($"${item.UnitPrice:N2}");
                                }
                                
                                if (_pdfOptions.IncludePrices)
                                {
                                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                        .PaddingVertical(8).AlignRight().Text($"${item.Total:N2}").Bold();
                                }
                            }
                        });
                    }
                }
            }

            // Totals Section
            if (_pdfOptions.IncludePrices && !_pdfOptions.UseFlatRate)
            {
                column.Item().PaddingTop(10).AlignRight().Column(totals =>
                {
                    if (_pdfOptions.ShowSubtotal)
                    {
                        totals.Item().Row(row =>
                        {
                            row.ConstantItem(120).Text("Subtotal:");
                            row.ConstantItem(100).AlignRight().Text($"${invoice.SubTotal:N2}");
                        });
                    }

                    if (_pdfOptions.IncludeTax)
                    {
                        if (!invoice.TaxIncluded)
                        {
                            totals.Item().Row(row =>
                            {
                                row.ConstantItem(120).Text($"Tax ({invoice.TaxRate:N2}%):");
                                row.ConstantItem(100).AlignRight().Text($"${invoice.TaxAmount:N2}");
                            });
                        }
                        else
                        {
                            totals.Item().Row(row =>
                            {
                                row.ConstantItem(120).Text("Tax:");
                                row.ConstantItem(100).AlignRight().Text("Included");
                            });
                        }
                    }

                    totals.Item().PaddingTop(5).Row(row =>
                    {
                        row.ConstantItem(120).Text("TOTAL:").Bold().FontSize(14);
                        row.ConstantItem(100).AlignRight().Text($"${invoice.Total:N2}")
                            .Bold().FontSize(14).FontColor(Colors.Blue.Medium);
                    });
                    
                    if (invoice.AmountPaid > 0)
                    {
                        totals.Item().Row(row =>
                        {
                            row.ConstantItem(120).Text("Amount Paid:");
                            row.ConstantItem(100).AlignRight().Text($"${invoice.AmountPaid:N2}");
                        });
                        
                        totals.Item().Row(row =>
                        {
                            row.ConstantItem(120).Text("Balance Due:").Bold();
                            row.ConstantItem(100).AlignRight().Text($"${(invoice.Total - invoice.AmountPaid):N2}").Bold();
                        });
                    }
                });
            }

            // Notes (respect HideInternalNotes option)
            if (!string.IsNullOrEmpty(invoice.Notes) && _pdfOptions.IncludeNotes)
            {
                column.Item().PaddingTop(20).Column(notes =>
                {
                    notes.Item().Text("Notes:").Bold();
                    notes.Item().Text(invoice.Notes);
                });
            }

            // Terms
            if (!string.IsNullOrEmpty(invoice.Terms) && _pdfOptions.IncludePaymentTerms)
            {
                column.Item().PaddingTop(10).Column(terms =>
                {
                    terms.Item().Text("Terms:").Bold();
                    terms.Item().Text(invoice.Terms).FontSize(8);
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
