namespace OneManVan.Shared.Constants;

/// <summary>
/// Constants for auto-generated entity ID prefixes.
/// Used throughout the application for consistent ID generation.
/// </summary>
public static class EntityIdPrefixes
{
    /// <summary>Customer ID prefix (e.g., C-0001)</summary>
    public const string Customer = "C-";
    
    /// <summary>Product number prefix (e.g., PROD-0001)</summary>
    public const string Product = "PROD-";
    
    /// <summary>Asset number prefix (e.g., AST-0001)</summary>
    public const string Asset = "AST-";
    
    /// <summary>Inventory SKU prefix (e.g., INV-0001)</summary>
    public const string Inventory = "INV-";
    
    /// <summary>Job number prefix (e.g., JOB-0001)</summary>
    public const string Job = "JOB-";
    
    /// <summary>Invoice number prefix (e.g., INV-0001)</summary>
    public const string Invoice = "INV-";
    
    /// <summary>Estimate number prefix (e.g., EST-0001)</summary>
    public const string Estimate = "EST-";
    
    /// <summary>Site number prefix (e.g., SITE-0001)</summary>
    public const string Site = "SITE-";
    
    /// <summary>Service Agreement number prefix (e.g., SA-0001)</summary>
    public const string ServiceAgreement = "SA-";
    
    /// <summary>
    /// Generates a formatted ID number with the given prefix and number.
    /// </summary>
    /// <param name="prefix">The prefix to use (e.g., "C-")</param>
    /// <param name="number">The sequential number</param>
    /// <param name="digits">Number of digits to pad (default 4)</param>
    /// <returns>Formatted ID string (e.g., "C-0001")</returns>
    public static string FormatId(string prefix, int number, int digits = 4)
    {
        return $"{prefix}{number.ToString($"D{digits}")}";
    }
}
