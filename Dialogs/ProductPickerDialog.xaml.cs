using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Services;

namespace OneManVan.Dialogs;

/// <summary>
/// Dialog for selecting products to add as invoice line items.
/// Triggers asset creation for products with serial numbers.
/// </summary>
public partial class ProductPickerDialog : Window
{
    private readonly OneManVanDbContext _dbContext;
    private readonly SerialNumberValidator _serialValidator;
    private readonly int _customerId;
    private readonly int? _siteId;
    private readonly string? _customerName;
    private readonly string? _locationAddress;
    private readonly DateTime _invoiceDate;
    
    private List<Product> _allProducts = new();
    
    public InvoiceLineItem? LineItem { get; private set; }
    public Asset? CreatedAsset { get; private set; }

    public ProductPickerDialog(
        OneManVanDbContext dbContext,
        int customerId,
        string? customerName = null,
        int? siteId = null,
        string? locationAddress = null,
        DateTime? invoiceDate = null)
    {
        InitializeComponent();
        
        _dbContext = dbContext;
        _serialValidator = new SerialNumberValidator(dbContext);
        _customerId = customerId;
        _customerName = customerName;
        _siteId = siteId;
        _locationAddress = locationAddress;
        _invoiceDate = invoiceDate ?? DateTime.Today;
        
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _allProducts = await _dbContext.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.Manufacturer)
                .ThenBy(p => p.ModelNumber)
                .ToListAsync();

            ProductsListView.ItemsSource = _allProducts;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load products: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnSearchTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        var searchText = SearchTextBox.Text?.ToLower() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(searchText))
        {
            ProductsListView.ItemsSource = _allProducts;
            return;
        }

        var filtered = _allProducts.Where(p =>
            p.Manufacturer?.ToLower().Contains(searchText) == true ||
            p.ModelNumber?.ToLower().Contains(searchText) == true ||
            p.ProductName?.ToLower().Contains(searchText) == true ||
            p.SerialNumber?.ToLower().Contains(searchText) == true
        ).ToList();

        ProductsListView.ItemsSource = filtered;
    }

    private async void OnAddClick(object sender, RoutedEventArgs e)
    {
        if (ProductsListView.SelectedItem is not Product product)
        {
            MessageBox.Show("Please select a product", "No Selection",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Check if product has serial number
        if (!string.IsNullOrWhiteSpace(product.SerialNumber))
        {
            // Validate serial number
            var validation = await _serialValidator.ValidateProductSerialAsync(product.SerialNumber, product.Id);

            if (validation.IsDuplicate)
            {
                // Show duplicate warning
                var duplicateDialog = new SerialNumberDuplicateDialog(validation, product.SerialNumber)
                {
                    Owner = this
                };
                
                duplicateDialog.ShowDialog();
                return; // Don't add if duplicate
            }

            // Prompt for asset creation
            var assetDialog = new QuickAssetDetailsDialog(
                product,
                _customerId,
                _customerName,
                _siteId,
                _locationAddress,
                _invoiceDate)
            {
                Owner = this
            };

            if (assetDialog.ShowDialog() == true && assetDialog.CreatedAsset != null)
            {
                // Asset will be created
                CreatedAsset = assetDialog.CreatedAsset;
            }
            else
            {
                // User skipped asset creation - still add line item
                var skipResult = MessageBox.Show(
                    "Asset creation skipped. Add product to invoice without creating asset?",
                    "Continue?",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (skipResult == MessageBoxResult.No)
                    return;
            }
        }

        // Create line item
        LineItem = new InvoiceLineItem
        {
            Source = LineItemSource.Product,
            SourceId = product.Id,
            Description = $"{product.Manufacturer} {product.ModelNumber}",
            Quantity = 1,
            UnitPrice = product.SuggestedSellPrice ?? 0,
            SerialNumber = product.SerialNumber
        };

        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
