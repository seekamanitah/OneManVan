using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using OneManVan.Shared.Models;

namespace OneManVan.Services;

/// <summary>
/// Service for printing documents like estimates and invoices.
/// </summary>
public static class PrintService
{
    /// <summary>
    /// Prints an estimate document.
    /// </summary>
    public static void PrintEstimate(Estimate estimate)
    {
        var document = CreateEstimateDocument(estimate);
        PrintDocument(document, $"Estimate - {estimate.Title}");
    }

    /// <summary>
    /// Prints an invoice document.
    /// </summary>
    public static void PrintInvoice(Invoice invoice)
    {
        var document = CreateInvoiceDocument(invoice);
        PrintDocument(document, $"Invoice - {invoice.InvoiceNumber}");
    }

    /// <summary>
    /// Shows print preview for an estimate.
    /// </summary>
    public static void PreviewEstimate(Estimate estimate)
    {
        var document = CreateEstimateDocument(estimate);
        ShowPrintPreview(document, $"Estimate Preview - {estimate.Title}");
    }

    /// <summary>
    /// Shows print preview for an invoice.
    /// </summary>
    public static void PreviewInvoice(Invoice invoice)
    {
        var document = CreateInvoiceDocument(invoice);
        ShowPrintPreview(document, $"Invoice Preview - {invoice.InvoiceNumber}");
    }

