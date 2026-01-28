using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using OneManVan.Dialogs;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;
using OneManVan.Shared.Utilities;

namespace OneManVan.Controls;

public partial class CompanyFormContent : UserControl
{
    private readonly OneManVanDbContext _dbContext;
    private List<Customer> _customers = new();
    private Customer? _selectedContact = null;
    private int? _contactCustomerId = null;

    public CompanyFormContent(OneManVanDbContext dbContext)
    {
        InitializeComponent();
        _dbContext = dbContext;
        CompanyTypeCombo.SelectedIndex = 0;
        
        // Load customers async
        Loaded += async (s, e) => await LoadCustomersAsync();
    }

    private async Task LoadCustomersAsync()
    {
        try
        {
            _customers = await _dbContext.Customers
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .AsNoTracking()
                .ToListAsync();
            
            ContactCustomerCombo.ItemsSource = _customers;
            
            // If we have a pre-selected contact, set it
            if (_contactCustomerId.HasValue)
            {
                ContactCustomerCombo.SelectedValue = _contactCustomerId.Value;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load customers: {ex.Message}");
        }
    }

    private void ContactCustomerCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedContact = ContactCustomerCombo.SelectedItem as Customer;
        UpdateContactPreview();
    }

    private void UpdateContactPreview()
    {
        if (_selectedContact != null)
        {
            ContactInfoPanel.Visibility = Visibility.Visible;
            ContactNameDisplay.Text = _selectedContact.FullName;
            ContactEmailDisplay.Text = !string.IsNullOrEmpty(_selectedContact.Email) 
                ? _selectedContact.Email 
                : "No email";
            ContactPhoneDisplay.Text = !string.IsNullOrEmpty(_selectedContact.Phone) 
                ? _selectedContact.Phone 
                : "No phone";
            ContactEmailDisplay.Visibility = !string.IsNullOrEmpty(_selectedContact.Email) 
                ? Visibility.Visible 
                : Visibility.Collapsed;
            ContactPhoneDisplay.Visibility = !string.IsNullOrEmpty(_selectedContact.Phone) 
                ? Visibility.Visible 
                : Visibility.Collapsed;
        }
        else
        {
            ContactInfoPanel.Visibility = Visibility.Collapsed;
        }
    }

    private async void OnNewContactClick(object sender, RoutedEventArgs e)
    {
        var dialog = new QuickAddCustomerDialog();
        if (dialog.ShowDialog() == true && dialog.CreatedCustomer != null)
        {
            // Reload customers and select the new one
            await LoadCustomersAsync();
            _contactCustomerId = dialog.CreatedCustomer.Id;
            ContactCustomerCombo.SelectedValue = dialog.CreatedCustomer.Id;
            _selectedContact = dialog.CreatedCustomer;
            UpdateContactPreview();
        }
    }

    public Company GetCompany()
    {
        return new Company
        {
            Name = NameTextBox.Text,
            LegalName = LegalNameTextBox.Text,
            CompanyType = CompanyTypeCombo.SelectedIndex switch
            {
                1 => CompanyType.Vendor,
                2 => CompanyType.Supplier,
                3 => CompanyType.Partner,
                4 => CompanyType.Competitor,
                _ => CompanyType.Customer
            },
            Industry = IndustryTextBox.Text,
            TaxId = TaxIdTextBox.Text,
            Website = WebsiteTextBox.Text,
            Phone = PhoneNumberFormatter.Unformat(PhoneTextBox.Text),
            Email = EmailTextBox.Text,
            ContactCustomerId = _selectedContact?.Id,
            BillingAddress = BillingAddressTextBox.Text,
            BillingCity = BillingCityTextBox.Text,
            BillingState = BillingStateTextBox.Text,
            BillingZipCode = BillingZipCodeTextBox.Text,
            Notes = NotesTextBox.Text,
            IsActive = true
        };
    }

    public async Task LoadCompanyAsync(Company company)
    {
        NameTextBox.Text = company.Name;
        LegalNameTextBox.Text = company.LegalName;
        IndustryTextBox.Text = company.Industry;
        TaxIdTextBox.Text = company.TaxId;
        WebsiteTextBox.Text = company.Website;
        PhoneTextBox.Text = PhoneNumberFormatter.Format(company.Phone);
        EmailTextBox.Text = company.Email;
        BillingAddressTextBox.Text = company.BillingAddress;
        BillingCityTextBox.Text = company.BillingCity;
        BillingStateTextBox.Text = company.BillingState;
        BillingZipCodeTextBox.Text = company.BillingZipCode;
        NotesTextBox.Text = company.Notes;
        
        // Set contact customer
        _contactCustomerId = company.ContactCustomerId;
        if (company.ContactCustomerId.HasValue)
        {
            await LoadCustomersAsync();
            ContactCustomerCombo.SelectedValue = company.ContactCustomerId.Value;
        }

        CompanyTypeCombo.SelectedIndex = company.CompanyType switch
        {
            CompanyType.Vendor => 1,
            CompanyType.Supplier => 2,
            CompanyType.Partner => 3,
            CompanyType.Competitor => 4,
            _ => 0
        };
    }

    // Synchronous wrapper for backward compatibility
    public void LoadCompany(Company company)
    {
        _ = LoadCompanyAsync(company);
    }

    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            return false;
        }
        return true;
    }

    private void PhoneTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        PhoneTextBox.Text = PhoneNumberFormatter.Format(PhoneTextBox.Text);
    }
}
