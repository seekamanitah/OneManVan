using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using OneManVan.Shared.Models;
using QuestDocument = QuestPDF.Fluent.Document;

namespace OneManVan.Services;

/// <summary>
/// Service for generating professional invoice PDFs with company branding.
/// </summary>
public class InvoicePdfService
{
    private readonly InvoicePdfSettings _settings;

    public InvoicePdfService()
    {
        // Configure QuestPDF license (free for open-source)
        QuestPDF.Settings.License = LicenseType.Community;
        
        _settings = LoadSettings();
    }

    /// <summary>
    /// Generates a PDF invoice and saves to file.
    /// </summary>
    public string GenerateInvoicePdf(Invoice invoice, string outputPath)
    {
        var document = QuestDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(40);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Black));

                page.Header().Element(ComposeHeader);
                page.Content().Element(container => ComposeContent(container, invoice));
                page.Footer().AlignCenter().Text(text =>
                {
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });

        document.GeneratePdf(outputPath);
        return outputPath;
    }

    /// <summary>
    /// Generates PDF and returns as byte array (for preview).
    /// </summary>
    public byte[] GenerateInvoicePdfBytes(Invoice invoice)
    {
        var document = QuestDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(40);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Black));

                page.Header().Element(ComposeHeader);
                page.Content().Element(container => ComposeContent(container, invoice));
                page.Footer().AlignCenter().Text(text =>
                {
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            // Left side - Company info
            row.RelativeItem().Column(column =>
            {
                column.Spacing(5);

                // Company logo
                if (!string.IsNullOrEmpty(_settings.LogoPath) && File.Exists(_settings.LogoPath))
                {
                    column.Item().Image(_settings.LogoPath).FitWidth();
                }

                // Company name
                if (!string.IsNullOrEmpty(_settings.CompanyName))
                {
                    column.Item().Text(_settings.CompanyName)
                        .FontSize(16)
                        .Bold()
                        .FontColor(_settings.PrimaryColor);
                }

                // Company contact info
                if (!string.IsNullOrEmpty(_settings.Phone))
                    column.Item().Text(_settings.Phone).FontSize(9);
                
                if (!string.IsNullOrEmpty(_settings.Email))
                    column.Item().Text(_settings.Email).FontSize(9);
                
                if (!string.IsNullOrEmpty(_settings.Website))
                    column.Item().Text(_settings.Website).FontSize(9).FontColor(Colors.Blue.Darken2);
                
                if (!string.IsNullOrEmpty(_settings.Address))
                    column.Item().Text(_settings.Address).FontSize(9);
            });

            // Right side - Invoice title and number
            row.RelativeItem().AlignRight().Column(column =>
            {
                column.Spacing(5);
                
                column.Item().AlignRight().Text("INVOICE")
                    .FontSize(24)
                    .Bold()
                    .FontColor(_settings.PrimaryColor);
            });
        });

        container.PaddingTop(10).BorderBottom(2).BorderColor(_settings.PrimaryColor);
    }

    private void ComposeContent(IContainer container, Invoice invoice)
    {
        container.PaddingVertical(20).Column(column =>
        {
            column.Spacing(15);

            // Invoice metadata
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(leftColumn =>
                {
                    leftColumn.Spacing(3);
                    
                    // Bill To
                    leftColumn.Item().Text("BILL TO").Bold().FontSize(11).FontColor(_settings.PrimaryColor);
                    leftColumn.Item().Text(invoice.Customer?.Name ?? "").FontSize(11).Bold();
                    if (!string.IsNullOrEmpty(invoice.Customer?.CompanyName))
                        leftColumn.Item().Text(invoice.Customer.CompanyName).FontSize(9);
                    if (!string.IsNullOrEmpty(invoice.Customer?.Email))
                        leftColumn.Item().Text(invoice.Customer.Email).FontSize(9);
                    if (!string.IsNullOrEmpty(invoice.Customer?.Phone))
                        leftColumn.Item().Text(invoice.Customer.Phone).FontSize(9);
                    
                    // Get address from job site if available
                    if (invoice.Job?.Site != null && !string.IsNullOrEmpty(invoice.Job.Site.Address))
                        leftColumn.Item().Text(invoice.Job.Site.Address).FontSize(9);
                });

                row.RelativeItem().Column(rightColumn =>
                {
                    rightColumn.Spacing(3);
                    
                    // Invoice details
                    rightColumn.Item().Row(r =>
                    {
                        r.AutoItem().Width(100).Text("Invoice #:").FontSize(9);
                        r.RelativeItem().Text(invoice.InvoiceNumber ?? "").FontSize(9).Bold();
                    });
                    
                    rightColumn.Item().Row(r =>
                    {
                        r.AutoItem().Width(100).Text("Date:").FontSize(9);
                        r.RelativeItem().Text(invoice.InvoiceDate.ToString("MMM dd, yyyy")).FontSize(9);
                    });
                    
                    rightColumn.Item().Row(r =>
                    {
                        r.AutoItem().Width(100).Text("Due Date:").FontSize(9);
                        r.RelativeItem().Text(invoice.DueDate.ToString("MMM dd, yyyy")).FontSize(9);
                    });

                    if (invoice.Job != null)
                    {
                        rightColumn.Item().Row(r =>
                        {
                            r.AutoItem().Width(100).Text("Job #:").FontSize(9);
                            r.RelativeItem().Text(invoice.Job.JobNumber ?? "").FontSize(9);
                        });
                    }
                });
            });

            // Items table (Labor, Parts, Other)
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(4);   // Description
                    columns.ConstantColumn(100); // Amount
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Background(_settings.PrimaryColor).Padding(5).Text("Description").FontColor(Colors.White).Bold();
                    header.Cell().Background(_settings.PrimaryColor).Padding(5).AlignRight().Text("Amount").FontColor(Colors.White).Bold();
                });

                // Labor
                if (invoice.LaborAmount > 0)
                {
                    table.Cell().Padding(5).Text("Labor");
                    table.Cell().Padding(5).AlignRight().Text($"${invoice.LaborAmount:N2}");
                }

                // Parts
                if (invoice.PartsAmount > 0)
                {
                    table.Cell().Background(Colors.Grey.Lighten4).Padding(5).Text("Parts");
                    table.Cell().Background(Colors.Grey.Lighten4).Padding(5).AlignRight().Text($"${invoice.PartsAmount:N2}");
                }

                // Other
                if (invoice.OtherAmount > 0)
                {
                    table.Cell().Padding(5).Text("Other");
                    table.Cell().Padding(5).AlignRight().Text($"${invoice.OtherAmount:N2}");
                }
            });

            // Totals section
            column.Item().PaddingTop(10).AlignRight().Width(250).Column(totalsColumn =>
            {
                totalsColumn.Spacing(3);

                // Subtotal
                totalsColumn.Item().Row(row =>
                {
                    row.RelativeItem().Text("Subtotal:").FontSize(10);
                    row.AutoItem().Text($"${invoice.SubTotal:N2}").FontSize(10).Bold();
                });

                // Tax
                if (invoice.TaxAmount > 0)
                {
                    totalsColumn.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Tax ({invoice.TaxRate:N2}%):").FontSize(10);
                        row.AutoItem().Text($"${invoice.TaxAmount:N2}").FontSize(10);
                    });
                }

                // Discount
                if (invoice.DiscountAmount > 0)
                {
                    totalsColumn.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Discount:").FontSize(10).FontColor(Colors.Red.Darken1);
                        row.AutoItem().Text($"-${invoice.DiscountAmount:N2}").FontSize(10).FontColor(Colors.Red.Darken1);
                    });
                }

                // Total line
                totalsColumn.Item().PaddingTop(5).BorderTop(1).BorderColor(Colors.Grey.Medium);

                // Total
                totalsColumn.Item().Row(row =>
                {
                    row.RelativeItem().Text("Total:").FontSize(12).Bold();
                    row.AutoItem().Text($"${invoice.Total:N2}")
                        .FontSize(12)
                        .Bold()
                        .FontColor(_settings.PrimaryColor);
                });

                // Amount paid
                if (invoice.AmountPaid > 0)
                {
                    totalsColumn.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Paid:").FontSize(10).FontColor(Colors.Green.Darken1);
                        row.AutoItem().Text($"-${invoice.AmountPaid:N2}").FontSize(10).FontColor(Colors.Green.Darken1);
                    });
                }

                // Balance due
                var balanceDue = invoice.BalanceDue;
                if (balanceDue > 0)
                {
                    totalsColumn.Item().PaddingTop(5).BorderTop(1).BorderColor(Colors.Grey.Medium);
                    
                    totalsColumn.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Balance Due:").FontSize(12).Bold();
                        row.AutoItem().Text($"${balanceDue:N2}")
                            .FontSize(12)
                            .Bold()
                            .FontColor(balanceDue > 0 ? Colors.Red.Darken2 : Colors.Green.Darken2);
                    });
                }
            });

            // Notes section
            if (!string.IsNullOrEmpty(invoice.Notes))
            {
                column.Item().PaddingTop(15).Column(notesColumn =>
                {
                    notesColumn.Item().Text("Notes:").Bold().FontSize(10).FontColor(_settings.PrimaryColor);
                    notesColumn.Item().PaddingTop(5).Text(invoice.Notes).FontSize(9);
                });
            }

            // Payment terms
            if (!string.IsNullOrEmpty(_settings.PaymentTerms))
            {
                column.Item().PaddingTop(15).Column(termsColumn =>
                {
                    termsColumn.Item().Text("Payment Terms:").Bold().FontSize(10).FontColor(_settings.PrimaryColor);
                    termsColumn.Item().PaddingTop(5).Text(_settings.PaymentTerms).FontSize(9);
                });
            }

            // Footer message
            if (!string.IsNullOrEmpty(_settings.FooterMessage))
            {
                column.Item().PaddingTop(20).AlignCenter().Text(_settings.FooterMessage)
                    .FontSize(9)
                    .Italic()
                    .FontColor(Colors.Grey.Darken1);
            }
        });
    }

    private InvoicePdfSettings LoadSettings()
    {
        try
        {
            var settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OneManVan", "invoice_pdf_settings.json");

            if (File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);
                return System.Text.Json.JsonSerializer.Deserialize<InvoicePdfSettings>(json) ?? GetDefaultSettings();
            }
        }
        catch { }

        return GetDefaultSettings();
    }

    public void SaveSettings(InvoicePdfSettings settings)
    {
        try
        {
            var settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OneManVan", "invoice_pdf_settings.json");

            var directory = Path.GetDirectoryName(settingsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsPath, json);
        }
        catch { }
    }

    private InvoicePdfSettings GetDefaultSettings()
    {
        // Try to load from business profile
        var businessProfile = LoadBusinessProfile();
        
        return new InvoicePdfSettings
        {
            CompanyName = businessProfile?.CompanyName ?? "Your Company Name",
            Phone = businessProfile?.Phone,
            Email = businessProfile?.Email,
            Website = businessProfile?.Website,
            Address = businessProfile?.Address,
            PrimaryColor = "#2196F3", // Material Blue
            PaymentTerms = "Payment is due within 30 days of invoice date.",
            FooterMessage = "Thank you for your business!"
        };
    }

    private BusinessProfileSettings? LoadBusinessProfile()
    {
        try
        {
            var profilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OneManVan", "business_profile.json");

            if (File.Exists(profilePath))
            {
                var json = File.ReadAllText(profilePath);
                return System.Text.Json.JsonSerializer.Deserialize<BusinessProfileSettings>(json);
            }
        }
        catch { }

        return null;
    }
}

/// <summary>
/// Settings for invoice PDF generation.
/// </summary>
public class InvoicePdfSettings
{
    public string? CompanyName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? Address { get; set; }
    public string? LogoPath { get; set; }
    public string PrimaryColor { get; set; } = "#2196F3";
    public string? PaymentTerms { get; set; }
    public string? FooterMessage { get; set; }
}

/// <summary>
/// Business profile for loading company info.
/// </summary>
public class BusinessProfileSettings
{
    public string? CompanyName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? Address { get; set; }
}
