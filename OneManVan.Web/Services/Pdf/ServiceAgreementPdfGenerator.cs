using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;

namespace OneManVan.Web.Services.Pdf;

public interface IServiceAgreementPdfGenerator
{
    byte[] GenerateAgreementPdf(ServiceAgreement agreement);
    byte[] GenerateAgreementPdf(ServiceAgreement agreement, CompanySettings? companySettings);
}

public class ServiceAgreementPdfGenerator : IServiceAgreementPdfGenerator
{
    private readonly IDbContextFactory<OneManVanDbContext> _contextFactory;
    private CompanySettings? _companySettings;

    public ServiceAgreementPdfGenerator(IDbContextFactory<OneManVanDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateAgreementPdf(ServiceAgreement agreement)
    {
        using var context = _contextFactory.CreateDbContext();
        _companySettings = context.CompanySettings.FirstOrDefault();
        
        return GenerateAgreementPdfInternal(agreement);
    }

    public byte[] GenerateAgreementPdf(ServiceAgreement agreement, CompanySettings? companySettings)
    {
        _companySettings = companySettings;
        return GenerateAgreementPdfInternal(agreement);
    }

    private byte[] GenerateAgreementPdfInternal(ServiceAgreement agreement)
    {
        var document = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(40);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(c => ComposeHeader(c, agreement));
                page.Content().Element(c => ComposeContent(c, agreement));
                page.Footer().Element(c => ComposeFooter(c, agreement));
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container, ServiceAgreement agreement)
    {
        container.Column(column =>
        {
            column.Spacing(10);

            // Title
            column.Item().AlignCenter().Text("HVAC Service Agreement - Services Included Document")
                .Bold().FontSize(16);

            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Medium);

            // Provider Info
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Provider:").Bold();
                    col.Item().Text(_companySettings?.CompanyName ?? "[Your Business Name]");
                    col.Item().Text(_companySettings?.Address ?? "[Your Address]");
                    col.Item().Text($"{_companySettings?.Phone ?? "[Phone]"} | {_companySettings?.Email ?? "[Email]"}");
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Customer:").Bold();
                    col.Item().Text(agreement.Customer?.Name ?? "[Customer Name]");
                    col.Item().Text(agreement.Customer?.HomeAddress ?? "[Customer Address]");
                    col.Item().Text($"{agreement.Customer?.Phone ?? ""} | {agreement.Customer?.Email ?? ""}");
                });
            });

            column.Item().Row(row =>
            {
                row.RelativeItem().Text($"Agreement Number: {agreement.AgreementNumber ?? "-"}").Bold();
                row.RelativeItem().AlignRight().Text($"Agreement Date: {agreement.StartDate:MMMM dd, yyyy}");
            });
        });
    }

    private void ComposeContent(IContainer container, ServiceAgreement agreement)
    {
        container.PaddingVertical(10).Column(column =>
        {
            column.Spacing(15);

            // Selected Service Level
            column.Item().Text("Selected Service Level:").Bold().FontSize(12);

            // Service Tier Cards
            column.Item().Row(row =>
            {
                row.Spacing(10);
                
                // Basic
                row.RelativeItem().Border(agreement.ServiceTier == ServiceTier.Basic ? 2 : 1)
                    .BorderColor(agreement.ServiceTier == ServiceTier.Basic ? Colors.Blue.Medium : Colors.Grey.Lighten2)
                    .Padding(8)
                    .Column(col =>
                    {
                        col.Item().Row(r =>
                        {
                            r.AutoItem().Text(agreement.ServiceTier == ServiceTier.Basic ? "[X]" : "[ ]").FontSize(10);
                            r.AutoItem().PaddingLeft(5).Text("Basic").Bold();
                        });
                        col.Item().Text("- 1 annual tune-up (AC or heating)").FontSize(8);
                        col.Item().Text("- Priority scheduling").FontSize(8);
                        col.Item().Text("- 10% discount on repairs and parts").FontSize(8);
                    });

                // Standard
                row.RelativeItem().Border(agreement.ServiceTier == ServiceTier.Standard ? 2 : 1)
                    .BorderColor(agreement.ServiceTier == ServiceTier.Standard ? Colors.Blue.Medium : Colors.Grey.Lighten2)
                    .Padding(8)
                    .Column(col =>
                    {
                        col.Item().Row(r =>
                        {
                            r.AutoItem().Text(agreement.ServiceTier == ServiceTier.Standard ? "[X]" : "[ ]").FontSize(10);
                            r.AutoItem().PaddingLeft(5).Text("Standard").Bold();
                        });
                        col.Item().Text("- 2 annual tune-ups (Spring AC + Fall Heating)").FontSize(8);
                        col.Item().Text("- Priority scheduling (within 24 hours)").FontSize(8);
                        col.Item().Text("- 15% discount on repairs and parts").FontSize(8);
                        col.Item().Text("- No emergency dispatch fee").FontSize(8);
                    });

                // Premium
                row.RelativeItem().Border(agreement.ServiceTier == ServiceTier.Premium ? 2 : 1)
                    .BorderColor(agreement.ServiceTier == ServiceTier.Premium ? Colors.Orange.Medium : Colors.Grey.Lighten2)
                    .Padding(8)
                    .Column(col =>
                    {
                        col.Item().Row(r =>
                        {
                            r.AutoItem().Text(agreement.ServiceTier == ServiceTier.Premium ? "[X]" : "[ ]").FontSize(10);
                            r.AutoItem().PaddingLeft(5).Text("Premium").Bold();
                        });
                        col.Item().Text("- 2 annual tune-ups + 1 priority check").FontSize(8);
                        col.Item().Text("- 20% discount on repairs and parts").FontSize(8);
                        col.Item().Text("- No emergency dispatch fee").FontSize(8);
                        col.Item().Text("- Free minor adjustments (belts, 2 filters/yr)").FontSize(8);
                    });
            });

            // Spring AC Tune-Up Tasks (if Standard or Premium)
            if (agreement.ServiceTier != ServiceTier.Basic && agreement.SpringAcTaskList.Any())
            {
                column.Item().Column(col =>
                {
                    col.Item().Text("Spring AC Tune-Up Includes:").Bold().FontSize(11);
                    col.Item().PaddingLeft(15).Column(tasks =>
                    {
                        foreach (var task in agreement.SpringAcTaskList)
                        {
                            tasks.Item().Text($"* {task}").FontSize(9);
                        }
                    });
                });
            }

            // Fall Heating Tune-Up Tasks (if Standard or Premium)
            if (agreement.ServiceTier != ServiceTier.Basic && agreement.FallHeatingTaskList.Any())
            {
                column.Item().Column(col =>
                {
                    col.Item().Text("Fall Heating Tune-Up Includes:").Bold().FontSize(11);
                    col.Item().PaddingLeft(15).Column(tasks =>
                    {
                        foreach (var task in agreement.FallHeatingTaskList)
                        {
                            tasks.Item().Text($"* {task}").FontSize(9);
                        }
                    });
                });
            }

            // General Services
            column.Item().Column(col =>
            {
                col.Item().Text("General Services Across All Levels:").Bold().FontSize(11);
                col.Item().PaddingLeft(15).Column(services =>
                {
                    services.Item().Text("* Visual inspection of system").FontSize(9);
                    services.Item().Text("* Before/after photos").FontSize(9);
                    services.Item().Text("* Performance report (delta T, pressures, findings)").FontSize(9);
                    services.Item().Text("* Recommendations & next service reminder").FontSize(9);
                    services.Item().Text("* Digital customer signature & emailed PDF summary").FontSize(9);
                });
            });

            // Exclusions
            column.Item().Column(col =>
            {
                col.Item().Text("Exclusions (all levels):").Bold().FontSize(11);
                col.Item().PaddingLeft(15).Text(
                    "Refrigerant recharge beyond minor top-off, major repairs/replacements, abuse, neglect, or failure to maintain filters.")
                    .FontSize(9).Italic();
            });

            // Pricing Summary
            column.Item().PaddingTop(10).Border(1).BorderColor(Colors.Grey.Medium).Padding(10).Column(pricing =>
            {
                pricing.Item().Text("Agreement Summary").Bold().FontSize(11);
                pricing.Item().Row(row =>
                {
                    row.RelativeItem().Text($"Annual Price: ${agreement.AnnualPrice:N2}");
                    row.RelativeItem().Text($"Visits Included: {agreement.IncludedVisitsPerYear}/year");
                    row.RelativeItem().Text($"Repair Discount: {agreement.RepairDiscountPercent}%");
                });
                pricing.Item().Row(row =>
                {
                    row.RelativeItem().Text($"Start Date: {agreement.StartDate:MMM dd, yyyy}");
                    row.RelativeItem().Text($"End Date: {agreement.EndDate:MMM dd, yyyy}");
                    row.RelativeItem().Text($"Auto-Renew: {(agreement.AutoRenew ? "Yes" : "No")}");
                });
            });
        });
    }

    private void ComposeFooter(IContainer container, ServiceAgreement agreement)
    {
        container.Column(column =>
        {
            column.Spacing(5);

            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Medium);

            column.Item().Text("Signatures").Bold().FontSize(11);

            column.Item().PaddingTop(20).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Provider: _________________________________");
                    col.Item().PaddingTop(5).Text($"Date: __________");
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Customer: _________________________________");
                    col.Item().PaddingTop(5).Text($"Date: __________");
                });
            });

            column.Item().PaddingTop(20).AlignCenter().Text(text =>
            {
                text.Span("Thank you for choosing ").FontSize(9).Italic();
                text.Span(_companySettings?.CompanyName ?? "our company").Bold().FontSize(9).Italic();
                text.Span(" for your HVAC maintenance needs!").FontSize(9).Italic();
            });
        });
    }
}
