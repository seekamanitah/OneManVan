using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;
using OneManVan.Services;

namespace OneManVan.ViewModels;

/// <summary>
/// ViewModel for estimate list and management.
/// </summary>
public class EstimateViewModel : BaseViewModel
{
    private readonly SchemaEditorService _schemaService = new();
    
    private ObservableCollection<Estimate> _estimates = [];
    private Estimate? _selectedEstimate;
    private string _searchText = string.Empty;
    private EstimateStatus? _statusFilter;
    private bool _isLoading;
    private bool _isEditing;

    // Edit fields
    private string _editTitle = string.Empty;
    private string _editDescription = string.Empty;
    private string _editNotes = string.Empty;
    private decimal _editTaxRate = 7.0m;
    private int? _selectedCustomerId;
    private int? _selectedSiteId;
    private int? _selectedAssetId;

    public ObservableCollection<Estimate> Estimates
    {
        get => _estimates;
        set => SetProperty(ref _estimates, value);
    }

    public Estimate? SelectedEstimate
    {
        get => _selectedEstimate;
        set
        {
            if (SetProperty(ref _selectedEstimate, value))
            {
                LoadEstimateDetails();
                OnPropertyChanged(nameof(HasSelectedEstimate));
                OnPropertyChanged(nameof(CanEditEstimate));
                OnPropertyChanged(nameof(CanConvertToJob));
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
                _ = LoadEstimatesAsync();
            }
        }
    }

