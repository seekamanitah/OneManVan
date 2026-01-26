using Microsoft.EntityFrameworkCore;
using OneManVan.Mobile.Services;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Mobile.Pages;

/// <summary>
/// Modal page for selecting a customer with search functionality.
/// </summary>
public partial class CustomerPickerPage : ContentPage
{
    private readonly OneManVanDbContext _db;
    private List<CustomerViewModel> _allCustomers = [];
    private TaskCompletionSource<Customer?>? _completionSource;

    public CustomerPickerPage(OneManVanDbContext db)
    {
        InitializeComponent();
        _db = db;
    }

    /// <summary>
    /// Shows the picker and returns the selected customer, or null if cancelled.
    /// </summary>
    public Task<Customer?> GetCustomerAsync()
    {
        _completionSource = new TaskCompletionSource<Customer?>();
        return _completionSource.Task;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadCustomersAsync();
        SearchEntry.Focus();
    }

    private async Task LoadCustomersAsync()
    {
        try
        {
            var customers = await _db.Customers
                .Where(c => c.Status != CustomerStatus.Archived && c.Status != CustomerStatus.DoNotService)
                .OrderBy(c => c.Name)
                .ToListAsync();

            _allCustomers = customers.Select(c => new CustomerViewModel(c)).ToList();
            CustomerCollection.ItemsSource = _allCustomers;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load customers: {ex.Message}", "OK");
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var search = e.NewTextValue?.Trim().ToLowerInvariant() ?? string.Empty;

        if (string.IsNullOrEmpty(search))
        {
            CustomerCollection.ItemsSource = _allCustomers;
            return;
        }

        var filtered = _allCustomers.Where(c =>
            c.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            (c.CompanyName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (c.Phone?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (c.Email?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
        ).ToList();

        CustomerCollection.ItemsSource = filtered;
    }

    private async void OnCustomerSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is CustomerViewModel vm)
        {
            CustomerCollection.SelectedItem = null;
            
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            
            _completionSource?.TrySetResult(vm.Customer);
            await Navigation.PopModalAsync();
        }
    }

    private async void OnAddNewCustomerTapped(object sender, TappedEventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        
        // Navigate to full customer form
        QuickAddCustomerService.LastCreatedCustomer = null;
        await Navigation.PopModalAsync();
        await Shell.Current.GoToAsync("CustomerDetail");
        
        // Signal that we're adding a new customer (caller should handle OnAppearing)
        _completionSource?.TrySetResult(null);
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        _completionSource?.TrySetResult(null);
        await Navigation.PopModalAsync();
    }
}

/// <summary>
/// View model for customer display in the picker.
/// </summary>
public class CustomerViewModel
{
    public Customer Customer { get; }

    public CustomerViewModel(Customer customer)
    {
        Customer = customer;
    }

    public string Name => Customer.Name;
    public string? CompanyName => Customer.CompanyName;
    public string? Phone => Customer.Phone;
    public string? Email => Customer.Email;
    
    public string InitialLetter => string.IsNullOrEmpty(Name) ? "?" : Name[0].ToString().ToUpper();
    public bool HasCompanyName => !string.IsNullOrEmpty(CompanyName);
    public bool HasPhone => !string.IsNullOrEmpty(Phone);
    
    public string TypeShortName => Customer.CustomerType switch
    {
        CustomerType.Residential => "RES",
        CustomerType.Commercial => "COM",
        CustomerType.PropertyManager => "PM",
        CustomerType.Government => "GOV",
        _ => "?"
    };

    public Color TypeBadgeColor => Customer.CustomerType switch
    {
        CustomerType.Commercial => Color.FromArgb("#9C27B0"),
        CustomerType.PropertyManager => Color.FromArgb("#FF9800"),
        CustomerType.Government => Color.FromArgb("#607D8B"),
        _ => Color.FromArgb("#4CAF50")
    };
}
