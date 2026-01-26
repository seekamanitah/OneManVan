using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Mobile.Pages;

public partial class InventoryListPage : ContentPage
{
    private readonly OneManVanDbContext _db;
    private List<InventoryItem> _allItems = [];
    private string _searchText = string.Empty;
    private InventoryCategory? _categoryFilter;
    private bool _showLowStockOnly;

    public InventoryListPage(OneManVanDbContext db)
    {
        InitializeComponent();
        _db = db;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadInventoryAsync();
    }

    private async Task LoadInventoryAsync()
    {
        try
        {
            // PERF: AsNoTracking for read-only list
            _allItems = await _db.InventoryItems
                .AsNoTracking()
                .Where(i => i.IsActive)
                .OrderBy(i => i.Name)
                .ToListAsync();

            ApplyFilters();
            UpdateStats();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load inventory: {ex.Message}", "OK");
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allItems.AsEnumerable();

        // Apply search
        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            var search = _searchText.ToLowerInvariant();
            filtered = filtered.Where(i =>
                (i.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (i.Sku?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (i.PartNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (i.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        // Apply low stock filter
        if (_showLowStockOnly)
        {
            filtered = filtered.Where(i => i.IsLowStock || i.IsOutOfStock);
        }

        // Apply category filter
        if (_categoryFilter.HasValue)
        {
            filtered = filtered.Where(i => i.Category == _categoryFilter.Value);
        }

        InventoryCollection.ItemsSource = filtered.ToList();
    }

    private void UpdateStats()
    {
        var total = _allItems.Count;
        var lowStock = _allItems.Count(i => i.IsLowStock || i.IsOutOfStock);
        var value = _allItems.Sum(i => i.QuantityOnHand * i.Cost);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            TotalItemsLabel.Text = total.ToString();
            LowStockLabel.Text = lowStock.ToString();
            TotalValueLabel.Text = $"${value:N0}";
        });
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = e.NewTextValue ?? string.Empty;
        ApplyFilters();
    }

    private void OnFilterClicked(object sender, EventArgs e)
    {
        // Reset all filter button styles
        ResetFilterStyles();

        // Set active filter
        if (sender is Button button)
        {
            button.BackgroundColor = Color.FromArgb("#1976D2");
            button.TextColor = Colors.White;

            _showLowStockOnly = false;
            _categoryFilter = null;

            if (button == LowStockFilter)
            {
                _showLowStockOnly = true;
            }
            else if (button == FiltersFilter)
            {
                _categoryFilter = InventoryCategory.Filters;
            }
            else if (button == PartsFilter)
            {
                _categoryFilter = InventoryCategory.Motors; // Or general parts
            }
            else if (button == RefrigerantsFilter)
            {
                _categoryFilter = InventoryCategory.Refrigerants;
            }
        }

        ApplyFilters();
    }

    private void ResetFilterStyles()
    {
        AllFilter.BackgroundColor = Color.FromArgb("#E3F2FD");
        AllFilter.TextColor = Color.FromArgb("#1976D2");
        LowStockFilter.BackgroundColor = Color.FromArgb("#FFF3E0");
        LowStockFilter.TextColor = Color.FromArgb("#FF9800");
        FiltersFilter.BackgroundColor = Color.FromArgb("#E3F2FD");
        FiltersFilter.TextColor = Color.FromArgb("#1976D2");
        PartsFilter.BackgroundColor = Color.FromArgb("#E3F2FD");
        PartsFilter.TextColor = Color.FromArgb("#1976D2");
        RefrigerantsFilter.BackgroundColor = Color.FromArgb("#E3F2FD");
        RefrigerantsFilter.TextColor = Color.FromArgb("#1976D2");
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadInventoryAsync();
        RefreshViewControl.IsRefreshing = false;
    }

    private async void OnItemSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is InventoryItem item)
        {
            InventoryCollection.SelectedItem = null;
            
            // Navigate to edit page
            await Shell.Current.GoToAsync($"EditInventoryItem?id={item.Id}");
        }
    }

    private async void OnAddItemClicked(object sender, EventArgs e)
    {
        // Navigate to full AddInventoryItemPage
        await Shell.Current.GoToAsync("AddInventoryItem");
    }

    private async void OnRestockSwipe(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.BindingContext is InventoryItem item)
        {
            var qtyStr = await DisplayPromptAsync(
                "Restock",
                $"Add quantity to '{item.Name}':",
                placeholder: "0",
                keyboard: Keyboard.Numeric);

            if (string.IsNullOrWhiteSpace(qtyStr) || !decimal.TryParse(qtyStr, out var qty) || qty <= 0)
                return;

            try
            {
                var qtyBefore = item.QuantityOnHand;
                item.QuantityOnHand += qty;
                item.LastRestockedAt = DateTime.UtcNow;

                var log = new InventoryLog
                {
                    InventoryItemId = item.Id,
                    ChangeType = InventoryChangeType.Restock,
                    QuantityChange = qty,
                    QuantityBefore = qtyBefore,
                    QuantityAfter = item.QuantityOnHand,
                    Notes = "Mobile restock",
                    Timestamp = DateTime.UtcNow
                };

                _db.InventoryLogs.Add(log);
                await _db.SaveChangesAsync();

                await LoadInventoryAsync();

                // Haptic feedback
                try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"Failed to restock: {ex.Message}", "OK");
            }
        }
    }

    private async void OnAdjustSwipe(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.BindingContext is InventoryItem item)
        {
            var action = await DisplayActionSheetAsync(
                $"Adjust '{item.Name}'",
                "Cancel",
                null,
                "Add Stock (+)",
                "Remove Stock (-)",
                "Set Quantity");

            if (action == "Cancel" || action == null)
                return;

            var qtyStr = await DisplayPromptAsync(
                "Adjust Stock",
                action == "Set Quantity" 
                    ? $"Set quantity for '{item.Name}':" 
                    : $"Enter amount:",
                placeholder: "0",
                keyboard: Keyboard.Numeric);

            if (string.IsNullOrWhiteSpace(qtyStr) || !decimal.TryParse(qtyStr, out var qty))
                return;

            try
            {
                var qtyBefore = item.QuantityOnHand;
                decimal change = 0;

                switch (action)
                {
                    case "Add Stock (+)":
                        change = qty;
                        item.QuantityOnHand += qty;
                        break;
                    case "Remove Stock (-)":
                        change = -qty;
                        item.QuantityOnHand = Math.Max(0, item.QuantityOnHand - qty);
                        break;
                    case "Set Quantity":
                        change = qty - item.QuantityOnHand;
                        item.QuantityOnHand = qty;
                        break;
                    default:
                        return;
                }

                var log = new InventoryLog
                {
                    InventoryItemId = item.Id,
                    ChangeType = InventoryChangeType.Adjustment,
                    QuantityChange = change,
                    QuantityBefore = qtyBefore,
                    QuantityAfter = item.QuantityOnHand,
                    Notes = $"Mobile adjustment: {action}",
                    Timestamp = DateTime.UtcNow
                };

                _db.InventoryLogs.Add(log);
                await _db.SaveChangesAsync();

                await LoadInventoryAsync();

                // Haptic feedback
                try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"Failed to adjust: {ex.Message}", "OK");
            }
        }
    }
}
