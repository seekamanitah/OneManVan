namespace OneManVan.Mobile.Services;

/// <summary>
/// Service for handling line item input dialogs across multiple pages.
/// Provides consistent UX for adding line items to estimates and invoices.
/// </summary>
public class LineItemDialogService
{
    /// <summary>
    /// Get line item input from user via prompts
    /// </summary>
    /// <param name="page">The page requesting input</param>
    /// <param name="itemTypePrompt">Optional custom prompt for item type (null to skip type selection)</param>
    /// <returns>LineItemInput data or null if cancelled</returns>
    public async Task<LineItemInput?> GetLineItemInputAsync(
        ContentPage page, 
        string? itemTypePrompt = null)
    {
        // Optional: Ask for line item type first
        string? selectedType = null;
        if (itemTypePrompt != null)
        {
            var types = new[] { "Labor", "Part", "Material", "Equipment", "Service", "Fee", "Discount" };
            selectedType = await page.DisplayActionSheet(itemTypePrompt, "Cancel", null, types);
            
            if (selectedType == "Cancel" || selectedType == null)
                return null;
        }

        // Get description
        var description = await page.DisplayPromptAsync(
            "Add Line Item", 
            "Description:", 
            placeholder: GetDescriptionExample(selectedType));
            
        if (string.IsNullOrWhiteSpace(description))
            return null;

        // Get quantity
        var qtyStr = await page.DisplayPromptAsync(
            "Quantity", 
            "Enter quantity:", 
            "Next", 
            "Cancel", 
            "1", 
            keyboard: Keyboard.Numeric);
            
        if (string.IsNullOrWhiteSpace(qtyStr))
            return null;
            
        if (!decimal.TryParse(qtyStr, out var quantity) || quantity <= 0)
        {
            await page.DisplayAlert("Invalid", "Please enter a valid quantity greater than 0", "OK");
            return null;
        }

        // Get unit price
        var priceStr = await page.DisplayPromptAsync(
            "Unit Price", 
            "Enter unit price:", 
            "Add", 
            "Cancel", 
            "0.00", 
            keyboard: Keyboard.Numeric);
            
        if (string.IsNullOrWhiteSpace(priceStr))
            return null;
            
        if (!decimal.TryParse(priceStr, out var unitPrice) || unitPrice < 0)
        {
            await page.DisplayAlert("Invalid", "Please enter a valid price", "OK");
            return null;
        }

        return new LineItemInput(description.Trim(), quantity, unitPrice, selectedType);
    }

    private static string GetDescriptionExample(string? type) => type switch
    {
        "Labor" => "e.g., Service call labor",
        "Part" => "e.g., Replacement part",
        "Material" => "e.g., Copper tubing",
        "Equipment" => "e.g., Equipment rental",
        "Service" => "e.g., Annual maintenance",
        "Fee" => "e.g., Service fee",
        "Discount" => "e.g., Promotional discount",
        _ => "e.g., Line item description"
    };
}

/// <summary>
/// Data returned from line item dialog
/// </summary>
public record LineItemInput(string Description, decimal Quantity, decimal UnitPrice, string? ItemType = null)
{
    public decimal Total => Quantity * UnitPrice;
}
