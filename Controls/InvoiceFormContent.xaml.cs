using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Controls;

public partial class InvoiceFormContent : UserControl
{
    private readonly OneManVanDbContext _dbContext;

    public InvoiceFormContent(OneManVanDbContext dbContext)
    {
        InitializeComponent();
        _dbContext = dbContext;
        
        LoadCustomers();
        
        // Set defaults
        InvoiceDatePicker.SelectedDate = DateTime.Today;
        DueDatePicker.SelectedDate = DateTime.Today.AddDays(30);
        TaxRateTextBox.Text = "7.0";
        LaborAmountTextBox.Text = "0.00";
        PartsAmountTextBox.Text = "0.00";
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
    
    public Invoice GetInvoice()
    {
        var laborAmount = decimal.TryParse(LaborAmountTextBox.Text, out var labor) ? labor : 0;
        var partsAmount = decimal.TryParse(PartsAmountTextBox.Text, out var parts) ? parts : 0;
        var taxRate = decimal.TryParse(TaxRateTextBox.Text, out var tax) ? tax : 7.0m;
        
        var subTotal = laborAmount + partsAmount;
        var taxAmount = subTotal * (taxRate / 100);
        var total = subTotal + taxAmount;
        
        return new Invoice
        {
            CustomerId = (int)CustomerCombo.SelectedValue,
            InvoiceDate = InvoiceDatePicker.SelectedDate ?? DateTime.Today,
            DueDate = DueDatePicker.SelectedDate ?? DateTime.Today.AddDays(30),
            LaborAmount = laborAmount,
            PartsAmount = partsAmount,
            SubTotal = subTotal,
            TaxRate = taxRate,
            TaxAmount = taxAmount,
            Total = total,
            Notes = NotesTextBox.Text,
            Status = InvoiceStatus.Draft
        };
    }
    
    public void LoadInvoice(Invoice invoice)
    {
        CustomerCombo.SelectedValue = invoice.CustomerId;
        InvoiceDatePicker.SelectedDate = invoice.InvoiceDate;
        DueDatePicker.SelectedDate = invoice.DueDate;
        LaborAmountTextBox.Text = invoice.LaborAmount.ToString("F2");
        PartsAmountTextBox.Text = invoice.PartsAmount.ToString("F2");
        TaxRateTextBox.Text = invoice.TaxRate.ToString("F2");
        NotesTextBox.Text = invoice.Notes;
    }
    
    public bool Validate()
    {
        if (CustomerCombo.SelectedValue == null)
        {
            return false;
        }
        if (InvoiceDatePicker.SelectedDate == null)
        {
            return false;
        }
        if (DueDatePicker.SelectedDate == null)
        {
            return false;
        }
        return true;
    }
}
