using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;
using OneManVan.Services;

namespace OneManVan.ViewModels;

/// <summary>
/// ViewModel for customer list and management with integrated asset view.
/// </summary>
public class CustomerViewModel : BaseViewModel
{
    private readonly SchemaEditorService _schemaService = new();
    
    private ObservableCollection<Customer> _customers = [];
    private Customer? _selectedCustomer;
    private string _searchText = string.Empty;
    private bool _isLoading;
    private bool _isEditing;
    private Asset? _selectedAsset;
    private bool _isViewingAsset;

    // Edit fields
    private string _editName = string.Empty;
    private string _editEmail = string.Empty;
    private string _editPhone = string.Empty;
    private string _editNotes = string.Empty;

    public ObservableCollection<Customer> Customers
    {
        get => _customers;
        set => SetProperty(ref _customers, value);
    }

    public Customer? SelectedCustomer
    {
        get => _selectedCustomer;
        set
        {
            if (SetProperty(ref _selectedCustomer, value))
            {
                LoadCustomerDetails();
                OnPropertyChanged(nameof(HasSelectedCustomer));
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
                _ = SearchCustomersAsync();
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

    public bool HasSelectedCustomer => SelectedCustomer != null;

    // Edit properties
    public string EditName
    {
        get => _editName;
        set => SetProperty(ref _editName, value);
    }

    public string EditEmail
    {
        get => _editEmail;
        set => SetProperty(ref _editEmail, value);
    }

    public string EditPhone
    {
        get => _editPhone;
        set => SetProperty(ref _editPhone, value);
    }

    public string EditNotes
    {
        get => _editNotes;
        set => SetProperty(ref _editNotes, value);
    }

    // Site management
    public ObservableCollection<Site> CustomerSites { get; } = [];
    public ObservableCollection<Asset> CustomerAssets { get; } = [];
    
    // Site with assets hierarchy
    public ObservableCollection<SiteWithAssets> SitesWithAssets { get; } = [];
    
    // Selected asset for inline viewing
    public Asset? SelectedAsset
    {
        get => _selectedAsset;
        set
        {
            if (SetProperty(ref _selectedAsset, value))
            {
                OnPropertyChanged(nameof(HasSelectedAsset));
            }
        }
    }
    
    public bool HasSelectedAsset => SelectedAsset != null;
    
    public bool IsViewingAsset
    {
        get => _isViewingAsset;
        set => SetProperty(ref _isViewingAsset, value);
    }
    
    // Stats for selected customer
    public int TotalSites => CustomerSites.Count;
    public int TotalAssets => CustomerAssets.Count;
    public int ExpiringWarranties => CustomerAssets.Count(a => a.WarrantyEndDate.HasValue && 
        a.WarrantyEndDate.Value > DateTime.Today && 
        a.WarrantyEndDate.Value <= DateTime.Today.AddDays(90));
    public int ExpiredWarranties => CustomerAssets.Count(a => a.WarrantyEndDate.HasValue && 
        a.WarrantyEndDate.Value < DateTime.Today);
    
    // Custom fields
    public ObservableCollection<SchemaDefinition> CustomFieldDefinitions { get; } = [];
    public ObservableCollection<CustomFieldValue> CustomFieldValues { get; } = [];
    
    public bool HasCustomFields => CustomFieldDefinitions.Count > 0;

    // Commands
    public ICommand LoadCustomersCommand { get; }
    public ICommand AddCustomerCommand { get; }
    public ICommand EditCustomerCommand { get; }
    public ICommand SaveCustomerCommand { get; }
    public ICommand CancelEditCommand { get; }
    public ICommand DeleteCustomerCommand { get; }
    public ICommand AddSiteCommand { get; }
    public ICommand DeleteSiteCommand { get; }
    public ICommand ViewAssetCommand { get; }
    public ICommand CloseAssetViewCommand { get; }
    public ICommand AddAssetToSiteCommand { get; }
    public ICommand CallCustomerCommand { get; }
    public ICommand EmailCustomerCommand { get; }

    public CustomerViewModel()
    {
        LoadCustomersCommand = new AsyncRelayCommand(LoadCustomersAsync);
        AddCustomerCommand = new RelayCommand(StartAddCustomer);
        EditCustomerCommand = new RelayCommand(StartEditCustomer, () => HasSelectedCustomer);
        SaveCustomerCommand = new AsyncRelayCommand(SaveCustomerAsync);
        CancelEditCommand = new RelayCommand(CancelEdit);
        DeleteCustomerCommand = new AsyncRelayCommand(DeleteCustomerAsync, () => HasSelectedCustomer);
        AddSiteCommand = new AsyncRelayCommand(AddSiteAsync, () => HasSelectedCustomer);
        DeleteSiteCommand = new RelayCommand<Site>(DeleteSite);
        ViewAssetCommand = new RelayCommand<Asset>(ViewAsset);
        CloseAssetViewCommand = new RelayCommand(CloseAssetView);
        AddAssetToSiteCommand = new RelayCommand<Site>(AddAssetToSite);
        CallCustomerCommand = new RelayCommand(CallCustomer, () => HasSelectedCustomer && !string.IsNullOrEmpty(SelectedCustomer?.Phone));
        EmailCustomerCommand = new RelayCommand(EmailCustomer, () => HasSelectedCustomer && !string.IsNullOrEmpty(SelectedCustomer?.Email));

        _ = LoadCustomersAsync();
        _ = LoadCustomFieldDefinitionsAsync();
    }
    
    private async Task LoadCustomFieldDefinitionsAsync()
    {
        try
        {
            var definitions = await _schemaService.GetSchemaDefinitionsAsync("Customer");
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
        IsLoading = true;

        try
        {
            var customers = await App.DbContext.Customers
                .Include(c => c.Sites)
                .Include(c => c.Assets)
                .OrderBy(c => c.Name)
                .AsNoTracking()
                .ToListAsync();

            Customers = new ObservableCollection<Customer>(customers);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load customers: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SearchCustomersAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            await LoadCustomersAsync();
            return;
        }

        IsLoading = true;

        try
        {
            var searchLower = SearchText.ToLower();
            var customers = await App.DbContext.Customers
                .Include(c => c.Sites)
                .Include(c => c.Assets)
                .Where(c => c.Name.ToLower().Contains(searchLower) ||
                           (c.Email != null && c.Email.ToLower().Contains(searchLower)) ||
                           (c.Phone != null && c.Phone.Contains(searchLower)) ||
                           (c.Notes != null && c.Notes.ToLower().Contains(searchLower)))
                .OrderBy(c => c.Name)
                .AsNoTracking()
                .ToListAsync();

            Customers = new ObservableCollection<Customer>(customers);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Search failed: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void LoadCustomerDetails()
    {
        CustomerSites.Clear();
        CustomerAssets.Clear();
        SitesWithAssets.Clear();
        SelectedAsset = null;
        IsViewingAsset = false;

        if (SelectedCustomer == null) return;

        foreach (var site in SelectedCustomer.Sites)
        {
            CustomerSites.Add(site);
        }

        foreach (var asset in SelectedCustomer.Assets)
        {
            CustomerAssets.Add(asset);
        }
        
        // Build hierarchy of sites with their assets
        BuildSitesWithAssetsHierarchy();
        
        // Update stats
        OnPropertyChanged(nameof(TotalSites));
        OnPropertyChanged(nameof(TotalAssets));
        OnPropertyChanged(nameof(ExpiringWarranties));
        OnPropertyChanged(nameof(ExpiredWarranties));
    }
    
    private void BuildSitesWithAssetsHierarchy()
    {
        SitesWithAssets.Clear();
        
        if (SelectedCustomer == null) return;
        
        // Group assets by site
        var assetsBySite = CustomerAssets
            .Where(a => a.SiteId.HasValue)
            .GroupBy(a => a.SiteId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());
        
        // Assets without a site
        var unassignedAssets = CustomerAssets.Where(a => !a.SiteId.HasValue).ToList();
        
        foreach (var site in CustomerSites.OrderByDescending(s => s.IsPrimary).ThenBy(s => s.Address))
        {
            var siteAssets = assetsBySite.TryGetValue(site.Id, out var assets) ? assets : [];
            SitesWithAssets.Add(new SiteWithAssets
            {
                Site = site,
                Assets = new ObservableCollection<Asset>(siteAssets),
                IsExpanded = true
            });
        }
        
        // Add unassigned assets as a virtual "site"
        if (unassignedAssets.Count > 0)
        {
            SitesWithAssets.Add(new SiteWithAssets
            {
                Site = null,
                Assets = new ObservableCollection<Asset>(unassignedAssets),
                IsExpanded = true,
                IsUnassigned = true
            });
        }
    }
    
    private void ViewAsset(Asset? asset)
    {
        if (asset == null) return;
        SelectedAsset = asset;
        IsViewingAsset = true;
    }
    
    private void CloseAssetView()
    {
        SelectedAsset = null;
        IsViewingAsset = false;
    }
    
    private void AddAssetToSite(Site? site)
    {
        if (SelectedCustomer == null) return;
        
        // Navigate to asset creation or show a dialog
        // For now, show a message
        var siteName = site?.Address ?? "this customer";
        MessageBox.Show($"Navigate to Assets page to add a new asset for {siteName}.", 
            "Add Asset", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    
    private void CallCustomer()
    {
        if (SelectedCustomer?.Phone == null) return;
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = $"tel:{SelectedCustomer.Phone}",
                UseShellExecute = true
            });
        }
        catch
        {
            Clipboard.SetText(SelectedCustomer.Phone);
            MessageBox.Show($"Phone number copied: {SelectedCustomer.Phone}", "Call Customer",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
    
    private void EmailCustomer()
    {
        if (SelectedCustomer?.Email == null) return;
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = $"mailto:{SelectedCustomer.Email}",
                UseShellExecute = true
            });
        }
        catch
        {
            Clipboard.SetText(SelectedCustomer.Email);
            MessageBox.Show($"Email copied: {SelectedCustomer.Email}", "Email Customer",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void StartAddCustomer()
    {
        SelectedCustomer = null;
        EditName = string.Empty;
        EditEmail = string.Empty;
        EditPhone = string.Empty;
        EditNotes = string.Empty;
        
        // Initialize custom field values with defaults
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

    private async void StartEditCustomer()
    {
        if (SelectedCustomer == null) return;

        EditName = SelectedCustomer.Name;
        EditEmail = SelectedCustomer.Email ?? string.Empty;
        EditPhone = SelectedCustomer.Phone ?? string.Empty;
        EditNotes = SelectedCustomer.Notes ?? string.Empty;
        
        // Load custom field values
        CustomFieldValues.Clear();
        var existingValues = await _schemaService.GetCustomFieldValuesAsync("Customer", SelectedCustomer.Id);
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

    private async Task SaveCustomerAsync()
    {
        if (string.IsNullOrWhiteSpace(EditName))
        {
            MessageBox.Show("Customer name is required.", "Validation Error",
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

        IsLoading = true;

        try
        {
            int customerId;
            
            if (SelectedCustomer == null)
            {
                // Add new customer
                var customer = new Customer
                {
                    Name = EditName.Trim(),
                    Email = string.IsNullOrWhiteSpace(EditEmail) ? null : EditEmail.Trim(),
                    Phone = string.IsNullOrWhiteSpace(EditPhone) ? null : EditPhone.Trim(),
                    Notes = string.IsNullOrWhiteSpace(EditNotes) ? null : EditNotes.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                App.DbContext.Customers.Add(customer);
                await App.DbContext.SaveChangesAsync();
                customerId = customer.Id;

                MessageBox.Show($"Customer '{customer.Name}' added successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // Update existing customer
                var customer = await App.DbContext.Customers.FindAsync(SelectedCustomer.Id);
                if (customer != null)
                {
                    customer.Name = EditName.Trim();
                    customer.Email = string.IsNullOrWhiteSpace(EditEmail) ? null : EditEmail.Trim();
                    customer.Phone = string.IsNullOrWhiteSpace(EditPhone) ? null : EditPhone.Trim();
                    customer.Notes = string.IsNullOrWhiteSpace(EditNotes) ? null : EditNotes.Trim();

                    await App.DbContext.SaveChangesAsync();
                    customerId = customer.Id;

                    MessageBox.Show($"Customer '{customer.Name}' updated successfully.", "Success",
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
                    "Customer", 
                    customerId, 
                    cf.FieldName, 
                    string.IsNullOrWhiteSpace(cf.Value) ? null : cf.Value,
                    cf.FieldType);
            }

            IsEditing = false;
            await LoadCustomersAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save customer: {ex.Message}", "Error",
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
        EditName = string.Empty;
        EditEmail = string.Empty;
        EditPhone = string.Empty;
        EditNotes = string.Empty;
    }

    private async Task DeleteCustomerAsync()
    {
        if (SelectedCustomer == null) return;

        var result = MessageBox.Show(
            $"Are you sure you want to delete '{SelectedCustomer.Name}'?\n\nThis will also delete all associated sites and assets.",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        IsLoading = true;

        try
        {
            var customer = await App.DbContext.Customers.FindAsync(SelectedCustomer.Id);
            if (customer != null)
            {
                App.DbContext.Customers.Remove(customer);
                await App.DbContext.SaveChangesAsync();

                MessageBox.Show($"Customer '{customer.Name}' deleted.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                SelectedCustomer = null;
                await LoadCustomersAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to delete customer: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task AddSiteAsync()
    {
        if (SelectedCustomer == null) return;

        // Simple site add - in a full implementation, this would open a dialog
        var site = new Site
        {
            CustomerId = SelectedCustomer.Id,
            Address = "New Site Address",
            IsPrimary = !CustomerSites.Any(),
            CreatedAt = DateTime.UtcNow
        };

        App.DbContext.Sites.Add(site);
        await App.DbContext.SaveChangesAsync();

        CustomerSites.Add(site);

        MessageBox.Show("New site added. Edit the address in the site details.", "Site Added",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void DeleteSite(Site? site)
    {
        if (site == null) return;

        var result = MessageBox.Show(
            $"Delete site at '{site.Address}'?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            var dbSite = await App.DbContext.Sites.FindAsync(site.Id);
            if (dbSite != null)
            {
                App.DbContext.Sites.Remove(dbSite);
                await App.DbContext.SaveChangesAsync();
                CustomerSites.Remove(site);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to delete site: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

/// <summary>
/// Helper class for editing custom field values in the UI.
/// </summary>
public class CustomFieldValue : BaseViewModel
{
    private string _value = string.Empty;
    
    public string FieldName { get; set; } = string.Empty;
    public string DisplayLabel { get; set; } = string.Empty;
    public CustomFieldType FieldType { get; set; }
    public bool IsRequired { get; set; }
    public List<string> EnumOptions { get; set; } = [];
    public string? Placeholder { get; set; }
    
    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }
    
    // For Yes/No fields
    public bool BoolValue
    {
        get => Value?.Equals("true", StringComparison.OrdinalIgnoreCase) == true || 
               Value?.Equals("yes", StringComparison.OrdinalIgnoreCase) == true;
        set
        {
            Value = value ? "Yes" : "No";
            OnPropertyChanged();
        }
    }
    
    // For Date fields
    public DateTime? DateValue
    {
        get => DateTime.TryParse(Value, out var dt) ? dt : null;
        set
        {
            Value = value?.ToString("yyyy-MM-dd") ?? string.Empty;
            OnPropertyChanged();
        }
    }
}

/// <summary>
/// Helper class for displaying sites with their nested assets.
/// </summary>
public class SiteWithAssets : BaseViewModel
{
    private bool _isExpanded = true;
    
    public Site? Site { get; set; }
    public ObservableCollection<Asset> Assets { get; set; } = [];
    public bool IsUnassigned { get; set; }
    
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }
    
    public string DisplayName => IsUnassigned ? "Unassigned Assets" : Site?.Address ?? "Unknown";
    public string DisplayLocation => IsUnassigned ? "" : $"{Site?.City}, {Site?.State}";
    public bool IsPrimary => Site?.IsPrimary ?? false;
    public int AssetCount => Assets.Count;
    
    /// <summary>
    /// Gets the warranty status color for display.
    /// </summary>
    public static Color GetWarrantyColor(Asset asset)
    {
        if (!asset.WarrantyEndDate.HasValue)
            return Color.FromRgb(108, 112, 134); // Gray
            
        var daysUntilExpiry = (asset.WarrantyEndDate.Value - DateTime.Today).Days;
        
        if (daysUntilExpiry < 0)
            return Color.FromRgb(243, 139, 168); // Red - expired
        if (daysUntilExpiry <= 90)
            return Color.FromRgb(249, 226, 175); // Yellow - expiring soon
        return Color.FromRgb(166, 227, 161); // Green - valid
    }
    
    /// <summary>
    /// Gets warranty status text for an asset.
    /// </summary>
    public static string GetWarrantyStatus(Asset asset)
    {
        if (!asset.WarrantyEndDate.HasValue)
            return "No Warranty";
            
        var daysUntilExpiry = (asset.WarrantyEndDate.Value - DateTime.Today).Days;
        
        if (daysUntilExpiry < 0)
            return "Expired";
        if (daysUntilExpiry <= 90)
            return $"Expires in {daysUntilExpiry}d";
        return "Valid";
    }
}