    private static FlowDocument CreateEstimateDocument(Estimate estimate)
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 12,
            PagePadding = new Thickness(50),
            ColumnWidth = double.MaxValue
        };

        // Header
        var header = new Paragraph(new Run("ESTIMATE"))
        {
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(137, 180, 250)),
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        };
        doc.Blocks.Add(header);

        // Company Info
        var companySection = new Section();
        companySection.Blocks.Add(new Paragraph(new Run("OneManVan HVAC Services"))
        {
            FontSize = 16,
            FontWeight = FontWeights.Bold
        });
        companySection.Blocks.Add(new Paragraph(new Run("Your Local HVAC Expert"))
        {
            Foreground = Brushes.Gray,
            Margin = new Thickness(0, 0, 0, 20)
        });
        doc.Blocks.Add(companySection);

        // Estimate Info Table
        var infoTable = new Table { CellSpacing = 0 };
        infoTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        infoTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

        var infoGroup = new TableRowGroup();
        
        // Customer column
        var infoRow = new TableRow();
        var customerCell = new TableCell(new Paragraph(new Run("Bill To:") { FontWeight = FontWeights.Bold }));
        customerCell.Blocks.Add(new Paragraph(new Run(estimate.Customer?.Name ?? "N/A")));
        customerCell.Blocks.Add(new Paragraph(new Run(estimate.Customer?.Email ?? "")));
        customerCell.Blocks.Add(new Paragraph(new Run(estimate.Customer?.Phone ?? "")));
        infoRow.Cells.Add(customerCell);

        // Estimate details column
        var detailsCell = new TableCell();
        detailsCell.Blocks.Add(new Paragraph(new Run($"Estimate #: EST-{estimate.Id:D5}") { FontWeight = FontWeights.Bold }));
        detailsCell.Blocks.Add(new Paragraph(new Run($"Date: {estimate.CreatedAt:MMM d, yyyy}")));
        detailsCell.Blocks.Add(new Paragraph(new Run($"Valid Until: {estimate.ExpiresAt:MMM d, yyyy}")));
        detailsCell.Blocks.Add(new Paragraph(new Run($"Status: {estimate.StatusDisplay}")));
        detailsCell.TextAlignment = TextAlignment.Right;
        infoRow.Cells.Add(detailsCell);

        infoGroup.Rows.Add(infoRow);
        infoTable.RowGroups.Add(infoGroup);
        doc.Blocks.Add(infoTable);

        // Title
        doc.Blocks.Add(new Paragraph(new Run(estimate.Title))
        {
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 20, 0, 10)
        });

        if (!string.IsNullOrEmpty(estimate.Description))
        {
            doc.Blocks.Add(new Paragraph(new Run(estimate.Description))
            {
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 20)
            });
        }

        // Line Items Table
        var itemsTable = new Table { CellSpacing = 0, BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(1) };
        itemsTable.Columns.Add(new TableColumn { Width = new GridLength(3, GridUnitType.Star) }); // Description
        itemsTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) }); // Qty
        itemsTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) }); // Unit Price
        itemsTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) }); // Total

        var headerGroup = new TableRowGroup();
        var headerRow = new TableRow { Background = new SolidColorBrush(Color.FromRgb(49, 50, 68)) };
        headerRow.Cells.Add(CreateHeaderCell("Description"));
        headerRow.Cells.Add(CreateHeaderCell("Qty"));
        headerRow.Cells.Add(CreateHeaderCell("Unit Price"));
        headerRow.Cells.Add(CreateHeaderCell("Total"));
        headerGroup.Rows.Add(headerRow);
        itemsTable.RowGroups.Add(headerGroup);

        var itemsGroup = new TableRowGroup();
        foreach (var line in estimate.Lines.OrderBy(l => l.SortOrder))
        {
            var row = new TableRow();
            row.Cells.Add(CreateCell(line.Description));
            row.Cells.Add(CreateCell($"{line.Quantity:N2} {line.Unit}"));
            row.Cells.Add(CreateCell($"${line.UnitPrice:N2}"));
            row.Cells.Add(CreateCell($"${line.Total:N2}"));
            itemsGroup.Rows.Add(row);
        }
        itemsTable.RowGroups.Add(itemsGroup);
        doc.Blocks.Add(itemsTable);

        // Totals
        var totalsSection = new Section { TextAlignment = TextAlignment.Right, Margin = new Thickness(0, 20, 0, 0) };
        totalsSection.Blocks.Add(new Paragraph(new Run($"Subtotal: ${estimate.SubTotal:N2}")));
        totalsSection.Blocks.Add(new Paragraph(new Run($"Tax ({estimate.TaxRate:N1}%): ${estimate.TaxAmount:N2}")));
        totalsSection.Blocks.Add(new Paragraph(new Run($"Total: ${estimate.Total:N2}"))
        {
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(166, 227, 161))
        });
        doc.Blocks.Add(totalsSection);

        // Terms
        if (!string.IsNullOrEmpty(estimate.Terms))
        {
            doc.Blocks.Add(new Paragraph(new Run("Terms & Conditions"))
            {
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 30, 0, 5)
            });
            doc.Blocks.Add(new Paragraph(new Run(estimate.Terms))
            {
                FontSize = 10,
                Foreground = Brushes.Gray
            });
        }

        // Footer
        doc.Blocks.Add(new Paragraph(new Run("Thank you for your business!"))
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 30, 0, 0),
            FontStyle = FontStyles.Italic
        });

        return doc;
    }

    private static FlowDocument CreateInvoiceDocument(Invoice invoice)
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 12,
            PagePadding = new Thickness(50),
            ColumnWidth = double.MaxValue
        };

        // Header
        var header = new Paragraph(new Run("INVOICE"))
        {
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(137, 180, 250)),
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        };
        doc.Blocks.Add(header);

        // Company Info
        var companySection = new Section();
        companySection.Blocks.Add(new Paragraph(new Run("OneManVan HVAC Services"))
        {
            FontSize = 16,
            FontWeight = FontWeights.Bold
        });
        companySection.Blocks.Add(new Paragraph(new Run("Your Local HVAC Expert"))
        {
            Foreground = Brushes.Gray,
            Margin = new Thickness(0, 0, 0, 20)
        });
        doc.Blocks.Add(companySection);

        // Invoice Info Table
        var infoTable = new Table { CellSpacing = 0 };
        infoTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        infoTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

        var infoGroup = new TableRowGroup();
        var infoRow = new TableRow();

        // Customer column
        var customerCell = new TableCell(new Paragraph(new Run("Bill To:") { FontWeight = FontWeights.Bold }));
        customerCell.Blocks.Add(new Paragraph(new Run(invoice.Customer?.Name ?? "N/A")));
        customerCell.Blocks.Add(new Paragraph(new Run(invoice.Customer?.Email ?? "")));
        customerCell.Blocks.Add(new Paragraph(new Run(invoice.Customer?.Phone ?? "")));
        infoRow.Cells.Add(customerCell);

        // Invoice details column
        var detailsCell = new TableCell();
        detailsCell.Blocks.Add(new Paragraph(new Run($"Invoice #: {invoice.InvoiceNumber}") { FontWeight = FontWeights.Bold }));
        detailsCell.Blocks.Add(new Paragraph(new Run($"Date: {invoice.CreatedAt:MMM d, yyyy}")));
        detailsCell.Blocks.Add(new Paragraph(new Run($"Due Date: {invoice.DueDate:MMM d, yyyy}")));
        detailsCell.Blocks.Add(new Paragraph(new Run($"Status: {invoice.StatusDisplay}")));
        detailsCell.TextAlignment = TextAlignment.Right;
        infoRow.Cells.Add(detailsCell);

        infoGroup.Rows.Add(infoRow);
        infoTable.RowGroups.Add(infoGroup);
        doc.Blocks.Add(infoTable);

        // Description
        if (!string.IsNullOrEmpty(invoice.Notes))
        {
            doc.Blocks.Add(new Paragraph(new Run(invoice.Notes))
            {
                Margin = new Thickness(0, 20, 0, 20)
            });
        }

        // Amount Details
        var amountsSection = new Section { Margin = new Thickness(0, 20, 0, 0) };
        
        var amountTable = new Table { CellSpacing = 0 };
        amountTable.Columns.Add(new TableColumn { Width = new GridLength(2, GridUnitType.Star) });
        amountTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

        var amountGroup = new TableRowGroup();

        // Subtotal
        var subtotalRow = new TableRow();
        subtotalRow.Cells.Add(CreateCell("Subtotal"));
        subtotalRow.Cells.Add(CreateCell($"${invoice.SubTotal:N2}", TextAlignment.Right));
        amountGroup.Rows.Add(subtotalRow);

        // Tax
        var taxRow = new TableRow();
        taxRow.Cells.Add(CreateCell($"Tax ({invoice.TaxRate:N1}%)"));
        taxRow.Cells.Add(CreateCell($"${invoice.TaxAmount:N2}", TextAlignment.Right));
        amountGroup.Rows.Add(taxRow);

        // Total
        var totalRow = new TableRow { Background = new SolidColorBrush(Color.FromRgb(49, 50, 68)) };
        totalRow.Cells.Add(CreateHeaderCell("Total Due"));
        totalRow.Cells.Add(CreateHeaderCell($"${invoice.Total:N2}"));
        amountGroup.Rows.Add(totalRow);

        // Amount Paid
        if (invoice.AmountPaid > 0)
        {
            var paidRow = new TableRow();
            paidRow.Cells.Add(CreateCell("Amount Paid"));
            paidRow.Cells.Add(CreateCell($"${invoice.AmountPaid:N2}", TextAlignment.Right));
            amountGroup.Rows.Add(paidRow);

            var balanceRow = new TableRow();
            balanceRow.Cells.Add(CreateBoldCell("Balance Due"));
            balanceRow.Cells.Add(CreateBoldCell($"${invoice.BalanceDue:N2}", TextAlignment.Right));
            amountGroup.Rows.Add(balanceRow);
        }

        amountTable.RowGroups.Add(amountGroup);
        amountsSection.Blocks.Add(amountTable);
        doc.Blocks.Add(amountsSection);

        // Payment Instructions
        doc.Blocks.Add(new Paragraph(new Run("Payment Information"))
        {
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 30, 0, 5)
        });
        doc.Blocks.Add(new Paragraph(new Run("Please make payment by the due date. Accepted methods: Cash, Check, Credit Card"))
        {
            FontSize = 10,
            Foreground = Brushes.Gray
        });

        // Footer
        doc.Blocks.Add(new Paragraph(new Run("Thank you for your business!"))
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 30, 0, 0),
            FontStyle = FontStyles.Italic
        });

        return doc;
    }

    private static TableCell CreateHeaderCell(string text)
    {
        return new TableCell(new Paragraph(new Run(text)
        {
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White
        }))
        {
            Padding = new Thickness(8, 5, 8, 5)
        };
    }

    private static TableCell CreateCell(string text, TextAlignment alignment = TextAlignment.Left)
    {
        return new TableCell(new Paragraph(new Run(text)) { TextAlignment = alignment })
        {
            Padding = new Thickness(8, 5, 8, 5),
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(0, 0, 0, 1)
        };
    }

    private static TableCell CreateBoldCell(string text, TextAlignment alignment = TextAlignment.Left)
    {
        return new TableCell(new Paragraph(new Run(text) { FontWeight = FontWeights.Bold }) { TextAlignment = alignment })
        {
            Padding = new Thickness(8, 5, 8, 5),
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(0, 0, 0, 1)
        };
    }

    private static void PrintDocument(FlowDocument document, string title)
    {
        var printDialog = new PrintDialog();
        if (printDialog.ShowDialog() == true)
        {
            var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
            paginator.PageSize = new Size(printDialog.PrintableAreaWidth, printDialog.PrintableAreaHeight);
            printDialog.PrintDocument(paginator, title);
        }
    }

    private static void ShowPrintPreview(FlowDocument document, string title)
    {
        // Simple preview - in a full implementation this would be a separate window
        var printDialog = new PrintDialog();
        if (printDialog.ShowDialog() == true)
        {
            var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
            paginator.PageSize = new Size(printDialog.PrintableAreaWidth, printDialog.PrintableAreaHeight);
            printDialog.PrintDocument(paginator, title);
        }
    }
}
