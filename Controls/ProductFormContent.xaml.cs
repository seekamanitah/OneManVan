using System;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Controls;

public partial class ProductFormContent : UserControl
{
    public ProductFormContent()
    {
        InitializeComponent();
        
        // Populate Category combo
        CategoryCombo.ItemsSource = Enum.GetValues(typeof(ProductCategory))
            .Cast<ProductCategory>()
            .Select(c => new ComboBoxItem { Content = c.ToString(), Tag = c })
            .ToList();
        CategoryCombo.SelectedIndex = 0;
    }

    public Product GetProduct()
    {
        var selectedCategory = (CategoryCombo.SelectedItem as ComboBoxItem)?.Tag as ProductCategory? ?? ProductCategory.Unknown;
        
        return new Product
        {
            Manufacturer = ManufacturerTextBox.Text,
            ModelNumber = ModelNumberTextBox.Text,
            ProductName = ProductNameTextBox.Text,
            Category = selectedCategory,
            WholesaleCost = decimal.TryParse(WholesaleCostTextBox.Text, out var cost) ? cost : null,
            SuggestedSellPrice = decimal.TryParse(SuggestedSellPriceTextBox.Text, out var price) ? price : null,
            Description = DescriptionTextBox.Text
        };
    }

    public void LoadProduct(Product product)
    {
        ManufacturerTextBox.Text = product.Manufacturer;
        ModelNumberTextBox.Text = product.ModelNumber;
        ProductNameTextBox.Text = product.ProductName;
        WholesaleCostTextBox.Text = product.WholesaleCost?.ToString("F2") ?? "";
        SuggestedSellPriceTextBox.Text = product.SuggestedSellPrice?.ToString("F2") ?? "";
        DescriptionTextBox.Text = product.Description;
        
        // Select category
        var item = CategoryCombo.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(i => (ProductCategory)i.Tag == product.Category);
        if (item != null)
        {
            CategoryCombo.SelectedItem = item;
        }
    }

    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(ManufacturerTextBox.Text))
        {
            return false;
        }
        if (string.IsNullOrWhiteSpace(ModelNumberTextBox.Text))
        {
            return false;
        }
        return true;
    }
}
