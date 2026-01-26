using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Mobile.Pages;

public partial class ProductListPage : ContentPage
{
    private readonly OneManVanDbContext _db;
    private List<Product> _allProducts = [];
    private string _searchText = string.Empty;
    private ProductCategory? _activeFilter;

    public ProductListPage(OneManVanDbContext db)
    {
        InitializeComponent();
        _db = db;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadProductsAsync();
    }

    private async Task LoadProductsAsync()
    {
        try
        {
            _db.ChangeTracker.Clear();

            // PERF: AsNoTracking for read-only list
            _allProducts = await _db.Products
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.Manufacturer)
                .ThenBy(p => p.ModelNumber)
                .ToListAsync();

            ApplyFilters();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load products: {ex.Message}", "OK");
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allProducts.AsEnumerable();

        // Apply search
        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            var search = _searchText.ToLowerInvariant();
            filtered = filtered.Where(p =>
                (p.Manufacturer?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.ModelNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.ProductName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        // Apply category filter
        if (_activeFilter.HasValue)
        {
            filtered = filtered.Where(p => p.Category == _activeFilter.Value);
        }

        var viewModels = filtered.Select(p => new ProductViewModel
        {
            Id = p.Id,
            Manufacturer = p.Manufacturer,
            ModelNumber = p.ModelNumber,
            ProductName = p.ProductName,
            Category = p.Category,
            CapacityDisplay = p.CapacityDisplay,
            EfficiencyDisplay = p.EfficiencyDisplay,
            RefrigerantDisplay = p.RefrigerantDisplay,
            SuggestedSellPrice = p.SuggestedSellPrice,
            WarrantyDisplay = p.WarrantyDisplay
        }).ToList();

        ProductCollection.ItemsSource = viewModels;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = e.NewTextValue ?? string.Empty;
        ApplyFilters();
    }

    private void OnFilterClicked(object sender, EventArgs e)
    {
        ResetFilterStyles();

        if (sender is Button button)
        {
            button.BackgroundColor = Color.FromArgb("#1976D2");
            button.TextColor = Colors.White;

            _activeFilter = button.Text switch
            {
                "AC" => ProductCategory.AirConditioner,
                "Furnace" => ProductCategory.GasFurnace,
                "Heat Pump" => ProductCategory.HeatPump,
                "Mini Split" => ProductCategory.MiniSplit,
                _ => null
            };
        }

        ApplyFilters();
    }

    private void ResetFilterStyles()
    {
        AllFilter.BackgroundColor = Color.FromArgb("#E3F2FD");
        AllFilter.TextColor = Color.FromArgb("#1976D2");
        AcFilter.BackgroundColor = Color.FromArgb("#E3F2FD");
        AcFilter.TextColor = Color.FromArgb("#1976D2");
        FurnaceFilter.BackgroundColor = Color.FromArgb("#FFF3E0");
        FurnaceFilter.TextColor = Color.FromArgb("#FF9800");
        HeatPumpFilter.BackgroundColor = Color.FromArgb("#E8F5E9");
        HeatPumpFilter.TextColor = Color.FromArgb("#4CAF50");
        MiniSplitFilter.BackgroundColor = Color.FromArgb("#F3E5F5");
        MiniSplitFilter.TextColor = Color.FromArgb("#9C27B0");
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadProductsAsync();
        RefreshViewControl.IsRefreshing = false;
    }

    private async void OnProductSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is ProductViewModel vm)
        {
            ProductCollection.SelectedItem = null;
            await Shell.Current.GoToAsync($"ProductDetail?id={vm.Id}");
        }
    }

    private async void OnAddProductClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AddProduct");
    }
}

public class ProductViewModel
{
    public int Id { get; set; }
    public string Manufacturer { get; set; } = string.Empty;
    public string ModelNumber { get; set; } = string.Empty;
    public string? ProductName { get; set; }
    public ProductCategory Category { get; set; }
    public string CapacityDisplay { get; set; } = string.Empty;
    public string EfficiencyDisplay { get; set; } = string.Empty;
    public string RefrigerantDisplay { get; set; } = string.Empty;
    public decimal? SuggestedSellPrice { get; set; }
    public string WarrantyDisplay { get; set; } = string.Empty;

    public bool HasEfficiency => !string.IsNullOrEmpty(EfficiencyDisplay);
    public bool HasPrice => SuggestedSellPrice.HasValue && SuggestedSellPrice > 0;

    public string CategoryIcon => Category switch
    {
        ProductCategory.AirConditioner => "AC",
        ProductCategory.HeatPump => "HP",
        ProductCategory.GasFurnace or ProductCategory.OilFurnace or ProductCategory.ElectricFurnace => "Furnace",
        ProductCategory.Boiler => "Boiler",
        ProductCategory.MiniSplit => "Mini",
        ProductCategory.PackagedUnit => "Package",
        ProductCategory.AirHandler => "AH",
        ProductCategory.Thermostat => "Thermo",
        _ => ""
    };

    public Color CategoryColor => Category switch
    {
        ProductCategory.AirConditioner => Color.FromArgb("#E3F2FD"),
        ProductCategory.HeatPump => Color.FromArgb("#E8F5E9"),
        ProductCategory.GasFurnace or ProductCategory.OilFurnace or ProductCategory.ElectricFurnace => Color.FromArgb("#FFF3E0"),
        ProductCategory.Boiler => Color.FromArgb("#FFEBEE"),
        ProductCategory.MiniSplit => Color.FromArgb("#F3E5F5"),
        ProductCategory.PackagedUnit => Color.FromArgb("#E0F7FA"),
        _ => Color.FromArgb("#F5F5F5")
    };
}
