using System;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Controls;

public partial class InventoryFormContent : UserControl
{
    public InventoryFormContent()
    {
        InitializeComponent();
        
        // Populate Category combo
        CategoryCombo.ItemsSource = Enum.GetValues(typeof(InventoryCategory))
            .Cast<InventoryCategory>()
            .Select(c => new ComboBoxItem { Content = c.ToString(), Tag = c })
            .ToList();
        CategoryCombo.SelectedIndex = 0;
    }

    public InventoryItem GetInventoryItem()
    {
        var selectedCategory = (CategoryCombo.SelectedItem as ComboBoxItem)?.Tag as InventoryCategory? ?? InventoryCategory.General;
        
        return new InventoryItem
        {
            Name = NameTextBox.Text,
            Sku = SkuTextBox.Text,
            Category = selectedCategory,
            QuantityOnHand = decimal.TryParse(QuantityTextBox.Text, out var qty) ? qty : 0,
            Cost = decimal.TryParse(CostTextBox.Text, out var cost) ? cost : 0,
            Price = decimal.TryParse(PriceTextBox.Text, out var price) ? price : 0,
            Location = LocationTextBox.Text,
            Description = DescriptionTextBox.Text,
            IsActive = true
        };
    }

    public void LoadInventoryItem(InventoryItem item)
    {
        NameTextBox.Text = item.Name;
        SkuTextBox.Text = item.Sku;
        QuantityTextBox.Text = item.QuantityOnHand.ToString("F2");
        CostTextBox.Text = item.Cost.ToString("F2");
        PriceTextBox.Text = item.Price.ToString("F2");
        LocationTextBox.Text = item.Location;
        DescriptionTextBox.Text = item.Description;
        
        // Select category
        var comboItem = CategoryCombo.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(i => (InventoryCategory)i.Tag == item.Category);
        if (comboItem != null)
        {
            CategoryCombo.SelectedItem = comboItem;
        }
    }

    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            return false;
        }
        return true;
    }
}
