using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Dialogs;

/// <summary>
/// Dialog for adding or editing an asset/equipment.
/// </summary>
public partial class AddEditAssetDialog : Window
{
    private readonly Asset? _existingAsset;
    private readonly int? _preselectedCustomerId;
    public Asset? SavedAsset { get; private set; }

    public AddEditAssetDialog(Asset? asset = null, int? customerId = null)
    {
        InitializeComponent();
        _existingAsset = asset;
        _preselectedCustomerId = customerId ?? asset?.CustomerId;

        InitializeComboBoxes();
        LoadCustomers();

        if (asset != null)
        {
            Title = "Edit Asset";
            SaveButton.Content = "Save Changes";
            LoadAssetData(asset);
        }
        else
        {
            Title = "Add Asset";
        }
    }

    private void InitializeComboBoxes()
    {
        // Equipment Type
        EquipmentTypeCombo.Items.Clear();
        foreach (EquipmentType type in Enum.GetValues<EquipmentType>())
        {
            EquipmentTypeCombo.Items.Add(new ComboBoxItem
            {
                Content = GetEquipmentTypeDisplayName(type),
                Tag = type
            });
        }
        EquipmentTypeCombo.SelectedIndex = 0;

        // Fuel Type
        FuelTypeCombo.Items.Clear();
        foreach (FuelType type in Enum.GetValues<FuelType>())
        {
            FuelTypeCombo.Items.Add(new ComboBoxItem
            {
                Content = type.ToString(),
                Tag = type
            });
        }
        FuelTypeCombo.SelectedIndex = 0;

        // Unit Config
        UnitConfigCombo.Items.Clear();
        foreach (UnitConfig config in Enum.GetValues<UnitConfig>())
        {
            UnitConfigCombo.Items.Add(new ComboBoxItem
            {
                Content = GetUnitConfigDisplayName(config),
                Tag = config
            });
        }
        UnitConfigCombo.SelectedIndex = 0;

        // Refrigerant Type
        RefrigerantCombo.Items.Clear();
        foreach (RefrigerantType type in Enum.GetValues<RefrigerantType>())
        {
            RefrigerantCombo.Items.Add(new ComboBoxItem
            {
                Content = GetRefrigerantDisplayName(type),
                Tag = type
            });
        }
        RefrigerantCombo.SelectedIndex = 0;

        // Filter Type
        FilterTypeCombo.Items.Clear();
        foreach (FilterType type in Enum.GetValues<FilterType>())
        {
            FilterTypeCombo.Items.Add(new ComboBoxItem
            {
                Content = type.ToString(),
                Tag = type
            });
        }
        FilterTypeCombo.SelectedIndex = 0;

        // Common Filter Sizes
        FilterSizeCombo.Items.Clear();
        FilterSizeCombo.Items.Add(new ComboBoxItem { Content = "" });
        FilterSizeCombo.Items.Add(new ComboBoxItem { Content = "16x20x1" });
        FilterSizeCombo.Items.Add(new ComboBoxItem { Content = "16x25x1" });
        FilterSizeCombo.Items.Add(new ComboBoxItem { Content = "20x20x1" });
        FilterSizeCombo.Items.Add(new ComboBoxItem { Content = "20x25x1" });
        FilterSizeCombo.Items.Add(new ComboBoxItem { Content = "20x25x4" });
        FilterSizeCombo.Items.Add(new ComboBoxItem { Content = "16x25x4" });
        FilterSizeCombo.Items.Add(new ComboBoxItem { Content = "20x20x4" });
        FilterSizeCombo.SelectedIndex = 0;

        // Common Brands
        BrandCombo.Items.Clear();
        BrandCombo.Items.Add(new ComboBoxItem { Content = "" });
        BrandCombo.Items.Add(new ComboBoxItem { Content = "Carrier" });
        BrandCombo.Items.Add(new ComboBoxItem { Content = "Trane" });
        BrandCombo.Items.Add(new ComboBoxItem { Content = "Lennox" });
        BrandCombo.Items.Add(new ComboBoxItem { Content = "Rheem" });
        BrandCombo.Items.Add(new ComboBoxItem { Content = "Goodman" });
        BrandCombo.Items.Add(new ComboBoxItem { Content = "York" });
        BrandCombo.Items.Add(new ComboBoxItem { Content = "Bryant" });
        BrandCombo.Items.Add(new ComboBoxItem { Content = "American Standard" });
        BrandCombo.Items.Add(new ComboBoxItem { Content = "Amana" });
        BrandCombo.Items.Add(new ComboBoxItem { Content = "Daikin" });
        BrandCombo.Items.Add(new ComboBoxItem { Content = "Mitsubishi" });
        BrandCombo.SelectedIndex = 0;

        // Status
        StatusCombo.Items.Clear();
        foreach (AssetStatus status in Enum.GetValues<AssetStatus>())
        {
            StatusCombo.Items.Add(new ComboBoxItem
            {
                Content = status.ToString(),
                Tag = status
            });
        }
        SelectComboByTag(StatusCombo, AssetStatus.Active);

        // Condition
        ConditionCombo.Items.Clear();
        foreach (AssetCondition condition in Enum.GetValues<AssetCondition>())
        {
            ConditionCombo.Items.Add(new ComboBoxItem
            {
                Content = condition.ToString(),
                Tag = condition
            });
        }
        SelectComboByTag(ConditionCombo, AssetCondition.Good);
    }

