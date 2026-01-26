using Microsoft.EntityFrameworkCore;
using OneManVan.Mobile.Helpers;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;

namespace OneManVan.Mobile.Pages;

[QueryProperty(nameof(ItemId), "id")]
public partial class InventoryDetailPage : ContentPage
{
    private readonly OneManVanDbContext _db;
    private InventoryItem? _item;
    
    public int ItemId { get; set; }

    public InventoryDetailPage(OneManVanDbContext dbContext)
    {
        InitializeComponent();
        _db = dbContext;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadItemAsync();
    }

    private async Task LoadItemAsync()
    {
        try
        {
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            _item = await _db.InventoryItems
                .Include(i => i.Logs.OrderByDescending(l => l.Timestamp).Take(10))
                .FirstOrDefaultAsync(i => i.Id == ItemId);

            if (_item == null)
            {
                await DisplayAlertAsync("Error", "Item not found", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            UpdateUI();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load item: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    private void UpdateUI()
    {
        if (_item == null) return;

        NameLabel.Text = _item.Name;
        SkuLabel.Text = _item.Sku ?? "N/A";
        CategoryLabel.Text = _item.Category.ToString();
        DescriptionLabel.Text = _item.Description ?? "";
        
        QuantityLabel.Text = $"{_item.QuantityOnHand} {_item.Unit}";
        CostLabel.Text = $"${_item.Cost:N2}";
        PriceLabel.Text = $"${_item.Price:N2}";
        MarginLabel.Text = $"{_item.ProfitMargin:N1}%";
        
        LocationLabel.Text = _item.Location ?? "Not set";
        SupplierLabel.Text = _item.Supplier ?? "Not set";
        LastRestockedLabel.Text = _item.LastRestockedAt.ToShortDate("Never");

        LowStockWarning.IsVisible = _item.IsLowStock;
        OutOfStockWarning.IsVisible = _item.IsOutOfStock;

        LogsCollection.ItemsSource = _item.Logs;
    }

    private async void OnEditClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"EditInventoryItem?id={ItemId}");
    }

    private async void OnAdjustStockClicked(object sender, EventArgs e)
    {
        var actions = new[] { "Add Stock", "Remove Stock", "Set Quantity" };
        var action = await DisplayActionSheetAsync("Adjust Stock", "Cancel", null, actions);

        if (action == "Cancel" || string.IsNullOrWhiteSpace(action)) return;

        var qtyStr = await DisplayPromptAsync("Quantity", "Enter quantity:", keyboard: Keyboard.Numeric);
        if (string.IsNullOrWhiteSpace(qtyStr) || !decimal.TryParse(qtyStr, out var qty)) return;

        var reason = await DisplayPromptAsync("Reason", "Enter reason for adjustment:", placeholder: "Optional");

        try
        {
            var oldQty = _item!.QuantityOnHand;

            if (action == "Add Stock")
                _item.QuantityOnHand += qty;
            else if (action == "Remove Stock")
                _item.QuantityOnHand -= qty;
            else
                _item.QuantityOnHand = qty;

            _item.LastRestockedAt = DateTime.UtcNow;

            var log = new InventoryLog
            {
                InventoryItemId = _item.Id,
                Timestamp = DateTime.UtcNow,
                ChangeType = action == "Add Stock" ? InventoryChangeType.Restock : 
                             action == "Remove Stock" ? InventoryChangeType.UsedOnJob : 
                             InventoryChangeType.Adjustment,
                QuantityBefore = oldQty,
                QuantityAfter = _item.QuantityOnHand,
                QuantityChange = _item.QuantityOnHand - oldQty,
                Notes = reason ?? action
            };

            _db.InventoryLogs.Add(log);
            await _db.SaveChangesAsync();

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

            await LoadItemAsync();
            await DisplayAlertAsync("Success", "Stock adjusted successfully", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to adjust stock: {ex.Message}", "OK");
        }
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        var confirm = await DisplayAlertAsync("Delete Item", 
            "Are you sure you want to delete this inventory item?", "Delete", "Cancel");

        if (!confirm) return;

        try
        {
            _db.InventoryItems.Remove(_item!);
            await _db.SaveChangesAsync();

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            
            await DisplayAlertAsync("Deleted", "Item has been deleted", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to delete item: {ex.Message}", "OK");
        }
    }
}
