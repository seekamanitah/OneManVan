using OneManVan.Mobile.Services;
using OneManVan.Shared.Models;

namespace OneManVan.Mobile.Helpers;

/// <summary>
/// Helper class to handle customer selection logic in pages.
/// Uses composition instead of inheritance for better flexibility with XAML pages.
/// </summary>
public class CustomerSelectionHelper
{
    private readonly ContentPage _page;
    private readonly CustomerPickerService _customerPicker;
    
    public Customer? SelectedCustomer { get; private set; }

    public CustomerSelectionHelper(ContentPage page, CustomerPickerService customerPicker)
    {
        _page = page;
        _customerPicker = customerPicker;
    }

    /// <summary>
    /// Check if returning from QuickAddCustomer and handle it
    /// Call this from the page's OnAppearing method
    /// </summary>
    public async Task HandleQuickAddCustomerAsync(Func<Customer, Task> onCustomerSelected)
    {
        if (QuickAddCustomerService.LastCreatedCustomer != null)
        {
            await SetSelectedCustomerAsync(QuickAddCustomerService.LastCreatedCustomer, onCustomerSelected);
            QuickAddCustomerService.LastCreatedCustomer = null;
        }
    }

    /// <summary>
    /// Handle customer selection tap - call this from tap event handler
    /// </summary>
    public async Task HandleCustomerSelectionAsync(Func<Customer, Task> onCustomerSelected)
    {
        var customer = await _customerPicker.PickCustomerAsync(_page);
        if (customer != null)
        {
            await SetSelectedCustomerAsync(customer, onCustomerSelected);
        }
    }

    /// <summary>
    /// Set the selected customer and invoke callback
    /// </summary>
    private async Task SetSelectedCustomerAsync(Customer customer, Func<Customer, Task> onCustomerSelected)
    {
        SelectedCustomer = customer;
        await onCustomerSelected(customer);
    }

    /// <summary>
    /// Helper method to update standard customer UI controls
    /// </summary>
    public static void UpdateCustomerUI(Customer customer, Label nameLabel, VisualElement? detailsPanel = null)
    {
        nameLabel.Text = customer.DisplayName;
        nameLabel.TextColor = Color.FromArgb("#333333");
        
        if (detailsPanel != null)
        {
            detailsPanel.IsVisible = true;
        }
    }
}
