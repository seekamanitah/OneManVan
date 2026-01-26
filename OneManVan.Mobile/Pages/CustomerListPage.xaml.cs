using Microsoft.EntityFrameworkCore;
using OneManVan.Mobile.Extensions;
using OneManVan.Mobile.Helpers;
using OneManVan.Mobile.Theme;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;

namespace OneManVan.Mobile.Pages;

public partial class CustomerListPage : ContentPage
{
    private readonly IDbContextFactory<OneManVanDbContext> _dbFactory;
    private CancellationTokenSource? _cts;
    private Timer? _searchTimer;
    private List<Customer> _allCustomers = [];
    private string _searchText = string.Empty;

    public List<Customer> Customers { get; set; } = [];
    public bool IsRefreshing { get; set; }

    public CustomerListPage(IDbContextFactory<OneManVanDbContext> dbFactory)
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
            await LoadCustomersAsync(_cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Page navigated away
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CustomerListPage.OnAppearing error: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _cts?.Cancel();
        _searchTimer?.Dispose();
    }

    private async Task LoadCustomersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using (new LoadingScope(LoadingIndicator))
            {
                await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
                
                // Add 30 second timeout to prevent indefinite hanging
                // PERF: AsNoTracking + removed Assets include (not displayed)
                _allCustomers = await db.Customers
                    .AsNoTracking()
                    .Include(c => c.Sites)
                    .OrderBy(c => c.Name)
                    .ToListAsync(cancellationToken)
                    .WithTimeout(TimeSpan.FromSeconds(30), cancellationToken);

                if (!cancellationToken.IsCancellationRequested)
                {
                    ApplyFilter();
                }
            }
        }
        catch (TimeoutException)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                await DisplayAlertAsync("Timeout", 
                    "Loading customers took too long. Please check your connection and try again.", "OK");
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                System.Diagnostics.Debug.WriteLine($"CustomerList load error: {ex}");
                await DisplayAlertAsync("Unable to Load", 
                    "Failed to load customers. Please check your connection and try again.", "OK");
            }
        }
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(_searchText))
        {
            Customers = [.. _allCustomers];
        }
        else
        {
            var search = _searchText.ToLowerInvariant();
            Customers = _allCustomers
                .Where(c => c.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                           (c.Email?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                           (c.Phone?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        }

        CustomerCollection.ItemsSource = null;
        CustomerCollection.ItemsSource = Customers;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = e.NewTextValue ?? string.Empty;
        
        // PERF: Debounce search - wait 300ms after typing stops
        _searchTimer?.Dispose();
        _searchTimer = new Timer(_ =>
        {
            MainThread.BeginInvokeOnMainThread(() => ApplyFilter());
        }, null, 300, Timeout.Infinite);
    }

    private void OnSearchButtonPressed(object sender, EventArgs e)
    {
        // Search already applied via TextChanged
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        await LoadCustomersAsync(_cts.Token);
        RefreshViewControl.IsRefreshing = false;
    }

    private async void OnCustomerSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Customer customer)
        {
            CustomerCollection.SelectedItem = null;
            await Shell.Current.GoToAsync($"CustomerDetail?id={customer.Id}");
        }
    }

    private async void OnAddCustomerClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("CustomerDetail");
    }

    private async void OnDeleteSwipe(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.BindingContext is Customer customer)
        {
            var confirm = await DisplayAlertAsync(
                "Delete Customer",
                $"Are you sure you want to delete {customer.Name}? This will also delete all associated sites and assets.",
                "Delete",
                "Cancel");

            if (confirm)
            {
                try
                {
                    await using var db = await _dbFactory.CreateDbContextAsync();
                    var dbCustomer = await db.Customers.FindAsync(customer.Id);
                    if (dbCustomer != null)
                    {
                        db.Customers.Remove(dbCustomer);
                        await db.SaveChangesAsync();
                    }
                    _cts?.Cancel();
                    _cts = new CancellationTokenSource();
                    await LoadCustomersAsync(_cts.Token);
                }
                catch (Exception ex)
                {
                    await DisplayAlertAsync("Error", $"Failed to delete: {ex.Message}", "OK");
                }
            }
        }
    }

    private async void OnCallSwipe(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.BindingContext is Customer customer)
        {
            if (!string.IsNullOrWhiteSpace(customer.Phone))
            {
                try
                {
                    PhoneDialer.Open(customer.Phone);
                }
                catch (Exception ex)
                {
                    await DisplayAlertAsync("Error", $"Could not open phone dialer: {ex.Message}", "OK");
                }
            }
            else
            {
                await DisplayAlertAsync("No Phone", "This customer has no phone number.", "OK");
            }
        }
    }

    private async void OnEmailSwipe(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.BindingContext is Customer customer)
        {
            if (!string.IsNullOrWhiteSpace(customer.Email))
            {
                try
                {
                    await Email.Default.ComposeAsync(new EmailMessage
                    {
                        To = [customer.Email],
                        Subject = "OneManVan FSM"
                    });
                }
                catch (Exception ex)
                {
                    await DisplayAlertAsync("Error", $"Could not open email: {ex.Message}", "OK");
                }
            }
            else
            {
                await DisplayAlertAsync("No Email", "This customer has no email address.", "OK");
            }
        }
    }

    private async void OnNavigateSwipe(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.BindingContext is Customer customer)
        {
            // Get the primary site or first site
            var site = customer.Sites?.FirstOrDefault(s => s.IsPrimary) 
                       ?? customer.Sites?.FirstOrDefault();

            if (site == null)
            {
                await DisplayAlertAsync("No Address", "This customer has no address on file.", "OK");
                return;
            }

            try
            {
                // Try GPS coordinates first
                if (site.Latitude.HasValue && site.Longitude.HasValue)
                {
                    var location = new Location((double)site.Latitude.Value, (double)site.Longitude.Value);
                    await Map.Default.OpenAsync(location, new MapLaunchOptions
                    {
                        NavigationMode = NavigationMode.Driving,
                        Name = customer.DisplayName
                    });
                }
                else
                {
                    // Fall back to address string
                    var address = $"{site.Address}, {site.City}, {site.State} {site.ZipCode}";
                    var placemark = new Placemark
                    {
                        Thoroughfare = site.Address,
                        Locality = site.City,
                        AdminArea = site.State,
                        PostalCode = site.ZipCode,
                        CountryName = "USA"
                    };
                    
                    await Map.Default.OpenAsync(placemark, new MapLaunchOptions
                    {
                        NavigationMode = NavigationMode.Driving,
                        Name = customer.DisplayName
                    });
                }

                try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"Could not open maps: {ex.Message}", "OK");
            }
        }
    }
}
