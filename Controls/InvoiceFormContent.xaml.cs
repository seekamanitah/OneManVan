using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;
using OneManVan.Dialogs;
using OneManVan.Services;

namespace OneManVan.Controls;

public partial class InvoiceFormContent : UserControl
{
    private readonly OneManVanDbContext _dbContext;
    private ObservableCollection<InvoiceLineItem> _lineItems = new();
    private Invoice? _currentInvoice;

    public InvoiceFormContent(OneManVanDbContext dbContext)
    {
        InitializeComponent();
        _dbContext = dbContext;
        
        // Bind line items to DataGrid
        LineItemsDataGrid.ItemsSource = _lineItems;
        _lineItems.CollectionChanged += (s, e) => UpdateTotals();
        
        LoadCustomers();
        
        // Set defaults
        InvoiceDatePicker.SelectedDate = DateTime.Today;
        DueDatePicker.SelectedDate = DateTime.Today.AddDays(30);
        TaxRateTextBox.Text = "8.0";
        TaxIncludedCheckBox.IsChecked = false;
        
        UpdateTotals();
        UpdateEmptyState();
    }

    private async void LoadCustomers()
    {
        try
        {
            var customers = await _dbContext.Customers
                .Where(c => c.Status != CustomerStatus.Inactive)
                .OrderBy(c => c.Name)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();
                
            CustomerCombo.ItemsSource = customers;
            CustomerCombo.DisplayMemberPath = "Name";
            CustomerCombo.SelectedValuePath = "Id";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load customers: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void OnQuickAddCustomerClick(object sender, RoutedEventArgs e)
    {
        var dialog = new QuickAddCustomerDialog
        {
            Owner = Window.GetWindow(this)
        };
        
        if (dialog.ShowDialog() == true && dialog.CreatedCustomer != null)
        {
            // Reload customers to include the new one
            LoadCustomers();
            
            // Select the newly created customer
            System.Threading.Tasks.Task.Delay(100).ContinueWith(_ =>
            {
                Dispatcher.Invoke(() =>
                {
                    CustomerCombo.SelectedValue = dialog.CreatedCustomer.Id;
                });
            });
        }
    }
    
    #region Line Items Management
    
    private void OnAddFromProductsClick(object sender, RoutedEventArgs e)
    {
        // Need to have customer selected first
        if (CustomerCombo.SelectedValue == null)
        {
            MessageBox.Show("Please select a customer first.", "Customer Required",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        var customerId = (int)CustomerCombo.SelectedValue;
        var customerName = CustomerCombo.Text;
        var invoiceDate = InvoiceDatePicker.SelectedDate ?? DateTime.Today;
        
        var dialog = new ProductPickerDialog(_dbContext, customerId, customerName, null, null, invoiceDate)
        {
            Owner = Window.GetWindow(this)
        };
        
        if (dialog.ShowDialog() == true && dialog.LineItem != null)
        {
            // Dialog already created a complete line item
            _lineItems.Add(dialog.LineItem);
            UpdateEmptyState();
        }
    }
    
    private void OnAddCustomLineItemClick(object sender, RoutedEventArgs e)
    {
        var dialog = new AddCustomLineItemDialog
        {
            Owner = Window.GetWindow(this)
        };
        
        // Prepare dialog for UI scaling
        DialogService.PrepareDialog(dialog);
        
        if (dialog.ShowDialog() == true && dialog.LineItem != null)
        {
            _lineItems.Add(dialog.LineItem);
            UpdateEmptyState();
        }
    }
    
    private void OnDeleteLineItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is InvoiceLineItem lineItem)
        {
            var result = MessageBox.Show(
                $"Delete line item '{lineItem.Description}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
                
            if (result == MessageBoxResult.Yes)
            {
                _lineItems.Remove(lineItem);
                UpdateEmptyState();
            }
        }
    }
    
    private void UpdateEmptyState()
    {
        if (_lineItems.Any())
        {
            EmptyLineItemsText.Visibility = Visibility.Collapsed;
            LineItemsDataGrid.Visibility = Visibility.Visible;
        }
        else
        {
            EmptyLineItemsText.Visibility = Visibility.Visible;
            LineItemsDataGrid.Visibility = Visibility.Collapsed;
        }
    }
    
    #endregion
    
    #region Totals Calculation
    
    private void OnTaxRateChanged(object sender, TextChangedEventArgs e)
    {
        UpdateTotals();
    }
    
    private void OnTaxIncludedChanged(object sender, RoutedEventArgs e)
    {
        UpdateTotals();
    }
    
    private void UpdateTotals()
    {
        var subtotal = _lineItems.Sum(item => item.Total);
        var taxRate = decimal.TryParse(TaxRateTextBox.Text, out var tax) ? tax : 0;
        var taxIncluded = TaxIncludedCheckBox.IsChecked == true;
        
        var taxAmount = taxIncluded ? 0 : subtotal * (taxRate / 100);
        var total = subtotal + taxAmount;
        
        // Update display
        SubtotalDisplay.Text = subtotal.ToString("C2");
        TaxAmountDisplay.Text = taxAmount.ToString("C2");
        TotalDisplay.Text = total.ToString("C2");
    }
    
    #endregion
    
    #region Public API
    
    public Invoice GetInvoice()
    {
        var taxRate = decimal.TryParse(TaxRateTextBox.Text, out var tax) ? tax : 8.0m;
        var taxIncluded = TaxIncludedCheckBox.IsChecked == true;
        
        var subtotal = _lineItems.Sum(item => item.Total);
        var taxAmount = taxIncluded ? 0 : subtotal * (taxRate / 100);
        var total = subtotal + taxAmount;
        
        var invoice = _currentInvoice ?? new Invoice
        {
            Status = InvoiceStatus.Draft
        };
        
        invoice.CustomerId = (int)CustomerCombo.SelectedValue;
        invoice.InvoiceDate = InvoiceDatePicker.SelectedDate ?? DateTime.Today;
        invoice.DueDate = DueDatePicker.SelectedDate ?? DateTime.Today.AddDays(30);
        invoice.SubTotal = subtotal;
        invoice.TaxRate = taxRate;
        invoice.TaxAmount = taxAmount;
        invoice.TaxIncluded = taxIncluded;
        invoice.Total = total;
        invoice.Notes = NotesTextBox.Text;
        
        // Calculate legacy fields for backward compatibility
        invoice.LaborAmount = _lineItems
            .Where(i => i.Description != null && i.Description.ToLower().Contains("labor"))
            .Sum(i => i.Total);
        invoice.PartsAmount = subtotal - invoice.LaborAmount;
        
        // Clear and add line items
        invoice.LineItems.Clear();
        foreach (var item in _lineItems)
        {
            invoice.LineItems.Add(new InvoiceLineItem
            {
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Notes = item.Notes
            });
        }
        
        return invoice;
    }
    
    public void LoadInvoice(Invoice invoice)
    {
        _currentInvoice = invoice;
        
        CustomerCombo.SelectedValue = invoice.CustomerId;
        InvoiceDatePicker.SelectedDate = invoice.InvoiceDate;
        DueDatePicker.SelectedDate = invoice.DueDate;
        TaxRateTextBox.Text = invoice.TaxRate.ToString("F2");
        TaxIncludedCheckBox.IsChecked = invoice.TaxIncluded;
        NotesTextBox.Text = invoice.Notes;
        
        // Load line items
        _lineItems.Clear();
        
        if (invoice.LineItems != null && invoice.LineItems.Any())
        {
            // Invoice has line items - use them
            foreach (var item in invoice.LineItems)
            {
                _lineItems.Add(new InvoiceLineItem
                {
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Notes = item.Notes
                });
            }
            
            LegacyFieldsPanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            // Legacy invoice without line items - show legacy fields
            LaborAmountTextBox.Text = invoice.LaborAmount.ToString("F2");
            PartsAmountTextBox.Text = invoice.PartsAmount.ToString("F2");
            LegacyFieldsPanel.Visibility = Visibility.Visible;
        }
        
        UpdateEmptyState();
        UpdateTotals();
    }
    
    public bool Validate()
    {
        if (CustomerCombo.SelectedValue == null)
        {
            MessageBox.Show("Please select a customer.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        
        if (InvoiceDatePicker.SelectedDate == null)
        {
            MessageBox.Show("Please select an invoice date.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        
        if (DueDatePicker.SelectedDate == null)
        {
            MessageBox.Show("Please select a due date.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        
        if (!_lineItems.Any() && LegacyFieldsPanel.Visibility != Visibility.Visible)
        {
            MessageBox.Show("Please add at least one line item.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        
        return true;
    }
    
    #endregion
}

