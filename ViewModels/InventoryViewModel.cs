using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.ViewModels;

/// <summary>
/// ViewModel for inventory management with HVAC compatibility filtering.
/// </summary>
public class InventoryViewModel : BaseViewModel
{
    private ObservableCollection<InventoryItem> _items = [];
    private InventoryItem? _selectedItem;
    private string _searchText = string.Empty;
    private InventoryCategory? _categoryFilter;
    private bool _showLowStockOnly;
    private bool _isLoading;
    private bool _isEditing;

    // Edit fields
    private string _editName = string.Empty;
    private string _editDescription = string.Empty;
    private string _editSku = string.Empty;
    private string _editPartNumber = string.Empty;
    private InventoryCategory _editCategory = InventoryCategory.General;
    private decimal _editQuantity;
    private string _editUnit = "ea";
    private decimal _editCost;
    private decimal _editPrice;
    private decimal _editReorderPoint;
    private int? _editBtuMin;
    private int? _editBtuMax;
    private FuelType? _editFuelType;
    private string _editFilterSize = string.Empty;
    private string _editRefrigerantType = string.Empty;
    private string _editLocation = string.Empty;
    private string _editSupplier = string.Empty;

    public ObservableCollection<InventoryItem> Items
    {
        get => _items;
        set => SetProperty(ref _items, value);
    }

