using Microsoft.EntityFrameworkCore;
using OneManVan.Mobile.Helpers;
using OneManVan.Mobile.Services;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Mobile.Pages;

[QueryProperty(nameof(CustomerId), "customerId")]
[QueryProperty(nameof(SiteId), "siteId")]
public partial class AddAssetPage : ContentPage
{
    private readonly OneManVanDbContext _db;
    private readonly IBarcodeScannerService _scannerService;
    private readonly CustomerSelectionHelper _customerHelper;
    private int _customerId;
    private int _siteId;
    private string? _photoPath;

    public int CustomerId
    {
        get => _customerId;
        set
        {
            _customerId = value;
            if (_customerId > 0)
            {
                LoadCustomerAsync();
            }
        }
    }
    
    public int SiteId
    {
        get => _siteId;
        set
        {
            _siteId = value;
            if (_siteId > 0)
            {
                LoadSiteAsync();
            }
        }
    }

    public AddAssetPage(OneManVanDbContext db, IBarcodeScannerService scannerService, CustomerPickerService customerPicker)
    {
        InitializeComponent();
        _db = db;
        _scannerService = scannerService;
        _customerHelper = new CustomerSelectionHelper(this, customerPicker);
        
        // Set defaults
        InstallDatePicker.Date = DateTime.Today;
        WarrantyYearsPicker.SelectedIndex = 2; // 10 Years default
        ConditionPicker.SelectedIndex = 1; // Good
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _customerHelper.HandleQuickAddCustomerAsync(OnCustomerSelectedAsync);
    }

