using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;
using OneManVan.Dialogs;

namespace OneManVan.Controls;

public partial class EstimateFormContent : UserControl
{
    private readonly OneManVanDbContext _dbContext;

    public EstimateFormContent(OneManVanDbContext dbContext)
    {
        InitializeComponent();
        _dbContext = dbContext;
        
        LoadCustomers();
        LoadEnums();
        
        // Set default tax rate
        TaxRateTextBox.Text = "7.0";
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
    
    private void OnCustomerChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CustomerCombo.SelectedValue is int customerId)
        {
            LoadSites(customerId);
        }
    }
    
    private async void LoadSites(int customerId)
    {
        try
        {
            var sites = await _dbContext.Sites
                .Where(s => s.CustomerId == customerId)
                .OrderBy(s => s.Address)
                .Select(s => new { s.Id, s.Address, s.City })
                .ToListAsync();
                
            if (sites.Any())
            {
                SiteCombo.ItemsSource = sites;
                SiteCombo.DisplayMemberPath = "Address";
                SiteCombo.SelectedValuePath = "Id";
                SiteCombo.IsEnabled = true;
            }
            else
            {
                SiteCombo.ItemsSource = null;
                SiteCombo.IsEnabled = false;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load sites: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void LoadEnums()
    {
        // Status
        StatusCombo.ItemsSource = Enum.GetValues(typeof(EstimateStatus))
            .Cast<EstimateStatus>()
            .Select(s => new ComboBoxItem { Content = s.ToString(), Tag = s })
            .ToList();
        StatusCombo.SelectedIndex = 0; // Draft
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
    
    public Estimate GetEstimate()
    {
        var selectedStatus = (StatusCombo.SelectedItem as ComboBoxItem)?.Tag as EstimateStatus? ?? EstimateStatus.Draft;
        
        return new Estimate
        {
            Title = TitleTextBox.Text,
            CustomerId = (int)CustomerCombo.SelectedValue,
            SiteId = SiteCombo.SelectedValue as int?,
            Status = selectedStatus,
            TaxRate = decimal.TryParse(TaxRateTextBox.Text, out var taxRate) ? taxRate : 0,
            TaxIncluded = TaxIncludedCheckBox.IsChecked == true,
            Description = DescriptionTextBox.Text,
            Notes = NotesTextBox.Text
        };
    }
    
    public async System.Threading.Tasks.Task LoadEstimate(Estimate estimate)
    {
        TitleTextBox.Text = estimate.Title;
        DescriptionTextBox.Text = estimate.Description;
        NotesTextBox.Text = estimate.Notes;
        TaxRateTextBox.Text = estimate.TaxRate.ToString("F2");
        TaxIncludedCheckBox.IsChecked = estimate.TaxIncluded;
        
        // Select customer
        CustomerCombo.SelectedValue = estimate.CustomerId;
        
        // Wait for sites to load
        await System.Threading.Tasks.Task.Delay(200);
        
        // Select site if available
        if (estimate.SiteId.HasValue)
        {
            SiteCombo.SelectedValue = estimate.SiteId;
        }
        
        // Select status
        var statusItem = StatusCombo.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(i => (EstimateStatus)i.Tag == estimate.Status);
        if (statusItem != null)
        {
            StatusCombo.SelectedItem = statusItem;
        }
    }
    
    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
        {
            return false;
        }
        if (CustomerCombo.SelectedValue == null)
        {
            return false;
        }
        return true;
    }
}
