using Microsoft.EntityFrameworkCore;
using OneManVan.Mobile.Extensions;
using OneManVan.Mobile.Helpers;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Mobile.Pages;

[QueryProperty(nameof(AssetId), "id")]
public partial class EditAssetPage : ContentPage
{
    private readonly OneManVanDbContext _db;
    private Asset? _asset;
    private int _assetId;

    public int AssetId
    {
        get => _assetId;
        set
        {
            _assetId = value;
            if (_assetId > 0)
            {
                LoadAssetAsync();
            }
        }
    }

    public EditAssetPage(OneManVanDbContext db)
    {
        InitializeComponent();
        _db = db;
    }

    private async void LoadAssetAsync()
    {
        try
        {
            _db.ChangeTracker.Clear();

            _asset = await _db.Assets
                .Include(a => a.Customer)
                .Include(a => a.Site)
                .FirstOrDefaultAsync(a => a.Id == _assetId);

            if (_asset == null)
            {
                await DisplayAlertAsync("Error", "Asset not found.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            PopulateForm();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load asset: {ex.Message}", "OK");
        }
    }

    private void PopulateForm()
    {
        if (_asset == null) return;

        // Location (Site or Customer)
        if (_asset.SiteId.HasValue && _asset.Site != null)
        {
            CustomerNameLabel.Text = $"?? {_asset.Site.Address}";
        }
        else if (_asset.CustomerId.HasValue && _asset.Customer != null)
        {
            CustomerNameLabel.Text = $"?? {_asset.Customer.DisplayName}";
        }
        else
        {
            CustomerNameLabel.Text = "No location assigned";
        }

        // Basic info
        NicknameEntry.Text = _asset.Nickname;
        BrandEntry.Text = _asset.Brand;
        ModelEntry.Text = _asset.Model;
        SerialEntry.Text = _asset.Serial;

        // Equipment Type
        EquipmentTypePicker.SelectedIndex = _asset.EquipmentType switch
        {
            EquipmentType.AirConditioner => 0,
            EquipmentType.HeatPump => 1,
            EquipmentType.GasFurnace or EquipmentType.ElectricFurnace => 2,
            EquipmentType.AirHandler => 3,
            EquipmentType.PackagedUnit => 4,
            EquipmentType.MiniSplit => 5,
            EquipmentType.Boiler => 6,
            EquipmentType.Thermostat => 8,
            _ => 9
        };

        // Status
        StatusPicker.SelectedIndex = _asset.Status switch
        {
            AssetStatus.Active => 0,
            AssetStatus.Inactive => 1,
            AssetStatus.Replaced => 2,
            AssetStatus.Removed => 3,
            _ => 0
        };

        // Condition
        ConditionPicker.SelectedIndex = _asset.Condition switch
        {
            AssetCondition.Excellent => 0,
            AssetCondition.Good => 1,
            AssetCondition.Fair => 2,
            AssetCondition.Poor => 3,
            AssetCondition.Failed => 4,
            _ => 1
        };

        // Capacity
        if (_asset.TonnageX10 > 0)
            TonnageEntry.Text = (_asset.TonnageX10 / 10.0m).ToString();
        if (_asset.BtuRating > 0)
            BtuEntry.Text = _asset.BtuRating.ToString();

        // Efficiency
        if (_asset.SeerRating > 0)
            SeerEntry.Text = _asset.SeerRating.Value.ToString("F1");
        if (_asset.AfueRating > 0)
            AfueEntry.Text = _asset.AfueRating.Value.ToString("F1");

        // Fuel Type
        FuelTypePicker.SelectedIndex = _asset.FuelType switch
        {
            FuelType.Electric => 0,
            FuelType.NaturalGas => 1,
            FuelType.Propane => 2,
            FuelType.Oil => 3,
            FuelType.DualFuel => 4,
            _ => -1
        };

        // Refrigerant
        RefrigerantPicker.SelectedIndex = _asset.RefrigerantType switch
        {
            RefrigerantType.R410A => 0,
            RefrigerantType.R22 => 1,
            RefrigerantType.R32 => 2,
            RefrigerantType.R454B => 3,
            _ => 4
        };

        // Dates
        if (_asset.InstallDate.HasValue)
            InstallDatePicker.Date = _asset.InstallDate.Value;

        if (_asset.WarrantyTermYears > 0)
            WarrantyYearsEntry.Text = _asset.WarrantyTermYears.ToString();

        // Filter
        FilterSizeEntry.Text = _asset.FilterSize;
        if (_asset.FilterChangeMonths > 0)
            FilterMonthsEntry.Text = _asset.FilterChangeMonths.ToString();

        // Notes
        NotesEditor.Text = _asset.TechnicalNotes;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (_asset == null) return;

        if (string.IsNullOrWhiteSpace(BrandEntry.Text))
        {
            await DisplayAlertAsync("Required", "Please enter a brand.", "OK");
            BrandEntry.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(SerialEntry.Text))
        {
            await DisplayAlertAsync("Required", "Please enter a serial number.", "OK");
            SerialEntry.Focus();
            return;
        }

        await this.SaveWithFeedbackAsync(async () =>
        {
            _asset.Nickname = NicknameEntry.Text?.Trim();
            _asset.Brand = BrandEntry.Text.Trim();
            _asset.Model = ModelEntry.Text?.Trim();
            _asset.Serial = SerialEntry.Text.Trim();

            // Equipment Type
            _asset.EquipmentType = EquipmentTypePicker.SelectedIndex switch
            {
                0 => EquipmentType.AirConditioner,
                1 => EquipmentType.HeatPump,
                2 => EquipmentType.GasFurnace,
                3 => EquipmentType.AirHandler,
                4 => EquipmentType.PackagedUnit,
                5 => EquipmentType.MiniSplit,
                6 => EquipmentType.Boiler,
                8 => EquipmentType.Thermostat,
                _ => EquipmentType.Other
            };

            // Status
            _asset.Status = StatusPicker.SelectedIndex switch
            {
                0 => AssetStatus.Active,
                1 => AssetStatus.Inactive,
                2 => AssetStatus.Replaced,
                3 => AssetStatus.Removed,
                _ => AssetStatus.Active
            };

            // Condition
            _asset.Condition = ConditionPicker.SelectedIndex switch
            {
                0 => AssetCondition.Excellent,
                1 => AssetCondition.Good,
                2 => AssetCondition.Fair,
                3 => AssetCondition.Poor,
                4 => AssetCondition.Failed,
                _ => AssetCondition.Good
            };

            // Capacity
            if (decimal.TryParse(TonnageEntry.Text, out var tonnage))
                _asset.TonnageX10 = (int)(tonnage * 10);
            if (int.TryParse(BtuEntry.Text, out var btu))
                _asset.BtuRating = btu;

            // Efficiency
            if (decimal.TryParse(SeerEntry.Text, out var seer))
                _asset.SeerRating = seer;
            if (decimal.TryParse(AfueEntry.Text, out var afue))
                _asset.AfueRating = afue;

            // Fuel Type
            _asset.FuelType = FuelTypePicker.SelectedIndex switch
            {
                0 => FuelType.Electric,
                1 => FuelType.NaturalGas,
                2 => FuelType.Propane,
                3 => FuelType.Oil,
                4 => FuelType.DualFuel,
                _ => FuelType.Unknown
            };

            // Refrigerant
            _asset.RefrigerantType = RefrigerantPicker.SelectedIndex switch
            {
                0 => RefrigerantType.R410A,
                1 => RefrigerantType.R22,
                2 => RefrigerantType.R32,
                3 => RefrigerantType.R454B,
                _ => RefrigerantType.Unknown
            };

            // Dates
            _asset.InstallDate = InstallDatePicker.Date;
            if (int.TryParse(WarrantyYearsEntry.Text, out var warrantyYears))
            {
                _asset.WarrantyTermYears = warrantyYears;
                _asset.WarrantyStartDate = _asset.InstallDate;
            }

            // Filter
            _asset.FilterSize = FilterSizeEntry.Text?.Trim();
            if (int.TryParse(FilterMonthsEntry.Text, out var filterMonths))
            {
                _asset.FilterChangeMonths = filterMonths;
                _asset.NextFilterDue = DateTime.Today.AddMonths(filterMonths);
            }

            // Notes
            _asset.TechnicalNotes = NotesEditor.Text?.Trim();

            await _db.SaveChangesAsync();
        }, 
        successMessage: "Asset updated.");
    }
    
    private async void OnChangeLocationTapped(object sender, TappedEventArgs e)
    {
        if (_asset == null) return;
        
        var choice = await DisplayActionSheet("Change Asset Location", "Cancel", null, "Site/Property", "Customer", "Clear Location");
        
        if (choice == "Site/Property")
        {
            await ChangeToSiteAsync();
        }
        else if (choice == "Customer")
        {
            await ChangeToCustomerAsync();
        }
        else if (choice == "Clear Location")
        {
            var confirm = await DisplayAlertAsync("Clear Location", "Remove location assignment? This is not recommended.", "Clear", "Cancel");
            if (confirm)
            {
                _asset.CustomerId = null;
                _asset.SiteId = null;
                await _db.SaveChangesAsync();
                PopulateForm();
                await DisplayAlertAsync("Updated", "Location cleared.", "OK");
            }
        }
    }
    
    private async Task ChangeToSiteAsync()
    {
        var sites = await _db.Sites
            .Include(s => s.Customer)
            .OrderBy(s => s.Address)
            .ToListAsync();
            
        if (sites.Count == 0)
        {
            await DisplayAlertAsync("No Sites", "No sites found. Please create a site first.", "OK");
            return;
        }
        
        var siteOptions = sites.Select(s => $"{s.Address} ({s.Customer?.Name ?? "Unknown"})").ToArray();
        var selected = await DisplayActionSheet("Select Site", "Cancel", null, siteOptions);
        
        if (selected != "Cancel" && !string.IsNullOrEmpty(selected))
        {
            var index = Array.IndexOf(siteOptions, selected);
            if (index >= 0 && index < sites.Count)
            {
                _asset!.SiteId = sites[index].Id;
                _asset.CustomerId = null; // Clear customer when site is set
                _asset.Site = sites[index]; // Update navigation property for display
                await _db.SaveChangesAsync();
                PopulateForm();
                await DisplayAlertAsync("Updated", "Asset location changed to site.", "OK");
            }
        }
    }
    
    private async Task ChangeToCustomerAsync()
    {
        var customers = await _db.Customers
            .OrderBy(c => c.Name)
            .ToListAsync();
            
        if (customers.Count == 0)
        {
            await DisplayAlertAsync("No Customers", "No customers found.", "OK");
            return;
        }
        
        var customerOptions = customers.Select(c => c.DisplayName).ToArray();
        var selected = await DisplayActionSheet("Select Customer", "Cancel", null, customerOptions);
        
        if (selected != "Cancel" && !string.IsNullOrEmpty(selected))
        {
            var index = Array.IndexOf(customerOptions, selected);
            if (index >= 0 && index < customers.Count)
            {
                _asset!.CustomerId = customers[index].Id;
                _asset.SiteId = null; // Clear site when customer is set
                _asset.Customer = customers[index]; // Update navigation property for display
                await _db.SaveChangesAsync();
                PopulateForm();
                await DisplayAlertAsync("Updated", "Asset location changed to customer.", "OK");
            }
        }
    }

    private async void OnDecommissionClicked(object sender, EventArgs e)
    {
        if (_asset == null) return;

        var confirm = await DisplayAlertAsync(
            "Decommission Asset",
            $"Mark '{_asset.Brand} {_asset.Model}' as removed/decommissioned?",
            "Decommission", "Cancel");

        if (!confirm) return;

        try
        {
            _asset.Status = AssetStatus.Removed;
            _asset.DecommissionedDate = DateTime.Today;
            await _db.SaveChangesAsync();

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to decommission: {ex.Message}", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
