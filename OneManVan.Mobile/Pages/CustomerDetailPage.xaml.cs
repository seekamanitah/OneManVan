using Microsoft.EntityFrameworkCore;
using OneManVan.Mobile.Services;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Mobile.Pages;

[QueryProperty(nameof(CustomerId), "id")]
public partial class CustomerDetailPage : ContentPage
{
    private readonly IDbContextFactory<OneManVanDbContext> _dbFactory;
    private int _customerId;
    private bool _isNewCustomer;
    private CancellationTokenSource? _cts;
    private CustomerCompanyRole? _companyRole;

    public Customer Customer { get; set; } = new();
    public bool IsExisting => _customerId > 0;
    public string PageTitle => IsExisting ? "Edit Customer" : "New Customer";

    public string CustomerId
    {
        set
        {
            if (int.TryParse(value, out var id))
            {
                _customerId = id;
            }
        }
    }

    public CustomerDetailPage(IDbContextFactory<OneManVanDbContext> dbFactory)
    {
        InitializeComponent();
        _dbFactory = dbFactory;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        
        try
        {
            await LoadCustomerAsync(_cts.Token);
            InitializePickers();
            LoadPrimaryAddress();
            await LoadCompanyRelationshipAsync();
        }
        catch (OperationCanceledException)
        {
            // Page navigated away
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CustomerDetailPage.OnAppearing error: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _cts?.Cancel();
    }

    private void InitializePickers()
    {
        // Set picker selections based on customer data
        CustomerTypePicker.SelectedIndex = Customer.CustomerType switch
        {
            CustomerType.Residential => 0,
            CustomerType.Commercial => 1,
            CustomerType.PropertyManager => 2,
            CustomerType.Government => 3,
            _ => 0
        };

        StatusPicker.SelectedIndex = Customer.Status switch
        {
            CustomerStatus.Active => 0,
            CustomerStatus.Lead => 1,
            CustomerStatus.VIP => 2,
            CustomerStatus.Inactive => 3,
            CustomerStatus.DoNotService => 4,
            _ => 0
        };

        PreferredContactPicker.SelectedIndex = Customer.PreferredContact switch
        {
            ContactMethod.Any => 0,
            ContactMethod.Phone => 1,
            ContactMethod.Email => 2,
            ContactMethod.Text => 3,
            _ => 0
        };

        PaymentTermsPicker.SelectedIndex = Customer.PaymentTerms switch
        {
            PaymentTerms.DueOnReceipt => 0,
            PaymentTerms.COD => 1,
            PaymentTerms.Net15 => 2,
            PaymentTerms.Net30 => 3,
            PaymentTerms.Net45 => 4,
            _ => 0
        };

        if (!string.IsNullOrEmpty(Customer.ReferralSource))
        {
            for (int i = 0; i < ReferralSourcePicker.Items.Count; i++)
            {
                if (ReferralSourcePicker.Items[i] == Customer.ReferralSource)
                {
                    ReferralSourcePicker.SelectedIndex = i;
                    break;
                }
            }
        }
    }

    private void LoadPrimaryAddress()
    {
        // Load primary site address into the address fields
        var primarySite = Customer.Sites?.FirstOrDefault(s => s.IsPrimary) 
                          ?? Customer.Sites?.FirstOrDefault();
        
        if (primarySite != null)
        {
            AddressEntry.Text = primarySite.Address;
            CityEntry.Text = primarySite.City;
            StateEntry.Text = primarySite.State;
            ZipEntry.Text = primarySite.ZipCode;
            GateCodeEntry.Text = primarySite.GateCode;
            HasDogSwitch.IsToggled = primarySite.HasDog;
            AccessNotesEntry.Text = primarySite.AccessNotes;
        }
    }

    private async Task LoadCustomerAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        
        if (_customerId > 0)
        {
            _isNewCustomer = false;
            var customer = await db.Customers
                .Include(c => c.Sites)
                .Include(c => c.Assets)
                .FirstOrDefaultAsync(c => c.Id == _customerId, cancellationToken);

            if (customer != null)
            {
                Customer = customer;
                
                // Auto-split Name into FirstName/LastName if they're not already set
                if (string.IsNullOrWhiteSpace(Customer.FirstName) && !string.IsNullOrWhiteSpace(Customer.Name))
                {
                    var nameParts = Customer.Name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    if (nameParts.Length >= 1)
                        Customer.FirstName = nameParts[0];
                    if (nameParts.Length >= 2)
                        Customer.LastName = nameParts[1];
                }
            }
        }
        else
        {
            _isNewCustomer = true;
            Customer = new Customer
            {
                CustomerType = CustomerType.Residential,
                Status = CustomerStatus.Active,
                PreferredContact = ContactMethod.Any,
                PaymentTerms = PaymentTerms.DueOnReceipt,
                FirstContactDate = DateTime.Today
            };
        }

        if (!cancellationToken.IsCancellationRequested)
        {
            OnPropertyChanged(nameof(Customer));
            OnPropertyChanged(nameof(IsExisting));
            OnPropertyChanged(nameof(PageTitle));
        }
    }

