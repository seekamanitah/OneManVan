using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Mobile.Services;

/// <summary>
/// Service for selecting and adding customers from any screen.
/// Uses a searchable picker page for better UX with many customers.
/// </summary>
public class CustomerPickerService
{
    private readonly OneManVanDbContext _db;
    
    /// <summary>
    /// Static property to hold the last created customer for retrieval after navigation.
    /// </summary>
    public static Customer? LastCreatedCustomer { get; set; }
    
    /// <summary>
    /// Event raised when a new customer is created from the detail page.
    /// </summary>
    public static event EventHandler<Customer>? CustomerCreated;

    public CustomerPickerService(OneManVanDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Called by CustomerDetailPage when a new customer is created.
    /// </summary>
    public static void NotifyCustomerCreated(Customer customer)
    {
        LastCreatedCustomer = customer;
        CustomerCreated?.Invoke(null, customer);
    }

    /// <summary>
    /// Shows a searchable customer picker page.
    /// Returns the selected customer, or null if cancelled or navigating to add new.
    /// When null is returned, check LastCreatedCustomer in OnAppearing for newly created customers.
    /// </summary>
    public async Task<Customer?> PickCustomerAsync(Page callingPage)
    {
        var pickerPage = new Pages.CustomerPickerPage(_db);
        await callingPage.Navigation.PushModalAsync(pickerPage);
        return await pickerPage.GetCustomerAsync();
    }

    /// <summary>
    /// Navigates to the full CustomerDetailPage to add a new customer.
    /// </summary>
    public async Task NavigateToAddCustomerAsync()
    {
        LastCreatedCustomer = null;
        await Shell.Current.GoToAsync("CustomerDetail");
    }

    /// <summary>
    /// Gets a customer by ID, refreshing from database.
    /// </summary>
    public async Task<Customer?> GetCustomerByIdAsync(int id)
    {
        return await _db.Customers.FindAsync(id);
    }
}

/// <summary>
/// Legacy service name - redirects to CustomerPickerService for compatibility.
/// </summary>
public class QuickAddCustomerService
{
    private readonly CustomerPickerService _pickerService;
    private readonly OneManVanDbContext _db;

    /// <summary>
    /// Static property for backward compatibility.
    /// </summary>
    public static Customer? LastCreatedCustomer
    {
        get => CustomerPickerService.LastCreatedCustomer;
        set => CustomerPickerService.LastCreatedCustomer = value;
    }

    /// <summary>
    /// Event for backward compatibility.
    /// </summary>
    public static event EventHandler<Customer>? CustomerCreated
    {
        add => CustomerPickerService.CustomerCreated += value;
        remove => CustomerPickerService.CustomerCreated -= value;
    }

    public QuickAddCustomerService(OneManVanDbContext db)
    {
        _db = db;
        _pickerService = new CustomerPickerService(db);
    }

    /// <summary>
    /// For backward compatibility - now uses searchable picker.
    /// </summary>
    public static void NotifyCustomerCreated(Customer customer)
    {
        CustomerPickerService.NotifyCustomerCreated(customer);
    }

    /// <summary>
    /// Shows searchable customer picker.
    /// </summary>
    public Task<Customer?> SelectOrAddCustomerAsync(Page page, string? promptTitle = null)
    {
        return _pickerService.PickCustomerAsync(page);
    }

    /// <summary>
    /// Navigate to add customer page.
    /// </summary>
    public Task NavigateToAddCustomerAsync()
    {
        return _pickerService.NavigateToAddCustomerAsync();
    }
}
