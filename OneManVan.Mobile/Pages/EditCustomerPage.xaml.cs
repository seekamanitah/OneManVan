using Microsoft.EntityFrameworkCore;
using OneManVan.Mobile.Extensions;
using OneManVan.Mobile.Helpers;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;
using OneManVan.Shared.Utilities;

namespace OneManVan.Mobile.Pages;

[QueryProperty(nameof(CustomerId), "id")]
public partial class EditCustomerPage : ContentPage
{
    private readonly OneManVanDbContext _db;
    private Customer? _customer;
    private CustomerCompanyRole? _companyRole;
    private int? _selectedCompanyId;
    
    public int CustomerId { get; set; }

    public EditCustomerPage(OneManVanDbContext db)
    {
        InitializeComponent();
        _db = db;
        RolePicker.SelectedIndex = 0; // Default to Contact
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadCustomerAsync();
    }

    private async Task LoadCustomerAsync()
    {
        try
        {
            using (new LoadingScope(LoadingIndicator))
            {
                _customer = await _db.Customers.FindAsync(CustomerId);

                if (_customer == null)
                {
                    await DisplayAlertAsync("Error", "Customer not found", "OK");
                    await Shell.Current.GoToAsync("..");
                    return;
                }

                // Populate form fields
                NameEntry.Text = _customer.Name;
                CompanyNameEntry.Text = _customer.CompanyName;
                EmailEntry.Text = _customer.Email;
                PhoneEntry.Text = PhoneNumberFormatter.Format(_customer.Phone);
                SecondaryPhoneEntry.Text = PhoneNumberFormatter.Format(_customer.SecondaryPhone);
                SecondaryEmailEntry.Text = _customer.SecondaryEmail;
                HomeAddressEditor.Text = _customer.HomeAddress;
                NotesEditor.Text = _customer.Notes;
                TagsEntry.Text = _customer.Tags;
                
                // Show/hide navigate button based on address
                NavigateButton.IsVisible = !string.IsNullOrWhiteSpace(_customer.HomeAddress);

                CustomerTypePicker.SelectedIndex = (int)_customer.CustomerType;
                StatusPicker.SelectedIndex = (int)_customer.Status;
                PaymentTermsPicker.SelectedIndex = (int)_customer.PaymentTerms;

                // Load company relationship
                await LoadCompanyRelationshipAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"EditCustomer load error: {ex}");
            await DisplayAlertAsync("Unable to Load", 
                "Failed to load customer data. Please try again.", "OK");
            await Shell.Current.GoToAsync("..");
        }
    }

