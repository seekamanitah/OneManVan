using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Mobile.Pages;

[QueryProperty(nameof(AgreementId), "id")]
[QueryProperty(nameof(CustomerId), "customerId")]
public partial class AddServiceAgreementPage : ContentPage
{
    private readonly OneManVanDbContext _db;
    private ServiceAgreement? _existingAgreement;
    private Customer? _selectedCustomer;

    public int AgreementId { get; set; }
    public int CustomerId { get; set; }

    public AddServiceAgreementPage(OneManVanDbContext db)
    {
        InitializeComponent();
        _db = db;
        
        // Set default dates
        StartDatePicker.Date = DateTime.Today;
        EndDatePicker.Date = DateTime.Today.AddYears(1);
        NextMaintenancePicker.Date = DateTime.Today.AddMonths(1);
        TypePicker.SelectedIndex = 1; // Standard
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (AgreementId > 0)
        {
            Title = "Edit Agreement";
            SaveButton.Text = "Update Agreement";
            await LoadAgreementAsync();
        }
        else if (CustomerId > 0)
        {
            await LoadCustomerAsync(CustomerId);
        }
    }

    private async Task LoadAgreementAsync()
    {
        try
        {
            _existingAgreement = await _db.ServiceAgreements
                .Include(a => a.Customer)
                .FirstOrDefaultAsync(a => a.Id == AgreementId);
                
            if (_existingAgreement == null)
            {
                await DisplayAlertAsync("Error", "Agreement not found.", "OK");
                await Navigation.PopAsync();
                return;
            }

            _selectedCustomer = _existingAgreement.Customer;
            UpdateCustomerDisplay();

            NameEntry.Text = _existingAgreement.Name;
            TypePicker.SelectedItem = _existingAgreement.Type.ToString();
            StartDatePicker.Date = _existingAgreement.StartDate;
            EndDatePicker.Date = _existingAgreement.EndDate;
            AnnualPriceEntry.Text = _existingAgreement.AnnualPrice.ToString("N2");
            DiscountEntry.Text = _existingAgreement.RepairDiscountPercent.ToString("N0");
            VisitsEntry.Text = _existingAgreement.IncludedVisitsPerYear.ToString();
            
            AcTuneUpSwitch.IsToggled = _existingAgreement.IncludesAcTuneUp;
            HeatingTuneUpSwitch.IsToggled = _existingAgreement.IncludesHeatingTuneUp;
            FilterSwitch.IsToggled = _existingAgreement.IncludesFilterReplacement;
            PrioritySwitch.IsToggled = _existingAgreement.PriorityService;
            NoOvertimeSwitch.IsToggled = _existingAgreement.WaiveTripCharge;
            
            if (_existingAgreement.NextMaintenanceDue.HasValue)
                NextMaintenancePicker.Date = _existingAgreement.NextMaintenanceDue.Value;
            
            NotesEditor.Text = _existingAgreement.InternalNotes;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load agreement: {ex.Message}", "OK");
        }
    }

    private async Task LoadCustomerAsync(int customerId)
    {
        try
        {
            _selectedCustomer = await _db.Customers.FindAsync(customerId);
            UpdateCustomerDisplay();
        }
        catch { }
    }

    private void UpdateCustomerDisplay()
    {
        if (_selectedCustomer != null)
        {
            CustomerNameLabel.Text = _selectedCustomer.DisplayName;
            CustomerNameLabel.TextColor = Color.FromArgb("#333333");
            CustomerDetailLabel.Text = _selectedCustomer.Phone ?? _selectedCustomer.Email ?? "Customer selected";
            CustomerFrame.BackgroundColor = Color.FromArgb("#E8F5E9");
        }
    }

    private async void OnSelectCustomerClicked(object sender, EventArgs e)
    {
        try
        {
            var customers = await _db.Customers
                .OrderBy(c => c.Name)
                .Take(50)
                .ToListAsync();

            if (!customers.Any())
            {
                await DisplayAlertAsync("No Customers", "Add a customer first.", "OK");
                return;
            }

            var options = customers.Select(c => c.Name).ToArray();
            var selected = await DisplayActionSheetAsync("Select Customer", "Cancel", null, options);

            if (selected == "Cancel" || selected == null)
                return;

            _selectedCustomer = customers.FirstOrDefault(c => c.Name == selected);
            UpdateCustomerDisplay();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load customers: {ex.Message}", "OK");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // Validate
        if (_selectedCustomer == null)
        {
            await DisplayAlertAsync("Required", "Please select a customer.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(NameEntry.Text))
        {
            await DisplayAlertAsync("Required", "Agreement name is required.", "OK");
            NameEntry.Focus();
            return;
        }

        if (!decimal.TryParse(AnnualPriceEntry.Text, out var annualPrice) || annualPrice <= 0)
        {
            await DisplayAlertAsync("Required", "Please enter a valid annual price.", "OK");
            AnnualPriceEntry.Focus();
            return;
        }

        try
        {
            SaveButton.IsEnabled = false;
            SaveButton.Text = "Saving...";

            var agreement = _existingAgreement ?? new ServiceAgreement();

            agreement.CustomerId = _selectedCustomer.Id;
            agreement.Name = NameEntry.Text.Trim();
            
            if (TypePicker.SelectedItem != null && Enum.TryParse<AgreementType>(TypePicker.SelectedItem.ToString(), out var type))
                agreement.Type = type;
            
            agreement.StartDate = StartDatePicker.Date ?? DateTime.Today;
            agreement.EndDate = EndDatePicker.Date ?? DateTime.Today.AddYears(1);
            agreement.AnnualPrice = annualPrice;
            
            if (decimal.TryParse(DiscountEntry.Text, out var discount))
                agreement.RepairDiscountPercent = discount;
            if (int.TryParse(VisitsEntry.Text, out var visits))
                agreement.IncludedVisitsPerYear = visits;
            
            agreement.IncludesAcTuneUp = AcTuneUpSwitch.IsToggled;
            agreement.IncludesHeatingTuneUp = HeatingTuneUpSwitch.IsToggled;
            agreement.IncludesFilterReplacement = FilterSwitch.IsToggled;
            agreement.PriorityService = PrioritySwitch.IsToggled;
            agreement.WaiveTripCharge = NoOvertimeSwitch.IsToggled;
            
            agreement.NextMaintenanceDue = NextMaintenancePicker.Date;
            agreement.InternalNotes = NotesEditor.Text?.Trim();
            
            // Set status based on dates
            agreement.Status = DateTime.Today >= agreement.StartDate && DateTime.Today <= agreement.EndDate
                ? AgreementStatus.Active
                : (DateTime.Today < agreement.StartDate ? AgreementStatus.Pending : AgreementStatus.Expired);

            if (_existingAgreement == null)
            {
                agreement.CreatedAt = DateTime.UtcNow;
                _db.ServiceAgreements.Add(agreement);
            }

            await _db.SaveChangesAsync();

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

            await DisplayAlertAsync("Saved", $"Service agreement {(_existingAgreement == null ? "created" : "updated")} successfully.", "OK");
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to save: {ex.Message}", "OK");
        }
        finally
        {
            SaveButton.IsEnabled = true;
            SaveButton.Text = _existingAgreement == null ? "Create Agreement" : "Update Agreement";
        }
    }
}
