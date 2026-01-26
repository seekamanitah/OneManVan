using Microsoft.EntityFrameworkCore;
using OneManVan.Mobile.Helpers;
using OneManVan.Mobile.Theme;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;
using System.Collections.ObjectModel;

namespace OneManVan.Mobile.Pages;

public partial class AssetListPage : ContentPage
{
    private readonly IDbContextFactory<OneManVanDbContext> _dbFactory;
    private CancellationTokenSource? _cts;
    private List<Asset> _allAssets = [];
    private string _searchText = string.Empty;
    private string _activeFilter = "All";

    public AssetListPage(IDbContextFactory<OneManVanDbContext> dbFactory)
    {
        InitializeComponent();
        _dbFactory = dbFactory;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        
        try
        {
            await LoadAssetsAsync(_cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Page navigated away
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AssetListPage.OnAppearing error: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _cts?.Cancel();
    }

    private async Task LoadAssetsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using (new LoadingScope(LoadingIndicator))
            {
                await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
                
                // PERF: AsNoTracking for read-only list
                _allAssets = await db.Assets
                    .AsNoTracking()
                    .Include(a => a.Customer)
                    .Include(a => a.Site)
                    .OrderBy(a => a.Customer.Name)
                    .ThenBy(a => a.Brand)
                    .ToListAsync(cancellationToken);

                if (!cancellationToken.IsCancellationRequested)
                {
                    ApplyFilters();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                await DisplayAlertAsync("Error", $"Failed to load assets: {ex.Message}", "OK");
            }
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allAssets.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            var search = _searchText.ToLowerInvariant();
            filtered = filtered.Where(a =>
                a.Serial.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (a.Brand?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (a.Model?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (a.Nickname?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                a.Customer.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        filtered = _activeFilter switch
        {
            "Gas" => filtered.Where(a => a.FuelType == FuelType.NaturalGas || a.FuelType == FuelType.Propane),
            "Electric" => filtered.Where(a => a.FuelType == FuelType.Electric),
            "Expiring" => filtered.Where(a => a.IsWarrantyExpiringSoon || a.IsWarrantyExpired),
            "R22" => filtered.Where(a => a.RefrigerantType == RefrigerantType.R22),
            _ => filtered
        };

        var grouped = filtered
            .GroupBy(a => a.Customer)
            .Select(g => new AssetGroup(g.Key.Name, g.ToList()))
            .ToList();

        AssetCollection.ItemsSource = grouped;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = e.NewTextValue ?? string.Empty;
        ApplyFilters();
    }

    private void OnFilterClicked(object sender, EventArgs e)
    {
        // Reset filter styles using AppColors
        AllFilter.BackgroundColor = AppColors.PrimarySurface;
        AllFilter.TextColor = AppColors.Primary;
        GasFilter.BackgroundColor = AppColors.PrimarySurface;
        GasFilter.TextColor = AppColors.Primary;
        ElectricFilter.BackgroundColor = AppColors.PrimarySurface;
        ElectricFilter.TextColor = AppColors.Primary;
        ExpiringFilter.BackgroundColor = AppColors.WarningSurface;
        ExpiringFilter.TextColor = AppColors.Warning;
        R22Filter.BackgroundColor = AppColors.ErrorSurface;
        R22Filter.TextColor = AppColors.Error;

        if (sender is Button button)
        {
            button.BackgroundColor = AppColors.Primary;
            button.TextColor = AppColors.TextOnDark;

            _activeFilter = button.Text switch
            {
                string s when s.Contains("Gas") => "Gas",
                string s when s.Contains("Electric") => "Electric",
                string s when s.Contains("Expiring") => "Expiring",
                string s when s.Contains("R-22") => "R22",
                _ => "All"
            };
        }

        ApplyFilters();
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        await LoadAssetsAsync(_cts.Token);
        RefreshViewControl.IsRefreshing = false;
    }

    private async void OnAssetSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Asset asset)
        {
            AssetCollection.SelectedItem = null;
            await Shell.Current.GoToAsync($"AssetDetail?id={asset.Id}");
        }
    }

    private async void OnDeleteSwipe(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.BindingContext is Asset asset)
        {
            var confirm = await DisplayAlertAsync(
                "Delete Asset",
                $"Are you sure you want to delete {asset.DisplayName} (Serial: {asset.Serial})?",
                "Delete",
                "Cancel");

            if (confirm)
            {
                try
                {
                    await using var db = await _dbFactory.CreateDbContextAsync();
                    var dbAsset = await db.Assets.FindAsync(asset.Id);
                    if (dbAsset != null)
                    {
                        db.Assets.Remove(dbAsset);
                        await db.SaveChangesAsync();
                    }
                    
                    _cts?.Cancel();
                    _cts = new CancellationTokenSource();
                    await LoadAssetsAsync(_cts.Token);
                }
                catch (Exception ex)
                {
                    await DisplayAlertAsync("Error", $"Failed to delete: {ex.Message}", "OK");
                }
            }
        }
    }

    private async void OnScanAssetClicked(object sender, EventArgs e)
    {
        await DisplayAlertAsync("Coming Soon", "Barcode scanning will be available in a future update.", "OK");
    }

    private async void OnAddAssetClicked(object sender, EventArgs e)
    {
        var action = await DisplayActionSheetAsync("Add New Asset", "Cancel", null,
            "Add Manually",
            "Scan Barcode/QR Code");

        switch (action)
        {
            case "Add Manually":
                await NavigateToAddAssetAsync();
                break;
            case "Scan Barcode/QR Code":
                await ScanAssetAsync();
                break;
        }
    }

    private async Task NavigateToAddAssetAsync()
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var customers = await db.Customers.OrderBy(c => c.Name).ToListAsync();
            
            // Allow adding asset without a customer
            if (!customers.Any())
            {
                // No customers exist - offer to add one or proceed without
                var choice = await DisplayAlertAsync(
                    "No Customers", 
                    "You can add an asset without a customer and assign it later. Would you like to add a customer first?", 
                    "Add Customer First", 
                    "Continue Without");
                
                if (choice) // User wants to add customer first
                {
                    await Shell.Current.GoToAsync("AddCustomer");
                    return;
                }
                else // User wants to add asset without customer
                {
                    await Shell.Current.GoToAsync("AddAsset");
                    return;
                }
            }

            // Customers exist - offer to select one or skip
            var options = new List<string> { "No Customer (Add Later)" };
            options.AddRange(customers.Select(c => c.Name));
            
            var selectedCustomer = await DisplayActionSheetAsync("Select Customer (Optional)", "Cancel", null, options.ToArray());
            
            if (string.IsNullOrEmpty(selectedCustomer) || selectedCustomer == "Cancel")
                return;

            if (selectedCustomer == "No Customer (Add Later)")
            {
                // Navigate without customer
                await Shell.Current.GoToAsync("AddAsset");
            }
            else
            {
                // Navigate with selected customer
                var customer = customers.FirstOrDefault(c => c.Name == selectedCustomer);
                if (customer != null)
                {
                    await Shell.Current.GoToAsync($"AddAsset?customerId={customer.Id}");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load customers: {ex.Message}", "OK");
        }
    }

    private async Task ScanAssetAsync()
    {
        await DisplayAlertAsync("Coming Soon", "Barcode scanning will be available in a future update.", "OK");
    }
}

/// <summary>
/// Groups assets by customer for the grouped CollectionView.
/// </summary>
public class AssetGroup : ObservableCollection<Asset>
{
    public string Name { get; }
    public int Count => Items.Count;

    public AssetGroup(string name, IEnumerable<Asset> assets) : base(assets)
    {
        Name = name;
    }
}
