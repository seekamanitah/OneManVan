using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Mobile.Pages;

[QueryProperty(nameof(ProductId), "id")]
public partial class AddProductPage : ContentPage
{
    private readonly OneManVanDbContext _db;
    private Product? _existingProduct;

    public int ProductId { get; set; }

    public AddProductPage(OneManVanDbContext db)
    {
        InitializeComponent();
        _db = db;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (ProductId > 0)
        {
            Title = "Edit Product";
            SaveButton.Text = "Update Product";
            await LoadProductAsync();
        }
    }

    private async Task LoadProductAsync()
    {
        try
        {
            _existingProduct = await _db.Products.FindAsync(ProductId);
            if (_existingProduct == null)
            {
                await DisplayAlertAsync("Error", "Product not found.", "OK");
                await Navigation.PopAsync();
                return;
            }

            // Populate fields
            ManufacturerEntry.Text = _existingProduct.Manufacturer;
            ModelNumberEntry.Text = _existingProduct.ModelNumber;
            ProductNameEntry.Text = _existingProduct.ProductName;
            CategoryPicker.SelectedItem = _existingProduct.Category.ToString().Replace("_", " ");
            
            if (_existingProduct.TonnageX10.HasValue)
                TonnageEntry.Text = (_existingProduct.TonnageX10.Value / 10.0m).ToString("N1");
            if (_existingProduct.CoolingBtu.HasValue)
                BtuEntry.Text = _existingProduct.CoolingBtu.Value.ToString();
            if (_existingProduct.SeerRating.HasValue)
                SeerEntry.Text = _existingProduct.SeerRating.Value.ToString("N1");
            if (_existingProduct.AfueRating.HasValue)
                AfueEntry.Text = _existingProduct.AfueRating.Value.ToString("N1");
            
            if (_existingProduct.RefrigerantType != RefrigerantType.Unknown)
                RefrigerantPicker.SelectedItem = _existingProduct.RefrigerantDisplay;
            if (_existingProduct.Voltage.HasValue)
                VoltagePicker.SelectedItem = $"{_existingProduct.Voltage}V";
            
            if (_existingProduct.Msrp.HasValue)
                MsrpEntry.Text = _existingProduct.Msrp.Value.ToString("N2");
            if (_existingProduct.WholesaleCost.HasValue)
                WholesaleEntry.Text = _existingProduct.WholesaleCost.Value.ToString("N2");
            if (_existingProduct.SuggestedSellPrice.HasValue)
                SellPriceEntry.Text = _existingProduct.SuggestedSellPrice.Value.ToString("N2");
            if (_existingProduct.LaborHoursEstimate.HasValue)
                LaborHoursEntry.Text = _existingProduct.LaborHoursEstimate.Value.ToString("N1");
            
            PartsWarrantyEntry.Text = _existingProduct.PartsWarrantyYears?.ToString();
            CompressorWarrantyEntry.Text = _existingProduct.CompressorWarrantyYears?.ToString();
            LaborWarrantyEntry.Text = _existingProduct.LaborWarrantyYears?.ToString();
            RegistrationSwitch.IsToggled = _existingProduct.RegistrationRequired;
            
            NotesEditor.Text = _existingProduct.Notes;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load product: {ex.Message}", "OK");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(ManufacturerEntry.Text))
        {
            await DisplayAlertAsync("Required", "Manufacturer is required.", "OK");
            ManufacturerEntry.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(ModelNumberEntry.Text))
        {
            await DisplayAlertAsync("Required", "Model Number is required.", "OK");
            ModelNumberEntry.Focus();
            return;
        }

        if (CategoryPicker.SelectedItem == null)
        {
            await DisplayAlertAsync("Required", "Category is required.", "OK");
            return;
        }

        try
        {
            SaveButton.IsEnabled = false;
            SaveButton.Text = "Saving...";

            var product = _existingProduct ?? new Product();

            product.Manufacturer = ManufacturerEntry.Text.Trim();
            product.ModelNumber = ModelNumberEntry.Text.Trim();
            product.ProductName = ProductNameEntry.Text?.Trim();
            
            // Parse category
            var categoryStr = CategoryPicker.SelectedItem.ToString()?.Replace(" ", "") ?? "Other";
            if (Enum.TryParse<ProductCategory>(categoryStr, out var category))
                product.Category = category;

            // Specifications
            if (decimal.TryParse(TonnageEntry.Text, out var tonnage))
                product.TonnageX10 = (int)(tonnage * 10);
            if (int.TryParse(BtuEntry.Text, out var btu))
                product.CoolingBtu = btu;
            if (decimal.TryParse(SeerEntry.Text, out var seer))
                product.SeerRating = seer;
            if (decimal.TryParse(AfueEntry.Text, out var afue))
                product.AfueRating = afue;
            
            if (RefrigerantPicker.SelectedItem != null)
            {
                var refStr = RefrigerantPicker.SelectedItem.ToString()?.Replace("-", "") ?? "";
                if (Enum.TryParse<RefrigerantType>(refStr, out var refType))
                    product.RefrigerantType = refType;
            }
            
            // Parse voltage from picker (e.g., "208/230V" -> 230)
            if (VoltagePicker.SelectedItem != null)
            {
                var voltStr = VoltagePicker.SelectedItem.ToString()?.Replace("V", "") ?? "";
                if (voltStr.Contains('/'))
                    voltStr = voltStr.Split('/').Last();
                if (int.TryParse(voltStr, out var volt))
                    product.Voltage = volt;
            }

            // Pricing
            if (decimal.TryParse(MsrpEntry.Text, out var msrp))
                product.Msrp = msrp;
            if (decimal.TryParse(WholesaleEntry.Text, out var wholesale))
                product.WholesaleCost = wholesale;
            if (decimal.TryParse(SellPriceEntry.Text, out var sellPrice))
                product.SuggestedSellPrice = sellPrice;
            if (decimal.TryParse(LaborHoursEntry.Text, out var laborHours))
                product.LaborHoursEstimate = laborHours;

            // Warranty
            if (int.TryParse(PartsWarrantyEntry.Text, out var partsWarranty))
                product.PartsWarrantyYears = partsWarranty;
            if (int.TryParse(CompressorWarrantyEntry.Text, out var compWarranty))
                product.CompressorWarrantyYears = compWarranty;
            if (int.TryParse(LaborWarrantyEntry.Text, out var laborWarranty))
                product.LaborWarrantyYears = laborWarranty;
            product.RegistrationRequired = RegistrationSwitch.IsToggled;

            product.Notes = NotesEditor.Text?.Trim();
            product.IsActive = true;

            if (_existingProduct == null)
            {
                product.CreatedAt = DateTime.UtcNow;
                _db.Products.Add(product);
            }
            else
            {
                product.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

            await DisplayAlertAsync("Saved", $"Product {(_existingProduct == null ? "added" : "updated")} successfully.", "OK");
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to save: {ex.Message}", "OK");
        }
        finally
        {
            SaveButton.IsEnabled = true;
            SaveButton.Text = _existingProduct == null ? "Save Product" : "Update Product";
        }
    }
}
