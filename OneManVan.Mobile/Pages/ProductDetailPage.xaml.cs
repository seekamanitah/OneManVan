using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;

namespace OneManVan.Mobile.Pages;

[QueryProperty(nameof(ProductId), "id")]
public partial class ProductDetailPage : ContentPage
{
    private readonly OneManVanDbContext _db;
    private Product? _product;
    private int _productId;

    public int ProductId
    {
        get => _productId;
        set
        {
            _productId = value;
            if (_productId > 0)
            {
                LoadProductAsync();
            }
        }
    }

    public ProductDetailPage(OneManVanDbContext db)
    {
        InitializeComponent();
        _db = db;
    }

    private async void LoadProductAsync()
    {
        try
        {
            _product = await _db.Products.FindAsync(_productId);

            if (_product == null)
            {
                await DisplayAlertAsync("Error", "Product not found.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            PopulateForm();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load product: {ex.Message}", "OK");
        }
    }

    private void PopulateForm()
    {
        if (_product == null) return;

        // Header
        ManufacturerLabel.Text = _product.Manufacturer;
        ModelLabel.Text = _product.ModelNumber;
        ProductNameLabel.Text = _product.ProductName ?? _product.DisplayName;
        CategoryLabel.Text = _product.CategoryDisplay;
        EfficiencyLabel.Text = _product.EfficiencyDisplay;

        // Pricing
        MsrpLabel.Text = _product.Msrp.HasValue ? $"${_product.Msrp:N0}" : "-";
        CostLabel.Text = _product.WholesaleCost.HasValue ? $"${_product.WholesaleCost:N0}" : "-";
        SellPriceLabel.Text = _product.SuggestedSellPrice.HasValue ? $"${_product.SuggestedSellPrice:N0}" : "-";
        LaborHoursLabel.Text = _product.LaborHoursEstimate.HasValue ? $"Est. Labor: {_product.LaborHoursEstimate:F1} hrs" : "";
        MarginLabel.Text = _product.ProfitMargin.HasValue ? $"Margin: {_product.ProfitMargin:F0}%" : "";

        // Specifications
        CapacityValueLabel.Text = _product.CapacityDisplay;
        RefrigerantValueLabel.Text = !string.IsNullOrEmpty(_product.RefrigerantDisplay) ? _product.RefrigerantDisplay : "-";
        VoltageValueLabel.Text = _product.ElectricalDisplay;
        DimensionsValueLabel.Text = !string.IsNullOrEmpty(_product.DimensionsDisplay) ? _product.DimensionsDisplay : "-";
        WeightValueLabel.Text = _product.WeightLbs.HasValue ? $"{_product.WeightLbs:F0} lbs" : "-";

        // Efficiency
        SeerValueLabel.Text = _product.SeerRating.HasValue ? $"{_product.SeerRating:F1}" : 
                              (_product.Seer2Rating.HasValue ? $"{_product.Seer2Rating:F1}" : "-");
        EerValueLabel.Text = _product.EerRating.HasValue ? $"{_product.EerRating:F1}" : "-";
        HspfValueLabel.Text = _product.HspfRating.HasValue ? $"{_product.HspfRating:F1}" : 
                              (_product.Hspf2Rating.HasValue ? $"{_product.Hspf2Rating:F1}" : "-");

        // Warranty
        PartsWarrantyLabel.Text = _product.PartsWarrantyYears.HasValue ? $"{_product.PartsWarrantyYears} years" : "-";
        CompressorWarrantyLabel.Text = _product.CompressorWarrantyYears.HasValue ? $"{_product.CompressorWarrantyYears} years" : "-";
        RegistrationLabel.Text = _product.RegistrationRequired ? "Yes" : "No";

        // Description
        if (!string.IsNullOrEmpty(_product.Description))
        {
            DescriptionSection.IsVisible = true;
            DescriptionLabel.Text = _product.Description;
        }
    }

    private async void OnCreateAssetClicked(object sender, EventArgs e)
    {
        if (_product == null) return;

        // Navigate to AddAsset with product data pre-filled
        // We'll pass the product ID so AddAssetPage can load and pre-fill
        await Shell.Current.GoToAsync($"AddAsset?productId={_product.Id}");
    }
}
