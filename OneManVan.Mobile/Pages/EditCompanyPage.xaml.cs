using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Mobile.Pages;

[QueryProperty(nameof(CompanyId), "companyId")]
public partial class EditCompanyPage : ContentPage
{
    private readonly OneManVanDbContext _db;
    private Company? _company;
    private List<Customer> _customers = new();
    private Customer? _selectedContact = null;
    
    public int CompanyId { get; set; }

    public EditCompanyPage(OneManVanDbContext db)
    {
        InitializeComponent();
        _db = db;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadCustomersAsync();
        await LoadCompanyAsync();
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

    private async Task LoadCompanyAsync()
    {
        try
        {
            _company = await _db.Companies
                .Include(c => c.ContactCustomer)
                .FirstOrDefaultAsync(c => c.Id == CompanyId);

            if (_company == null)
            {
                await DisplayAlertAsync("Error", "Company not found", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            // Populate form fields
            NameEntry.Text = _company.Name;
            LegalNameEntry.Text = _company.LegalName ?? string.Empty;
            IndustryEntry.Text = _company.Industry ?? string.Empty;
            PhoneEntry.Text = _company.Phone ?? string.Empty;
            EmailEntry.Text = _company.Email ?? string.Empty;
            WebsiteEntry.Text = _company.Website ?? string.Empty;
            TaxIdEntry.Text = _company.TaxId ?? string.Empty;
            BillingAddressEntry.Text = _company.BillingAddress ?? string.Empty;
            BillingCityEntry.Text = _company.BillingCity ?? string.Empty;
            BillingStateEntry.Text = _company.BillingState ?? string.Empty;
            BillingZipCodeEntry.Text = _company.BillingZipCode ?? string.Empty;
            NotesEditor.Text = _company.Notes ?? string.Empty;
            IsActiveSwitch.IsToggled = _company.IsActive;

            // Set contact customer picker
            if (_company.ContactCustomerId.HasValue)
            {
                var index = _customers.FindIndex(c => c.Id == _company.ContactCustomerId.Value);
                if (index >= 0)
                {
                    ContactCustomerPicker.SelectedIndex = index;
                }
            }

            // Set company type picker
            CompanyTypePicker.SelectedIndex = _company.CompanyType switch
            {
                CompanyType.Customer => 0,
                CompanyType.Vendor => 1,
                CompanyType.Supplier => 2,
                CompanyType.Partner => 3,
                CompanyType.Competitor => 4,
                _ => 0
            };

            Title = $"Edit {_company.Name}";
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load company: {ex.Message}", "OK");
            await Shell.Current.GoToAsync("..");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (_company == null) return;

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

            // Update company
            _company.Name = NameEntry.Text.Trim();
            _company.LegalName = string.IsNullOrWhiteSpace(LegalNameEntry.Text) ? null : LegalNameEntry.Text.Trim();
            _company.CompanyType = companyType;
            _company.Industry = string.IsNullOrWhiteSpace(IndustryEntry.Text) ? null : IndustryEntry.Text.Trim();
            _company.Phone = string.IsNullOrWhiteSpace(PhoneEntry.Text) ? null : PhoneEntry.Text.Trim();
            _company.Email = string.IsNullOrWhiteSpace(EmailEntry.Text) ? null : EmailEntry.Text.Trim().ToLower();
            _company.ContactCustomerId = _selectedContact?.Id;
            _company.Website = string.IsNullOrWhiteSpace(WebsiteEntry.Text) ? null : WebsiteEntry.Text.Trim();
            _company.TaxId = string.IsNullOrWhiteSpace(TaxIdEntry.Text) ? null : TaxIdEntry.Text.Trim();
            _company.BillingAddress = string.IsNullOrWhiteSpace(BillingAddressEntry.Text) ? null : BillingAddressEntry.Text.Trim();
            _company.BillingCity = string.IsNullOrWhiteSpace(BillingCityEntry.Text) ? null : BillingCityEntry.Text.Trim();
            _company.BillingState = string.IsNullOrWhiteSpace(BillingStateEntry.Text) ? null : BillingStateEntry.Text.Trim().ToUpper();
            _company.BillingZipCode = string.IsNullOrWhiteSpace(BillingZipCodeEntry.Text) ? null : BillingZipCodeEntry.Text.Trim();
            _company.Notes = string.IsNullOrWhiteSpace(NotesEditor.Text) ? null : NotesEditor.Text.Trim();
            _company.IsActive = IsActiveSwitch.IsToggled;
            _company.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

            await DisplayAlertAsync("Success", $"Company '{_company.Name}' has been updated.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to update company: {ex.Message}", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        if (_company == null)
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        // Check if any changes were made
        var hasChanges = 
            NameEntry.Text != _company.Name ||
            (LegalNameEntry.Text ?? string.Empty) != (_company.LegalName ?? string.Empty) ||
            (PhoneEntry.Text ?? string.Empty) != (_company.Phone ?? string.Empty) ||
            (EmailEntry.Text ?? string.Empty) != (_company.Email ?? string.Empty) ||
            (IndustryEntry.Text ?? string.Empty) != (_company.Industry ?? string.Empty) ||
            IsActiveSwitch.IsToggled != _company.IsActive;

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

