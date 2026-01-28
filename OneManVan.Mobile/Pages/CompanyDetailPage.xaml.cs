using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;

namespace OneManVan.Mobile.Pages;

[QueryProperty(nameof(CompanyId), "companyId")]
public partial class CompanyDetailPage : ContentPage
{
    private readonly OneManVanDbContext _db;
    private Company? _company;

    public int CompanyId { get; set; }

    public CompanyDetailPage(OneManVanDbContext db)
    {
        InitializeComponent();
        _db = db;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadCompanyAsync();
    }

    private async Task LoadCompanyAsync()
    {
        try
        {
            _company = await _db.Companies
                .FirstOrDefaultAsync(c => c.Id == CompanyId);

            if (_company == null)
            {
                await DisplayAlertAsync("Error", "Company not found", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            // Populate fields
            NameLabel.Text = _company.Name;
            
            // Legal Name (hide if empty)
            if (!string.IsNullOrWhiteSpace(_company.LegalName))
            {
                LegalNameLabel.Text = _company.LegalName;
                LegalNameGrid.IsVisible = true;
            }
            else
            {
                LegalNameGrid.IsVisible = false;
            }

            // Company Type
            CompanyTypeLabel.Text = _company.CompanyType.ToString();

            // Industry (hide if empty)
            if (!string.IsNullOrWhiteSpace(_company.Industry))
            {
                IndustryLabel.Text = _company.Industry;
                IndustryGrid.IsVisible = true;
            }
            else
            {
                IndustryGrid.IsVisible = false;
            }

            // Status
            StatusLabel.Text = _company.IsActive ? "Active" : "Inactive";
            StatusBadge.BackgroundColor = _company.IsActive 
                ? Color.FromArgb("#10B981") 
                : Color.FromArgb("#6B7280");

            // Phone (hide if empty)
            if (!string.IsNullOrWhiteSpace(_company.Phone))
            {
                PhoneLabel.Text = _company.Phone;
                PhoneGrid.IsVisible = true;
            }
            else
            {
                PhoneGrid.IsVisible = false;
            }

            // Email (hide if empty)
            if (!string.IsNullOrWhiteSpace(_company.Email))
            {
                EmailLabel.Text = _company.Email;
                EmailGrid.IsVisible = true;
            }
            else
            {
                EmailGrid.IsVisible = false;
            }

            // Website (hide if empty)
            if (!string.IsNullOrWhiteSpace(_company.Website))
            {
                WebsiteLabel.Text = _company.Website;
                WebsiteGrid.IsVisible = true;
            }
            else
            {
                WebsiteGrid.IsVisible = false;
            }

            // Tax ID (hide if empty)
            if (!string.IsNullOrWhiteSpace(_company.TaxId))
            {
                TaxIdLabel.Text = _company.TaxId;
                TaxIdGrid.IsVisible = true;
            }
            else
            {
                TaxIdGrid.IsVisible = false;
            }

            // Billing Address (hide if empty)
            var address = BuildAddressString();
            if (!string.IsNullOrWhiteSpace(address))
            {
                BillingAddressLabel.Text = address;
                BillingAddressCard.IsVisible = true;
            }
            else
            {
                BillingAddressCard.IsVisible = false;
            }

            // Notes (hide if empty)
            if (!string.IsNullOrWhiteSpace(_company.Notes))
            {
                NotesLabel.Text = _company.Notes;
                NotesCard.IsVisible = true;
            }
            else
            {
                NotesCard.IsVisible = false;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load company: {ex.Message}", "OK");
        }
    }

    private string BuildAddressString()
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(_company?.BillingAddress))
            parts.Add(_company.BillingAddress);

        var cityStateZip = new List<string>();
        if (!string.IsNullOrWhiteSpace(_company?.BillingCity))
            cityStateZip.Add(_company.BillingCity);
        if (!string.IsNullOrWhiteSpace(_company?.BillingState))
            cityStateZip.Add(_company.BillingState);
        if (!string.IsNullOrWhiteSpace(_company?.BillingZipCode))
            cityStateZip.Add(_company.BillingZipCode);

        if (cityStateZip.Any())
            parts.Add(string.Join(", ", cityStateZip));

        return string.Join("\n", parts);
    }

    private async void OnPhoneTapped(object sender, EventArgs e)
    {
        if (_company?.Phone != null)
        {
            var action = await DisplayActionSheetAsync(
                "Contact", 
                "Cancel", 
                null, 
                $"Call {_company.Phone}");

            if (action == $"Call {_company.Phone}")
            {
                try
                {
                    PhoneDialer.Open(_company.Phone);
                }
                catch (Exception ex)
                {
                    await DisplayAlertAsync("Error", $"Unable to open phone dialer: {ex.Message}", "OK");
                }
            }
        }
    }

    private async void OnEmailTapped(object sender, EventArgs e)
    {
        if (_company?.Email != null)
        {
            var action = await DisplayActionSheetAsync(
                "Contact", 
                "Cancel", 
                null, 
                $"Email {_company.Email}");

            if (action == $"Email {_company.Email}")
            {
                try
                {
                    await Email.ComposeAsync(new EmailMessage
                    {
                        To = new List<string> { _company.Email },
                        Subject = $"Regarding {_company.Name}"
                    });
                }
                catch (Exception ex)
                {
                    await DisplayAlertAsync("Error", $"Unable to open email: {ex.Message}", "OK");
                }
            }
        }
    }

    private async void OnWebsiteTapped(object sender, EventArgs e)
    {
        if (_company?.Website != null)
        {
            try
            {
                var uri = _company.Website.StartsWith("http") 
                    ? _company.Website 
                    : $"https://{_company.Website}";
                await Browser.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"Unable to open website: {ex.Message}", "OK");
            }
        }
    }

    private async void OnEditClicked(object sender, EventArgs e)
    {
        if (_company != null)
        {
            await Shell.Current.GoToAsync($"EditCompany?companyId={_company.Id}");
        }
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (_company == null) return;

        var confirm = await DisplayAlertAsync(
            "Delete Company",
            $"Are you sure you want to delete {_company.Name}? This action cannot be undone.",
            "Delete",
            "Cancel");

        if (!confirm) return;

        try
        {
            _db.Companies.Remove(_company);
            await _db.SaveChangesAsync();

            await DisplayAlertAsync("Success", $"{_company.Name} has been deleted.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to delete company: {ex.Message}", "OK");
        }
    }
}
