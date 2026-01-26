using Microsoft.EntityFrameworkCore;
using OneManVan.Mobile.Constants;
using OneManVan.Mobile.Helpers;
using OneManVan.Mobile.Services;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;
using System.Collections.ObjectModel;

namespace OneManVan.Mobile.Pages;

/// <summary>
/// Full-form page for creating a new invoice with all details on one screen.
/// Now uses CustomerSelectionHelper for cleaner code.
/// </summary>
public partial class AddInvoicePage
{
    private readonly OneManVanDbContext _db;
    private readonly CustomerSelectionHelper _customerHelper;
    private readonly LineItemDialogService _lineItemDialog;
    private ObservableCollection<InvoiceLineItem> _lineItems = [];

    public AddInvoicePage(
        OneManVanDbContext db, 
        CustomerPickerService customerPicker,
        LineItemDialogService lineItemDialog)
    {
        InitializeComponent();
        _db = db;
        _customerHelper = new CustomerSelectionHelper(this, customerPicker);
        _lineItemDialog = lineItemDialog;
        InitializeDefaults();
    }

    private void InitializeDefaults()
    {
        InvoiceDatePicker.Date = DateTime.Today;
        DueDatePicker.Date = DateTime.Today.AddDays(BusinessDefaults.DefaultInvoicePaymentTermDays);
        StatusPicker.SelectedIndex = 0; // Draft
        
        // Load tax rate from settings
        TaxRateEntry.Text = Preferences.Get("TaxRate", BusinessDefaults.DefaultTaxRate.ToString("F1"));
        
        LineItemsCollection.ItemsSource = _lineItems;
        UpdateTotals();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _customerHelper.HandleQuickAddCustomerAsync(OnCustomerSelectedAsync);
    }

    private async void OnSelectCustomerTapped(object sender, TappedEventArgs e)
    {
        await _customerHelper.HandleCustomerSelectionAsync(OnCustomerSelectedAsync);
    }

    private async Task OnCustomerSelectedAsync(Customer customer)
    {
        // Update UI using helper
        CustomerSelectionHelper.UpdateCustomerUI(customer, CustomerNameLabel);
        CustomerNameLabel.TextColor = Color.FromArgb("#333333");

        // Show billing info
        BillingInfoPanel.IsVisible = true;
        CustomerEmailLabel.Text = $"Email: {customer.BillingEmail ?? customer.Email ?? "Not provided"}";
        
        var paymentTerms = customer.PaymentTerms switch
        {
            PaymentTerms.COD => "COD",
            PaymentTerms.DueOnReceipt => "Due on Receipt",
            PaymentTerms.Net15 => "Net 15",
            PaymentTerms.Net30 => "Net 30",
            PaymentTerms.Net45 => "Net 45",
            PaymentTerms.Net60 => "Net 60",
            _ => "Due on Receipt"
        };
        PaymentTermsLabel.Text = $"Terms: {paymentTerms}";

        // Adjust due date based on payment terms
        DueDatePicker.Date = _customerHelper.SelectedCustomer.PaymentTerms switch
        {
            PaymentTerms.COD => DateTime.Today,
            PaymentTerms.DueOnReceipt => DateTime.Today,
            PaymentTerms.Net15 => DateTime.Today.AddDays(15),
            PaymentTerms.Net30 => DateTime.Today.AddDays(30),
            PaymentTerms.Net45 => DateTime.Today.AddDays(45),
            PaymentTerms.Net60 => DateTime.Today.AddDays(60),
            _ => DateTime.Today.AddDays(30)
        };
    }

    private async void OnAddLineItemClicked(object sender, EventArgs e)
    {
        // First ask what type of item to add
        var itemType = await DisplayActionSheetAsync(
            "Add Line Item",
            "Cancel",
            null,
            "From Inventory",
            "From Product Catalog", 
            "Manual Entry");

        if (itemType == "Cancel" || string.IsNullOrWhiteSpace(itemType))
            return;

        InvoiceLineItem? lineItem = null;

        switch (itemType)
        {
            case "From Inventory":
                lineItem = await AddFromInventoryAsync();
                break;
            case "From Product Catalog":
                lineItem = await AddFromProductCatalogAsync();
                break;
            case "Manual Entry":
                lineItem = await AddManualEntryAsync();
                break;
        }

        if (lineItem != null)
        {
            _lineItems.Add(lineItem);
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            UpdateTotals();
        }
    }

    private async Task<InvoiceLineItem?> AddFromInventoryAsync()
    {
        try
        {
            // Get active inventory items with stock
            var inventoryItems = await _db.InventoryItems
                .Where(i => i.IsActive && i.QuantityOnHand > 0)
                .OrderBy(i => i.Name)
                .ToListAsync();

            if (!inventoryItems.Any())
            {
                await DisplayAlertAsync("No Inventory", "No inventory items available.", "OK");
                return null;
            }

            var itemOptions = inventoryItems.Select(i => $"{i.Name} (${i.Price:N2}) - {i.QuantityOnHand} in stock").ToArray();
            var selected = await DisplayActionSheetAsync("Select Inventory Item", "Cancel", null, itemOptions);

            if (selected == "Cancel" || string.IsNullOrWhiteSpace(selected))
                return null;

            var selectedIndex = Array.IndexOf(itemOptions, selected);
            var selectedItem = inventoryItems[selectedIndex];

            // Ask for quantity
            var maxQty = Math.Floor(selectedItem.QuantityOnHand);
            var qtyStr = await DisplayPromptAsync(
                "Quantity", 
                $"Enter quantity (max: {maxQty}):", 
                "Next", 
                "Cancel", 
                "1", 
                keyboard: Keyboard.Numeric);

            if (string.IsNullOrWhiteSpace(qtyStr))
                return null;

            if (!decimal.TryParse(qtyStr, out var qty) || qty <= 0 || qty > selectedItem.QuantityOnHand)
            {
                await DisplayAlertAsync("Invalid Quantity", $"Please enter a quantity between 1 and {selectedItem.QuantityOnHand}.", "OK");
                return null;
            }

            return new InvoiceLineItem
            {
                Description = selectedItem.Name,
                Quantity = qty,
                UnitPrice = selectedItem.Price,
                InventoryItemId = selectedItem.Id
            };
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load inventory: {ex.Message}", "OK");
            return null;
        }
    }