    public EstimateStatus? StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (SetProperty(ref _statusFilter, value))
            {
                _ = LoadEstimatesAsync();
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

    public bool HasSelectedEstimate => SelectedEstimate != null;
    public bool CanEditEstimate => SelectedEstimate?.CanEdit ?? false;

    // Edit properties
    public string EditTitle
    {
        get => _editTitle;
        set => SetProperty(ref _editTitle, value);
    }

    public string EditDescription
    {
        get => _editDescription;
        set => SetProperty(ref _editDescription, value);
    }

    public string EditNotes
    {
        get => _editNotes;
        set => SetProperty(ref _editNotes, value);
    }

    public decimal EditTaxRate
    {
        get => _editTaxRate;
        set => SetProperty(ref _editTaxRate, value);
    }

    public int? SelectedCustomerId
    {
        get => _selectedCustomerId;
        set
        {
            if (SetProperty(ref _selectedCustomerId, value))
            {
                _ = LoadCustomerSitesAsync();
            }
        }
    }

    public int? SelectedSiteId
    {
        get => _selectedSiteId;
        set => SetProperty(ref _selectedSiteId, value);
    }

    public int? SelectedAssetId
    {
        get => _selectedAssetId;
        set => SetProperty(ref _selectedAssetId, value);
    }

    // Collections for dropdowns
    public ObservableCollection<Customer> Customers { get; } = [];
    public ObservableCollection<Site> CustomerSites { get; } = [];
    public ObservableCollection<Asset> CustomerAssets { get; } = [];
    public ObservableCollection<EstimateLine> EstimateLines { get; } = [];

    // Status filter options
    public static EstimateStatus[] StatusOptions => Enum.GetValues<EstimateStatus>();

    // Commands
    public ICommand LoadEstimatesCommand { get; }
    public ICommand AddEstimateCommand { get; }
    public ICommand EditEstimateCommand { get; }
    public ICommand SaveEstimateCommand { get; }
    public ICommand CancelEditCommand { get; }
    public ICommand DeleteEstimateCommand { get; }
    public ICommand DuplicateEstimateCommand { get; }
    public ICommand SendEstimateCommand { get; }
    public ICommand AcceptEstimateCommand { get; }
    public ICommand DeclineEstimateCommand { get; }
    public ICommand ConvertToJobCommand { get; }
    public ICommand ClearFilterCommand { get; }

    // Line item commands
    public ICommand AddLineCommand { get; }
    public ICommand RemoveLineCommand { get; }
    public ICommand AddFromInventoryCommand { get; }
    public ICommand PrintEstimateCommand { get; }

    public bool CanConvertToJob => SelectedEstimate?.Status == EstimateStatus.Accepted;

    // Inventory items for selection
    public ObservableCollection<InventoryItem> InventoryItems { get; } = [];
    
    // Custom fields
    public ObservableCollection<SchemaDefinition> CustomFieldDefinitions { get; } = [];
    public ObservableCollection<CustomFieldValue> CustomFieldValues { get; } = [];
    public bool HasCustomFields => CustomFieldDefinitions.Count > 0;

    public EstimateViewModel()
    {
        LoadEstimatesCommand = new AsyncRelayCommand(LoadEstimatesAsync);
        AddEstimateCommand = new RelayCommand(StartAddEstimate);
        EditEstimateCommand = new RelayCommand(StartEditEstimate, () => CanEditEstimate);
        SaveEstimateCommand = new AsyncRelayCommand(SaveEstimateAsync);
        CancelEditCommand = new RelayCommand(CancelEdit);
        DeleteEstimateCommand = new AsyncRelayCommand(DeleteEstimateAsync, () => HasSelectedEstimate);
        DuplicateEstimateCommand = new AsyncRelayCommand(DuplicateEstimateAsync, () => HasSelectedEstimate);
        SendEstimateCommand = new AsyncRelayCommand(SendEstimateAsync, () => CanEditEstimate);
        AcceptEstimateCommand = new AsyncRelayCommand(AcceptEstimateAsync, () => SelectedEstimate?.Status == EstimateStatus.Sent);
        DeclineEstimateCommand = new AsyncRelayCommand(DeclineEstimateAsync, () => SelectedEstimate?.Status == EstimateStatus.Sent);
        ConvertToJobCommand = new AsyncRelayCommand(ConvertToJobAsync, () => CanConvertToJob);
        ClearFilterCommand = new RelayCommand(() => StatusFilter = null);
        AddLineCommand = new RelayCommand(AddLine);
        RemoveLineCommand = new RelayCommand<EstimateLine>(RemoveLine);
        AddFromInventoryCommand = new AsyncRelayCommand(ShowInventoryPickerAsync);
        PrintEstimateCommand = new RelayCommand(PrintEstimate, () => HasSelectedEstimate);

        _ = LoadEstimatesAsync();
        _ = LoadCustomersAsync();
        _ = LoadInventoryAsync();
        _ = LoadCustomFieldDefinitionsAsync();
    }
    
    private async Task LoadCustomFieldDefinitionsAsync()
    {
        try
        {
            var definitions = await _schemaService.GetSchemaDefinitionsAsync("Estimate");
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

    private void PrintEstimate()
    {
        if (SelectedEstimate == null) return;
        
        try
        {
            PrintService.PrintEstimate(SelectedEstimate);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to print estimate: {ex.Message}", "Print Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadInventoryAsync()
    {
        var items = await App.DbContext.InventoryItems
            .Where(i => i.IsActive && i.QuantityOnHand > 0)
            .OrderBy(i => i.Name)
            .AsNoTracking()
            .ToListAsync();

        InventoryItems.Clear();
        foreach (var item in items)
        {
            InventoryItems.Add(item);
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

    private async Task LoadCustomerSitesAsync()
    {
        CustomerSites.Clear();
        CustomerAssets.Clear();

        if (SelectedCustomerId == null) return;

        var sites = await App.DbContext.Sites
            .Where(s => s.CustomerId == SelectedCustomerId)
            .OrderBy(s => s.Address)
            .AsNoTracking()
            .ToListAsync();

        foreach (var site in sites)
        {
            CustomerSites.Add(site);
        }

        var assets = await App.DbContext.Assets
            .Where(a => a.CustomerId == SelectedCustomerId)
            .OrderBy(a => a.Serial)
            .AsNoTracking()
            .ToListAsync();

        foreach (var asset in assets)
        {
            CustomerAssets.Add(asset);
        }
    }

    private async Task LoadEstimatesAsync()
    {
        IsLoading = true;

        try
        {
            var query = App.DbContext.Estimates
                .Include(e => e.Customer)
                .Include(e => e.Lines)
                .AsNoTracking();

            if (StatusFilter.HasValue)
            {
                query = query.Where(e => e.Status == StatusFilter.Value);
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                query = query.Where(e =>
                    e.Title.ToLower().Contains(searchLower) ||
                    e.Customer.Name.ToLower().Contains(searchLower) ||
                    (e.Description != null && e.Description.ToLower().Contains(searchLower)));
            }

            var estimates = await query
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            Estimates = new ObservableCollection<Estimate>(estimates);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load estimates: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void LoadEstimateDetails()
    {
        EstimateLines.Clear();

        if (SelectedEstimate == null) return;

        foreach (var line in SelectedEstimate.Lines.OrderBy(l => l.SortOrder))
        {
            EstimateLines.Add(line);
        }
    }

    private void StartAddEstimate()
    {
        SelectedEstimate = null;
        EditTitle = string.Empty;
        EditDescription = string.Empty;
        EditNotes = string.Empty;
        EditTaxRate = 7.0m;
        SelectedCustomerId = Customers.FirstOrDefault()?.Id;
        SelectedSiteId = null;
        SelectedAssetId = null;
        EstimateLines.Clear();
        
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

    private async void StartEditEstimate()
    {
        if (SelectedEstimate == null || !SelectedEstimate.CanEdit) return;

        EditTitle = SelectedEstimate.Title;
        EditDescription = SelectedEstimate.Description ?? string.Empty;
        EditNotes = SelectedEstimate.Notes ?? string.Empty;
        EditTaxRate = SelectedEstimate.TaxRate;
        SelectedCustomerId = SelectedEstimate.CustomerId;
        SelectedSiteId = SelectedEstimate.SiteId;
        SelectedAssetId = SelectedEstimate.AssetId;

        EstimateLines.Clear();
        foreach (var line in SelectedEstimate.Lines.OrderBy(l => l.SortOrder))
        {
            EstimateLines.Add(new EstimateLine
            {
                Description = line.Description,
                Type = line.Type,
                Quantity = line.Quantity,
                Unit = line.Unit,
                UnitPrice = line.UnitPrice,
                Total = line.Total,
                Notes = line.Notes,
                SortOrder = line.SortOrder
            });
        }
        
        // Load custom field values
        CustomFieldValues.Clear();
        var existingValues = await _schemaService.GetCustomFieldValuesAsync("Estimate", SelectedEstimate.Id);
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

    private async Task SaveEstimateAsync()
    {
        if (string.IsNullOrWhiteSpace(EditTitle))
        {
            MessageBox.Show("Estimate title is required.", "Validation Error",
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

        IsLoading = true;

        try
        {
            Estimate estimate;
            int estimateId;

            if (SelectedEstimate == null)
            {
                // Create new estimate
                estimate = new Estimate
                {
                    CustomerId = SelectedCustomerId.Value,
                    SiteId = SelectedSiteId,
                    AssetId = SelectedAssetId,
                    Title = EditTitle.Trim(),
                    Description = string.IsNullOrWhiteSpace(EditDescription) ? null : EditDescription.Trim(),
                    Notes = string.IsNullOrWhiteSpace(EditNotes) ? null : EditNotes.Trim(),
                    TaxRate = EditTaxRate,
                    Status = EstimateStatus.Draft,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(30)
                };

                App.DbContext.Estimates.Add(estimate);
            }
            else
            {
                // Update existing
                estimate = await App.DbContext.Estimates
                    .Include(e => e.Lines)
                    .FirstAsync(e => e.Id == SelectedEstimate.Id);

                estimate.CustomerId = SelectedCustomerId.Value;
                estimate.SiteId = SelectedSiteId;
                estimate.AssetId = SelectedAssetId;
                estimate.Title = EditTitle.Trim();
                estimate.Description = string.IsNullOrWhiteSpace(EditDescription) ? null : EditDescription.Trim();
                estimate.Notes = string.IsNullOrWhiteSpace(EditNotes) ? null : EditNotes.Trim();
                estimate.TaxRate = EditTaxRate;

                // Remove existing lines
                App.DbContext.EstimateLines.RemoveRange(estimate.Lines);
            }

            // Add lines
            var sortOrder = 0;
            foreach (var line in EstimateLines)
            {
                line.RecalculateTotal();
                estimate.Lines.Add(new EstimateLine
                {
                    Description = line.Description,
                    Type = line.Type,
                    Quantity = line.Quantity,
                    Unit = line.Unit,
                    UnitPrice = line.UnitPrice,
                    Total = line.Total,
                    Notes = line.Notes,
                    SortOrder = sortOrder++,
                    InventoryItemId = line.InventoryItemId
                });
            }

            estimate.RecalculateTotals();
            await App.DbContext.SaveChangesAsync();
            estimateId = estimate.Id;
            
            // Save custom field values
            foreach (var cf in CustomFieldValues)
            {
                await _schemaService.SetCustomFieldValueAsync(
                    "Estimate", 
                    estimateId, 
                    cf.FieldName, 
                    string.IsNullOrWhiteSpace(cf.Value) ? null : cf.Value,
                    cf.FieldType);
            }

            MessageBox.Show($"Estimate '{estimate.Title}' saved successfully.", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);

            IsEditing = false;
            await LoadEstimatesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save estimate: {ex.Message}", "Error",
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
        EstimateLines.Clear();
        if (SelectedEstimate != null)
        {
            LoadEstimateDetails();
        }
    }

    private async Task DeleteEstimateAsync()
    {
        if (SelectedEstimate == null) return;

        var result = MessageBox.Show(
            $"Delete estimate '{SelectedEstimate.Title}'?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        IsLoading = true;

        try
        {
            var estimate = await App.DbContext.Estimates.FindAsync(SelectedEstimate.Id);
            if (estimate != null)
            {
                App.DbContext.Estimates.Remove(estimate);
                await App.DbContext.SaveChangesAsync();

                MessageBox.Show("Estimate deleted.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                SelectedEstimate = null;
                await LoadEstimatesAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to delete estimate: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task DuplicateEstimateAsync()
    {
        if (SelectedEstimate == null) return;

        IsLoading = true;

        try
        {
            var original = await App.DbContext.Estimates
                .Include(e => e.Lines)
                .FirstAsync(e => e.Id == SelectedEstimate.Id);

            var duplicate = new Estimate
            {
                CustomerId = original.CustomerId,
                SiteId = original.SiteId,
                AssetId = original.AssetId,
                Title = $"{original.Title} (Copy)",
                Description = original.Description,
                Notes = original.Notes,
                TaxRate = original.TaxRate,
                Terms = original.Terms,
                Status = EstimateStatus.Draft,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };

            foreach (var line in original.Lines)
            {
                duplicate.Lines.Add(new EstimateLine
                {
                    Description = line.Description,
                    Type = line.Type,
                    Quantity = line.Quantity,
                    Unit = line.Unit,
                    UnitPrice = line.UnitPrice,
                    Total = line.Total,
                    Notes = line.Notes,
                    SortOrder = line.SortOrder,
                    InventoryItemId = line.InventoryItemId
                });
            }

            duplicate.RecalculateTotals();
            App.DbContext.Estimates.Add(duplicate);
            await App.DbContext.SaveChangesAsync();

            MessageBox.Show($"Estimate duplicated as '{duplicate.Title}'.", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);

            await LoadEstimatesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to duplicate estimate: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SendEstimateAsync()
    {
        if (SelectedEstimate == null || SelectedEstimate.Status != EstimateStatus.Draft) return;

        var result = MessageBox.Show(
            $"Mark estimate '{SelectedEstimate.Title}' as sent?",
            "Send Estimate",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            var estimate = await App.DbContext.Estimates.FindAsync(SelectedEstimate.Id);
            if (estimate != null)
            {
                estimate.Status = EstimateStatus.Sent;
                estimate.SentAt = DateTime.UtcNow;
                await App.DbContext.SaveChangesAsync();

                await LoadEstimatesAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to update status: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task AcceptEstimateAsync()
    {
        if (SelectedEstimate == null || SelectedEstimate.Status != EstimateStatus.Sent) return;

        try
        {
            var estimate = await App.DbContext.Estimates.FindAsync(SelectedEstimate.Id);
            if (estimate != null)
            {
                estimate.Status = EstimateStatus.Accepted;
                estimate.AcceptedAt = DateTime.UtcNow;
                await App.DbContext.SaveChangesAsync();

                MessageBox.Show("Estimate accepted! Ready to convert to job.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadEstimatesAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to update status: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task DeclineEstimateAsync()
    {
        if (SelectedEstimate == null || SelectedEstimate.Status != EstimateStatus.Sent) return;

        try
        {
            var estimate = await App.DbContext.Estimates.FindAsync(SelectedEstimate.Id);
            if (estimate != null)
            {
                estimate.Status = EstimateStatus.Declined;
                await App.DbContext.SaveChangesAsync();

                await LoadEstimatesAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to update status: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddLine()
    {
        EstimateLines.Add(new EstimateLine
        {
            Description = "New Line Item",
            Type = LineItemType.Labor,
            Quantity = 1,
            Unit = "ea",
            UnitPrice = 0,
            SortOrder = EstimateLines.Count
        });
        OnPropertyChanged(nameof(EditSubTotal));
        OnPropertyChanged(nameof(EditTaxAmount));
        OnPropertyChanged(nameof(EditTotal));
    }

    private async Task ShowInventoryPickerAsync()
    {
        // Reload inventory to ensure fresh data
        await LoadInventoryAsync();

        if (InventoryItems.Count == 0)
        {
            MessageBox.Show("No inventory items available. Add items in the Inventory page first.",
                "No Inventory", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Show the inventory picker dialog
        var owner = Application.Current.MainWindow;
        var selectedItem = Dialogs.InventoryPickerDialog.ShowDialog(owner, InventoryItems);
        
        if (selectedItem != null)
        {
            AddLineFromInventory(selectedItem);
        }
    }

    private void AddLineFromInventory(InventoryItem item)
    {
        EstimateLines.Add(new EstimateLine
        {
            Description = item.Name,
            Type = LineItemType.Part,
            Quantity = 1,
            Unit = item.Unit ?? "ea",
            UnitPrice = item.Price,
            InventoryItemId = item.Id,
            SortOrder = EstimateLines.Count
        });
        
        OnPropertyChanged(nameof(EditSubTotal));
        OnPropertyChanged(nameof(EditTaxAmount));
        OnPropertyChanged(nameof(EditTotal));
    }

    private void RemoveLine(EstimateLine? line)
    {
        if (line != null)
        {
            EstimateLines.Remove(line);
            OnPropertyChanged(nameof(EditSubTotal));
            OnPropertyChanged(nameof(EditTaxAmount));
            OnPropertyChanged(nameof(EditTotal));
        }
    }

    // Calculated totals for editing
    public decimal EditSubTotal => EstimateLines.Sum(l =>
    {
        l.RecalculateTotal();
        return l.Total;
    });

    public decimal EditTaxAmount => EditSubTotal * (EditTaxRate / 100);
    public decimal EditTotal => EditSubTotal + EditTaxAmount;

    public void RecalculateTotals()
    {
        OnPropertyChanged(nameof(EditSubTotal));
        OnPropertyChanged(nameof(EditTaxAmount));
        OnPropertyChanged(nameof(EditTotal));
    }

    private async Task ConvertToJobAsync()
    {
        if (SelectedEstimate == null || SelectedEstimate.Status != EstimateStatus.Accepted) return;

        var result = MessageBox.Show(
            $"Convert estimate '{SelectedEstimate.Title}' to a job?\n\nThis will create a new job and mark the estimate as converted.",
            "Convert to Job",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        IsLoading = true;

        try
        {
            var estimate = await App.DbContext.Estimates
                .Include(e => e.Lines)
                .FirstAsync(e => e.Id == SelectedEstimate.Id);

            // Create job from estimate
            var job = Job.FromEstimate(estimate);
            App.DbContext.Jobs.Add(job);

            // Mark estimate as converted
            estimate.Status = EstimateStatus.Converted;

            await App.DbContext.SaveChangesAsync();

            MessageBox.Show(
                $"Job created from estimate!\n\nJob: {job.Title}\nTotal: ${job.Total:N2}\n\nNavigate to Jobs to schedule it.",
                "Job Created",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            await LoadEstimatesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to convert estimate: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
