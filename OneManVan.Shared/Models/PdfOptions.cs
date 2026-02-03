namespace OneManVan.Shared.Models;

/// <summary>
/// Options for customizing PDF generation across different document types.
/// </summary>
public class PdfOptions
{
    /// <summary>
    /// Include individual line item prices.
    /// </summary>
    public bool IncludePrices { get; set; } = true;

    /// <summary>
    /// Show labor breakdown (hourly rate x hours) vs flat labor amount.
    /// </summary>
    public bool ShowLaborBreakdown { get; set; } = true;

    /// <summary>
    /// Use flat rate display (single total) instead of itemized.
    /// </summary>
    public bool UseFlatRate { get; set; } = false;

    /// <summary>
    /// Hide internal notes from the PDF.
    /// </summary>
    public bool HideInternalNotes { get; set; } = true;

    /// <summary>
    /// Include company header with logo and contact info.
    /// </summary>
    public bool IncludeCompanyHeader { get; set; } = true;

    /// <summary>
    /// Include customer information section.
    /// </summary>
    public bool IncludeCustomerInfo { get; set; } = true;

    /// <summary>
    /// Include payment terms and due date.
    /// </summary>
    public bool IncludePaymentTerms { get; set; } = true;

    /// <summary>
    /// For material lists: minimal format for supply house (item, qty, notes only).
    /// </summary>
    public bool SupplyHouseFormat { get; set; } = false;

    /// <summary>
    /// Include quantity column.
    /// </summary>
    public bool IncludeQuantity { get; set; } = true;

    /// <summary>
    /// Include notes/description column.
    /// </summary>
    public bool IncludeNotes { get; set; } = true;

    /// <summary>
    /// Include tax breakdown.
    /// </summary>
    public bool IncludeTax { get; set; } = true;

    /// <summary>
    /// Show subtotal before tax.
    /// </summary>
    public bool ShowSubtotal { get; set; } = true;
}
