using Microsoft.EntityFrameworkCore;
using OneManVan.Mobile.Extensions;
using OneManVan.Mobile.Helpers;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Mobile.Pages;

[QueryProperty(nameof(CustomerId), "id")]
public partial class EditCustomerPage : ContentPage
{
    private readonly OneManVanDbContext _db;
    private Customer? _customer;
    
    public int CustomerId { get; set; }

    public EditCustomerPage(OneManVanDbContext db)
    {
        InitializeComponent();
        _db = db;
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
                PhoneEntry.Text = _customer.Phone;
                SecondaryPhoneEntry.Text = _customer.SecondaryPhone;
                SecondaryEmailEntry.Text = _customer.SecondaryEmail;
                NotesEditor.Text = _customer.Notes;
                TagsEntry.Text = _customer.Tags;

                CustomerTypePicker.SelectedIndex = (int)_customer.CustomerType;
                StatusPicker.SelectedIndex = (int)_customer.Status;
                PaymentTermsPicker.SelectedIndex = (int)_customer.PaymentTerms;
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
            _customer.Phone = PhoneEntry.Text?.Trim();
            _customer.SecondaryPhone = SecondaryPhoneEntry.Text?.Trim();
            _customer.SecondaryEmail = SecondaryEmailEntry.Text?.Trim();
            _customer.Notes = NotesEditor.Text?.Trim();
            _customer.Tags = TagsEntry.Text?.Trim();
            
            _customer.CustomerType = (CustomerType)CustomerTypePicker.SelectedIndex;
            _customer.Status = (CustomerStatus)StatusPicker.SelectedIndex;
            _customer.PaymentTerms = (PaymentTerms)PaymentTermsPicker.SelectedIndex;

            // Use retry logic for save operation
            await _db.SaveChangesWithRetryAsync();
        }, 
        successMessage: "Customer updated successfully");
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
