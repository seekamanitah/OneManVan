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

public partial class JobFormContent : UserControl
{
    private readonly OneManVanDbContext _dbContext;

    public JobFormContent(OneManVanDbContext dbContext)
    {
        InitializeComponent();
        _dbContext = dbContext;
        
        LoadCustomers();
        LoadEnums();
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
        // Job Type
        JobTypeCombo.ItemsSource = Enum.GetValues(typeof(JobType))
            .Cast<JobType>()
            .Select(jt => new ComboBoxItem { Content = jt.ToString(), Tag = jt })
            .ToList();
        JobTypeCombo.SelectedIndex = 0;
        
        // Priority
        PriorityCombo.ItemsSource = Enum.GetValues(typeof(JobPriority))
            .Cast<JobPriority>()
            .Select(p => new ComboBoxItem { Content = p.ToString(), Tag = p })
            .ToList();
        PriorityCombo.SelectedIndex = 1; // Normal
    }
    
    public Job GetJob()
    {
        var selectedJobType = (JobTypeCombo.SelectedItem as ComboBoxItem)?.Tag as JobType? ?? JobType.ServiceCall;
        var selectedPriority = (PriorityCombo.SelectedItem as ComboBoxItem)?.Tag as JobPriority? ?? JobPriority.Normal;
        
        var laborTotal = decimal.TryParse(LaborTotalTextBox.Text, out var labor) ? labor : 0;
        var partsTotal = decimal.TryParse(PartsTotalTextBox.Text, out var parts) ? parts : 0;
        var taxRate = decimal.TryParse(TaxRateTextBox.Text, out var tax) ? tax / 100 : 0.07m; // Convert % to decimal
        var taxIncluded = TaxIncludedCheckBox.IsChecked == true;
        
        var subTotal = laborTotal + partsTotal;
        var taxAmount = taxIncluded ? 0 : subTotal * taxRate;
        var total = subTotal + taxAmount;
        
        return new Job
        {
            Title = TitleTextBox.Text,
            CustomerId = (int)CustomerCombo.SelectedValue,
            SiteId = SiteCombo.SelectedValue as int?,
            ScheduledDate = ScheduledDatePicker.SelectedDate,
            JobType = selectedJobType,
            Priority = selectedPriority,
            Description = DescriptionTextBox.Text,
            LaborTotal = laborTotal,
            PartsTotal = partsTotal,
            SubTotal = subTotal,
            TaxRate = taxRate,
            TaxAmount = taxAmount,
            TaxIncluded = taxIncluded,
            Total = total,
            Status = JobStatus.Scheduled
        };
    }
    
    public async System.Threading.Tasks.Task LoadJob(Job job)
    {
        TitleTextBox.Text = job.Title;
        DescriptionTextBox.Text = job.Description;
        ScheduledDatePicker.SelectedDate = job.ScheduledDate;
        LaborTotalTextBox.Text = job.LaborTotal.ToString("F2");
        PartsTotalTextBox.Text = job.PartsTotal.ToString("F2");
        TaxRateTextBox.Text = (job.TaxRate * 100).ToString("F2"); // Convert decimal to %
        TaxIncludedCheckBox.IsChecked = job.TaxIncluded;
        
        // Select customer
        CustomerCombo.SelectedValue = job.CustomerId;
        
        // Wait for sites to load
        await System.Threading.Tasks.Task.Delay(200);
        
        // Select site if available
        if (job.SiteId.HasValue)
        {
            SiteCombo.SelectedValue = job.SiteId;
        }
        
        // Select job type
        var jobTypeItem = JobTypeCombo.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(i => (JobType)i.Tag == job.JobType);
        if (jobTypeItem != null)
        {
            JobTypeCombo.SelectedItem = jobTypeItem;
        }
        
        // Select priority
        var priorityItem = PriorityCombo.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(i => (JobPriority)i.Tag == job.Priority);
        if (priorityItem != null)
        {
            PriorityCombo.SelectedItem = priorityItem;
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
