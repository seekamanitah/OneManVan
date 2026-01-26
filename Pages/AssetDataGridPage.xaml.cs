using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;
using OneManVan.Services;

namespace OneManVan.Pages;

/// <summary>
/// DataGrid-based asset management page with warranty highlighting and HVAC field display.
/// </summary>
public partial class AssetDataGridPage : UserControl
{
    private List<Asset> _allAssets = [];
    private ICollectionView? _assetsView;
    private int _currentPage = 1;
    private int _pageSize = 50;
    private int _totalPages = 1;
    private string _searchText = string.Empty;
    private string _warrantyFilter = "All";
    private EquipmentType? _equipmentFilter = null;
    private readonly CsvExportService _exportService = new();

    public AssetDataGridPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await LoadAssetsAsync();
    }

    private async Task LoadAssetsAsync()
    {
        LoadingOverlay.Visibility = Visibility.Visible;

        try
        {
            _allAssets = await App.DbContext.Assets
                .Include(a => a.Customer)
                .Include(a => a.Site)
                .OrderBy(a => a.Customer.Name)
                .ThenBy(a => a.Serial)
                .AsNoTracking()
                .ToListAsync();

            ApplyFiltersAndPagination();
            UpdateStats();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load assets: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
        }
    }

    private void ApplyFiltersAndPagination()
    {
        // Guard against calls during InitializeComponent before controls are created
        if (AssetGrid == null || PageInfo == null) return;

        var filtered = _allAssets.AsEnumerable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            var search = _searchText.ToLowerInvariant();
            filtered = filtered.Where(a =>
                a.Serial.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (a.Brand?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (a.Model?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (a.Nickname?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (a.AssetTag?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                a.Customer.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        // Apply warranty filter
        filtered = _warrantyFilter switch
        {
            "Valid" => filtered.Where(a => a.WarrantyEndDate.HasValue && !a.IsWarrantyExpired && !a.IsWarrantyExpiringSoon),
            "Expiring" => filtered.Where(a => a.IsWarrantyExpiringSoon),
            "Expired" => filtered.Where(a => a.IsWarrantyExpired),
            "R22" => filtered.Where(a => a.RefrigerantType == RefrigerantType.R22),
            _ => filtered
        };

        // Apply equipment type filter
        if (_equipmentFilter.HasValue)
        {
            filtered = filtered.Where(a => a.EquipmentType == _equipmentFilter.Value);
        }

        var filteredList = filtered.ToList();

        // Calculate pagination
        var totalItems = filteredList.Count;
        _totalPages = _pageSize > 0 ? (int)Math.Ceiling(totalItems / (double)_pageSize) : 1;
        _currentPage = Math.Min(_currentPage, Math.Max(1, _totalPages));

        // Apply pagination
        var pagedData = _pageSize > 0
            ? filteredList.Skip((_currentPage - 1) * _pageSize).Take(_pageSize).ToList()
            : filteredList;

        // Update DataGrid
        _assetsView = CollectionViewSource.GetDefaultView(pagedData);
        AssetGrid.ItemsSource = _assetsView;

        // Update count text
        AssetCountText.Text = $"{totalItems:N0} assets found";

        // Update pagination UI
        UpdatePaginationUI(totalItems, filteredList.Count);
    }

    private void UpdatePaginationUI(int totalItems, int filteredCount)
    {
        var start = _pageSize > 0 ? ((_currentPage - 1) * _pageSize) + 1 : 1;
        var end = _pageSize > 0 ? Math.Min(_currentPage * _pageSize, filteredCount) : filteredCount;

        if (filteredCount == 0)
        {
            PageInfo.Text = "No assets to display";
        }
        else
        {
            PageInfo.Text = $"Showing {start:N0}-{end:N0} of {filteredCount:N0} assets";
        }

        CurrentPageText.Text = $"Page {_currentPage} of {_totalPages}";

        // Enable/disable navigation buttons
        FirstPageBtn.IsEnabled = _currentPage > 1;
        PrevPageBtn.IsEnabled = _currentPage > 1;
        NextPageBtn.IsEnabled = _currentPage < _totalPages;
        LastPageBtn.IsEnabled = _currentPage < _totalPages;
    }

    private void UpdateStats()
    {
        TotalCount.Text = _allAssets.Count.ToString("N0");
        ValidCount.Text = _allAssets.Count(a => a.WarrantyEndDate.HasValue && !a.IsWarrantyExpired && !a.IsWarrantyExpiringSoon).ToString("N0");
        ExpiringCount.Text = _allAssets.Count(a => a.IsWarrantyExpiringSoon).ToString("N0");
        ExpiredCount.Text = _allAssets.Count(a => a.IsWarrantyExpired).ToString("N0");
        R22Count.Text = _allAssets.Count(a => a.RefrigerantType == RefrigerantType.R22).ToString("N0");
    }

    #region Event Handlers

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = SearchBox.Text;
        SearchPlaceholder.Visibility = string.IsNullOrEmpty(_searchText) ? Visibility.Visible : Visibility.Collapsed;
        _currentPage = 1;
        ApplyFiltersAndPagination();
    }

    private void OnWarrantyFilterClick(object sender, RoutedEventArgs e)
    {
        // Reset all filter buttons
        FilterAll.IsChecked = false;
        FilterValid.IsChecked = false;
        FilterExpiring.IsChecked = false;
        FilterExpired.IsChecked = false;
        FilterR22.IsChecked = false;

        // Set the clicked filter
        if (sender is ToggleButton btn)
        {
            btn.IsChecked = true;

            _warrantyFilter = btn.Name switch
            {
                "FilterValid" => "Valid",
                "FilterExpiring" => "Expiring",
                "FilterExpired" => "Expired",
                "FilterR22" => "R22",
                _ => "All"
            };
        }

        _currentPage = 1;
        ApplyFiltersAndPagination();
    }

    private void OnEquipmentFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        if (EquipmentFilter.SelectedItem is ComboBoxItem item)
        {
            var content = item.Content?.ToString() ?? "";
            _equipmentFilter = content switch
            {
                string s when s.Contains("Furnace") => EquipmentType.GasFurnace,
                string s when s.Contains("Air Conditioner") => EquipmentType.AirConditioner,
                string s when s.Contains("Heat Pump") => EquipmentType.HeatPump,
                string s when s.Contains("Mini Split") => EquipmentType.MiniSplit,
                string s when s.Contains("Thermostat") => EquipmentType.Thermostat,
                _ => null
            };
        }

        _currentPage = 1;
        ApplyFiltersAndPagination();
    }

    private void OnDataGridSorting(object sender, DataGridSortingEventArgs e)
    {
        // Let default sorting handle it
    }

    private void OnRowDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (AssetGrid.SelectedItem is Asset asset)
        {
            NavigateToAssetDetail(asset);
        }
    }

    private void OnAddAssetClick(object sender, RoutedEventArgs e)
    {
        var formContent = new Controls.AssetFormContent();
        
        _ = DrawerService.Instance.OpenDrawerAsync(
            title: "Add Asset",
            content: formContent,
            saveButtonText: "Save Asset",
            onSave: async () =>
            {
                if (!formContent.Validate())
                {
                    MessageBox.Show("Serial number is required", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    var asset = formContent.GetAsset();
                    App.DbContext.Assets.Add(asset);
                    await App.DbContext.SaveChangesAsync();
                    
                    await DrawerService.Instance.CompleteDrawerAsync();
                    await LoadAssetsAsync();
                    
                    ToastService.Success($"Asset '{asset.Serial}' added successfully!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save asset: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        );
    }
    
    private void OnAssetSelected(object sender, SelectionChangedEventArgs e)
    {
        if (AssetGrid.SelectedItem is Asset asset)
        {
            // Open drawer for editing
            var formContent = new Controls.AssetFormContent();
            formContent.LoadAsset(asset);
            
            _ = DrawerService.Instance.OpenDrawerAsync(
                title: "Edit Asset",
                content: formContent,
                saveButtonText: "Save Changes",
                onSave: async () =>
                {
                    if (!formContent.Validate())
                    {
                        MessageBox.Show("Serial number is required", "Validation Error",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    try
                    {
                        var updated = formContent.GetAsset();
                        asset.Serial = updated.Serial;
                        asset.Brand = updated.Brand;
                        asset.Model = updated.Model;
                        asset.Nickname = updated.Nickname;
                        asset.EquipmentType = updated.EquipmentType;
                        asset.Notes = updated.Notes;
                        
                        await App.DbContext.SaveChangesAsync();
                        await DrawerService.Instance.CompleteDrawerAsync();
                        await LoadAssetsAsync();
                        
                        ToastService.Success($"Asset '{asset.Serial}' updated!");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to update asset: {ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            );
        }
    }

    private async void OnExportClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                FileName = $"Assets_{DateTime.Now:yyyyMMdd}"
            };

            if (dialog.ShowDialog() == true)
            {
                var data = _allAssets.Select(a => new
                {
                    a.Serial,
                    a.AssetTag,
                    a.Brand,
                    a.Model,
                    a.Nickname,
                    Customer = a.Customer.Name,
                    Site = a.Site?.Address,
                    EquipmentType = a.EquipmentTypeDisplay,
                    a.BtuRating,
                    a.SeerRating,
                    Refrigerant = a.RefrigerantTypeDisplay,
                    InstallDate = a.InstallDate?.ToString("yyyy-MM-dd"),
                    WarrantyEnd = a.WarrantyEndDate?.ToString("yyyy-MM-dd"),
                    WarrantyStatus = GetWarrantyStatus(a),
                    a.FilterSize,
                    LastService = a.LastServiceDate?.ToString("yyyy-MM-dd")
                }).ToList();

                await _exportService.ExportToCsvAsync(data, dialog.FileName);
                MessageBox.Show($"Exported {data.Count} assets to {dialog.FileName}", "Export Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnViewClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Asset asset)
        {
            NavigateToAssetDetail(asset);
        }
    }

    private void OnEditClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Asset asset)
        {
            // Open edit dialog with the asset
            var dialog = new Dialogs.AddEditAssetDialog(asset);
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true && dialog.SavedAsset != null)
            {
                _ = LoadAssetsAsync();
            }
        }
    }

    private async void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Asset asset)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to delete '{asset.Serial}'?\n\n{asset.Brand} {asset.Model}",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var dbAsset = await App.DbContext.Assets.FindAsync(asset.Id);
                    if (dbAsset != null)
                    {
                        App.DbContext.Assets.Remove(dbAsset);
                        await App.DbContext.SaveChangesAsync();
                        await LoadAssetsAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to delete asset: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    #endregion

    #region Pagination

    private void OnPageSizeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PageSizeCombo.SelectedItem is ComboBoxItem item)
        {
            var value = item.Content?.ToString();
            _pageSize = value == "All" ? 0 : int.Parse(value ?? "50");
            _currentPage = 1;
            ApplyFiltersAndPagination();
        }
    }

    private void OnFirstPageClick(object sender, RoutedEventArgs e)
    {
        _currentPage = 1;
        ApplyFiltersAndPagination();
    }

    private void OnPrevPageClick(object sender, RoutedEventArgs e)
    {
        if (_currentPage > 1)
        {
            _currentPage--;
            ApplyFiltersAndPagination();
        }
    }

    private void OnNextPageClick(object sender, RoutedEventArgs e)
    {
        if (_currentPage < _totalPages)
        {
            _currentPage++;
            ApplyFiltersAndPagination();
        }
    }

    private void OnLastPageClick(object sender, RoutedEventArgs e)
    {
        _currentPage = _totalPages;
        ApplyFiltersAndPagination();
    }


    #endregion

    private void NavigateToAssetDetail(Asset asset)
    {
        // Use navigation request service to navigate
        NavigationRequest.Navigate("Assets", $"View asset: {asset.Serial}");
    }

    private static string GetWarrantyStatus(Asset asset)
    {
        if (!asset.WarrantyEndDate.HasValue)
            return "No Warranty";
        
        if (asset.IsWarrantyExpired)
            return "Expired";
        
        if (asset.IsWarrantyExpiringSoon)
            return "Expiring Soon";
        
        return "Active";
    }
}
