using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;
using OneManVan.Services;

namespace OneManVan.Dialogs;

/// <summary>
/// Dialog for adding or editing a job/service call.
/// </summary>
public partial class AddEditJobDialog : Window
{
    private readonly OneManVanDbContext _dbContext;
    private readonly Job? _existingJob;
    private readonly int? _preselectedCustomerId;
    public Job? SavedJob { get; private set; }

    private List<Customer> _customers = [];
    private List<Site> _customerSites = [];
    private List<Asset> _customerAssets = [];

    public AddEditJobDialog(OneManVanDbContext dbContext, Job? job = null, int? customerId = null)
    {
        InitializeComponent();
        _dbContext = dbContext;
        _existingJob = job;
        _preselectedCustomerId = customerId ?? job?.CustomerId;

        InitializeComboBoxes();
        LoadCustomersAsync();

        if (job != null)
        {
            Title = "Edit Job";
            HeaderTitle.Text = "Edit Job";
            SaveButton.Content = "Save Changes";
            LoadJobData(job);
        }
        else
        {
            Title = "Add Job";
            // Set defaults for new job
            StatusCombo.SelectedIndex = 0; // Draft
            PriorityCombo.SelectedIndex = 1; // Normal
            JobTypeCombo.SelectedIndex = 0; // ServiceCall
            ScheduledDatePicker.SelectedDate = DateTime.Today;
            DurationTextBox.Text = "60";
        }
    }

    private void InitializeComboBoxes()
    {
        // Job Type
        JobTypeCombo.Items.Clear();
        foreach (JobType type in Enum.GetValues<JobType>())
        {
            JobTypeCombo.Items.Add(new ComboBoxItem
            {
                Content = GetJobTypeDisplayName(type),
                Tag = type
            });
        }

        // Priority
        PriorityCombo.Items.Clear();
        foreach (JobPriority priority in Enum.GetValues<JobPriority>())
        {
            PriorityCombo.Items.Add(new ComboBoxItem
            {
                Content = GetPriorityDisplayName(priority),
                Tag = priority
            });
        }

        // Status
        StatusCombo.Items.Clear();
        foreach (JobStatus status in Enum.GetValues<JobStatus>())
        {
            StatusCombo.Items.Add(new ComboBoxItem
            {
                Content = status.ToString(),
                Tag = status
            });
        }
    }

    private static string GetJobTypeDisplayName(JobType type) => type switch
    {
        JobType.ServiceCall => "Service Call",
        JobType.StartUp => "Start-Up",
        _ => type.ToString()
    };

    private static string GetPriorityDisplayName(JobPriority priority) => priority switch
    {
        JobPriority.Low => "Low",
        JobPriority.Normal => "Normal",
        JobPriority.High => "High",
        JobPriority.Urgent => "Urgent",
        JobPriority.Emergency => "Emergency",
        _ => priority.ToString()
    };

    private async void LoadCustomersAsync()
    {
        try
        {
            _customers = await _dbContext.Customers
                .Where(c => c.Status == CustomerStatus.Active)
                .OrderBy(c => c.Name)
                .ToListAsync();

            CustomerCombo.Items.Clear();
            CustomerCombo.Items.Add(new ComboBoxItem { Content = "-- Select Customer --", Tag = null });

            foreach (var customer in _customers)
            {
                CustomerCombo.Items.Add(new ComboBoxItem
                {
                    Content = customer.Name,
                    Tag = customer
                });
            }

            CustomerCombo.SelectedIndex = 0;

            // Preselect customer if provided
            if (_preselectedCustomerId.HasValue)
            {
                var customer = _customers.FirstOrDefault(c => c.Id == _preselectedCustomerId.Value);
                if (customer != null)
                {
                    SelectComboByTag(CustomerCombo, customer);
                    await LoadCustomerSitesAsync(customer.Id);
                }
            }
        }
        catch (Exception ex)
        {
            ToastService.Error($"Failed to load customers: {ex.Message}");
        }
    }

