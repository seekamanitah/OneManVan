using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OneManVan.Dialogs;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;
using OneManVan.Shared.Services;

namespace OneManVan.Controls;

public partial class ProductFormContent : UserControl
{
    private readonly OneManVanDbContext _dbContext;
    private readonly SerialNumberValidator _serialValidator;
    private int? _productId;

    public ProductFormContent()
    {
        InitializeComponent();
        
        _dbContext = App.DbContext;
        _serialValidator = new SerialNumberValidator(_dbContext);
        
        // Populate Category combo
        CategoryCombo.ItemsSource = Enum.GetValues(typeof(ProductCategory))
            .Cast<ProductCategory>()
            .Select(c => new ComboBoxItem { Content = c.ToString(), Tag = c })
            .ToList();
        CategoryCombo.SelectedIndex = 0;
    }

    private async void OnSerialNumberLostFocus(object sender, RoutedEventArgs e)
    {
        var serialNumber = SerialNumberTextBox.Text?.Trim();
        
        // Hide indicator by default
        SerialValidationIndicator.Visibility = Visibility.Collapsed;
        
        if (string.IsNullOrWhiteSpace(serialNumber))
        {
            return;
        }

        // Validate serial number
        var result = await _serialValidator.ValidateProductSerialAsync(serialNumber, _productId);

        if (result.IsDuplicate)
        {
            // Show warning indicator
            SerialValidationIndicator.Background = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
            SerialValidationIndicator.Visibility = Visibility.Visible;
            ((TextBlock)SerialValidationIndicator.Child).Text = "!";

            // Show duplicate dialog
            var dialog = new SerialNumberDuplicateDialog(result, serialNumber)
            {
                Owner = Window.GetWindow(this)
            };
            
            var viewExisting = dialog.ShowDialog() == true;
            
            if (viewExisting && result.DuplicateId.HasValue)
            {
                // Navigate to existing item
                if (result.DuplicateType == "Asset")
                {
                    // TODO: Navigate to asset
                    MessageBox.Show($"Asset ID: {result.DuplicateId}", "Navigate to Asset", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (result.DuplicateType == "Product")
                {
                    // TODO: Navigate to product
                    MessageBox.Show($"Product ID: {result.DuplicateId}", "Navigate to Product", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        else if (result.IsValid)
        {
            // Show success indicator
            SerialValidationIndicator.Background = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Green
            SerialValidationIndicator.Visibility = Visibility.Visible;
            ((TextBlock)SerialValidationIndicator.Child).Text = "?";
        }
    }

    public Product GetProduct()
    {
        var selectedCategory = (CategoryCombo.SelectedItem as ComboBoxItem)?.Tag as ProductCategory? ?? ProductCategory.Unknown;
        
        return new Product
        {
            Manufacturer = ManufacturerTextBox.Text,
            ModelNumber = ModelNumberTextBox.Text,
            SerialNumber = string.IsNullOrWhiteSpace(SerialNumberTextBox.Text) ? null : SerialNumberTextBox.Text.Trim(),
            ProductName = ProductNameTextBox.Text,
            Category = selectedCategory,
            WholesaleCost = decimal.TryParse(WholesaleCostTextBox.Text, out var cost) ? cost : null,
            SuggestedSellPrice = decimal.TryParse(SuggestedSellPriceTextBox.Text, out var price) ? price : null,
            Description = DescriptionTextBox.Text
        };
    }

    public void LoadProduct(Product product)
    {
        _productId = product.Id;
        
        ManufacturerTextBox.Text = product.Manufacturer;
        ModelNumberTextBox.Text = product.ModelNumber;
        SerialNumberTextBox.Text = product.SerialNumber;
        ProductNameTextBox.Text = product.ProductName;
        WholesaleCostTextBox.Text = product.WholesaleCost?.ToString("F2") ?? "";
        SuggestedSellPriceTextBox.Text = product.SuggestedSellPrice?.ToString("F2") ?? "";
        DescriptionTextBox.Text = product.Description;
        
        // Select category
        var item = CategoryCombo.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(i => (ProductCategory)i.Tag == product.Category);
        if (item != null)
        {
            CategoryCombo.SelectedItem = item;
        }
    }

    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(ManufacturerTextBox.Text))
        {
            return false;
        }
        if (string.IsNullOrWhiteSpace(ModelNumberTextBox.Text))
        {
            return false;
        }
        return true;
    }
}
