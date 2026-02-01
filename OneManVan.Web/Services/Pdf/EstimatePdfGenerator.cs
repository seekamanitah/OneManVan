using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using OneManVan.Shared.Models;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;

namespace OneManVan.Web.Services.Pdf;

public interface IEstimatePdfGenerator
{
    byte[] GenerateEstimatePdf(Estimate estimate);
    byte[] GenerateEstimatePdf(Estimate estimate, CompanySettings? companySettings);
}

public class EstimatePdfGenerator : IEstimatePdfGenerator
{
    private readonly IDbContextFactory<OneManVanDbContext> _contextFactory;
    private CompanySettings? _companySettings;

    public EstimatePdfGenerator(IDbContextFactory<OneManVanDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateEstimatePdf(Estimate estimate)
    {
        // Load company settings
        using var context = _contextFactory.CreateDbContext();
        _companySettings = context.CompanySettings.FirstOrDefault();
        
        return GenerateEstimatePdfInternal(estimate);
    }

    public byte[] GenerateEstimatePdf(Estimate estimate, CompanySettings? companySettings)
    {
        _companySettings = companySettings;
        return GenerateEstimatePdfInternal(estimate);
    }

    private byte[] GenerateEstimatePdfInternal(Estimate estimate)
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
                page.Content().Element(c => ComposeContent(c, estimate));
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
                column.Item().Text("ESTIMATE")
                    .FontSize(28)
                    .Bold()
                    .FontColor(Colors.Orange.Medium);

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
                
                if (!string.IsNullOrEmpty(_companySettings?.LicenseNumber))
                {
                    column.Item().Text($"License #: {_companySettings.LicenseNumber}")
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

    private void ComposeContent(IContainer container, Estimate estimate)
    {
        container.PaddingVertical(20).Column(column =>
        {
            column.Spacing(15);

            // Estimate Details Section
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Prepared For:").Bold();
                    col.Item().Text(estimate.Customer?.Name ?? "N/A");
                    if (!string.IsNullOrEmpty(estimate.Customer?.HomeAddress))
                        col.Item().Text(estimate.Customer.HomeAddress);
                    if (!string.IsNullOrEmpty(estimate.Customer?.Phone))
                        col.Item().Text($"Phone: {estimate.Customer.Phone}");
                    if (!string.IsNullOrEmpty(estimate.Customer?.Email))
                        col.Item().Text($"Email: {estimate.Customer.Email}");
                });

                row.ConstantItem(200).Column(col =>
                {
                    col.Item().Text($"Estimate #: {estimate.Title}").Bold();
                    col.Item().Text($"Created: {estimate.CreatedAt:MM/dd/yyyy}");
                    col.Item().Text($"Valid Until: {estimate.ExpiresAt:MM/dd/yyyy}");
                    col.Item().Text($"Status: {estimate.Status}");
                });
            });

            // Description
            if (!string.IsNullOrEmpty(estimate.Description))
            {
                column.Item().PaddingTop(10).Column(col =>
                {
                    col.Item().Text("Description").Bold();
                    col.Item().Text(estimate.Description);
                });
            }

            // Notes
            if (!string.IsNullOrEmpty(estimate.Notes))
            {
                column.Item().PaddingTop(10).Column(col =>
                {
                    col.Item().Text("Notes").Bold();
                    col.Item().Text(estimate.Notes);
                });
            }

            // Divider
            column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            // Pricing Summary
            column.Item().Text("Pricing Summary").Bold().FontSize(12);

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(1);
                });

                table.Cell().Padding(5).Text("Subtotal:");
                table.Cell().Padding(5).AlignRight().Text($"{estimate.SubTotal:C}");

                if (!estimate.TaxIncluded)
                {
                    table.Cell().Padding(5).Text($"Tax ({estimate.TaxRate}%):");
                    table.Cell().Padding(5).AlignRight().Text($"{estimate.TaxAmount:C}");
                }
                else
                {
                    table.Cell().Padding(5).Text("Tax:");
                    table.Cell().Padding(5).AlignRight().Text("Included");
                }

                table.Cell().Padding(5).Text("Total:").Bold();
                table.Cell().Padding(5).AlignRight().Text($"{estimate.Total:C}").Bold();
            });

            // Terms and Conditions
            column.Item().PaddingTop(20).Column(col =>
            {
                col.Item().Text("Terms & Conditions").Bold();
                col.Item().Text("- This estimate is valid for 30 days from the date above");
                col.Item().Text("- Prices subject to change based on actual conditions");
                col.Item().Text("- Payment terms: 50% deposit, balance due upon completion");
                col.Item().Text("- All work guaranteed for 1 year from completion");
            });

            // Signature Line
            column.Item().PaddingTop(40).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Customer Acceptance:");
                    col.Item().PaddingTop(30).LineHorizontal(1).LineColor(Colors.Black);
                    col.Item().Text("Signature").FontSize(8);
                });
                row.ConstantItem(50);
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("");
                    col.Item().PaddingTop(30).LineHorizontal(1).LineColor(Colors.Black);
                    col.Item().Text("Date").FontSize(8);
                });
            });
        });
    }
}