    private async void OnCustomerChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedItem = CustomerCombo.SelectedItem as ComboBoxItem;
        var customer = selectedItem?.Tag as Customer;

        SiteCombo.Items.Clear();
        AssetCombo.Items.Clear();
        _customerSites.Clear();
        _customerAssets.Clear();

        if (customer != null)
        {
            await LoadCustomerSitesAsync(customer.Id);
        }
    }

    private async Task LoadCustomerSitesAsync(int customerId)
    {
        try
        {
            _customerSites = await _dbContext.Sites
                .Where(s => s.CustomerId == customerId)
                .OrderBy(s => s.Address)
                .ToListAsync();

            SiteCombo.Items.Clear();
            SiteCombo.Items.Add(new ComboBoxItem { Content = "-- No Site --", Tag = null });

            foreach (var site in _customerSites)
            {
                SiteCombo.Items.Add(new ComboBoxItem
                {
                    Content = site.Address,
                    Tag = site
                });
            }

            SiteCombo.SelectedIndex = 0;

            // Load all customer assets (not filtered by site initially)
            await LoadCustomerAssetsAsync(customerId);
        }
        catch (Exception ex)
        {
            ToastService.Error($"Failed to load sites: {ex.Message}");
        }
    }

    private async void OnSiteChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedItem = SiteCombo.SelectedItem as ComboBoxItem;
        var site = selectedItem?.Tag as Site;

        var selectedCustomer = (CustomerCombo.SelectedItem as ComboBoxItem)?.Tag as Customer;
        if (selectedCustomer == null)
            return;

        // Reload assets filtered by site if selected
        if (site != null)
        {
            await LoadSiteAssetsAsync(site.Id);
        }
        else
        {
            // Show all customer assets if no site selected
            await LoadCustomerAssetsAsync(selectedCustomer.Id);
        }
    }

    private async Task LoadCustomerAssetsAsync(int customerId)
    {
        try
        {
            _customerAssets = await _dbContext.Assets
                .Where(a => a.CustomerId == customerId)
                .OrderBy(a => a.Nickname)
                .ToListAsync();

            PopulateAssetCombo();
        }
        catch (Exception ex)
        {
            ToastService.Error($"Failed to load assets: {ex.Message}");
        }
    }

    private async Task LoadSiteAssetsAsync(int siteId)
    {
        try
        {
            _customerAssets = await _dbContext.Assets
                .Where(a => a.SiteId == siteId)
                .OrderBy(a => a.Nickname)
                .ToListAsync();

            PopulateAssetCombo();
        }
        catch (Exception ex)
        {
            ToastService.Error($"Failed to load assets: {ex.Message}");
        }
    }

    private void PopulateAssetCombo()
    {
        AssetCombo.Items.Clear();
        AssetCombo.Items.Add(new ComboBoxItem { Content = "-- No Asset --", Tag = null });

        foreach (var asset in _customerAssets)
        {
            var display = $"{asset.Nickname ?? asset.Brand} {asset.Model}";
            if (!string.IsNullOrEmpty(asset.Serial))
                display += $" ({asset.Serial})";

            AssetCombo.Items.Add(new ComboBoxItem
            {
                Content = display,
                Tag = asset
            });
        }

        AssetCombo.SelectedIndex = 0;
    }

    private void LoadJobData(Job job)
    {
        TitleTextBox.Text = job.Title;
        DescriptionTextBox.Text = job.Description;
        JobNumberTextBox.Text = job.JobNumber;

        SelectComboByTag(JobTypeCombo, job.JobType);
        SelectComboByTag(PriorityCombo, job.Priority);
        SelectComboByTag(StatusCombo, job.Status);

        if (job.ScheduledDate.HasValue)
            ScheduledDatePicker.SelectedDate = job.ScheduledDate.Value;

        if (job.ArrivalWindowStart.HasValue)
            StartTimeTextBox.Text = job.ArrivalWindowStart.Value.ToString("h:mm tt");

        if (job.EstimatedHours > 0)
            DurationTextBox.Text = ((int)(job.EstimatedHours * 60)).ToString();

        // Financial fields removed - Job model doesn't have EstimatedTotal/ActualTotal
        // These would be calculated from JobLineItems or come from Estimate/Invoice
    }

    private void OnDurationChanged(object sender, TextChangedEventArgs e)
    {
        // Auto-calculate end time if start time and duration are provided
        if (string.IsNullOrWhiteSpace(StartTimeTextBox.Text) || 
            string.IsNullOrWhiteSpace(DurationTextBox.Text))
        {
            EndTimeTextBox.Text = string.Empty;
            return;
        }

        if (TimeSpan.TryParse(StartTimeTextBox.Text, out var startTime) &&
            int.TryParse(DurationTextBox.Text, out var durationMinutes))
        {
            var endTime = startTime.Add(TimeSpan.FromMinutes(durationMinutes));
            EndTimeTextBox.Text = endTime.ToString(@"h\:mm");
        }
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        try
        {
            // Validation
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                ToastService.Warning("Please enter a job title");
                TitleTextBox.Focus();
                return;
            }

            var selectedCustomer = (CustomerCombo.SelectedItem as ComboBoxItem)?.Tag as Customer;
            if (selectedCustomer == null)
            {
                ToastService.Warning("Please select a customer");
                CustomerCombo.Focus();
                return;
            }

            var selectedJobType = (JobTypeCombo.SelectedItem as ComboBoxItem)?.Tag as JobType?;
            var selectedPriority = (PriorityCombo.SelectedItem as ComboBoxItem)?.Tag as JobPriority?;
            var selectedStatus = (StatusCombo.SelectedItem as ComboBoxItem)?.Tag as JobStatus?;

            if (!selectedJobType.HasValue || !selectedPriority.HasValue || !selectedStatus.HasValue)
            {
                ToastService.Warning("Please select job type, priority, and status");
                return;
            }

            // Create or update job
            var job = _existingJob ?? new Job
            {
                CreatedAt = DateTime.Now
            };

            job.Title = TitleTextBox.Text.Trim();
            job.Description = DescriptionTextBox.Text?.Trim();
            job.CustomerId = selectedCustomer.Id;

            var selectedSite = (SiteCombo.SelectedItem as ComboBoxItem)?.Tag as Site;
            job.SiteId = selectedSite?.Id;

            var selectedAsset = (AssetCombo.SelectedItem as ComboBoxItem)?.Tag as Asset;
            job.AssetId = selectedAsset?.Id;

            job.JobType = selectedJobType.Value;
            job.Priority = selectedPriority.Value;
            job.Status = selectedStatus.Value;
            job.ScheduledDate = ScheduledDatePicker.SelectedDate;

            // Parse start time
            if (!string.IsNullOrWhiteSpace(StartTimeTextBox.Text) && 
                job.ScheduledDate.HasValue &&
                TimeSpan.TryParse(StartTimeTextBox.Text, out var startTime))
            {
                job.ArrivalWindowStart = startTime;
                job.ArrivalWindowEnd = startTime.Add(TimeSpan.FromMinutes(30)); // Default 30 min window
            }

            // Parse duration (convert minutes to hours)
            if (int.TryParse(DurationTextBox.Text, out var durationMinutes))
            {
                job.EstimatedHours = durationMinutes / 60.0m;
            }

            job.UpdatedAt = DateTime.Now;

            // Save to database
            if (_existingJob == null)
            {
                _dbContext.Jobs.Add(job);
            }

            await _dbContext.SaveChangesAsync();

            SavedJob = job;
            ToastService.Success(_existingJob == null ? "Job created successfully" : "Job updated successfully");
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ToastService.Error($"Failed to save job: {ex.Message}");
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private static void SelectComboByTag(ComboBox combo, object? tag)
    {
        if (tag == null)
        {
            combo.SelectedIndex = 0;
            return;
        }

        foreach (ComboBoxItem item in combo.Items)
        {
            if (item.Tag?.Equals(tag) == true)
            {
                combo.SelectedItem = item;
                return;
            }
        }
    }
}
