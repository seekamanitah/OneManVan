namespace OneManVan.Mobile.Constants;

/// <summary>
/// Line item types for estimates and invoices.
/// Standardized options for line item categorization.
/// </summary>
public static class LineItemTypes
{
    public static readonly string[] All = 
    {
        Labor,
        Part,
        Material,
        Equipment,
        Service,
        Fee,
        Discount
    };
    
    public const string Labor = "Labor";
    public const string Part = "Part";
    public const string Material = "Material";
    public const string Equipment = "Equipment";
    public const string Service = "Service";
    public const string Fee = "Fee";
    public const string Discount = "Discount";
}