    private async Task<InvoiceLineItem?> AddFromProductCatalogAsync()
    {
        try
        {
            // Get active products
            var products = await _db.Products
                .Where(p => p.IsActive && !p.IsDiscontinued)
                .OrderBy(p => p.DisplayName)
                .ToListAsync();

            if (!products.Any())
            {
                await DisplayAlertAsync("No Products", "No products available in catalog.", "OK");
                return null;
            }

            var productOptions = products.Select(p => $"{p.DisplayName} (${p.SuggestedSellPrice:N2})").ToArray();
            var selected = await DisplayActionSheetAsync("Select Product", "Cancel", null, productOptions);

            if (selected == "Cancel" || string.IsNullOrWhiteSpace(selected))
                return null;

            var selectedIndex = Array.IndexOf(productOptions, selected);
            var selectedProduct = products[selectedIndex];

            // Ask for quantity
            var qtyStr = await DisplayPromptAsync(
                "Quantity", 
                "Enter quantity:", 
                "Add", 
                "Cancel", 
                "1", 
                keyboard: Keyboard.Numeric);

            if (string.IsNullOrWhiteSpace(qtyStr))
                return null;

            if (!decimal.TryParse(qtyStr, out var qty) || qty <= 0)
            {
                await DisplayAlertAsync("Invalid Quantity", "Please enter a valid quantity.", "OK");
                return null;
            }

            return new InvoiceLineItem
            {
                Description = selectedProduct.DisplayName,
                Quantity = qty,
                UnitPrice = selectedProduct.SuggestedSellPrice ?? 0,
                ProductId = selectedProduct.Id
            };
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load products: {ex.Message}", "OK");
            return null;
        }
    }

    private async Task<InvoiceLineItem?> AddManualEntryAsync()
    {
        // Use LineItemDialogService for consistent UX
        var input = await _lineItemDialog.GetLineItemInputAsync(this);
        
        if (input == null)
            return null;

        return new InvoiceLineItem
        {
            Description = input.Description,
            Quantity = input.Quantity,
            UnitPrice = input.UnitPrice
        };
    }

    private void OnRemoveLineItemClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is InvoiceLineItem item)
        {
            _lineItems.Remove(item);
            UpdateTotals();
        }
    }

    private void OnTaxRateChanged(object sender, TextChangedEventArgs e)
    {
        UpdateTotals();
    }

    private void UpdateTotals()
    {
        var subtotal = _lineItems.Sum(i => i.Total);
        SubtotalLabel.Text = $"${subtotal:N2}";

        if (!decimal.TryParse(TaxRateEntry.Text, out var taxRate))
            taxRate = 0;

        var taxAmount = subtotal * (taxRate / 100);
        TaxAmountLabel.Text = $"${taxAmount:N2}";

        var total = subtotal + taxAmount;
        TotalLabel.Text = $"${total:N2}";
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // Validation
        if (_customerHelper.SelectedCustomer == null)
        {
            await DisplayAlertAsync("Required", "Please select a customer.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(TitleEntry.Text))
        {
            await DisplayAlertAsync("Required", "Please enter an invoice title.", "OK");
            TitleEntry.Focus();
            return;
        }

        try
        {
            // Calculate totals
            var subtotal = _lineItems.Sum(i => i.Total);
            if (!decimal.TryParse(TaxRateEntry.Text, out var taxRate))
                taxRate = 0;
            var taxAmount = subtotal * (taxRate / 100);
            var total = subtotal + taxAmount;

            // Generate invoice number
            var lastInvoice = await _db.Invoices.OrderByDescending(i => i.Id).FirstOrDefaultAsync();
            var lastNumber = lastInvoice?.Id ?? 0;
            var invoiceNumber = $"INV-{DateTime.UtcNow:yyyy}-{(lastNumber + 1):D4}";

            // Parse status
            var status = StatusPicker.SelectedIndex switch
            {
                0 => InvoiceStatus.Draft,
                1 => InvoiceStatus.Sent,
                _ => InvoiceStatus.Draft
            };

            var invoice = new Invoice
            {
                InvoiceNumber = invoiceNumber,
                CustomerId = _customerHelper.SelectedCustomer.Id,
                Status = status,
                InvoiceDate = InvoiceDatePicker.Date ?? DateTime.Today,
                DueDate = DueDatePicker.Date ?? DateTime.Today.AddDays(30),
                // Put all line items into OtherAmount for simplicity
                OtherAmount = subtotal,
                SubTotal = subtotal,
                TaxRate = taxRate,
                TaxAmount = taxAmount,
                Total = total,
                Notes = $"{TitleEntry.Text.Trim()}\n\n{NotesEditor.Text?.Trim()}",
                CreatedAt = DateTime.UtcNow
            };

            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync();

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

            await DisplayAlertAsync("Success", $"Invoice {invoiceNumber} created for {_customerHelper.SelectedCustomer.Name}!", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to create invoice: {ex.Message}", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}

/// <summary>
/// View model for invoice line items.
/// </summary>
public class InvoiceLineItem
{
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal Total => Quantity * UnitPrice;
    
    // Optional references to source items
    public int? InventoryItemId { get; set; }
    public int? ProductId { get; set; }
}