    public InventoryItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                LoadItemLogs();
                OnPropertyChanged(nameof(HasSelectedItem));
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
                _ = LoadItemsAsync();
            }
        }
    }

    public InventoryCategory? CategoryFilter
    {
        get => _categoryFilter;
        set
        {
            if (SetProperty(ref _categoryFilter, value))
            {
                _ = LoadItemsAsync();
            }
        }
    }

    public bool ShowLowStockOnly
    {
        get => _showLowStockOnly;
        set
        {
            if (SetProperty(ref _showLowStockOnly, value))
            {
                _ = LoadItemsAsync();
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

    public bool HasSelectedItem => SelectedItem != null || IsEditing;

    // Edit properties
    public string EditName
    {
        get => _editName;
        set => SetProperty(ref _editName, value);
    }

    public string EditDescription
    {
        get => _editDescription;
        set => SetProperty(ref _editDescription, value);
    }

    public string EditSku
    {
        get => _editSku;
        set => SetProperty(ref _editSku, value);
    }

    public string EditPartNumber
    {
        get => _editPartNumber;
        set => SetProperty(ref _editPartNumber, value);
    }

    public InventoryCategory EditCategory
    {
        get => _editCategory;
        set => SetProperty(ref _editCategory, value);
    }

    public decimal EditQuantity
    {
        get => _editQuantity;
        set => SetProperty(ref _editQuantity, value);
    }

    public string EditUnit
    {
        get => _editUnit;
        set => SetProperty(ref _editUnit, value);
    }

    public decimal EditCost
    {
        get => _editCost;
        set => SetProperty(ref _editCost, value);
    }

    public decimal EditPrice
    {
        get => _editPrice;
        set => SetProperty(ref _editPrice, value);
    }

    public decimal EditReorderPoint
    {
        get => _editReorderPoint;
        set => SetProperty(ref _editReorderPoint, value);
    }

    public int? EditBtuMin
    {
        get => _editBtuMin;
        set => SetProperty(ref _editBtuMin, value);
    }

    public int? EditBtuMax
    {
        get => _editBtuMax;
        set => SetProperty(ref _editBtuMax, value);
    }

    public FuelType? EditFuelType
    {
        get => _editFuelType;
        set => SetProperty(ref _editFuelType, value);
    }

    public string EditFilterSize
    {
        get => _editFilterSize;
        set => SetProperty(ref _editFilterSize, value);
    }

    public string EditRefrigerantType
    {
        get => _editRefrigerantType;
        set => SetProperty(ref _editRefrigerantType, value);
    }

    public string EditLocation
    {
        get => _editLocation;
        set => SetProperty(ref _editLocation, value);
    }

    public string EditSupplier
    {
        get => _editSupplier;
        set => SetProperty(ref _editSupplier, value);
    }

    // Stock adjustment
    private decimal _adjustmentQuantity;
    private string _adjustmentNotes = string.Empty;

    public decimal AdjustmentQuantity
    {
        get => _adjustmentQuantity;
        set => SetProperty(ref _adjustmentQuantity, value);
    }

    public string AdjustmentNotes
    {
        get => _adjustmentNotes;
        set => SetProperty(ref _adjustmentNotes, value);
    }

    // Collections
    public ObservableCollection<InventoryLog> ItemLogs { get; } = [];
    public Array CategoryOptions => Enum.GetValues(typeof(InventoryCategory));
    public Array FuelTypeOptions => Enum.GetValues(typeof(FuelType));

    // Stats
    public int TotalItems => Items.Count;
    public int LowStockCount => Items.Count(i => i.IsLowStock);
    public int OutOfStockCount => Items.Count(i => i.IsOutOfStock);
    public decimal TotalValue => Items.Sum(i => i.QuantityOnHand * i.Cost);

    // Commands
    public ICommand LoadItemsCommand { get; }
    public ICommand AddItemCommand { get; }
    public ICommand EditItemCommand { get; }
    public ICommand SaveItemCommand { get; }
    public ICommand CancelEditCommand { get; }
    public ICommand DeleteItemCommand { get; }
    public ICommand RestockCommand { get; }
    public ICommand AdjustStockCommand { get; }
    public ICommand ClearFilterCommand { get; }

    public InventoryViewModel()
    {
        LoadItemsCommand = new AsyncRelayCommand(LoadItemsAsync);
        AddItemCommand = new RelayCommand(StartAddItem);
        EditItemCommand = new RelayCommand(StartEditItem, () => HasSelectedItem);
        SaveItemCommand = new AsyncRelayCommand(SaveItemAsync);
        CancelEditCommand = new RelayCommand(CancelEdit);
        DeleteItemCommand = new AsyncRelayCommand(DeleteItemAsync, () => HasSelectedItem);
        RestockCommand = new AsyncRelayCommand(RestockAsync, () => HasSelectedItem);
        AdjustStockCommand = new AsyncRelayCommand(AdjustStockAsync, () => HasSelectedItem);
        ClearFilterCommand = new RelayCommand(ClearFilters);

        _ = LoadItemsAsync();
    }

    private async Task LoadItemsAsync()
    {
        IsLoading = true;

        try
        {
            var query = App.DbContext.InventoryItems
                .Where(i => i.IsActive)
                .AsNoTracking();

            if (CategoryFilter.HasValue)
            {
                query = query.Where(i => i.Category == CategoryFilter.Value);
            }

            if (ShowLowStockOnly)
            {
                query = query.Where(i => i.QuantityOnHand <= i.ReorderPoint);
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                query = query.Where(i =>
                    i.Name.ToLower().Contains(searchLower) ||
                    (i.Sku != null && i.Sku.ToLower().Contains(searchLower)) ||
                    (i.PartNumber != null && i.PartNumber.ToLower().Contains(searchLower)) ||
                    (i.Description != null && i.Description.ToLower().Contains(searchLower)));
            }

            var items = await query
                .OrderBy(i => i.Category)
                .ThenBy(i => i.Name)
                .ToListAsync();

            Items = new ObservableCollection<InventoryItem>(items);

            OnPropertyChanged(nameof(TotalItems));
            OnPropertyChanged(nameof(LowStockCount));
            OnPropertyChanged(nameof(OutOfStockCount));
            OnPropertyChanged(nameof(TotalValue));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load inventory: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async void LoadItemLogs()
    {
        ItemLogs.Clear();

        if (SelectedItem == null) return;

        try
        {
            var logs = await App.DbContext.InventoryLogs
                .Where(l => l.InventoryItemId == SelectedItem.Id)
                .OrderByDescending(l => l.Timestamp)
                .Take(20)
                .AsNoTracking()
                .ToListAsync();

            foreach (var log in logs)
            {
                ItemLogs.Add(log);
            }
        }
        catch
        {
            // Silently fail on log load
        }
    }

    private void StartAddItem()
    {
        SelectedItem = null;
        EditName = string.Empty;
        EditDescription = string.Empty;
        EditSku = string.Empty;
        EditPartNumber = string.Empty;
        EditCategory = InventoryCategory.General;
        EditQuantity = 0;
        EditUnit = "ea";
        EditCost = 0;
        EditPrice = 0;
        EditReorderPoint = 0;
        EditBtuMin = null;
        EditBtuMax = null;
        EditFuelType = null;
        EditFilterSize = string.Empty;
        EditRefrigerantType = string.Empty;
        EditLocation = string.Empty;
        EditSupplier = string.Empty;
        IsEditing = true;
        OnPropertyChanged(nameof(HasSelectedItem));
    }

    private void StartEditItem()
    {
        if (SelectedItem == null) return;

        EditName = SelectedItem.Name;
        EditDescription = SelectedItem.Description ?? string.Empty;
        EditSku = SelectedItem.Sku ?? string.Empty;
        EditPartNumber = SelectedItem.PartNumber ?? string.Empty;
        EditCategory = SelectedItem.Category;
        EditQuantity = SelectedItem.QuantityOnHand;
        EditUnit = SelectedItem.Unit;
        EditCost = SelectedItem.Cost;
        EditPrice = SelectedItem.Price;
        EditReorderPoint = SelectedItem.ReorderPoint;
        EditBtuMin = SelectedItem.BtuMinCompatibility;
        EditBtuMax = SelectedItem.BtuMaxCompatibility;
        EditFuelType = SelectedItem.FuelTypeCompatibility;
        EditFilterSize = SelectedItem.FilterSize ?? string.Empty;
        EditRefrigerantType = SelectedItem.RefrigerantType ?? string.Empty;
        EditLocation = SelectedItem.Location ?? string.Empty;
        EditSupplier = SelectedItem.Supplier ?? string.Empty;
        IsEditing = true;
    }

    private async Task SaveItemAsync()
    {
        if (string.IsNullOrWhiteSpace(EditName))
        {
            MessageBox.Show("Item name is required.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsLoading = true;

        try
        {
            InventoryItem item;
            var isNew = SelectedItem == null;

            if (isNew)
            {
                item = new InventoryItem
                {
                    CreatedAt = DateTime.UtcNow
                };
                App.DbContext.InventoryItems.Add(item);
            }
            else
            {
                item = await App.DbContext.InventoryItems.FindAsync(SelectedItem!.Id)
                    ?? throw new Exception("Item not found");
            }

            item.Name = EditName.Trim();
            item.Description = string.IsNullOrWhiteSpace(EditDescription) ? null : EditDescription.Trim();
            item.Sku = string.IsNullOrWhiteSpace(EditSku) ? null : EditSku.Trim();
            item.PartNumber = string.IsNullOrWhiteSpace(EditPartNumber) ? null : EditPartNumber.Trim();
            item.Category = EditCategory;
            item.Unit = EditUnit;
            item.Cost = EditCost;
            item.Price = EditPrice;
            item.ReorderPoint = EditReorderPoint;
            item.BtuMinCompatibility = EditBtuMin;
            item.BtuMaxCompatibility = EditBtuMax;
            item.FuelTypeCompatibility = EditFuelType;
            item.FilterSize = string.IsNullOrWhiteSpace(EditFilterSize) ? null : EditFilterSize.Trim();
            item.RefrigerantType = string.IsNullOrWhiteSpace(EditRefrigerantType) ? null : EditRefrigerantType.Trim();
            item.Location = string.IsNullOrWhiteSpace(EditLocation) ? null : EditLocation.Trim();
            item.Supplier = string.IsNullOrWhiteSpace(EditSupplier) ? null : EditSupplier.Trim();

            // Handle initial quantity for new items
            if (isNew && EditQuantity > 0)
            {
                item.QuantityOnHand = EditQuantity;
                item.Logs.Add(new InventoryLog
                {
                    ChangeType = InventoryChangeType.Initial,
                    QuantityChange = EditQuantity,
                    QuantityBefore = 0,
                    QuantityAfter = EditQuantity,
                    Notes = "Initial stock",
                    Timestamp = DateTime.UtcNow
                });
            }

            await App.DbContext.SaveChangesAsync();

            MessageBox.Show($"Item '{item.Name}' saved successfully.", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);

            IsEditing = false;
            await LoadItemsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save item: {ex.Message}", "Error",
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

    private async Task DeleteItemAsync()
    {
        if (SelectedItem == null) return;

        var result = MessageBox.Show(
            $"Delete item '{SelectedItem.Name}'?\n\nThis will also delete all stock history.",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        IsLoading = true;

        try
        {
            var item = await App.DbContext.InventoryItems.FindAsync(SelectedItem.Id);
            if (item != null)
            {
                // Soft delete - mark as inactive
                item.IsActive = false;
                await App.DbContext.SaveChangesAsync();

                MessageBox.Show("Item deleted.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                SelectedItem = null;
                await LoadItemsAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to delete item: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RestockAsync()
    {
        if (SelectedItem == null || AdjustmentQuantity <= 0) return;

        try
        {
            var item = await App.DbContext.InventoryItems.FindAsync(SelectedItem.Id);
            if (item != null)
            {
                var before = item.QuantityOnHand;
                item.QuantityOnHand += AdjustmentQuantity;
                item.LastRestockedAt = DateTime.UtcNow;

                App.DbContext.InventoryLogs.Add(new InventoryLog
                {
                    InventoryItemId = item.Id,
                    ChangeType = InventoryChangeType.Restock,
                    QuantityChange = AdjustmentQuantity,
                    QuantityBefore = before,
                    QuantityAfter = item.QuantityOnHand,
                    Notes = string.IsNullOrWhiteSpace(AdjustmentNotes) ? null : AdjustmentNotes.Trim(),
                    Timestamp = DateTime.UtcNow
                });

                await App.DbContext.SaveChangesAsync();

                MessageBox.Show($"Restocked {AdjustmentQuantity} {item.Unit}(s). New quantity: {item.QuantityOnHand}", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                AdjustmentQuantity = 0;
                AdjustmentNotes = string.Empty;
                await LoadItemsAsync();
                LoadItemLogs();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to restock: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task AdjustStockAsync()
    {
        if (SelectedItem == null || AdjustmentQuantity == 0) return;

        try
        {
            var item = await App.DbContext.InventoryItems.FindAsync(SelectedItem.Id);
            if (item != null)
            {
                var before = item.QuantityOnHand;
                item.QuantityOnHand += AdjustmentQuantity;

                if (item.QuantityOnHand < 0)
                {
                    MessageBox.Show("Cannot adjust to negative quantity.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                App.DbContext.InventoryLogs.Add(new InventoryLog
                {
                    InventoryItemId = item.Id,
                    ChangeType = InventoryChangeType.Adjustment,
                    QuantityChange = AdjustmentQuantity,
                    QuantityBefore = before,
                    QuantityAfter = item.QuantityOnHand,
                    Notes = string.IsNullOrWhiteSpace(AdjustmentNotes) ? null : AdjustmentNotes.Trim(),
                    Timestamp = DateTime.UtcNow
                });

                await App.DbContext.SaveChangesAsync();

                MessageBox.Show($"Stock adjusted. New quantity: {item.QuantityOnHand}", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                AdjustmentQuantity = 0;
                AdjustmentNotes = string.Empty;
                await LoadItemsAsync();
                LoadItemLogs();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to adjust stock: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ClearFilters()
    {
        CategoryFilter = null;
        ShowLowStockOnly = false;
        SearchText = string.Empty;
    }
}

