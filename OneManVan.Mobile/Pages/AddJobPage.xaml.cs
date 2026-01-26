using Microsoft.EntityFrameworkCore;
using OneManVan.Mobile.Helpers;
using OneManVan.Mobile.Services;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Mobile.Pages;

/// <summary>
/// Full-form page for creating a new job with all details on one screen.
/// Now uses CustomerSelectionHelper for cleaner code.
/// </summary>
public partial class AddJobPage
{
    private readonly OneManVanDbContext _db;
    private readonly CustomerSelectionHelper _customerHelper;
    private List<Site> _customerSites = [];
    private List<Asset> _customerAssets = [];

    public AddJobPage(OneManVanDbContext db, CustomerPickerService customerPicker)
    {
        InitializeComponent();
        _db = db;
        _customerHelper = new CustomerSelectionHelper(this, customerPicker);
        InitializeDefaults();
    }

    private void InitializeDefaults()
    {
        ScheduledDatePicker.Date = DateTime.Today;
        ScheduledTimePicker.Time = TimeSpan.FromHours(DateTime.Now.Hour + 1);
        JobTypePicker.SelectedIndex = 0;
        PriorityPicker.SelectedIndex = 1;
        StatusPicker.SelectedIndex = 1;
        EstimatedHoursEntry.Text = "1";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _customerHelper.HandleQuickAddCustomerAsync(OnCustomerSelectedAsync);
    }

    private async void OnSelectCustomerTapped(object sender, TappedEventArgs e)
    {
        await _customerHelper.HandleCustomerSelectionAsync(OnCustomerSelectedAsync);
    }

    private async Task OnCustomerSelectedAsync(Customer customer)
    {
        // Update UI using helper
        CustomerSelectionHelper.UpdateCustomerUI(customer, CustomerNameLabel, CustomerDetailsPanel);
        CustomerPhoneLabel.Text = customer.Phone ?? "No phone on file";
        
        // Load customer sites
        _customerSites = await _db.Sites
            .Where(s => s.CustomerId == customer.Id)
            .ToListAsync();

        if (_customerSites.Count > 0)
        {
            var primarySite = _customerSites.FirstOrDefault(s => s.IsPrimary) ?? _customerSites[0];
            CustomerAddressLabel.Text = $"{primarySite.Address}, {primarySite.City}";
        }
        else
        {
            CustomerAddressLabel.Text = "No address on file";
        }

        if (_customerSites.Count > 1)
        {
            SiteSection.IsVisible = true;
            SitePicker.ItemsSource = _customerSites.Select(s => 
                $"{s.Address}{(s.IsPrimary ? " (Primary)" : "")}").ToList();
            SitePicker.SelectedIndex = _customerSites.FindIndex(s => s.IsPrimary);
            if (SitePicker.SelectedIndex < 0) SitePicker.SelectedIndex = 0;
        }
        else
        {
            SiteSection.IsVisible = false;
        }

        // Load customer assets
        _customerAssets = await _db.Assets
            .Where(a => a.CustomerId == customer.Id)
            .ToListAsync();

        if (_customerAssets.Count > 0)
        {
            var assetNames = new List<string> { "(None)" };
            assetNames.AddRange(_customerAssets.Select(a => 
                string.IsNullOrEmpty(a.Nickname) 
                    ? $"{a.Brand} {a.Model} - {a.Serial}"
                    : $"{a.Nickname} ({a.Brand})"));
            AssetPicker.ItemsSource = assetNames;
            AssetPicker.SelectedIndex = 0;
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (_customerHelper.SelectedCustomer == null)
        {
            await DisplayAlertAsync("Required", "Please select a customer.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(TitleEntry.Text))
        {
            await DisplayAlertAsync("Required", "Please enter a job title.", "OK");
            TitleEntry.Focus();
            return;
        }

        try
        {
            int? siteId = null;
            if (_customerSites.Count > 0)
            {
                var siteIndex = SitePicker.SelectedIndex;
                if (siteIndex >= 0 && siteIndex < _customerSites.Count)
                {
                    siteId = _customerSites[siteIndex].Id;
                }
                else
                {
                    siteId = _customerSites.FirstOrDefault(s => s.IsPrimary)?.Id ?? _customerSites[0].Id;
                }
            }

            int? assetId = null;
            if (AssetPicker.SelectedIndex > 0 && AssetPicker.SelectedIndex <= _customerAssets.Count)
            {
                assetId = _customerAssets[AssetPicker.SelectedIndex - 1].Id;
            }

            var jobType = JobTypePicker.SelectedIndex switch
            {
                0 => JobType.ServiceCall,
                1 => JobType.Repair,
                2 => JobType.Maintenance,
                3 => JobType.Installation,
                4 => JobType.Inspection,
                5 => JobType.Warranty,
                6 => JobType.Callback,
                _ => JobType.Other
            };

            var priority = PriorityPicker.SelectedIndex switch
            {
                0 => JobPriority.Low,
                1 => JobPriority.Normal,
                2 => JobPriority.High,
                3 => JobPriority.Urgent,
                4 => JobPriority.Emergency,
                _ => JobPriority.Normal
            };

            var status = StatusPicker.SelectedIndex == 0 ? JobStatus.Draft : JobStatus.Scheduled;
            var scheduledDate = ScheduledDatePicker.Date.GetValueOrDefault(DateTime.Today)
                .Add(ScheduledTimePicker.Time.GetValueOrDefault(TimeSpan.FromHours(9)));

            decimal estimatedHours = 1;
            if (decimal.TryParse(EstimatedHoursEntry.Text, out var hours))
            {
                estimatedHours = hours;
            }

            var job = new Job
            {
                CustomerId = _customerHelper.SelectedCustomer.Id,
                SiteId = siteId,
                AssetId = assetId,
                Title = TitleEntry.Text.Trim(),
                Description = DescriptionEditor.Text?.Trim(),
                JobType = jobType,
                Priority = priority,
                Status = status,
                ScheduledDate = scheduledDate,
                EstimatedHours = estimatedHours,
                InternalNotes = InternalNotesEditor.Text?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _db.Jobs.Add(job);
            await _db.SaveChangesAsync();

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

            await DisplayAlertAsync("Success", $"Job created for {_customerHelper.SelectedCustomer.Name}!", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to create job: {ex.Message}", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
