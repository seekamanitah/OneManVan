using System.Windows;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Dialogs;

/// <summary>
/// Dialog for adding or editing a product in the catalog.
/// </summary>
public partial class AddEditProductDialog : Window
{
    private readonly Product? _existingProduct;
    public Product? SavedProduct { get; private set; }

    public AddEditProductDialog(Product? product = null)
    {
        InitializeComponent();
        _existingProduct = product;

        InitializeComboBoxes();

        if (product != null)
        {
            Title = "Edit Product";
            SaveButton.Content = "Save Changes";
            LoadProductData(product);
        }
        else
        {
            Title = "Add Product";
        }
    }

    private void InitializeComboBoxes()
    {
        // Category
        CategoryCombo.Items.Clear();
        foreach (ProductCategory category in Enum.GetValues<ProductCategory>())
        {
            CategoryCombo.Items.Add(new System.Windows.Controls.ComboBoxItem
            {
                Content = GetCategoryDisplayName(category),
                Tag = category
            });
        }
        CategoryCombo.SelectedIndex = 0;

        // Equipment Type
        EquipmentTypeCombo.Items.Clear();
        foreach (EquipmentType type in Enum.GetValues<EquipmentType>())
        {
            EquipmentTypeCombo.Items.Add(new System.Windows.Controls.ComboBoxItem
            {
                Content = GetEquipmentTypeDisplayName(type),
                Tag = type
            });
        }
        EquipmentTypeCombo.SelectedIndex = 0;

        // Refrigerant Type
        RefrigerantCombo.Items.Clear();
        foreach (RefrigerantType type in Enum.GetValues<RefrigerantType>())
        {
            RefrigerantCombo.Items.Add(new System.Windows.Controls.ComboBoxItem
            {
                Content = GetRefrigerantDisplayName(type),
                Tag = type
            });
        }
        RefrigerantCombo.SelectedIndex = 0;

        // Fuel Type
        FuelTypeCombo.Items.Clear();
        foreach (FuelType type in Enum.GetValues<FuelType>())
        {
            FuelTypeCombo.Items.Add(new System.Windows.Controls.ComboBoxItem
            {
                Content = type.ToString(),
                Tag = type
            });
        }
        FuelTypeCombo.SelectedIndex = 0;
    }

    private void LoadProductData(Product product)
    {
        // Basic Info
        ManufacturerBox.Text = product.Manufacturer;
        ModelNumberBox.Text = product.ModelNumber;
        ProductNameBox.Text = product.ProductName;
        SelectComboByTag(CategoryCombo, product.Category);
        SelectComboByTag(EquipmentTypeCombo, product.EquipmentType);

        // Capacity & Efficiency
        TonnageBox.Text = product.Tonnage?.ToString("0.#");
        CoolingBtuBox.Text = product.CoolingBtu?.ToString();
        HeatingBtuBox.Text = product.HeatingBtu?.ToString();
        SeerBox.Text = product.SeerRating?.ToString("0.#");
        Seer2Box.Text = product.Seer2Rating?.ToString("0.#");
        EerBox.Text = product.EerRating?.ToString("0.#");
        AfueBox.Text = product.AfueRating?.ToString("0.##");
        HspfBox.Text = product.HspfRating?.ToString("0.#");
        Hspf2Box.Text = product.Hspf2Rating?.ToString("0.#");

        // Refrigerant & Electrical
        SelectComboByTag(RefrigerantCombo, product.RefrigerantType);
        RefrigerantChargeBox.Text = product.RefrigerantChargeOz?.ToString("0.#");
        SelectComboByTag(FuelTypeCombo, product.FuelType);
        VoltageBox.Text = product.Voltage?.ToString();
        AmperageBox.Text = product.Amperage?.ToString("0.#");
        MinCircuitBox.Text = product.MinCircuitAmpacity?.ToString("0.#");

        // Pricing
        MsrpBox.Text = product.Msrp?.ToString("0.00");
        CostBox.Text = product.WholesaleCost?.ToString("0.00");
        SellPriceBox.Text = product.SuggestedSellPrice?.ToString("0.00");
        LaborHoursBox.Text = product.LaborHoursEstimate?.ToString("0.#");

        // Warranty
        PartsWarrantyBox.Text = product.PartsWarrantyYears?.ToString();
        CompressorWarrantyBox.Text = product.CompressorWarrantyYears?.ToString();
        HeatExchangerWarrantyBox.Text = product.HeatExchangerWarrantyYears?.ToString();
        LaborWarrantyBox.Text = product.LaborWarrantyYears?.ToString();
        RegistrationRequiredCheck.IsChecked = product.RegistrationRequired;

        // Physical
        HeightBox.Text = product.HeightIn?.ToString("0.#");
        WidthBox.Text = product.WidthIn?.ToString("0.#");
        DepthBox.Text = product.DepthIn?.ToString("0.#");
        WeightBox.Text = product.WeightLbs?.ToString("0.#");

        // Links & Notes
        ManufacturerUrlBox.Text = product.ManufacturerUrl;
        VideoUrlBox.Text = product.VideoUrl;
        FilterSizeBox.Text = product.FilterSize;
        TagsBox.Text = product.Tags;
        NotesBox.Text = product.Notes;

        // Status
        IsActiveCheck.IsChecked = product.IsActive;
        IsDiscontinuedCheck.IsChecked = product.IsDiscontinued;
    }

