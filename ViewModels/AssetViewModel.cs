using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;
using OneManVan.Services;

namespace OneManVan.ViewModels;

/// <summary>
/// ViewModel for asset list and management with HVAC-specific fields.
/// </summary>
public class AssetViewModel : BaseViewModel
{
    private readonly SchemaEditorService _schemaService = new();
    
    private ObservableCollection<AssetTreeNode> _assetTree = [];
    private Asset? _selectedAsset;
    private string _searchText = string.Empty;
    private bool _isLoading;
    private bool _isEditing;

    // Edit fields
    private string _editSerial = string.Empty;
    private string _editBrand = string.Empty;
    private string _editModel = string.Empty;
    private FuelType _editFuelType = FuelType.Unknown;
    private UnitConfig _editUnitConfig = UnitConfig.Unknown;
    private int? _editBtuRating;
    private decimal? _editSeerRating;
    private DateTime? _editInstallDate;
    private DateTime? _editWarrantyStartDate;
    private int _editWarrantyTermYears = 10;
    private string _editNotes = string.Empty;
    private int? _selectedCustomerId;

    public ObservableCollection<AssetTreeNode> AssetTree
    {
        get => _assetTree;
        set => SetProperty(ref _assetTree, value);
    }

    public Asset? SelectedAsset
    {
        get => _selectedAsset;
        set
        {
            if (SetProperty(ref _selectedAsset, value))
            {
                OnPropertyChanged(nameof(HasSelectedAsset));
                OnPropertyChanged(nameof(WarrantyStatusText));
                OnPropertyChanged(nameof(WarrantyStatusColor));
            }
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                _ = LoadAssetsAsync();
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    public bool HasSelectedAsset => SelectedAsset != null;

    // Warranty status display
    public string WarrantyStatusText
    {
        get
        {
            if (SelectedAsset?.WarrantyEndDate == null) return "No warranty info";
            var days = SelectedAsset.DaysUntilWarrantyExpires;
            if (days == null) return "No warranty info";
            if (days < 0) return $"Expired {Math.Abs(days.Value)} days ago";
            if (days < 90) return $"Expires in {days} days";
            if (days < 365) return $"Expires in {days / 30} months";
            return $"Expires in {days / 365} years";
        }
    }

    public string WarrantyStatusColor
    {
        get
        {
            if (SelectedAsset?.DaysUntilWarrantyExpires == null) return "#a6adc8";
            var days = SelectedAsset.DaysUntilWarrantyExpires.Value;
            if (days < 0) return "#f38ba8"; // Red - expired
            if (days < 90) return "#f9e2af"; // Yellow - expiring soon
            return "#a6e3a1"; // Green - valid
        }
    }

    // Edit properties
    public string EditSerial
    {
        get => _editSerial;
        set => SetProperty(ref _editSerial, value);
    }

    public string EditBrand
    {
        get => _editBrand;
        set => SetProperty(ref _editBrand, value);
    }

    public string EditModel
    {
        get => _editModel;
        set => SetProperty(ref _editModel, value);
    }

    public FuelType EditFuelType
    {
        get => _editFuelType;
        set => SetProperty(ref _editFuelType, value);
    }

    public UnitConfig EditUnitConfig
    {
        get => _editUnitConfig;
        set => SetProperty(ref _editUnitConfig, value);
    }

    public int? EditBtuRating
    {
        get => _editBtuRating;
        set => SetProperty(ref _editBtuRating, value);
    }

    public decimal? EditSeerRating
    {
        get => _editSeerRating;
        set => SetProperty(ref _editSeerRating, value);
    }

    public DateTime? EditInstallDate
    {
        get => _editInstallDate;
        set => SetProperty(ref _editInstallDate, value);
    }

    public DateTime? EditWarrantyStartDate
    {
        get => _editWarrantyStartDate;
        set => SetProperty(ref _editWarrantyStartDate, value);
    }

    public int EditWarrantyTermYears
    {
        get => _editWarrantyTermYears;
        set => SetProperty(ref _editWarrantyTermYears, value);
    }

    public string EditNotes
    {
        get => _editNotes;
        set => SetProperty(ref _editNotes, value);
    }

    public int? SelectedCustomerId
    {
        get => _selectedCustomerId;
        set => SetProperty(ref _selectedCustomerId, value);
    }

    // Enum values for ComboBoxes
    public Array FuelTypes => Enum.GetValues(typeof(FuelType));
    public Array UnitConfigs => Enum.GetValues(typeof(UnitConfig));

    // Customer list for assignment
    public ObservableCollection<Customer> Customers { get; } = [];
    
    // Custom fields
    public ObservableCollection<SchemaDefinition> CustomFieldDefinitions { get; } = [];
    public ObservableCollection<CustomFieldValue> CustomFieldValues { get; } = [];
    public bool HasCustomFields => CustomFieldDefinitions.Count > 0;

    // Commands
    public ICommand LoadAssetsCommand { get; }
    public ICommand AddAssetCommand { get; }
    public ICommand EditAssetCommand { get; }
    public ICommand SaveAssetCommand { get; }
    public ICommand CancelEditCommand { get; }
    public ICommand DeleteAssetCommand { get; }
    public ICommand SelectAssetCommand { get; }

    public AssetViewModel()
    {
        LoadAssetsCommand = new AsyncRelayCommand(LoadAssetsAsync);
        AddAssetCommand = new RelayCommand(StartAddAsset);
        EditAssetCommand = new RelayCommand(StartEditAsset, () => HasSelectedAsset);
        SaveAssetCommand = new AsyncRelayCommand(SaveAssetAsync);
        CancelEditCommand = new RelayCommand(CancelEdit);
        DeleteAssetCommand = new AsyncRelayCommand(DeleteAssetAsync, () => HasSelectedAsset);
        SelectAssetCommand = new RelayCommand<Asset>(SelectAsset);

        _ = LoadAssetsAsync();
        _ = LoadCustomersAsync();
        _ = LoadCustomFieldDefinitionsAsync();
    }
    
    private async Task LoadCustomFieldDefinitionsAsync()
    {
        try
        {
            var definitions = await _schemaService.GetSchemaDefinitionsAsync("Asset");
            CustomFieldDefinitions.Clear();
            foreach (var def in definitions)
            {
                CustomFieldDefinitions.Add(def);
            }
            OnPropertyChanged(nameof(HasCustomFields));
        }
        catch
        {
            // Silently fail - custom fields are optional
        }
    }

    private async Task LoadCustomersAsync()
    {
        var customers = await App.DbContext.Customers
            .OrderBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync();

        Customers.Clear();
        foreach (var customer in customers)
        {
            Customers.Add(customer);
        }
    }

    private async Task LoadAssetsAsync()
    {
        IsLoading = true;

        try
        {
            var query = App.DbContext.Assets
                .Include(a => a.Customer)
                .Include(a => a.Site)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                query = query.Where(a =>
                    a.Serial.ToLower().Contains(searchLower) ||
                    (a.Brand != null && a.Brand.ToLower().Contains(searchLower)) ||
                    (a.Model != null && a.Model.ToLower().Contains(searchLower)) ||
                    a.Customer.Name.ToLower().Contains(searchLower));
            }

            var assets = await query.OrderBy(a => a.Customer.Name).ThenBy(a => a.Serial).ToListAsync();

            // Build tree structure: Customer > Site > Assets
            var tree = new ObservableCollection<AssetTreeNode>();
            var customerGroups = assets.GroupBy(a => a.Customer);

            foreach (var customerGroup in customerGroups)
            {
                var customerNode = new AssetTreeNode
                {
                    Name = customerGroup.Key.Name,
                    NodeType = "Customer",
                    IsExpanded = true
                };

                var siteGroups = customerGroup.GroupBy(a => a.Site?.Address ?? "No Site");
                foreach (var siteGroup in siteGroups)
                {
                    var siteNode = new AssetTreeNode
                    {
                        Name = siteGroup.Key,
                        NodeType = "Site",
                        IsExpanded = true
                    };

                    foreach (var asset in siteGroup)
                    {
                        siteNode.Children.Add(new AssetTreeNode
                        {
                            Name = $"{asset.Serial} - {asset.Brand} {asset.Model}",
                            NodeType = "Asset",
                            Asset = asset,
                            WarrantyStatus = GetWarrantyStatus(asset)
                        });
                    }

                    customerNode.Children.Add(siteNode);
                }

                tree.Add(customerNode);
            }

            AssetTree = tree;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load assets: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static string GetWarrantyStatus(Asset asset)
    {
        if (asset.WarrantyEndDate == null) return "Unknown";
        var days = asset.DaysUntilWarrantyExpires;
        if (days == null) return "Unknown";
        if (days < 0) return "Expired";
        if (days < 90) return "Expiring";
        return "Valid";
    }

    private void SelectAsset(Asset? asset)
    {
        SelectedAsset = asset;
    }

    private void StartAddAsset()
    {
        SelectedAsset = null;
        EditSerial = string.Empty;
        EditBrand = string.Empty;
        EditModel = string.Empty;
        EditFuelType = FuelType.Unknown;
        EditUnitConfig = UnitConfig.Unknown;
        EditBtuRating = null;
        EditSeerRating = null;
        EditInstallDate = DateTime.Today;
        EditWarrantyStartDate = DateTime.Today;
        EditWarrantyTermYears = 10;
        EditNotes = string.Empty;
        SelectedCustomerId = Customers.FirstOrDefault()?.Id;
        
        // Initialize custom field values
        CustomFieldValues.Clear();
        foreach (var def in CustomFieldDefinitions)
        {
            CustomFieldValues.Add(new CustomFieldValue
            {
                FieldName = def.FieldName,
                DisplayLabel = def.DisplayLabel,
                FieldType = def.FieldType,
                Value = def.DefaultValue ?? string.Empty,
                IsRequired = def.IsRequired,
                EnumOptions = def.EnumOptionsList,
                Placeholder = def.Placeholder
            });
        }
        
        IsEditing = true;
    }

    private async void StartEditAsset()
    {
        if (SelectedAsset == null) return;

        EditSerial = SelectedAsset.Serial;
        EditBrand = SelectedAsset.Brand ?? string.Empty;
        EditModel = SelectedAsset.Model ?? string.Empty;
        EditFuelType = SelectedAsset.FuelType;
        EditUnitConfig = SelectedAsset.UnitConfig;
        EditBtuRating = SelectedAsset.BtuRating;
        EditSeerRating = SelectedAsset.SeerRating;
        EditInstallDate = SelectedAsset.InstallDate;
        EditWarrantyStartDate = SelectedAsset.WarrantyStartDate;
        EditWarrantyTermYears = SelectedAsset.WarrantyTermYears;
        EditNotes = SelectedAsset.Notes ?? string.Empty;
        SelectedCustomerId = SelectedAsset.CustomerId;
        
        // Load custom field values
        CustomFieldValues.Clear();
        var existingValues = await _schemaService.GetCustomFieldValuesAsync("Asset", SelectedAsset.Id);
        foreach (var def in CustomFieldDefinitions)
        {
            var value = existingValues.TryGetValue(def.FieldName, out var v) ? v : def.DefaultValue;
            CustomFieldValues.Add(new CustomFieldValue
            {
                FieldName = def.FieldName,
                DisplayLabel = def.DisplayLabel,
                FieldType = def.FieldType,
                Value = value ?? string.Empty,
                IsRequired = def.IsRequired,
                EnumOptions = def.EnumOptionsList,
                Placeholder = def.Placeholder
            });
        }
        
        IsEditing = true;
    }

    private async Task SaveAssetAsync()
    {
        if (string.IsNullOrWhiteSpace(EditSerial))
        {
            MessageBox.Show("Serial number is required.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (SelectedCustomerId == null)
        {
            MessageBox.Show("Please select a customer.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        // Validate required custom fields
        foreach (var cf in CustomFieldValues.Where(cf => cf.IsRequired))
        {
            if (string.IsNullOrWhiteSpace(cf.Value))
            {
                MessageBox.Show($"'{cf.DisplayLabel}' is required.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        // Validate BTU rating
        if (EditBtuRating.HasValue && EditBtuRating < 12000)
        {
            MessageBox.Show("BTU rating should be at least 12,000 for most HVAC units.", "Validation Warning",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        IsLoading = true;

        try
        {
            int assetId;
            
            if (SelectedAsset == null)
            {
                // Check for duplicate serial
                var serialExists = await App.DbContext.Assets
                    .AnyAsync(a => a.Serial == EditSerial.Trim());

                if (serialExists)
                {
                    MessageBox.Show("An asset with this serial number already exists.", "Duplicate Serial",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    IsLoading = false;
                    return;
                }

                // Add new asset
                var asset = new Asset
                {
                    CustomerId = SelectedCustomerId.Value,
                    Serial = EditSerial.Trim(),
                    Brand = string.IsNullOrWhiteSpace(EditBrand) ? null : EditBrand.Trim(),
                    Model = string.IsNullOrWhiteSpace(EditModel) ? null : EditModel.Trim(),
                    FuelType = EditFuelType,
                    UnitConfig = EditUnitConfig,
                    BtuRating = EditBtuRating,
                    SeerRating = EditSeerRating,
                    InstallDate = EditInstallDate,
                    WarrantyStartDate = EditWarrantyStartDate,
                    WarrantyTermYears = EditWarrantyTermYears,
                    Notes = string.IsNullOrWhiteSpace(EditNotes) ? null : EditNotes.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                App.DbContext.Assets.Add(asset);
                await App.DbContext.SaveChangesAsync();
                assetId = asset.Id;

                MessageBox.Show($"Asset '{asset.Serial}' added successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // Update existing asset
                var asset = await App.DbContext.Assets.FindAsync(SelectedAsset.Id);
                if (asset != null)
                {
                    asset.Serial = EditSerial.Trim();
                    asset.Brand = string.IsNullOrWhiteSpace(EditBrand) ? null : EditBrand.Trim();
                    asset.Model = string.IsNullOrWhiteSpace(EditModel) ? null : EditModel.Trim();
                    asset.FuelType = EditFuelType;
                    asset.UnitConfig = EditUnitConfig;
                    asset.BtuRating = EditBtuRating;
                    asset.SeerRating = EditSeerRating;
                    asset.InstallDate = EditInstallDate;
                    asset.WarrantyStartDate = EditWarrantyStartDate;
                    asset.WarrantyTermYears = EditWarrantyTermYears;
                    asset.Notes = string.IsNullOrWhiteSpace(EditNotes) ? null : EditNotes.Trim();

                    await App.DbContext.SaveChangesAsync();
                    assetId = asset.Id;

                    MessageBox.Show($"Asset '{asset.Serial}' updated successfully.", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    return;
                }
            }
            
            // Save custom field values
            foreach (var cf in CustomFieldValues)
            {
                await _schemaService.SetCustomFieldValueAsync(
                    "Asset", 
                    assetId, 
                    cf.FieldName, 
                    string.IsNullOrWhiteSpace(cf.Value) ? null : cf.Value,
                    cf.FieldType);
            }

            IsEditing = false;
            await LoadAssetsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save asset: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void CancelEdit()
    {
        IsEditing = false;
    }

    private async Task DeleteAssetAsync()
    {
        if (SelectedAsset == null) return;

        var result = MessageBox.Show(
            $"Are you sure you want to delete asset '{SelectedAsset.Serial}'?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        IsLoading = true;

        try
        {
            var asset = await App.DbContext.Assets.FindAsync(SelectedAsset.Id);
            if (asset != null)
            {
                App.DbContext.Assets.Remove(asset);
                await App.DbContext.SaveChangesAsync();

                MessageBox.Show($"Asset '{asset.Serial}' deleted.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                SelectedAsset = null;
                await LoadAssetsAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to delete asset: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }
}

/// <summary>
/// Tree node for hierarchical asset display.
/// </summary>
public class AssetTreeNode : BaseViewModel
{
    private bool _isExpanded = true;
    private bool _isSelected;

    public string Name { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;
    public Asset? Asset { get; set; }
    public string WarrantyStatus { get; set; } = string.Empty;
    public ObservableCollection<AssetTreeNode> Children { get; } = [];

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
