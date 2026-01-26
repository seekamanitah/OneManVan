using Microsoft.EntityFrameworkCore;
using OneManVan.Mobile.Helpers;
using OneManVan.Mobile.Services;
using OneManVan.Mobile.Theme;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Mobile.Pages;

public partial class EstimateListPage : ContentPage
{
    private readonly IDbContextFactory<OneManVanDbContext> _dbFactory;
    private readonly QuickAddCustomerService _quickAddCustomer;
    private CancellationTokenSource? _cts;
    private List<Estimate> _allEstimates = [];
    private string _searchText = string.Empty;
    private EstimateStatus? _activeFilter;
    private bool _pendingNewEstimate;

    public EstimateListPage(IDbContextFactory<OneManVanDbContext> dbFactory, QuickAddCustomerService quickAddCustomer)
    {
        InitializeComponent();
        _dbFactory = dbFactory;
        _quickAddCustomer = quickAddCustomer;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        
        try
        {
            await LoadEstimatesAsync(_cts.Token);

            // Check if we returned from adding a new customer
            if (_pendingNewEstimate && QuickAddCustomerService.LastCreatedCustomer != null)
            {
                _pendingNewEstimate = false;
                var customer = QuickAddCustomerService.LastCreatedCustomer;
                QuickAddCustomerService.LastCreatedCustomer = null;
                
                // Continue creating the estimate with the new customer
                await CreateEstimateForCustomerAsync(customer);
            }
        }
        catch (OperationCanceledException)
        {
            // Page navigated away
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"EstimateListPage.OnAppearing error: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _cts?.Cancel();
    }

    private async Task LoadEstimatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using (new LoadingScope(LoadingIndicator))
            {
                await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
                
                // PERF: AsNoTracking + removed Lines include (not displayed)
                _allEstimates = await db.Estimates
                    .AsNoTracking()
                    .Include(e => e.Customer)
                    .OrderByDescending(e => e.CreatedAt)
                    .ToListAsync(cancellationToken);

                if (!cancellationToken.IsCancellationRequested)
                {
                    ApplyFilters();
                    UpdateStats();
                }
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
                await DisplayAlertAsync("Error", $"Failed to load estimates: {ex.Message}", "OK");
            }
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allEstimates.AsEnumerable();

        // Apply search
        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            var search = _searchText.ToLowerInvariant();
            filtered = filtered.Where(e =>
                (e.Title?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (e.Customer?.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (e.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        // Apply status filter
        if (_activeFilter.HasValue)
        {
            filtered = filtered.Where(e => e.Status == _activeFilter.Value);
        }

        EstimateCollection.ItemsSource = filtered.ToList();
    }

    private void UpdateStats()
    {
        var total = _allEstimates.Count;
        var pending = _allEstimates.Count(e => e.Status == EstimateStatus.Draft || e.Status == EstimateStatus.Sent);
        var value = _allEstimates.Where(e => e.Status == EstimateStatus.Accepted).Sum(e => e.Total);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            TotalCountLabel.Text = total.ToString();
            PendingCountLabel.Text = pending.ToString();
            TotalValueLabel.Text = $"${value:N0}";
        });
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = e.NewTextValue ?? string.Empty;
        ApplyFilters();
    }

    private void OnFilterClicked(object sender, EventArgs e)
    {
        // Reset filter styles using AppColors
        AllFilter.BackgroundColor = AppColors.PrimarySurface;
        AllFilter.TextColor = AppColors.Primary;
        DraftFilter.BackgroundColor = AppColors.PrimarySurface;
        DraftFilter.TextColor = AppColors.Primary;
        SentFilter.BackgroundColor = AppColors.PrimarySurface;
        SentFilter.TextColor = AppColors.Primary;
        AcceptedFilter.BackgroundColor = AppColors.SuccessSurface;
        AcceptedFilter.TextColor = AppColors.Success;

        // Set active filter
        if (sender is Button button)
        {
            button.BackgroundColor = AppColors.Primary;
            button.TextColor = AppColors.TextOnDark;

            _activeFilter = button.Text switch
            {
                string s when s.Contains("Draft") => EstimateStatus.Draft,
                string s when s.Contains("Sent") => EstimateStatus.Sent,
                string s when s.Contains("Accepted") => EstimateStatus.Accepted,
                _ => null
            };
        }

        ApplyFilters();
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        await LoadEstimatesAsync(_cts.Token);
        RefreshViewControl.IsRefreshing = false;
    }

    private async void OnEstimateSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Estimate estimate)
        {
            EstimateCollection.SelectedItem = null;
            // Navigate to estimate edit page
            await Shell.Current.GoToAsync($"EditEstimate?id={estimate.Id}");
        }
    }

