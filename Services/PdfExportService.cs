using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.IO.Font.Constants;
using OneManVan.ViewModels;

namespace OneManVan.Services;

/// <summary>
/// Service for exporting reports to PDF format.
/// </summary>
public class PdfExportService
{
    /// <summary>
    /// Exports the reports dashboard data to a PDF file.
    /// </summary>
    public async Task ExportReportToPdfAsync(ReportsViewModel viewModel, string filePath)
    {
        await Task.Run(() =>
        {
            using var writer = new PdfWriter(filePath);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf);

            var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var regularFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // Title
            document.Add(new Paragraph("OneManVan Business Report")
                .SetFont(boldFont)
                .SetFontSize(24)
                .SetMarginBottom(5));

            document.Add(new Paragraph($"Period: {viewModel.SelectedPeriod}")
                .SetFont(regularFont)
                .SetFontSize(12)
                .SetFontColor(ColorConstants.GRAY)
                .SetMarginBottom(5));

            document.Add(new Paragraph($"Generated: {DateTime.Now:MMMM dd, yyyy h:mm tt}")
                .SetFont(regularFont)
                .SetFontSize(10)
                .SetFontColor(ColorConstants.GRAY)
                .SetMarginBottom(20));

            // Revenue Section
            AddSectionHeader(document, "Revenue Metrics", boldFont);
            var revenueTable = CreateMetricsTable(4);
            AddMetricCell(revenueTable, "Total Revenue", $"${viewModel.TotalRevenue:N0}", boldFont, regularFont);
            AddMetricCell(revenueTable, "Paid", $"${viewModel.PaidRevenue:N0}", boldFont, regularFont);
            AddMetricCell(revenueTable, "Outstanding", $"${viewModel.OutstandingRevenue:N0}", boldFont, regularFont);
            AddMetricCell(revenueTable, "Avg Job Value", $"${viewModel.AverageJobValue:N0}", boldFont, regularFont);
            document.Add(revenueTable);
            document.Add(new Paragraph().SetMarginBottom(15));

            // Jobs Section
            AddSectionHeader(document, "Job Metrics", boldFont);
            var jobsTable = CreateMetricsTable(4);
            AddMetricCell(jobsTable, "Total Jobs", viewModel.TotalJobs.ToString(), boldFont, regularFont);
            AddMetricCell(jobsTable, "Completed", viewModel.CompletedJobs.ToString(), boldFont, regularFont);
            AddMetricCell(jobsTable, "Cancelled", viewModel.CancelledJobs.ToString(), boldFont, regularFont);
            AddMetricCell(jobsTable, "Completion Rate", $"{viewModel.CompletionRate:N0}%", boldFont, regularFont);
            document.Add(jobsTable);
            document.Add(new Paragraph().SetMarginBottom(15));

            // Customers & Assets Section
            AddSectionHeader(document, "Customers & Assets", boldFont);
            var customersTable = CreateMetricsTable(5);
            AddMetricCell(customersTable, "Total Customers", viewModel.TotalCustomers.ToString(), boldFont, regularFont);
            AddMetricCell(customersTable, "New Customers", viewModel.NewCustomers.ToString(), boldFont, regularFont);
            AddMetricCell(customersTable, "Active Customers", viewModel.ActiveCustomers.ToString(), boldFont, regularFont);
            AddMetricCell(customersTable, "Total Assets", viewModel.TotalAssets.ToString(), boldFont, regularFont);
            AddMetricCell(customersTable, "Expiring Warranties", viewModel.ExpiringWarranties.ToString(), boldFont, regularFont);
            document.Add(customersTable);
            document.Add(new Paragraph().SetMarginBottom(20));

            // Revenue by Fuel Type
            if (viewModel.RevenueByFuel.Count > 0)
            {
                AddSectionHeader(document, "Revenue by Fuel Type", boldFont);
                var fuelTable = new Table(UnitValue.CreatePercentArray([3, 2, 2])).UseAllAvailableWidth();
                AddTableHeader(fuelTable, boldFont, "Fuel Type", "Jobs", "Revenue");

                foreach (var item in viewModel.RevenueByFuel)
                {
                    fuelTable.AddCell(CreateCell(item.FuelType, regularFont));
                    fuelTable.AddCell(CreateCell(item.JobCount.ToString(), regularFont));
                    fuelTable.AddCell(CreateCell($"${item.Revenue:N0}", regularFont));
                }
                document.Add(fuelTable);
                document.Add(new Paragraph().SetMarginBottom(15));
            }

            // Jobs by Status
            if (viewModel.JobsByStatusData.Count > 0)
            {
                AddSectionHeader(document, "Jobs by Status", boldFont);
                var statusTable = new Table(UnitValue.CreatePercentArray([3, 2])).UseAllAvailableWidth();
                AddTableHeader(statusTable, boldFont, "Status", "Count");

                foreach (var item in viewModel.JobsByStatusData)
                {
                    statusTable.AddCell(CreateCell(item.Status, regularFont));
                    statusTable.AddCell(CreateCell(item.Count.ToString(), regularFont));
                }
                document.Add(statusTable);
                document.Add(new Paragraph().SetMarginBottom(15));
            }

            // Top Customers
            if (viewModel.TopCustomers.Count > 0)
            {
                AddSectionHeader(document, "Top Customers", boldFont);
                var customersDetailTable = new Table(UnitValue.CreatePercentArray([4, 2, 2])).UseAllAvailableWidth();
                AddTableHeader(customersDetailTable, boldFont, "Customer", "Jobs", "Revenue");

                foreach (var item in viewModel.TopCustomers)
                {
                    customersDetailTable.AddCell(CreateCell(item.Name, regularFont));
                    customersDetailTable.AddCell(CreateCell(item.JobCount.ToString(), regularFont));
                    customersDetailTable.AddCell(CreateCell($"${item.Revenue:N0}", regularFont));
                }
                document.Add(customersDetailTable);
            }

            // Footer
            document.Add(new Paragraph()
                .SetMarginTop(30)
                .Add(new Text("Generated by OneManVan - HVAC Field Service Management")
                    .SetFont(regularFont)
                    .SetFontSize(9)
                    .SetFontColor(ColorConstants.GRAY)));
        });
    }

    private static void AddSectionHeader(Document document, string title, PdfFont boldFont)
    {
        document.Add(new Paragraph(title)
            .SetFont(boldFont)
            .SetFontSize(14)
            .SetMarginBottom(10));
    }

    private static Table CreateMetricsTable(int columns)
    {
        var percentages = new float[columns];
        for (int i = 0; i < columns; i++)
        {
            percentages[i] = 1f;
        }
        return new Table(UnitValue.CreatePercentArray(percentages))
            .UseAllAvailableWidth();
    }

    private static void AddMetricCell(Table table, string label, string value, PdfFont boldFont, PdfFont regularFont)
    {
        var cell = new Cell()
            .SetBorder(Border.NO_BORDER)
            .SetPadding(10)
            .SetBackgroundColor(new DeviceRgb(245, 245, 245));

        cell.Add(new Paragraph(label)
            .SetFont(regularFont)
            .SetFontSize(10)
            .SetFontColor(ColorConstants.GRAY)
            .SetMarginBottom(3));

        cell.Add(new Paragraph(value)
            .SetFont(boldFont)
            .SetFontSize(18));

        table.AddCell(cell);
    }

    private static void AddTableHeader(Table table, PdfFont boldFont, params string[] headers)
    {
        foreach (var header in headers)
        {
            table.AddHeaderCell(new Cell()
                .SetBackgroundColor(new DeviceRgb(220, 220, 220))
                .SetPadding(8)
                .Add(new Paragraph(header).SetFont(boldFont).SetFontSize(11)));
        }
    }

    private static Cell CreateCell(string text, PdfFont font)
    {
        return new Cell()
            .SetPadding(8)
            .Add(new Paragraph(text).SetFont(font).SetFontSize(11));
    }
}
