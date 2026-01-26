using Microsoft.EntityFrameworkCore;
using OneManVan.Mobile.Helpers;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Mobile.Pages;

[QueryProperty(nameof(AssetId), "id")]
[QueryProperty(nameof(CustomerId), "customerId")]
public partial class AssetDetailPage : ContentPage
{
    private readonly OneManVanDbContext _db;
    private int _assetId;
    private int _customerId;

    public Asset Asset { get; set; } = new();
    public bool IsExisting => _assetId > 0;
    public string PageTitle => IsExisting ? "Edit Asset" : "New Asset";

    // Picker sources
    public List<string> FuelTypes { get; } = Enum.GetNames<FuelType>().ToList();
    public List<string> UnitConfigs { get; } = Enum.GetNames<UnitConfig>().ToList();

    // Selected values for pickers
    public string SelectedFuelType { get; set; } = FuelType.Unknown.ToString();
    public string SelectedUnitConfig { get; set; } = UnitConfig.Unknown.ToString();

    // Numeric values
    public double BtuRating { get; set; } = 24000;
    public double SeerRating { get; set; } = 14;

    // Date values
    public DateTime InstallDate { get; set; } = DateTime.Today;
    public DateTime WarrantyStartDate { get; set; } = DateTime.Today;

    // Photo
    public bool HasPhoto => !string.IsNullOrEmpty(Asset.PhotoPath);
    public ImageSource? PhotoSource => HasPhoto ? ImageSource.FromFile(Asset.PhotoPath) : null;

    // Warranty display
    public string WarrantyStatusText => Asset.IsWarrantyExpired ? "Expired" :
        Asset.IsWarrantyExpiringSoon ? "Expiring Soon" : "Active";

    public Color WarrantyStatusColor => Asset.IsWarrantyExpired ? Color.FromArgb("#F44336") :
        Asset.IsWarrantyExpiringSoon ? Color.FromArgb("#FF9800") : Color.FromArgb("#4CAF50");

    public string WarrantyEndDateText => Asset.WarrantyEndDate.ToShortDate("Not set");

    public string DaysRemainingText => Asset.DaysUntilWarrantyExpires?.ToString() ?? "--";

    public double WarrantyProgress
    {
        get
        {
            if (!Asset.WarrantyStartDate.HasValue || !Asset.WarrantyEndDate.HasValue)
                return 0;

            var totalDays = (Asset.WarrantyEndDate.Value - Asset.WarrantyStartDate.Value).TotalDays;
            var elapsed = (DateTime.Today - Asset.WarrantyStartDate.Value).TotalDays;
            return Math.Max(0, Math.Min(1, 1 - (elapsed / totalDays)));
        }
    }

    public string AssetId
    {
        set => _ = int.TryParse(value, out _assetId);
    }

    public string CustomerId
    {
        set => _ = int.TryParse(value, out _customerId);
    }

    public AssetDetailPage(OneManVanDbContext db)
    {
        InitializeComponent();
        _db = db;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAssetAsync();
    }

    private async Task LoadAssetAsync()
    {
        if (_assetId > 0)
        {
            var asset = await _db.Assets
                .Include(a => a.Customer)
                .Include(a => a.Site)
                .FirstOrDefaultAsync(a => a.Id == _assetId);

            if (asset != null)
            {
                Asset = asset;
                _customerId = asset.CustomerId ?? 0;

                // Set picker values
                SelectedFuelType = asset.FuelType.ToString();
                SelectedUnitConfig = asset.UnitConfig.ToString();

                // Set numeric values
                BtuRating = asset.BtuRating ?? 24000;
                SeerRating = (double)(asset.SeerRating ?? 14);

                // Set dates
                InstallDate = asset.InstallDate ?? DateTime.Today;
                WarrantyStartDate = asset.WarrantyStartDate ?? DateTime.Today;
            }
        }
        else
        {
            Asset = new Asset
            {
                CustomerId = _customerId,
                WarrantyTermYears = 10
            };
        }

        RefreshBindings();
    }

    private void RefreshBindings()
    {
        OnPropertyChanged(nameof(Asset));
        OnPropertyChanged(nameof(IsExisting));
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(SelectedFuelType));
        OnPropertyChanged(nameof(SelectedUnitConfig));
        OnPropertyChanged(nameof(BtuRating));
        OnPropertyChanged(nameof(SeerRating));
        OnPropertyChanged(nameof(InstallDate));
        OnPropertyChanged(nameof(WarrantyStartDate));
        OnPropertyChanged(nameof(HasPhoto));
        OnPropertyChanged(nameof(PhotoSource));
        OnPropertyChanged(nameof(WarrantyStatusText));
        OnPropertyChanged(nameof(WarrantyStatusColor));
        OnPropertyChanged(nameof(WarrantyEndDateText));
        OnPropertyChanged(nameof(DaysRemainingText));
        OnPropertyChanged(nameof(WarrantyProgress));
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // Validate
        if (string.IsNullOrWhiteSpace(Asset.Serial))
        {
            await DisplayAlertAsync("Validation Error", "Serial number is required.", "OK");
            SerialEntry.Focus();
            return;
        }

        if (_customerId == 0)
        {
            await DisplayAlertAsync("Error", "No customer selected. Please select a customer first.", "OK");
            return;
        }

        try
        {
            // Apply values from UI
            Asset.CustomerId = _customerId;
            Asset.FuelType = Enum.Parse<FuelType>(SelectedFuelType);
            Asset.UnitConfig = Enum.Parse<UnitConfig>(SelectedUnitConfig);
            Asset.BtuRating = (int)BtuRating;
            Asset.SeerRating = (decimal)SeerRating;
            Asset.InstallDate = InstallDate;
            Asset.WarrantyStartDate = WarrantyStartDate;

            if (_assetId == 0)
            {
                // New asset
                Asset.CreatedAt = DateTime.UtcNow;
                _db.Assets.Add(Asset);
            }
            else
            {
                // Update existing
                _db.Assets.Update(Asset);
            }

            await _db.SaveChangesAsync();
            await Shell.Current.GoToAsync("..");
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE") == true)
        {
            await DisplayAlertAsync("Error", "An asset with this serial number already exists.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to save: {ex.Message}", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnScanSerialClicked(object sender, EventArgs e)
    {
        // TODO: Implement ZXing barcode scanning
        await DisplayAlertAsync("Coming Soon", "Barcode scanning will be available in a future update.", "OK");
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
                Title = "Asset Photo"
            });

            if (photo != null)
            {
                // Save to app data directory
                var fileName = $"asset_{Asset.Serial}_{DateTime.Now:yyyyMMddHHmmss}.jpg";
                var filePath = Path.Combine(FileSystem.AppDataDirectory, "Photos", fileName);

                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                // Copy photo to app storage
                using var sourceStream = await photo.OpenReadAsync();
                using var destStream = File.OpenWrite(filePath);
                await sourceStream.CopyToAsync(destStream);

                Asset.PhotoPath = filePath;
                OnPropertyChanged(nameof(HasPhoto));
                OnPropertyChanged(nameof(PhotoSource));
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Could not take photo: {ex.Message}", "OK");
        }
    }

    private async void OnEditFullDetailsClicked(object sender, EventArgs e)
    {
        if (_assetId > 0)
        {
            await Shell.Current.GoToAsync($"EditAsset?id={_assetId}");
        }
        else
        {
            await DisplayAlertAsync("Save First", "Please save the asset before editing full details.", "OK");
        }
    }
}