    private async Task LoadCompanyRelationshipAsync()
    {
        try
        {
            _companyRole = await _db.CustomerCompanyRoles
                .Include(r => r.Company)
                .FirstOrDefaultAsync(r => r.CustomerId == CustomerId && r.EndDate == null);

            if (_companyRole != null)
            {
                _selectedCompanyId = _companyRole.CompanyId;
                LinkedCompanyLabel.Text = _companyRole.Company.Name;
                LinkedCompanyLabel.TextColor = Colors.Black;
                
                // Show role section
                RoleSection.IsVisible = true;
                
                // Populate role fields
                RolePicker.SelectedIndex = _companyRole.Role switch
                {
                    "Contact" => 0,
                    "Owner" => 1,
                    "Manager" => 2,
                    "Employee" => 3,
                    "Technician" => 4,
                    "Contractor" => 5,
                    _ => 0
                };
                
                TitleEntry.Text = _companyRole.Title;
                DepartmentEntry.Text = _companyRole.Department;
                IsPrimaryContactCheckBox.IsChecked = _companyRole.IsPrimaryContact;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Load company relationship error: {ex}");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // Validate data is loaded
        if (_customer == null)
        {
            await DisplayAlertAsync("Cannot Save", 
                "Customer data not loaded. Please go back and try again.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(NameEntry.Text))
        {
            await DisplayAlertAsync("Required", "Customer name is required", "OK");
            NameEntry.Focus();
            return;
        }

        // Use PageExtensions.SaveWithFeedbackAsync for clean save pattern
        await this.SaveWithFeedbackAsync(async () =>
        {
            _customer.Name = NameEntry.Text.Trim();
            _customer.CompanyName = CompanyNameEntry.Text?.Trim();
            _customer.Email = EmailEntry.Text?.Trim();
            _customer.Phone = PhoneNumberFormatter.Unformat(PhoneEntry.Text?.Trim());
            _customer.SecondaryPhone = PhoneNumberFormatter.Unformat(SecondaryPhoneEntry.Text?.Trim());
            _customer.SecondaryEmail = SecondaryEmailEntry.Text?.Trim();
            _customer.HomeAddress = HomeAddressEditor.Text?.Trim();
            _customer.Notes = NotesEditor.Text?.Trim();
            _customer.Tags = TagsEntry.Text?.Trim();
            
            _customer.CustomerType = (CustomerType)CustomerTypePicker.SelectedIndex;
            _customer.Status = (CustomerStatus)StatusPicker.SelectedIndex;
            _customer.PaymentTerms = (PaymentTerms)PaymentTermsPicker.SelectedIndex;

            // Save company relationship
            if (_selectedCompanyId.HasValue)
            {
                var role = RolePicker.SelectedIndex switch
                {
                    0 => "Contact",
                    1 => "Owner",
                    2 => "Manager",
                    3 => "Employee",
                    4 => "Technician",
                    5 => "Contractor",
                    _ => "Contact"
                };

                if (_companyRole == null)
                {
                    // Create new relationship
                    _companyRole = new CustomerCompanyRole
                    {
                        CustomerId = _customer.Id,
                        CompanyId = _selectedCompanyId.Value,
                        Role = role,
                        Title = TitleEntry.Text?.Trim(),
                        Department = DepartmentEntry.Text?.Trim(),
                        IsPrimaryContact = IsPrimaryContactCheckBox.IsChecked,
                        StartDate = DateTime.UtcNow
                    };
                    _db.CustomerCompanyRoles.Add(_companyRole);
                }
                else if (_companyRole.CompanyId != _selectedCompanyId.Value)
                {
                    // Company changed - end old relationship and create new
                    _companyRole.EndDate = DateTime.UtcNow;
                    
                    _companyRole = new CustomerCompanyRole
                    {
                        CustomerId = _customer.Id,
                        CompanyId = _selectedCompanyId.Value,
                        Role = role,
                        Title = TitleEntry.Text?.Trim(),
                        Department = DepartmentEntry.Text?.Trim(),
                        IsPrimaryContact = IsPrimaryContactCheckBox.IsChecked,
                        StartDate = DateTime.UtcNow
                    };
                    _db.CustomerCompanyRoles.Add(_companyRole);
                }
                else
                {
                    // Update existing relationship
                    _companyRole.Role = role;
                    _companyRole.Title = TitleEntry.Text?.Trim();
                    _companyRole.Department = DepartmentEntry.Text?.Trim();
                    _companyRole.IsPrimaryContact = IsPrimaryContactCheckBox.IsChecked;
                }
            }
            else if (_companyRole != null)
            {
                // Company link removed - end the relationship
                _companyRole.EndDate = DateTime.UtcNow;
            }

            // Use retry logic for save operation
            await _db.SaveChangesWithRetryAsync();
        }, 
        successMessage: "Customer updated successfully");
    }

    private async void OnSelectCompanyClicked(object sender, EventArgs e)
    {
        try
        {
            var companies = await _db.Companies
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            if (!companies.Any())
            {
                await DisplayAlertAsync("No Companies", "No companies found. Please create a company first.", "OK");
                return;
            }

            var companyNames = companies.Select(c => c.Name).ToArray();
            var selected = await DisplayActionSheetAsync("Select Company", "Cancel", null, companyNames);

            if (selected != null && selected != "Cancel")
            {
                var company = companies.First(c => c.Name == selected);
                _selectedCompanyId = company.Id;
                LinkedCompanyLabel.Text = company.Name;
                LinkedCompanyLabel.TextColor = Colors.Black;
                RoleSection.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load companies: {ex.Message}", "OK");
        }
    }

    private async void OnRemoveCompanyLinkClicked(object sender, EventArgs e)
    {
        var confirm = await DisplayAlertAsync(
            "Remove Company Link",
            "Are you sure you want to remove the link to this company?",
            "Remove",
            "Cancel");

        if (confirm)
        {
            _selectedCompanyId = null;
            LinkedCompanyLabel.Text = "No company linked";
            LinkedCompanyLabel.TextColor = Color.FromArgb("#9CA3AF");
            RoleSection.IsVisible = false;
        }
    }

    private void OnHomeAddressChanged(object sender, TextChangedEventArgs e)
    {
        NavigateButton.IsVisible = !string.IsNullOrWhiteSpace(HomeAddressEditor.Text);
    }

    private async void OnNavigateClicked(object sender, EventArgs e)
    {
        var address = HomeAddressEditor.Text?.Trim();
        if (string.IsNullOrWhiteSpace(address))
            return;

        try
        {
            var encodedAddress = Uri.EscapeDataString(address);
            
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                await Launcher.OpenAsync($"geo:0,0?q={encodedAddress}");
            }
            else if (DeviceInfo.Platform == DevicePlatform.iOS)
            {
                await Launcher.OpenAsync($"http://maps.apple.com/?q={encodedAddress}");
            }
            else
            {
                await Launcher.OpenAsync($"https://www.google.com/maps/search/?api=1&query={encodedAddress}");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Navigation Error", $"Could not open maps: {ex.Message}", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
