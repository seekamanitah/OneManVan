using OneManVan.Mobile.Services;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Mobile.Pages;

/// <summary>
/// Full-form page for adding a new inventory item with all details on one screen.
/// </summary>
public partial class AddInventoryItemPage : ContentPage
{
    private readonly OneManVanDbContext _db;
    private readonly IBarcodeScannerService _scannerService;

    public AddInventoryItemPage(OneManVanDbContext db, IBarcodeScannerService scannerService)
    {
        InitializeComponent();
        _db = db;
        _scannerService = scannerService;
        InitializeDefaults();
    }

    public AddInventoryItemPage(OneManVanDbContext db) : this(db, new BarcodeScannerService())
    {
    }

    private void InitializeDefaults()
    {
        CategoryPicker.SelectedIndex = 0; // General
        UnitPicker.SelectedIndex = 0; // ea
        QuantityEntry.Text = "0";
        ReorderPointEntry.Text = "0";
    }

    private async void OnScanSkuClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await _scannerService.ScanSimpleAsync("Scan SKU/Barcode");
            if (!string.IsNullOrEmpty(result))
            {
                SkuEntry.Text = result;
                try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Scan Error", $"Failed to scan: {ex.Message}", "OK");
        }
    }

    private void OnPriceChanged(object sender, TextChangedEventArgs e)
    {
        UpdateMargin();
    }

    private void UpdateMargin()
    {
        if (decimal.TryParse(CostEntry.Text, out var cost) && 
            decimal.TryParse(PriceEntry.Text, out var price) && 
            cost > 0)
        {
            var margin = ((price - cost) / cost) * 100;
            MarginLabel.Text = $"{margin:N0}%";
            MarginLabel.TextColor = margin >= 0 
                ? Color.FromArgb("#4CAF50") 
                : Color.FromArgb("#F44336");
        }
        else
        {
            MarginLabel.Text = "0%";
            MarginLabel.TextColor = Color.FromArgb("#757575");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(NameEntry.Text))
        {
            await DisplayAlertAsync("Required", "Please enter an item name.", "OK");
            NameEntry.Focus();
            return;
        }

        try
        {
            // Parse category
            var category = CategoryPicker.SelectedIndex switch
            {
                0 => InventoryCategory.General,
                1 => InventoryCategory.Filters,
                2 => InventoryCategory.Coils,
                3 => InventoryCategory.Refrigerants,
                4 => InventoryCategory.Motors,
                5 => InventoryCategory.Thermostats,
                6 => InventoryCategory.Capacitors,
                7 => InventoryCategory.Contactors,
                8 => InventoryCategory.Ductwork,
                9 => InventoryCategory.Fittings,
                10 => InventoryCategory.Electrical,
                11 => InventoryCategory.Tools,
                12 => InventoryCategory.Consumables,
                _ => InventoryCategory.General
            };

            // Parse unit
            var unit = UnitPicker.SelectedItem?.ToString() ?? "ea";

            // Parse numeric values
            if (!decimal.TryParse(QuantityEntry.Text, out var quantity))
                quantity = 0;
            if (!decimal.TryParse(ReorderPointEntry.Text, out var reorderPoint))
                reorderPoint = 0;
            if (!decimal.TryParse(CostEntry.Text, out var cost))
                cost = 0;
            if (!decimal.TryParse(PriceEntry.Text, out var price))
                price = 0;

            var item = new InventoryItem
            {
                Name = NameEntry.Text.Trim(),
                Description = DescriptionEditor.Text?.Trim(),
                Sku = SkuEntry.Text?.Trim(),
                PartNumber = PartNumberEntry.Text?.Trim(),
                Category = category,
                QuantityOnHand = quantity,
                Unit = unit,
                ReorderPoint = reorderPoint,
                Cost = cost,
                Price = price,
                Location = LocationEntry.Text?.Trim(),
                FilterSize = FilterSizeEntry.Text?.Trim(),
                RefrigerantType = RefrigerantEntry.Text?.Trim(),
                Supplier = SupplierEntry.Text?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastRestockedAt = quantity > 0 ? DateTime.UtcNow : null
            };

            _db.InventoryItems.Add(item);
            await _db.SaveChangesAsync();

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

            await DisplayAlertAsync("Success", $"'{item.Name}' added to inventory!", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to add item: {ex.Message}", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
