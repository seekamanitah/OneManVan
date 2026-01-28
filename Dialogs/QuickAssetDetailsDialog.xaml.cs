using System;
using System.Windows;
using OneManVan.Shared.Models;

namespace OneManVan.Dialogs;

/// <summary>
/// Dialog for quickly creating an asset from an invoice product with serial number.
/// Pre-fills information from product and invoice, allows user to add additional details.
/// </summary>
public partial class QuickAssetDetailsDialog : Window
{
    public Asset? CreatedAsset { get; private set; }

    private readonly Product _product;
    private readonly int _customerId;
    private readonly int? _siteId;
    private readonly string? _customerName;
    private readonly string? _locationAddress;
    private readonly DateTime _installDate;

    public QuickAssetDetailsDialog(
        Product product,
        int customerId,
        string? customerName,
        int? siteId = null,
        string? locationAddress = null,
        DateTime? installDate = null)
    {
        InitializeComponent();

        _product = product;
        _customerId = customerId;
        _customerName = customerName;
        _siteId = siteId;
        _locationAddress = locationAddress;
        _installDate = installDate ?? DateTime.Today;

        LoadPrefilledData();
    }

    private void LoadPrefilledData()
    {
        // Pre-filled information display
        SerialNumberText.Text = _product.SerialNumber ?? "N/A";
        BrandText.Text = _product.Manufacturer;
        ModelText.Text = _product.ModelNumber;
        CustomerText.Text = _customerName ?? "N/A";
        LocationText.Text = _locationAddress ?? "N/A";

        // Pre-fill form fields with smart defaults
        AssetNameTextBox.Text = $"{_product.Manufacturer} {_product.ModelNumber}";
        
        // Set default warranty dates (10 years is common for HVAC)
        WarrantyStartDatePicker.SelectedDate = _installDate;
        WarrantyExpirationDatePicker.SelectedDate = _installDate.AddYears(10);

        // Default registration date to today if already registered
        RegistrationDatePicker.SelectedDate = DateTime.Today;
    }

    #region Warranty Preset Handlers

    private void OnWarranty1YearClick(object sender, RoutedEventArgs e)
    {
        ApplyWarrantyPreset(1);
    }

    private void OnWarranty5YearsClick(object sender, RoutedEventArgs e)
    {
        ApplyWarrantyPreset(5);
    }

    private void OnWarranty10YearsClick(object sender, RoutedEventArgs e)
    {
        ApplyWarrantyPreset(10);
    }

    private void OnWarranty12YearsClick(object sender, RoutedEventArgs e)
    {
        ApplyWarrantyPreset(12);
    }

    private void ApplyWarrantyPreset(int years)
    {
        var startDate = WarrantyStartDatePicker.SelectedDate ?? _installDate;
        WarrantyStartDatePicker.SelectedDate = startDate;
        WarrantyExpirationDatePicker.SelectedDate = startDate.AddYears(years);
    }

    #endregion

    private void OnRegistrationChanged(object sender, RoutedEventArgs e)
    {
        RegistrationDetailsPanel.Visibility = IsRegisteredOnlineCheckBox.IsChecked == true
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void OnCreateClick(object sender, RoutedEventArgs e)
    {
        if (!Validate())
            return;

        // Create asset with all provided information
        CreatedAsset = new Asset
        {
            // From product
            Serial = _product.SerialNumber ?? "",
            Brand = _product.Manufacturer,
            Model = _product.ModelNumber,
            
            // From user input
            AssetName = AssetNameTextBox.Text.Trim(),
            Location = LocationDetailTextBox.Text.Trim(),
            Description = string.IsNullOrWhiteSpace(DescriptionTextBox.Text) 
                ? null 
                : DescriptionTextBox.Text.Trim(),
            
            // Warranty information
            IsWarrantiedBySEHVAC = IsWarrantiedBySEHVACCheckBox.IsChecked == true,
            WarrantyStartDate = WarrantyStartDatePicker.SelectedDate,
            WarrantyExpiration = WarrantyExpirationDatePicker.SelectedDate,
            
            // Registration tracking
            IsRegisteredOnline = IsRegisteredOnlineCheckBox.IsChecked == true,
            RegistrationDate = IsRegisteredOnlineCheckBox.IsChecked == true 
                ? RegistrationDatePicker.SelectedDate 
                : null,
            RegistrationConfirmation = IsRegisteredOnlineCheckBox.IsChecked == true 
                ? RegistrationConfirmationTextBox.Text?.Trim() 
                : null,
            
            // Installation information
            InstallDate = _installDate,
            
            // Links
            CustomerId = _customerId,
            SiteId = _siteId,
            
            // Metadata
            CreatedAt = DateTime.UtcNow
        };

        DialogResult = true;
        Close();
    }

    private void OnSkipClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private bool Validate()
    {
        if (string.IsNullOrWhiteSpace(AssetNameTextBox.Text))
        {
            MessageBox.Show("Asset Name is required", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            AssetNameTextBox.Focus();
            return false;
        }

        return true;
    }
}