    private void SelectComboByTag(System.Windows.Controls.ComboBox combo, object tag)
    {
        for (int i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is System.Windows.Controls.ComboBoxItem item && 
                item.Tag?.Equals(tag) == true)
            {
                combo.SelectedIndex = i;
                return;
            }
        }
    }

    private T GetSelectedComboTag<T>(System.Windows.Controls.ComboBox combo, T defaultValue)
    {
        if (combo.SelectedItem is System.Windows.Controls.ComboBoxItem item && item.Tag is T value)
        {
            return value;
        }
        return defaultValue;
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(ManufacturerBox.Text))
        {
            ValidationMessage.Text = "Manufacturer is required.";
            ManufacturerBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(ModelNumberBox.Text))
        {
            ValidationMessage.Text = "Model Number is required.";
            ModelNumberBox.Focus();
            return;
        }

        try
        {
            Product product;
            if (_existingProduct != null)
            {
                product = await App.DbContext.Products.FindAsync(_existingProduct.Id) ?? new Product();
            }
            else
            {
                product = new Product();
                // Generate product number
                var maxNumber = App.DbContext.Products
                    .Select(p => p.ProductNumber)
                    .AsEnumerable()
                    .Where(n => n != null && n.StartsWith("P-"))
                    .Select(n => int.TryParse(n!.Substring(2), out var num) ? num : 0)
                    .DefaultIfEmpty(0)
                    .Max();
                product.ProductNumber = $"P-{maxNumber + 1:D4}";
                product.CreatedAt = DateTime.UtcNow;
            }

            // Basic Info
            product.Manufacturer = ManufacturerBox.Text.Trim();
            product.ModelNumber = ModelNumberBox.Text.Trim();
            product.ProductName = string.IsNullOrWhiteSpace(ProductNameBox.Text) ? null : ProductNameBox.Text.Trim();
            product.Category = GetSelectedComboTag(CategoryCombo, ProductCategory.Unknown);
            product.EquipmentType = GetSelectedComboTag(EquipmentTypeCombo, EquipmentType.Unknown);

            // Capacity & Efficiency
            product.TonnageX10 = TryParseDecimal(TonnageBox.Text, out var tonnage) ? (int)(tonnage * 10) : null;
            product.CoolingBtu = TryParseInt(CoolingBtuBox.Text);
            product.HeatingBtu = TryParseInt(HeatingBtuBox.Text);
            product.SeerRating = TryParseNullableDecimal(SeerBox.Text);
            product.Seer2Rating = TryParseNullableDecimal(Seer2Box.Text);
            product.EerRating = TryParseNullableDecimal(EerBox.Text);
            product.AfueRating = TryParseNullableDecimal(AfueBox.Text);
            product.HspfRating = TryParseNullableDecimal(HspfBox.Text);
            product.Hspf2Rating = TryParseNullableDecimal(Hspf2Box.Text);

            // Refrigerant & Electrical
            product.RefrigerantType = GetSelectedComboTag(RefrigerantCombo, RefrigerantType.Unknown);
            product.RefrigerantChargeOz = TryParseNullableDecimal(RefrigerantChargeBox.Text);
            product.FuelType = GetSelectedComboTag(FuelTypeCombo, FuelType.Unknown);
            product.Voltage = TryParseInt(VoltageBox.Text);
            product.Amperage = TryParseNullableDecimal(AmperageBox.Text);
            product.MinCircuitAmpacity = TryParseNullableDecimal(MinCircuitBox.Text);

            // Pricing
            product.Msrp = TryParseNullableDecimal(MsrpBox.Text);
            product.WholesaleCost = TryParseNullableDecimal(CostBox.Text);
            product.SuggestedSellPrice = TryParseNullableDecimal(SellPriceBox.Text);
            product.LaborHoursEstimate = TryParseNullableDecimal(LaborHoursBox.Text);

            // Warranty
            product.PartsWarrantyYears = TryParseInt(PartsWarrantyBox.Text);
            product.CompressorWarrantyYears = TryParseInt(CompressorWarrantyBox.Text);
            product.HeatExchangerWarrantyYears = TryParseInt(HeatExchangerWarrantyBox.Text);
            product.LaborWarrantyYears = TryParseInt(LaborWarrantyBox.Text);
            product.RegistrationRequired = RegistrationRequiredCheck.IsChecked == true;

            // Physical
            product.HeightIn = TryParseNullableDecimal(HeightBox.Text);
            product.WidthIn = TryParseNullableDecimal(WidthBox.Text);
            product.DepthIn = TryParseNullableDecimal(DepthBox.Text);
            product.WeightLbs = TryParseNullableDecimal(WeightBox.Text);

            // Links & Notes
            product.ManufacturerUrl = string.IsNullOrWhiteSpace(ManufacturerUrlBox.Text) ? null : ManufacturerUrlBox.Text.Trim();
            product.VideoUrl = string.IsNullOrWhiteSpace(VideoUrlBox.Text) ? null : VideoUrlBox.Text.Trim();
            product.FilterSize = string.IsNullOrWhiteSpace(FilterSizeBox.Text) ? null : FilterSizeBox.Text.Trim();
            product.Tags = string.IsNullOrWhiteSpace(TagsBox.Text) ? null : TagsBox.Text.Trim();
            product.Notes = string.IsNullOrWhiteSpace(NotesBox.Text) ? null : NotesBox.Text.Trim();

            // Status
            product.IsActive = IsActiveCheck.IsChecked == true;
            product.IsDiscontinued = IsDiscontinuedCheck.IsChecked == true;
            product.UpdatedAt = DateTime.UtcNow;

            if (_existingProduct == null)
            {
                App.DbContext.Products.Add(product);
            }

            await App.DbContext.SaveChangesAsync();

            SavedProduct = product;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ValidationMessage.Text = $"Error saving product: {ex.Message}";
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

    private static string GetCategoryDisplayName(ProductCategory category) => category switch
    {
        ProductCategory.AirConditioner => "Air Conditioner",
        ProductCategory.HeatPump => "Heat Pump",
        ProductCategory.GasFurnace => "Gas Furnace",
        ProductCategory.OilFurnace => "Oil Furnace",
        ProductCategory.ElectricFurnace => "Electric Furnace",
        ProductCategory.Boiler => "Boiler",
        ProductCategory.MiniSplit => "Mini Split",
        ProductCategory.PackagedUnit => "Packaged Unit",
        ProductCategory.AirHandler => "Air Handler",
        ProductCategory.Coil => "Coil",
        ProductCategory.Thermostat => "Thermostat",
        ProductCategory.AirPurifier => "Air Purifier",
        ProductCategory.Humidifier => "Humidifier",
        ProductCategory.Dehumidifier => "Dehumidifier",
        ProductCategory.Accessories => "Accessories",
        _ => "Unknown"
    };

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
