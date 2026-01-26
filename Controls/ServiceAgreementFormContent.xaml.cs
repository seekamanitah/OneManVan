using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Controls;

public partial class ServiceAgreementFormContent : UserControl
{
    private readonly OneManVanDbContext _dbContext;

    public ServiceAgreementFormContent(OneManVanDbContext dbContext)
    {
        InitializeComponent();
        _dbContext = dbContext;
        
        LoadCustomers();
        LoadEnums();
        
        // Set defaults
        StartDatePicker.SelectedDate = DateTime.Today;
        EndDatePicker.SelectedDate = DateTime.Today.AddYears(1);
        AnnualPriceTextBox.Text = "0.00";
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
        // Agreement Type
        TypeCombo.ItemsSource = Enum.GetValues(typeof(AgreementType))
            .Cast<AgreementType>()
            .Select(t => new ComboBoxItem { Content = t.ToString(), Tag = t })
            .ToList();
        TypeCombo.SelectedIndex = 0;
    }
    
    public ServiceAgreement GetServiceAgreement()
    {
        var selectedType = (TypeCombo.SelectedItem as ComboBoxItem)?.Tag as AgreementType? ?? AgreementType.Annual;
        
        return new ServiceAgreement
        {
            Name = NameTextBox.Text,
            CustomerId = (int)CustomerCombo.SelectedValue,
            SiteId = SiteCombo.SelectedValue as int?,
            Type = selectedType,
            StartDate = StartDatePicker.SelectedDate ?? DateTime.Today,
            EndDate = EndDatePicker.SelectedDate ?? DateTime.Today.AddYears(1),
            AnnualPrice = decimal.TryParse(AnnualPriceTextBox.Text, out var price) ? price : 0,
            Description = DescriptionTextBox.Text,
            Status = AgreementStatus.Draft
        };
    }
    
    public async System.Threading.Tasks.Task LoadServiceAgreement(ServiceAgreement agreement)
    {
        NameTextBox.Text = agreement.Name;
        DescriptionTextBox.Text = agreement.Description;
        StartDatePicker.SelectedDate = agreement.StartDate;
        EndDatePicker.SelectedDate = agreement.EndDate;
        AnnualPriceTextBox.Text = agreement.AnnualPrice.ToString("F2");
        
        // Select customer
        CustomerCombo.SelectedValue = agreement.CustomerId;
        
        // Wait for sites to load
        await System.Threading.Tasks.Task.Delay(200);
        
        // Select site if available
        if (agreement.SiteId.HasValue)
        {
            SiteCombo.SelectedValue = agreement.SiteId;
        }
        
        // Select type
        var typeItem = TypeCombo.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(i => (AgreementType)i.Tag == agreement.Type);
        if (typeItem != null)
        {
            TypeCombo.SelectedItem = typeItem;
        }
    }
    
    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
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
