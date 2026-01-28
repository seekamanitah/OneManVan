using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Mobile.Pages;

public partial class AddCompanyPage : ContentPage
{
    private readonly OneManVanDbContext _db;
    private List<Customer> _customers = new();
    private Customer? _selectedContact = null;

    public AddCompanyPage(OneManVanDbContext db)
    {
        InitializeComponent();
        _db = db;
        InitializeDefaults();
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        await LoadCustomersAsync();
    }

    private async Task LoadCustomersAsync()
    {
        try
        {
            _customers = await _db.Customers
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .ToListAsync();
            
            ContactCustomerPicker.ItemsSource = _customers;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load customers: {ex.Message}");
        }
    }

    private void OnContactCustomerChanged(object sender, EventArgs e)
    {
        _selectedContact = ContactCustomerPicker.SelectedItem as Customer;
        UpdateContactPreview();
    }

    private void UpdateContactPreview()
    {
        if (_selectedContact != null)
        {
            ContactInfoFrame.IsVisible = true;
            ContactNameLabel.Text = _selectedContact.FullName;
            ContactEmailLabel.Text = _selectedContact.Email ?? "";
            ContactPhoneLabel.Text = _selectedContact.Phone ?? "";
            ContactEmailLabel.IsVisible = !string.IsNullOrEmpty(_selectedContact.Email);
            ContactPhoneLabel.IsVisible = !string.IsNullOrEmpty(_selectedContact.Phone);
        }
        else
        {
            ContactInfoFrame.IsVisible = false;
        }
    }

    private async void OnNewContactClicked(object sender, EventArgs e)
    {
        // Quick add customer using prompts
        var firstName = await DisplayPromptAsync("New Contact", "First Name:", "Next", "Cancel", "John");
        if (string.IsNullOrWhiteSpace(firstName)) return;

        var lastName = await DisplayPromptAsync("New Contact", "Last Name:", "Next", "Cancel", "Doe");
        if (string.IsNullOrWhiteSpace(lastName)) return;

        var email = await DisplayPromptAsync("New Contact", "Email (optional):", "Next", "Skip", "", keyboard: Keyboard.Email);
        var phone = await DisplayPromptAsync("New Contact", "Phone (optional):", "Create", "Skip", "", keyboard: Keyboard.Telephone);

        try
        {
            var newCustomer = new Customer
            {
                FirstName = firstName.Trim(),
                LastName = lastName.Trim(),
                Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _db.Customers.Add(newCustomer);
            await _db.SaveChangesAsync();

            // Reload customers and select the new one
            await LoadCustomersAsync();
            var index = _customers.FindIndex(c => c.Id == newCustomer.Id);
            if (index >= 0)
            {
                ContactCustomerPicker.SelectedIndex = index;
            }

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to create contact: {ex.Message}", "OK");
        }
    }

    private void InitializeDefaults()
    {
        // Set default company type
        CompanyTypePicker.SelectedIndex = 0; // Customer
        IsActiveSwitch.IsToggled = true;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(NameEntry.Text))
        {
            await DisplayAlertAsync("Required", "Please enter a company name.", "OK");
            NameEntry.Focus();
            return;
        }

        // Validate email if provided
        if (!string.IsNullOrWhiteSpace(EmailEntry.Text) && !IsValidEmail(EmailEntry.Text))
        {
            await DisplayAlertAsync("Invalid Email", "Please enter a valid email address.", "OK");
            EmailEntry.Focus();
            return;
        }

        try
        {
            // Parse company type
            var companyType = CompanyTypePicker.SelectedIndex switch
            {
                0 => CompanyType.Customer,
                1 => CompanyType.Vendor,
                2 => CompanyType.Supplier,
                3 => CompanyType.Partner,
                4 => CompanyType.Competitor,
                _ => CompanyType.Customer
            };

            // Create company
            var company = new Company
            {
                Name = NameEntry.Text.Trim(),
                LegalName = string.IsNullOrWhiteSpace(LegalNameEntry.Text) ? null : LegalNameEntry.Text.Trim(),
                CompanyType = companyType,
                Industry = string.IsNullOrWhiteSpace(IndustryEntry.Text) ? null : IndustryEntry.Text.Trim(),
                Phone = string.IsNullOrWhiteSpace(PhoneEntry.Text) ? null : PhoneEntry.Text.Trim(),
                Email = string.IsNullOrWhiteSpace(EmailEntry.Text) ? null : EmailEntry.Text.Trim().ToLower(),
                ContactCustomerId = _selectedContact?.Id,
                Website = string.IsNullOrWhiteSpace(WebsiteEntry.Text) ? null : WebsiteEntry.Text.Trim(),
                TaxId = string.IsNullOrWhiteSpace(TaxIdEntry.Text) ? null : TaxIdEntry.Text.Trim(),
                BillingAddress = string.IsNullOrWhiteSpace(BillingAddressEntry.Text) ? null : BillingAddressEntry.Text.Trim(),
                BillingCity = string.IsNullOrWhiteSpace(BillingCityEntry.Text) ? null : BillingCityEntry.Text.Trim(),
                BillingState = string.IsNullOrWhiteSpace(BillingStateEntry.Text) ? null : BillingStateEntry.Text.Trim().ToUpper(),
                BillingZipCode = string.IsNullOrWhiteSpace(BillingZipCodeEntry.Text) ? null : BillingZipCodeEntry.Text.Trim(),
                Notes = string.IsNullOrWhiteSpace(NotesEditor.Text) ? null : NotesEditor.Text.Trim(),
                IsActive = IsActiveSwitch.IsToggled,
                CreatedAt = DateTime.UtcNow
            };

            _db.Companies.Add(company);
            await _db.SaveChangesAsync();

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

            await DisplayAlertAsync("Success", $"Company '{company.Name}' has been created.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to create company: {ex.Message}", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        var hasChanges = !string.IsNullOrWhiteSpace(NameEntry.Text) ||
                        !string.IsNullOrWhiteSpace(LegalNameEntry.Text) ||
                        !string.IsNullOrWhiteSpace(PhoneEntry.Text) ||
                        !string.IsNullOrWhiteSpace(EmailEntry.Text);

        if (hasChanges)
        {
            var discard = await DisplayAlertAsync(
                "Discard Changes",
                "You have unsaved changes. Are you sure you want to go back?",
                "Discard",
                "Continue Editing");

            if (!discard) return;
        }

        await Shell.Current.GoToAsync("..");
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