    private async void LoadCustomerAsync()
    {
        try
        {
            var customer = await _db.Customers
                .Include(c => c.Sites)
                .FirstOrDefaultAsync(c => c.Id == _customerId);
                
            if (customer != null)
            {
                await OnCustomerSelectedAsync(customer);
                
                // Smart suggestion: If customer has exactly one site, suggest using it
                if (customer.Sites.Count == 1)
                {
                    var suggestSite = await DisplayAlertAsync(
                        "Suggestion", 
                        $"This customer has one property at {customer.Sites.First().Address}. Link asset to this site instead of customer?",
                        "Yes, Use Site", 
                        "No, Use Customer");
                        
                    if (suggestSite)
                    {
                        await OnSiteSelectedAsync(customer.Sites.First());
                    }
                }
                else if (customer.Sites.Count > 1)
                {
                    // Warning: Multiple sites exist
                    var chooseNow = await DisplayAlertAsync(
                        "Multiple Properties", 
                        $"This customer has {customer.Sites.Count} properties. Would you like to select a specific site now?",
                        "Select Site", 
                        "Use Customer");
                        
                    if (chooseNow)
                    {
                        await SelectSiteForCustomerAsync(customer);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load customer: {ex.Message}", "OK");
        }
    }
    
    private async Task SelectSiteForCustomerAsync(Customer customer)
    {
        var siteOptions = customer.Sites.Select(s => s.Address).ToArray();
        var selected = await DisplayActionSheetAsync("Select Site", "Cancel", null, siteOptions);
        
        if (selected != "Cancel" && !string.IsNullOrEmpty(selected))
        {
            var site = customer.Sites.FirstOrDefault(s => s.Address == selected);
            if (site != null)
            {
                await OnSiteSelectedAsync(site);
            }
        }
    }
    
    private async void LoadSiteAsync()
    {
        try
        {
            var site = await _db.Sites
                .Include(s => s.Customer)
                .FirstOrDefaultAsync(s => s.Id == _siteId);
                
            if (site != null)
            {
                await OnSiteSelectedAsync(site);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load site: {ex.Message}", "OK");
        }
    }

    private async void OnSelectLocationTapped(object sender, TappedEventArgs e)
    {
        var choice = await DisplayActionSheetAsync("Link Asset To", "Cancel", null, "Site/Property", "Customer", "No Location (Add Later)");
        
        if (choice == "Site/Property")
        {
            await SelectSiteAsync();
        }
        else if (choice == "Customer")
        {
            await _customerHelper.HandleCustomerSelectionAsync(OnCustomerSelectedAsync);
        }
        else if (choice == "No Location (Add Later)")
        {
            ClearLocation();
        }
    }
    
    private async Task SelectSiteAsync()
    {
        // Get all sites
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
        var selected = await DisplayActionSheetAsync("Select Site", "Cancel", null, siteOptions);
        
        if (selected != "Cancel" && !string.IsNullOrEmpty(selected))
        {
            var index = Array.IndexOf(siteOptions, selected);
            if (index >= 0 && index < sites.Count)
            {
                await OnSiteSelectedAsync(sites[index]);
            }
        }
    }
    
    private async Task OnSiteSelectedAsync(Site site)
    {
        _siteId = site.Id;
        _customerId = 0; // Clear customer when site is selected
        LocationNameLabel.Text = $"?? {site.Address}";
        LocationNameLabel.TextColor = Color.FromArgb("#333333");
        
        if (site.Customer != null)
        {
            LocationDetailLabel.Text = $"Owner: {site.Customer.DisplayName}";
            LocationDetailLabel.IsVisible = true;
        }
    }

    private async void OnSelectCustomerTapped(object sender, TappedEventArgs e)
    {
        await _customerHelper.HandleCustomerSelectionAsync(OnCustomerSelectedAsync);
    }

    private async Task OnCustomerSelectedAsync(Customer customer)
    {
        _customerId = customer.Id;
        _siteId = 0; // Clear site when customer is selected
        LocationNameLabel.Text = $"?? {customer.DisplayName}";
        LocationNameLabel.TextColor = Application.Current?.RequestedTheme == AppTheme.Dark 
            ? Colors.White 
            : Color.FromArgb("#333333");
        LocationDetailLabel.IsVisible = false;
    }

    private void ClearLocation()
    {
        _customerId = 0;
        _siteId = 0;
        LocationNameLabel.Text = "?? No location assigned";
        LocationNameLabel.TextColor = Application.Current?.RequestedTheme == AppTheme.Dark 
            ? Color.FromArgb("#B0B0B0") 
            : Color.FromArgb("#757575");
        LocationDetailLabel.Text = "Asset can be assigned to a location later";
        LocationDetailLabel.IsVisible = true;
    }

    private async void OnScanSerialClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await _scannerService.ScanSimpleAsync("Scan Serial Number");
            if (!string.IsNullOrEmpty(result))
            {
                SerialEntry.Text = result;
                try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Scan Error", $"Failed to scan: {ex.Message}", "OK");
        }
    }

    private async void OnTakePhotoClicked(object sender, EventArgs e)
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlertAsync("Permission Denied", "Camera permission is required to take photos.", "OK");
                    return;
                }
            }

            var photo = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
            {
                Title = "Asset Data Plate Photo"
            });