    private async void LoadCustomers()
    {
        var customers = await App.DbContext.Customers
            .Where(c => c.Status != CustomerStatus.DoNotService && c.Status != CustomerStatus.Archived)
            .OrderBy(c => c.Name)
            .ToListAsync();

        CustomerCombo.ItemsSource = customers;

        if (_preselectedCustomerId.HasValue)
        {
            var selectedCustomer = customers.FirstOrDefault(c => c.Id == _preselectedCustomerId);
            if (selectedCustomer != null)
            {
                CustomerCombo.SelectedItem = selectedCustomer;
            }
        }
    }

    private async void CustomerCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CustomerCombo.SelectedItem is Customer customer)
        {
            var sites = await App.DbContext.Sites
                .Where(s => s.CustomerId == customer.Id)
                .OrderBy(s => s.IsPrimary ? 0 : 1)
                .ThenBy(s => s.Address)
                .ToListAsync();

            SiteCombo.ItemsSource = sites;
            if (sites.Any())
            {
                SiteCombo.SelectedIndex = 0;
            }
        }
        else
        {
            SiteCombo.ItemsSource = null;
        }
    }

    private void LoadAssetData(Asset asset)
    {
        // Customer & Site will be set after customers load
        SerialTextBox.Text = asset.Serial;
        AssetTagTextBox.Text = asset.AssetTag;
        BrandCombo.Text = asset.Brand ?? "";
        ModelTextBox.Text = asset.Model;
        NicknameTextBox.Text = asset.Nickname;

        SelectComboByTag(EquipmentTypeCombo, asset.EquipmentType);
        SelectComboByTag(FuelTypeCombo, asset.FuelType);
        SelectComboByTag(UnitConfigCombo, asset.UnitConfig);

        BtuTextBox.Text = asset.BtuRating?.ToString();
        TonnageTextBox.Text = asset.Tonnage?.ToString("0.#");
        SeerTextBox.Text = asset.SeerRating?.ToString("0.#");
        AfueTextBox.Text = asset.AfueRating?.ToString("0.##");
        HspfTextBox.Text = asset.HspfRating?.ToString("0.#");

        SelectComboByTag(RefrigerantCombo, asset.RefrigerantType);
        RefrigerantChargeTextBox.Text = asset.RefrigerantChargeOz?.ToString("0.#");

        InstallDatePicker.SelectedDate = asset.InstallDate;
        WarrantyStartPicker.SelectedDate = asset.WarrantyStartDate;
        WarrantyYearsTextBox.Text = asset.WarrantyTermYears.ToString();

        FilterSizeCombo.Text = asset.FilterSize ?? "";
        SelectComboByTag(FilterTypeCombo, asset.FilterType);

        NotesTextBox.Text = asset.Notes;

        SelectComboByTag(StatusCombo, asset.Status);
        SelectComboByTag(ConditionCombo, asset.Condition);
    }

    private void SelectComboByTag(ComboBox combo, object tag)
    {
        for (int i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is ComboBoxItem item && item.Tag?.Equals(tag) == true)
            {
                combo.SelectedIndex = i;
                return;
            }
        }
    }

    private T GetSelectedComboTag<T>(ComboBox combo, T defaultValue)
    {
        if (combo.SelectedItem is ComboBoxItem item && item.Tag is T value)
        {
            return value;
        }
        return defaultValue;
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        // Validate required fields
        if (CustomerCombo.SelectedItem is not Customer selectedCustomer)
        {
            ValidationMessage.Text = "Please select a customer.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SerialTextBox.Text))
        {
            ValidationMessage.Text = "Serial number is required.";
            SerialTextBox.Focus();
            return;
        }

        try
        {
            Asset asset;
            if (_existingAsset != null)
            {
                asset = await App.DbContext.Assets.FindAsync(_existingAsset.Id) ?? new Asset();
            }
            else
            {
                asset = new Asset();
                asset.CreatedAt = DateTime.UtcNow;
            }

            // Customer & Site
            asset.CustomerId = selectedCustomer.Id;
            asset.SiteId = (SiteCombo.SelectedItem as Site)?.Id;

            // Identification
            asset.Serial = SerialTextBox.Text.Trim();
            asset.AssetTag = string.IsNullOrWhiteSpace(AssetTagTextBox.Text) ? null : AssetTagTextBox.Text.Trim();
            asset.Brand = string.IsNullOrWhiteSpace(BrandCombo.Text) ? null : BrandCombo.Text.Trim();
            asset.Model = string.IsNullOrWhiteSpace(ModelTextBox.Text) ? null : ModelTextBox.Text.Trim();
            asset.Nickname = string.IsNullOrWhiteSpace(NicknameTextBox.Text) ? null : NicknameTextBox.Text.Trim();

            // Equipment Type
            asset.EquipmentType = GetSelectedComboTag(EquipmentTypeCombo, EquipmentType.Unknown);
            asset.FuelType = GetSelectedComboTag(FuelTypeCombo, FuelType.Unknown);
            asset.UnitConfig = GetSelectedComboTag(UnitConfigCombo, UnitConfig.Unknown);

            // Capacity & Efficiency
            asset.BtuRating = TryParseInt(BtuTextBox.Text);
            asset.TonnageX10 = TryParseDecimal(TonnageTextBox.Text, out var tonnage) ? (int)(tonnage * 10) : null;
            asset.SeerRating = TryParseNullableDecimal(SeerTextBox.Text);
            asset.AfueRating = TryParseNullableDecimal(AfueTextBox.Text);
            asset.HspfRating = TryParseNullableDecimal(HspfTextBox.Text);

            // Refrigerant
            asset.RefrigerantType = GetSelectedComboTag(RefrigerantCombo, RefrigerantType.Unknown);
            asset.RefrigerantChargeOz = TryParseNullableDecimal(RefrigerantChargeTextBox.Text);

            // Installation & Warranty
            asset.InstallDate = InstallDatePicker.SelectedDate;
            asset.WarrantyStartDate = WarrantyStartPicker.SelectedDate;
            asset.WarrantyTermYears = TryParseInt(WarrantyYearsTextBox.Text) ?? 10;

            // Filter
            asset.FilterSize = string.IsNullOrWhiteSpace(FilterSizeCombo.Text) ? null : FilterSizeCombo.Text.Trim();
            asset.FilterType = GetSelectedComboTag(FilterTypeCombo, FilterType.Unknown);

            // Notes
            asset.Notes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim();

            // Status
            asset.Status = GetSelectedComboTag(StatusCombo, AssetStatus.Active);
            asset.Condition = GetSelectedComboTag(ConditionCombo, AssetCondition.Good);

            if (_existingAsset == null)
            {
                App.DbContext.Assets.Add(asset);
            }

            await App.DbContext.SaveChangesAsync();

            SavedAsset = asset;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ValidationMessage.Text = $"Error saving asset: {ex.Message}";
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private static int? TryParseInt(string text)
    {
        return int.TryParse(text, out var result) ? result : null;
    }

    private static bool TryParseDecimal(string text, out decimal result)
    {
        return decimal.TryParse(text, out result);
    }

    private static decimal? TryParseNullableDecimal(string text)
    {
        return decimal.TryParse(text, out var result) ? result : null;
    }

    private static string GetEquipmentTypeDisplayName(EquipmentType type) => type switch
    {
        EquipmentType.GasFurnace => "Gas Furnace",
        EquipmentType.OilFurnace => "Oil Furnace",
        EquipmentType.ElectricFurnace => "Electric Furnace",
        EquipmentType.Boiler => "Boiler",
        EquipmentType.AirConditioner => "Air Conditioner",
        EquipmentType.HeatPump => "Heat Pump",
        EquipmentType.MiniSplit => "Mini Split",
        EquipmentType.DuctlessMiniSplit => "Ductless Mini Split",
        EquipmentType.PackagedUnit => "Packaged Unit",
        EquipmentType.RooftopUnit => "Rooftop Unit",
        EquipmentType.Coil => "Coil",
        EquipmentType.Condenser => "Condenser",
        EquipmentType.AirHandler => "Air Handler",
        EquipmentType.Humidifier => "Humidifier",
        EquipmentType.Dehumidifier => "Dehumidifier",
        EquipmentType.AirPurifier => "Air Purifier",
        EquipmentType.UVLight => "UV Light",
        EquipmentType.Thermostat => "Thermostat",
        _ => "Unknown"
    };

    private static string GetUnitConfigDisplayName(UnitConfig config) => config switch
    {
        UnitConfig.Split => "Split System",
        UnitConfig.Packaged => "Packaged Unit",
        UnitConfig.MiniSplit => "Ductless Mini-Split",
        UnitConfig.Furnace => "Furnace",
        UnitConfig.Coil => "Coil",
        UnitConfig.Condenser => "Condenser",
        UnitConfig.HeatPump => "Heat Pump",
        UnitConfig.Boiler => "Boiler",
        _ => "Unknown"
    };

    private static string GetRefrigerantDisplayName(RefrigerantType type) => type switch
    {
        RefrigerantType.R22 => "R-22 (Legacy)",
        RefrigerantType.R410A => "R-410A",
        RefrigerantType.R407C => "R-407C",
        RefrigerantType.R134a => "R-134a",
        RefrigerantType.R32 => "R-32",
        RefrigerantType.R454B => "R-454B",
        RefrigerantType.R452B => "R-452B",
        RefrigerantType.R290 => "R-290 (Propane)",
        _ => "Unknown"
    };
}
