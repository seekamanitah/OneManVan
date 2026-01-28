using System;
using System.Windows;
using OneManVan.Shared.Models;

namespace OneManVan.Dialogs;

/// <summary>
/// Dialog for adding a custom line item to an invoice.
/// </summary>
public partial class AddCustomLineItemDialog : Window
{
    public InvoiceLineItem? LineItem { get; private set; }

    public AddCustomLineItemDialog()
    {
        InitializeComponent();
        // Initialize with default values
        QuantityTextBox.Text = "1";
        UnitPriceTextBox.Text = "0.00";
        UpdateTotal();
    }

    private void OnQuantityOrPriceChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        UpdateTotal();
    }

    private void UpdateTotal()
    {
        // Add null checks to prevent crash during initialization
        if (QuantityTextBox == null || UnitPriceTextBox == null || TotalText == null)
            return;
            
        if (decimal.TryParse(QuantityTextBox.Text, out var quantity) &&
            decimal.TryParse(UnitPriceTextBox.Text, out var unitPrice))
        {
            var total = quantity * unitPrice;
            TotalText.Text = $"${total:N2}";
        }
        else
        {
            TotalText.Text = "$0.00";
        }
    }

    private void OnAddClick(object sender, RoutedEventArgs e)
    {
        if (!Validate())
            return;

        LineItem = new InvoiceLineItem
        {
            Source = LineItemSource.Custom,
            Description = DescriptionTextBox.Text.Trim(),
            Quantity = decimal.Parse(QuantityTextBox.Text),
            UnitPrice = decimal.Parse(UnitPriceTextBox.Text),
            Notes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim()
        };

        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private bool Validate()
    {
        if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
        {
            MessageBox.Show("Description is required", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            DescriptionTextBox.Focus();
            return false;
        }

        if (!decimal.TryParse(QuantityTextBox.Text, out var quantity) || quantity <= 0)
        {
            MessageBox.Show("Quantity must be a positive number", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            QuantityTextBox.Focus();
            return false;
        }

        if (!decimal.TryParse(UnitPriceTextBox.Text, out var unitPrice) || unitPrice < 0)
        {
            MessageBox.Show("Unit Price must be a valid number", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            UnitPriceTextBox.Focus();
            return false;
        }

        return true;
    }
}