    private void ApplyPickerSelections()
    {
        // Customer Type
        Customer.CustomerType = CustomerTypePicker.SelectedIndex switch
        {
            0 => CustomerType.Residential,
            1 => CustomerType.Commercial,
            2 => CustomerType.PropertyManager,
            3 => CustomerType.Government,
            _ => CustomerType.Residential
        };

        // Status
        Customer.Status = StatusPicker.SelectedIndex switch
        {
            0 => CustomerStatus.Active,
            1 => CustomerStatus.Lead,
            2 => CustomerStatus.VIP,
            3 => CustomerStatus.Inactive,
            4 => CustomerStatus.DoNotService,
            _ => CustomerStatus.Active
        };

        // Preferred Contact
        Customer.PreferredContact = PreferredContactPicker.SelectedIndex switch
        {
            0 => ContactMethod.Any,
            1 => ContactMethod.Phone,
            2 => ContactMethod.Email,
            3 => ContactMethod.Text,
            _ => ContactMethod.Any
        };

        // Payment Terms
        Customer.PaymentTerms = PaymentTermsPicker.SelectedIndex switch
        {
            0 => PaymentTerms.DueOnReceipt,
            1 => PaymentTerms.COD,
            2 => PaymentTerms.Net15,
            3 => PaymentTerms.Net30,
            4 => PaymentTerms.Net45,
            _ => PaymentTerms.DueOnReceipt
        };

        // Referral Source
        if (ReferralSourcePicker.SelectedIndex > 0)
        {
            Customer.ReferralSource = ReferralSourcePicker.SelectedItem?.ToString();
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // Validate FirstName or LastName is provided
        if (string.IsNullOrWhiteSpace(Customer.FirstName) && string.IsNullOrWhiteSpace(Customer.LastName))
        {
            await DisplayAlertAsync("Validation Error", "Please enter at least a first name or last name.", "OK");
            FirstNameEntry.Focus();
            return;
        }

        // Construct full name from FirstName and LastName for backward compatibility
        Customer.Name = $"{Customer.FirstName?.Trim()} {Customer.LastName?.Trim()}".Trim();

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            ApplyPickerSelections();

            if (_customerId == 0)
            {
                // New customer
                var maxId = await db.Customers.AnyAsync() ? await db.Customers.MaxAsync(c => c.Id) : 0;
                Customer.CustomerNumber = $"C-{(maxId + 1):D4}";
                Customer.CreatedAt = DateTime.UtcNow;
                db.Customers.Add(Customer);
                await db.SaveChangesAsync();

                // Create primary site from address fields if provided
                if (!string.IsNullOrWhiteSpace(AddressEntry.Text))
                {
                    var site = new Site
                    {
                        CustomerId = Customer.Id,
                        Address = AddressEntry.Text.Trim(),
                        City = CityEntry.Text?.Trim() ?? "",
                        State = StateEntry.Text?.Trim() ?? "",
                        ZipCode = ZipEntry.Text?.Trim() ?? "",
                        GateCode = GateCodeEntry.Text?.Trim(),
                        HasDog = HasDogSwitch.IsToggled,
                        AccessNotes = AccessNotesEntry.Text?.Trim(),
                        IsPrimary = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    db.Sites.Add(site);
                    await db.SaveChangesAsync();
                }
            }
            else
            {
                // Update existing customer
                var existingCustomer = await db.Customers
                    .Include(c => c.Sites)
                    .FirstOrDefaultAsync(c => c.Id == _customerId);
                    
                if (existingCustomer != null)
                {
                    // Update customer properties
                    existingCustomer.Name = Customer.Name;
                    existingCustomer.CompanyName = Customer.CompanyName;
                    existingCustomer.Phone = Customer.Phone;
                    existingCustomer.SecondaryPhone = Customer.SecondaryPhone;
                    existingCustomer.Email = Customer.Email;
                    existingCustomer.BillingEmail = Customer.BillingEmail;
                    existingCustomer.CustomerType = Customer.CustomerType;
                    existingCustomer.Status = Customer.Status;
                    existingCustomer.PreferredContact = Customer.PreferredContact;
                    existingCustomer.PaymentTerms = Customer.PaymentTerms;
                    existingCustomer.TaxExempt = Customer.TaxExempt;
                    existingCustomer.TaxExemptId = Customer.TaxExemptId;
                    existingCustomer.EmergencyContact = Customer.EmergencyContact;
                    existingCustomer.EmergencyPhone = Customer.EmergencyPhone;
                    existingCustomer.PreferredTechnicianNotes = Customer.PreferredTechnicianNotes;
                    existingCustomer.ReferralSource = Customer.ReferralSource;
                    existingCustomer.ServiceNotes = Customer.ServiceNotes;

                    // Update or create primary site
                    if (!string.IsNullOrWhiteSpace(AddressEntry.Text))
                    {
                        var primarySite = existingCustomer.Sites?.FirstOrDefault(s => s.IsPrimary);
                        if (primarySite != null)
                        {
                            primarySite.Address = AddressEntry.Text.Trim();
                            primarySite.City = CityEntry.Text?.Trim() ?? "";
                            primarySite.State = StateEntry.Text?.Trim() ?? "";
                            primarySite.ZipCode = ZipEntry.Text?.Trim() ?? "";
                            primarySite.GateCode = GateCodeEntry.Text?.Trim();
                            primarySite.HasDog = HasDogSwitch.IsToggled;
                            primarySite.AccessNotes = AccessNotesEntry.Text?.Trim();
                        }
                        else
                        {
                            // Create new primary site
                            var site = new Site
                            {
                                CustomerId = existingCustomer.Id,
                                Address = AddressEntry.Text.Trim(),
                                City = CityEntry.Text?.Trim() ?? "",
                                State = StateEntry.Text?.Trim() ?? "",
                                ZipCode = ZipEntry.Text?.Trim() ?? "",
                                GateCode = GateCodeEntry.Text?.Trim(),
                                HasDog = HasDogSwitch.IsToggled,
                                AccessNotes = AccessNotesEntry.Text?.Trim(),
                                IsPrimary = true,
                                CreatedAt = DateTime.UtcNow
                            };
                            db.Sites.Add(site);
                        }
                    }

                    await db.SaveChangesAsync();
                }
            }
            
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

            if (_isNewCustomer)
            {
                QuickAddCustomerService.NotifyCustomerCreated(Customer);
            }
            
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to save: {ex.Message}", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnAddSiteClicked(object sender, EventArgs e)
    {
        // Navigate to full AddSitePage
        await Shell.Current.GoToAsync($"AddSite?customerId={Customer.Id}");
    }

    private async void OnRemoveSiteClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is Site site)
        {
            var confirm = await DisplayAlertAsync(
                "Remove Site",
                $"Remove site at {site.Address}?",
                "Remove",
                "Cancel");

            if (confirm)
            {
                try
                {
                    await using var db = await _dbFactory.CreateDbContextAsync();
                    var dbSite = await db.Sites.FindAsync(site.Id);
                    if (dbSite != null)
                    {
                        db.Sites.Remove(dbSite);
                        await db.SaveChangesAsync();
                    }

                    Customer.Sites.Remove(site);
                    SitesCollection.ItemsSource = null;
                    SitesCollection.ItemsSource = Customer.Sites;
                }
                catch (Exception ex)
                {
                    await DisplayAlertAsync("Error", $"Failed to remove site: {ex.Message}", "OK");
                }
            }
        }
    }

    private async void OnAddAssetClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"AddAsset?customerId={Customer.Id}");
    }

    private async void OnAssetSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Asset asset)
        {
            AssetsCollection.SelectedItem = null;
            await Shell.Current.GoToAsync($"AssetDetail?id={asset.Id}");
        }
    }

    private async void OnNavigateClicked(object sender, EventArgs e)
    {
        // Get the primary site or use address fields
        var site = Customer.Sites?.FirstOrDefault(s => s.IsPrimary) 
                   ?? Customer.Sites?.FirstOrDefault();

        string? address = null;
        string? city = null;
        string? state = null;
        string? zip = null;
        decimal? lat = null;
        decimal? lon = null;

        if (site != null)
        {
            address = site.Address;
            city = site.City;
            state = site.State;
            zip = site.ZipCode;
            lat = site.Latitude;
            lon = site.Longitude;
        }
        else if (!string.IsNullOrWhiteSpace(AddressEntry.Text))
        {
            // Use the form fields
            address = AddressEntry.Text;
            city = CityEntry.Text;
            state = StateEntry.Text;
            zip = ZipEntry.Text;
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            await DisplayAlertAsync("No Address", "No address available to navigate to.", "OK");
            return;
        }

        try
        {
            // Try GPS coordinates first
            if (lat.HasValue && lon.HasValue)
            {
                var location = new Location((double)lat.Value, (double)lon.Value);
                await Map.Default.OpenAsync(location, new MapLaunchOptions
                {
                    NavigationMode = NavigationMode.Driving,
                    Name = Customer.DisplayName
                });
            }
            else
            {
                // Fall back to address string
                var placemark = new Placemark
                {
                    Thoroughfare = address,
                    Locality = city ?? "",
                    AdminArea = state ?? "",
                    PostalCode = zip ?? "",
                    CountryName = "USA"
                };
                
                await Map.Default.OpenAsync(placemark, new MapLaunchOptions
                {
                    NavigationMode = NavigationMode.Driving,
                    Name = Customer.DisplayName
                });
            }

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Could not open maps: {ex.Message}", "OK");
        }
    }

    private async Task LoadCompanyRelationshipAsync()
    {
        if (_customerId <= 0) return;

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            _companyRole = await db.CustomerCompanyRoles
                .Include(r => r.Company)
                .FirstOrDefaultAsync(r => r.CustomerId == _customerId && r.EndDate == null);

            if (_companyRole != null)
            {
                // Show company relationship card
                CompanyRelationshipCard.IsVisible = true;
                
                // Populate fields
                LinkedCompanyNameLabel.Text = _companyRole.Company.Name;
                RoleLabel.Text = _companyRole.Role;
                
                // Show/hide optional fields
                if (!string.IsNullOrWhiteSpace(_companyRole.Title))
                {
                    CompanyTitleLabel.Text = _companyRole.Title;
                    TitleGrid.IsVisible = true;
                }
                else
                {
                    TitleGrid.IsVisible = false;
                }
                
                if (!string.IsNullOrWhiteSpace(_companyRole.Department))
                {
                    DepartmentLabel.Text = _companyRole.Department;
                    DepartmentGrid.IsVisible = true;
                }
                else
                {
                    DepartmentGrid.IsVisible = false;
                }
                
                // Show primary contact badge
                PrimaryContactBadge.IsVisible = _companyRole.IsPrimaryContact;
            }
            else
            {
                CompanyRelationshipCard.IsVisible = false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Load company relationship error: {ex}");
        }
    }

    private async void OnLinkedCompanyTapped(object sender, EventArgs e)
    {
        if (_companyRole != null)
        {
            await Shell.Current.GoToAsync($"CompanyDetail?companyId={_companyRole.CompanyId}");
        }
    }
}
