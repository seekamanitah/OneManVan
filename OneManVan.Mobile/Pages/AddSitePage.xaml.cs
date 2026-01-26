using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Mobile.Pages;

[QueryProperty(nameof(CustomerId), "customerId")]
public partial class AddSitePage : ContentPage
{
    private readonly OneManVanDbContext _db;
    private int _customerId;
    private Customer? _customer;

    public int CustomerId
    {
        get => _customerId;
        set
        {
            _customerId = value;
            if (_customerId > 0)
            {
                LoadCustomerAsync();
            }
        }
    }

    public AddSitePage(OneManVanDbContext db)
    {
        InitializeComponent();
        _db = db;
    }

    private async void LoadCustomerAsync()
    {
        try
        {
            _customer = await _db.Customers.FindAsync(_customerId);
            if (_customer != null)
            {
                CustomerNameLabel.Text = _customer.DisplayName;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load customer: {ex.Message}", "OK");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(AddressEntry.Text))
        {
            await DisplayAlertAsync("Required", "Please enter a street address.", "OK");
            AddressEntry.Focus();
            return;
        }

        try
        {
            // Generate site number
            var lastSite = await _db.Sites.OrderByDescending(s => s.Id).FirstOrDefaultAsync();
            var siteNumber = $"S-{(lastSite?.Id ?? 0) + 1:D4}";

            var site = new Site
            {
                CustomerId = _customerId,
                SiteNumber = siteNumber,
                Address = AddressEntry.Text.Trim(),
                Address2 = Address2Entry.Text?.Trim(),
                City = CityEntry.Text?.Trim(),
                State = StateEntry.Text?.Trim(),
                ZipCode = ZipEntry.Text?.Trim(),
                PropertyType = ParsePropertyType(PropertyTypePicker.SelectedItem?.ToString()),
                GateCode = GateCodeEntry.Text?.Trim(),
                LockboxCode = LockboxEntry.Text?.Trim(),
                HasGate = HasGateCheck.IsChecked,
                HasDog = HasDogCheck.IsChecked,
                IsPrimary = IsPrimaryCheck.IsChecked,
                AccessInstructions = AccessEditor.Text?.Trim(),
                ParkingInstructions = ParkingEntry.Text?.Trim(),
                SiteContactName = ContactNameEntry.Text?.Trim(),
                SiteContactPhone = ContactPhoneEntry.Text?.Trim(),
                SiteContactEmail = ContactEmailEntry.Text?.Trim(),
                Notes = NotesEditor.Text?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            // Parse optional numeric fields
            if (int.TryParse(YearBuiltEntry.Text, out var yearBuilt))
                site.YearBuilt = yearBuilt;

            if (int.TryParse(SqFtEntry.Text, out var sqFt))
                site.SquareFootage = sqFt;

            // If marking as primary, unmark other sites
            if (site.IsPrimary)
            {
                var otherPrimary = await _db.Sites
                    .Where(s => s.CustomerId == _customerId && s.IsPrimary)
                    .ToListAsync();
                foreach (var s in otherPrimary)
                    s.IsPrimary = false;
            }

            _db.Sites.Add(site);
            await _db.SaveChangesAsync();

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

            await DisplayAlertAsync("Success", $"Site '{site.ShortAddress}' has been added.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to save site: {ex.Message}", "OK");
        }
    }

    private static PropertyType ParsePropertyType(string? value) => value switch
    {
        "Single Family" => PropertyType.SingleFamily,
        "Multi-Family" => PropertyType.MultiFamily,
        "Condo" => PropertyType.Condo,
        "Townhouse" => PropertyType.Townhouse,
        "Mobile Home" => PropertyType.MobileHome,
        "Apartment" => PropertyType.Apartment,
        "Commercial" => PropertyType.Commercial,
        "Other" => PropertyType.Other,
        _ => PropertyType.Unknown
    };

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
