using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Controls;

public partial class AssetFormContent : UserControl
{
    public int? CustomerId { get; set; }
    public int? SiteId { get; set; }

    public AssetFormContent()
    {
        InitializeComponent();
        
        // Populate Equipment Type combo
        EquipmentTypeCombo.ItemsSource = Enum.GetValues(typeof(EquipmentType))
            .Cast<EquipmentType>()
            .Select(e => new ComboBoxItem { Content = e.ToString(), Tag = e })
            .ToList();
        EquipmentTypeCombo.SelectedIndex = 0;
        
        // Load customers
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var dbContext = App.DbContext;
            var customers = await dbContext.Customers
                .Where(c => c.Status == CustomerStatus.Active)
                .OrderBy(c => c.Name)
                .ToListAsync();
            
            CustomerComboBox.ItemsSource = customers;
        }
        catch (Exception ex)
        {
            // Log error but don't crash
            System.Diagnostics.Debug.WriteLine($"Failed to load customers: {ex.Message}");
        }
    }

    private void OnClearCustomerClick(object sender, RoutedEventArgs e)
    {
        CustomerComboBox.SelectedItem = null;
        CustomerId = null;
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
        var today = DateTime.Today;
        var expirationDate = today.AddYears(years);
        
        WarrantyExpirationDatePicker.SelectedDate = expirationDate;
        
        // Optionally check the SEHVAC warranty checkbox
        // IsWarrantiedBySEHVACCheckBox.IsChecked = true;
    }

    #endregion

    public Asset GetAsset()
    {
        var selectedEquipmentType = (EquipmentTypeCombo.SelectedItem as ComboBoxItem)?.Tag as EquipmentType? ?? EquipmentType.Unknown;
        
        // Get customer from ComboBox if selected
        var selectedCustomerId = CustomerComboBox.SelectedValue as int?;
        
        return new Asset
        {
            AssetName = AssetNameTextBox.Text,
            Description = DescriptionTextBox.Text,
            Serial = SerialTextBox.Text,
            Brand = BrandTextBox.Text,
            Model = ModelTextBox.Text,
            Nickname = NicknameTextBox.Text,
            Location = LocationTextBox.Text,
            EquipmentType = selectedEquipmentType,
            IsWarrantiedBySEHVAC = IsWarrantiedBySEHVACCheckBox.IsChecked == true,
            WarrantyExpiration = WarrantyExpirationDatePicker.SelectedDate,
            Notes = NotesTextBox.Text,
            CustomerId = selectedCustomerId ?? CustomerId,  // Use ComboBox selection or passed-in value
            SiteId = SiteId
        };
    }

    public void LoadAsset(Asset asset)
    {
        AssetNameTextBox.Text = asset.AssetName;
        DescriptionTextBox.Text = asset.Description;
        SerialTextBox.Text = asset.Serial;
        BrandTextBox.Text = asset.Brand;
        ModelTextBox.Text = asset.Model;
        NicknameTextBox.Text = asset.Nickname;
        LocationTextBox.Text = asset.Location;
        IsWarrantiedBySEHVACCheckBox.IsChecked = asset.IsWarrantiedBySEHVAC;
        
        // Load warranty expiration date
        if (asset.WarrantyExpiration.HasValue)
        {
            WarrantyExpirationDatePicker.SelectedDate = asset.WarrantyExpiration.Value;
        }
        
        NotesTextBox.Text = asset.Notes;
        CustomerId = asset.CustomerId;
        SiteId = asset.SiteId;
        
        // Select equipment type
        var item = EquipmentTypeCombo.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(i => (EquipmentType)i.Tag == asset.EquipmentType);
        if (item != null)
        {
            EquipmentTypeCombo.SelectedItem = item;
        }
    }

    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(SerialTextBox.Text))
        {
            return false;
        }
        return true;
    }
}