            if (photo != null)
            {
                // Save to app data directory
                var fileName = $"asset_{DateTime.Now:yyyyMMddHHmmss}.jpg";
                var filePath = Path.Combine(FileSystem.AppDataDirectory, "Photos", fileName);

                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                using var sourceStream = await photo.OpenReadAsync();
                using var destStream = File.OpenWrite(filePath);
                await sourceStream.CopyToAsync(destStream);

                _photoPath = filePath;
                PhotoPreview.Source = ImageSource.FromFile(filePath);
                PhotoPreview.IsVisible = true;
                PhotoButton.Text = "Retake Photo";

                try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Could not take photo: {ex.Message}", "OK");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // NOTE: Location (customer OR site) is now OPTIONAL
        // Assets can be created without a location and assigned later

        // Validate required fields
        if (EquipmentTypePicker.SelectedIndex < 0)
        {
            await DisplayAlertAsync("Required", "Please select an equipment type.", "OK");
            return;
        }

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

        try
        {
            var installDate = InstallDatePicker.Date ?? DateTime.Today;
            var warrantyYears = ParseWarrantyYears(WarrantyYearsPicker.SelectedItem?.ToString());
            
            var asset = new Asset
            {
                CustomerId = _customerId > 0 ? _customerId : null,
                SiteId = _siteId > 0 ? _siteId : null,
                EquipmentType = ParseEquipmentType(EquipmentTypePicker.SelectedItem?.ToString()),
                Nickname = NicknameEntry.Text?.Trim(),
                Brand = BrandEntry.Text.Trim(),
                Model = ModelEntry.Text?.Trim(),
                Serial = SerialEntry.Text.Trim(),
                Condition = ParseCondition(ConditionPicker.SelectedItem?.ToString()),
                FuelType = ParseFuelType(FuelTypePicker.SelectedItem?.ToString()),
                RefrigerantType = ParseRefrigerantType(RefrigerantPicker.SelectedItem?.ToString()),
                InstallDate = installDate,
                WarrantyStartDate = installDate,
                WarrantyTermYears = warrantyYears,
                FilterSize = FilterSizeEntry.Text?.Trim(),
                Notes = NotesEditor.Text?.Trim(),
                PhotoPath = _photoPath,
                Status = AssetStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            // Location (store in notes for now, or use a location field if exists)
            var location = LocationPicker.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(location))
            {
                asset.TechnicalNotes = string.IsNullOrEmpty(asset.TechnicalNotes)
                    ? $"Location: {location}"
                    : $"Location: {location}\n{asset.TechnicalNotes}";
            }

            // Parse numeric fields
            if (decimal.TryParse(TonnageEntry.Text, out var tonnage))
                asset.TonnageX10 = (int)(tonnage * 10);

            if (int.TryParse(BtuEntry.Text, out var btu))
                asset.BtuRating = btu;

            if (decimal.TryParse(SeerEntry.Text, out var seer))
                asset.SeerRating = seer;

            if (decimal.TryParse(AfueEntry.Text, out var afue))
                asset.AfueRating = afue;

            // Filter change interval
            if (int.TryParse(FilterMonthsEntry.Text, out var filterMonths) && filterMonths > 0)
            {
                asset.FilterChangeMonths = filterMonths;
                asset.NextFilterDue = DateTime.Today.AddMonths(filterMonths);
            }

            _db.Assets.Add(asset);
            await _db.SaveChangesAsync();

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

            await DisplayAlertAsync("Success", $"Asset '{asset.DisplayName}' has been added.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE") == true)
        {
            await DisplayAlertAsync("Duplicate", "An asset with this serial number already exists.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to save asset: {ex.Message}", "OK");
        }
    }

    private static int ParseWarrantyYears(string? value) => value switch
    {
        "1 Year" => 1,
        "5 Years" => 5,
        "10 Years" => 10,
        "12 Years" => 12,
        "Lifetime" => 99,
        _ => 10
    };

    private static AssetCondition ParseCondition(string? value) => value switch
    {
        "Excellent" => AssetCondition.Excellent,
        "Good" => AssetCondition.Good,
        "Fair" => AssetCondition.Fair,
        "Poor" => AssetCondition.Poor,
        "Failed" => AssetCondition.Failed,
        _ => AssetCondition.Good
    };

    private static EquipmentType ParseEquipmentType(string? value) => value switch
    {
        "Air Conditioner" => EquipmentType.AirConditioner,
        "Heat Pump" => EquipmentType.HeatPump,
        "Furnace" => EquipmentType.GasFurnace,
        "Air Handler" => EquipmentType.AirHandler,
        "Package Unit" => EquipmentType.PackagedUnit,
        "Mini Split" => EquipmentType.MiniSplit,
        "Boiler" => EquipmentType.Boiler,
        "Water Heater" => EquipmentType.Other,
        "Thermostat" => EquipmentType.Thermostat,
        _ => EquipmentType.Other
    };

    private static FuelType ParseFuelType(string? value) => value switch
    {
        "Electric" => FuelType.Electric,
        "Natural Gas" => FuelType.NaturalGas,
        "Propane" => FuelType.Propane,
        "Oil" => FuelType.Oil,
        "Dual Fuel" => FuelType.DualFuel,
        _ => FuelType.Unknown
    };

    private static RefrigerantType ParseRefrigerantType(string? value) => value switch
    {
        "R-410A" => RefrigerantType.R410A,
        "R-22" => RefrigerantType.R22,
        "R-32" => RefrigerantType.R32,
        "R-454B" => RefrigerantType.R454B,
        "None" => RefrigerantType.Unknown,
        _ => RefrigerantType.Unknown
    };
}
