using Microsoft.EntityFrameworkCore;
using OneManVan.Mobile.Extensions;
using OneManVan.Mobile.Helpers;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Mobile.Pages;

[QueryProperty(nameof(ProductId), "id")]
public partial class EditProductPage : ContentPage
{
    private readonly OneManVanDbContext _db;
    private Product? _product;
    
    public int ProductId { get; set; }

    public EditProductPage(OneManVanDbContext db)
    {
        InitializeComponent();
        _db = db;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadProductAsync();
    }

    private async Task LoadProductAsync()
    {
        try
        {
            using (new LoadingScope(LoadingIndicator))
            {
                _product = await _db.Products.FindAsync(ProductId);

                if (_product == null)
                {
                    await DisplayAlertAsync("Error", "Product not found", "OK");
                    await Shell.Current.GoToAsync("..");
                    return;
                }

                ManufacturerEntry.Text = _product.Manufacturer;
                ModelNumberEntry.Text = _product.ModelNumber;
                ProductNameEntry.Text = _product.ProductName;
                DescriptionEditor.Text = _product.Description;
                MsrpEntry.Text = _product.Msrp?.ToString("N2");
                WholesaleCostEntry.Text = _product.WholesaleCost?.ToString("N2");
                SuggestedSellPriceEntry.Text = _product.SuggestedSellPrice?.ToString("N2");
                CategoryPicker.SelectedIndex = (int)_product.Category;
                IsActiveSwitch.IsToggled = _product.IsActive;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"EditProduct load error: {ex}");
            await DisplayAlertAsync("Unable to Load", 
                "Failed to load product data. Please try again.", "OK");
            await Shell.Current.GoToAsync("..");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // Validate data is loaded
        if (_product == null)
        {
            await DisplayAlertAsync("Cannot Save", 
                "Product data not loaded. Please go back and try again.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(ManufacturerEntry.Text) || string.IsNullOrWhiteSpace(ModelNumberEntry.Text))
        {
            await DisplayAlertAsync("Required", "Manufacturer and Model Number are required", "OK");
            return;
        }

        // Use SaveWithFeedbackAsync for clean save pattern
        await this.SaveWithFeedbackAsync(async () =>
        {
            _product.Manufacturer = ManufacturerEntry.Text.Trim();
            _product.ModelNumber = ModelNumberEntry.Text.Trim();
            _product.ProductName = ProductNameEntry.Text?.Trim();
            _product.Description = DescriptionEditor.Text?.Trim();
            _product.Msrp = decimal.TryParse(MsrpEntry.Text, out var msrp) ? msrp : null;
            _product.WholesaleCost = decimal.TryParse(WholesaleCostEntry.Text, out var cost) ? cost : null;
            _product.SuggestedSellPrice = decimal.TryParse(SuggestedSellPriceEntry.Text, out var price) ? price : null;
            _product.Category = (ProductCategory)CategoryPicker.SelectedIndex;
            _product.IsActive = IsActiveSwitch.IsToggled;
            _product.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
        }, 
        successMessage: "Product updated successfully");
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
