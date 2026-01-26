using Microsoft.EntityFrameworkCore;
using OneManVan.Mobile.Extensions;
using OneManVan.Mobile.Helpers;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Mobile.Pages;

[QueryProperty(nameof(ItemId), "id")]
public partial class EditInventoryItemPage : ContentPage
{
    private readonly OneManVanDbContext _db;
    private InventoryItem? _item;
    private int _itemId;

    public int ItemId
    {
        get => _itemId;
        set
        {
            _itemId = value;
            LoadItemAsync();
        }
    }

    public EditInventoryItemPage(OneManVanDbContext db)
    {
        InitializeComponent();
        _db = db;
    }

    private async void LoadItemAsync()
    {
        try
        {
            _item = await _db.InventoryItems.FindAsync(_itemId);
            if (_item == null)
            {
                await DisplayAlertAsync("Error", "Item not found.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            PopulateForm();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load item: {ex.Message}", "OK");
        }
    }

    private void PopulateForm()
    {
        if (_item == null) return;

        NameEntry.Text = _item.Name;
        DescriptionEditor.Text = _item.Description;
        SkuEntry.Text = _item.Sku;
        PartNumberEntry.Text = _item.PartNumber;
        QuantityEntry.Text = _item.QuantityOnHand.ToString("F0");
        ReorderPointEntry.Text = _item.ReorderPoint.ToString("F0");
        LocationEntry.Text = _item.Location;
        CostEntry.Text = _item.Cost.ToString("F2");
        PriceEntry.Text = _item.Price.ToString("F2");
        FilterSizeEntry.Text = _item.FilterSize;
        RefrigerantEntry.Text = _item.RefrigerantType;
        SupplierEntry.Text = _item.Supplier;

        // Set category picker
        var categoryIndex = _item.Category switch
        {
            InventoryCategory.General => 0,
            InventoryCategory.Filters => 1,
            InventoryCategory.Coils => 2,
            InventoryCategory.Refrigerants => 3,
            InventoryCategory.Motors => 4,
            InventoryCategory.Thermostats => 5,
            InventoryCategory.Capacitors => 6,
            InventoryCategory.Contactors => 7,
            InventoryCategory.Ductwork => 8,
            InventoryCategory.Fittings => 9,
            InventoryCategory.Electrical => 10,
            InventoryCategory.Tools => 11,
            InventoryCategory.Consumables => 12,
            _ => 0
        };
        CategoryPicker.SelectedIndex = categoryIndex;

        // Set unit picker
        var units = new[] { "ea", "lb", "oz", "ft", "box", "case", "roll" };
        var unitIndex = Array.IndexOf(units, _item.Unit?.ToLower() ?? "ea");
        UnitPicker.SelectedIndex = unitIndex >= 0 ? unitIndex : 0;

        UpdateMargin();
    }

    private void OnPriceChanged(object sender, TextChangedEventArgs e)
    {
        UpdateMargin();
    }

    private void UpdateMargin()
    {
        if (decimal.TryParse(CostEntry.Text, out var cost) &&
            decimal.TryParse(PriceEntry.Text, out var price) &&
            cost > 0)
        {
            var margin = ((price - cost) / cost) * 100;
            MarginLabel.Text = $"{margin:F0}%";
            MarginLabel.TextColor = margin >= 0 
                ? Color.FromArgb("#4CAF50") 
                : Color.FromArgb("#F44336");
        }
        else
        {
            MarginLabel.Text = "0%";
            MarginLabel.TextColor = Color.FromArgb("#4CAF50");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (_item == null) return;

        if (string.IsNullOrWhiteSpace(NameEntry.Text))
        {
            await DisplayAlertAsync("Required", "Please enter an item name.", "OK");
            NameEntry.Focus();
            return;
        }

        await this.SaveWithFeedbackAsync(async () =>
        {
            _item.Name = NameEntry.Text.Trim();
            _item.Description = DescriptionEditor.Text?.Trim();
            _item.Sku = SkuEntry.Text?.Trim();
            _item.PartNumber = PartNumberEntry.Text?.Trim();
            _item.Location = LocationEntry.Text?.Trim();
            _item.FilterSize = FilterSizeEntry.Text?.Trim();
            _item.RefrigerantType = RefrigerantEntry.Text?.Trim();
            _item.Supplier = SupplierEntry.Text?.Trim();

            if (decimal.TryParse(QuantityEntry.Text, out var qty))
                _item.QuantityOnHand = qty;

            if (decimal.TryParse(ReorderPointEntry.Text, out var reorder))
                _item.ReorderPoint = reorder;

            if (decimal.TryParse(CostEntry.Text, out var cost))
                _item.Cost = cost;

            if (decimal.TryParse(PriceEntry.Text, out var price))
                _item.Price = price;

            _item.Unit = UnitPicker.SelectedItem?.ToString() ?? "ea";

            _item.Category = CategoryPicker.SelectedIndex switch
            {
                0 => InventoryCategory.General,
                1 => InventoryCategory.Filters,
                2 => InventoryCategory.Coils,
                3 => InventoryCategory.Refrigerants,
                4 => InventoryCategory.Motors,
                5 => InventoryCategory.Thermostats,
                6 => InventoryCategory.Capacitors,
                7 => InventoryCategory.Contactors,
                8 => InventoryCategory.Ductwork,
                9 => InventoryCategory.Fittings,
                10 => InventoryCategory.Electrical,
                11 => InventoryCategory.Tools,
                12 => InventoryCategory.Consumables,
                _ => InventoryCategory.General
            };

            await _db.SaveChangesAsync();
        }, 
        successMessage: "Inventory item updated.");
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (_item == null) return;

        var confirm = await DisplayAlertAsync(
            "Delete Item",
            $"Are you sure you want to delete '{_item.Name}'?\n\nThis will mark the item as inactive.",
            "Delete", "Cancel");

        if (!confirm) return;

        try
        {
            _item.IsActive = false;
            await _db.SaveChangesAsync();

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to delete: {ex.Message}", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