    private async void OnAddEstimateClicked(object sender, EventArgs e)
    {
        // Navigate to full AddEstimatePage
        await Shell.Current.GoToAsync("AddEstimate");
    }

    private async Task CreateEstimateForCustomerAsync(Customer customer)
    {
        // Create new estimate
        var title = await DisplayPromptAsync("New Estimate", "Enter estimate title:", placeholder: "e.g., AC Repair");
        if (string.IsNullOrWhiteSpace(title))
            return;

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            var estimate = new Estimate
            {
                CustomerId = customer.Id,
                Title = title,
                Status = EstimateStatus.Draft,
                CreatedAt = DateTime.UtcNow,
                TaxRate = 0.08m // 8% default
            };

            db.Estimates.Add(estimate);
            await db.SaveChangesAsync();
            
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            await LoadEstimatesAsync(_cts.Token);

            await DisplayAlertAsync("Success", $"Estimate created for {customer.Name}!", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to create estimate: {ex.Message}", "OK");
        }
    }

    private async void OnDeleteSwipe(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.BindingContext is Estimate estimate)
        {
            if (estimate.Status == EstimateStatus.Accepted)
            {
                await DisplayAlertAsync("Cannot Delete", "Accepted estimates cannot be deleted.", "OK");
                return;
            }

            var confirm = await DisplayAlertAsync(
                "Delete Estimate",
                $"Are you sure you want to delete '{estimate.Title}'?",
                "Delete",
                "Cancel");

            if (confirm)
            {
                try
                {
                    await using var db = await _dbFactory.CreateDbContextAsync();
                    var dbEstimate = await db.Estimates.FindAsync(estimate.Id);
                    if (dbEstimate != null)
                    {
                        db.Estimates.Remove(dbEstimate);
                        await db.SaveChangesAsync();
                    }
                    _cts?.Cancel();
                    _cts = new CancellationTokenSource();
                    await LoadEstimatesAsync(_cts.Token);
                }
                catch (Exception ex)
                {
                    await DisplayAlertAsync("Error", $"Failed to delete: {ex.Message}", "OK");
                }
            }
        }
    }

    private async void OnConvertToJobSwipe(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.BindingContext is Estimate estimate)
        {
            if (estimate.Status != EstimateStatus.Accepted)
            {
                await DisplayAlertAsync("Cannot Convert", "Only accepted estimates can be converted to jobs.", "OK");
                return;
            }

            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync();
                
                // Check if already converted
                var existingJob = await db.Jobs.FirstOrDefaultAsync(j => j.EstimateId == estimate.Id);
                if (existingJob != null)
                {
                    await DisplayAlertAsync("Already Converted", "This estimate has already been converted to a job.", "OK");
                    return;
                }

                var confirm = await DisplayAlertAsync(
                    "Convert to Job",
                    $"Convert '{estimate.Title}' to a job?",
                    "Convert",
                    "Cancel");

                if (confirm)
                {
                    var dbEstimate = await db.Estimates.FindAsync(estimate.Id);
                    if (dbEstimate != null)
                    {
                        var job = new Job
                        {
                            EstimateId = estimate.Id,
                            CustomerId = estimate.CustomerId,
                            SiteId = estimate.SiteId,
                            AssetId = estimate.AssetId,
                            Title = estimate.Title,
                            Description = estimate.Description,
                            Status = JobStatus.Draft,
                            CreatedAt = DateTime.UtcNow
                        };

                        dbEstimate.Status = EstimateStatus.Converted;
                        db.Jobs.Add(job);
                        await db.SaveChangesAsync();
                    }

                    _cts?.Cancel();
                    _cts = new CancellationTokenSource();
                    await LoadEstimatesAsync(_cts.Token);

                    await DisplayAlertAsync("Success", "Job created! View it in the Jobs tab.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"Failed to convert: {ex.Message}", "OK");
            }
        }
    }
}
