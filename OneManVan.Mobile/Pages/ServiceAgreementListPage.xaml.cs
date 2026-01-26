using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Mobile.Pages;

public partial class ServiceAgreementListPage : ContentPage
{
    private readonly OneManVanDbContext _db;
    private List<ServiceAgreement> _allAgreements = [];
    private ServiceAgreementFilter _activeFilter = ServiceAgreementFilter.All;

    public ServiceAgreementListPage(OneManVanDbContext db)
    {
        InitializeComponent();
        _db = db;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAgreementsAsync();
    }

    private async Task LoadAgreementsAsync()
    {
        try
        {
            _db.ChangeTracker.Clear();

            // PERF: AsNoTracking for read-only list
            _allAgreements = await _db.ServiceAgreements
                .AsNoTracking()
                .Include(a => a.Customer)
                .OrderByDescending(a => a.Status == AgreementStatus.Active)
                .ThenBy(a => a.EndDate)
                .ToListAsync();

            ApplyFilters();
            UpdateStats();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load agreements: {ex.Message}", "OK");
        }
    }

    private void ApplyFilters()
    {
        var filtered = _activeFilter switch
        {
            ServiceAgreementFilter.Active => _allAgreements.Where(a => a.IsActive),
            ServiceAgreementFilter.Expiring => _allAgreements.Where(a => a.IsExpiringSoon),
            ServiceAgreementFilter.DueForService => _allAgreements.Where(a => 
                a.IsActive && a.NextMaintenanceDue.HasValue && 
                a.NextMaintenanceDue.Value <= DateTime.Today.AddDays(30)),
            _ => _allAgreements
        };

        var viewModels = filtered.Select(a => new ServiceAgreementViewModel
        {
            Id = a.Id,
            Name = a.Name,
            CustomerName = a.Customer?.Name ?? "Unknown",
            TypeDisplay = a.TypeDisplay,
            StatusDisplay = a.StatusDisplay,
            AnnualPrice = a.AnnualPrice,
            VisitsUsed = a.VisitsUsed,
            VisitsIncluded = a.IncludedVisitsPerYear,
            EndDate = a.EndDate,
            IsActive = a.IsActive,
            IsExpiringSoon = a.IsExpiringSoon
        }).ToList();

        AgreementCollection.ItemsSource = viewModels;
    }

    private void UpdateStats()
    {
        var active = _allAgreements.Count(a => a.IsActive);
        var expiring = _allAgreements.Count(a => a.IsExpiringSoon);
        var revenue = _allAgreements.Where(a => a.IsActive).Sum(a => a.AnnualPrice);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            ActiveCountLabel.Text = active.ToString();
            ExpiringCountLabel.Text = expiring.ToString();
            RevenueLabel.Text = $"${revenue:N0}";
        });
    }

    private void OnFilterClicked(object sender, EventArgs e)
    {
        ResetFilterStyles();

        if (sender is Button button)
        {
            button.BackgroundColor = Color.FromArgb("#1976D2");
            button.TextColor = Colors.White;

            _activeFilter = button.Text switch
            {
                "Active" => ServiceAgreementFilter.Active,
                "Expiring Soon" => ServiceAgreementFilter.Expiring,
                "Due for Service" => ServiceAgreementFilter.DueForService,
                _ => ServiceAgreementFilter.All
            };
        }

        ApplyFilters();
    }

    private void ResetFilterStyles()
    {
        AllFilter.BackgroundColor = Color.FromArgb("#E3F2FD");
        AllFilter.TextColor = Color.FromArgb("#1976D2");
        ActiveFilter.BackgroundColor = Color.FromArgb("#E8F5E9");
        ActiveFilter.TextColor = Color.FromArgb("#4CAF50");
        ExpiringFilter.BackgroundColor = Color.FromArgb("#FFF3E0");
        ExpiringFilter.TextColor = Color.FromArgb("#FF9800");
        DueFilter.BackgroundColor = Color.FromArgb("#E3F2FD");
        DueFilter.TextColor = Color.FromArgb("#1976D2");
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadAgreementsAsync();
        RefreshViewControl.IsRefreshing = false;
    }

    private async void OnAgreementSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is ServiceAgreementViewModel vm)
        {
            AgreementCollection.SelectedItem = null;

            var agreement = _allAgreements.FirstOrDefault(a => a.Id == vm.Id);
            if (agreement == null) return;

            var details = $"Customer: {agreement.Customer?.Name}\n" +
                         $"Type: {agreement.TypeDisplay}\n" +
                         $"Status: {agreement.StatusDisplay}\n\n" +
                         $"Start: {agreement.StartDate:MMM dd, yyyy}\n" +
                         $"End: {agreement.EndDate:MMM dd, yyyy}\n\n" +
                         $"Visits: {agreement.VisitsUsed} / {agreement.IncludedVisitsPerYear} used\n" +
                         $"Annual Price: ${agreement.AnnualPrice:N2}\n" +
                         $"Repair Discount: {agreement.RepairDiscountPercent}%\n\n" +
                         $"Includes:\n" +
                         $"  - AC Tune-up: {(agreement.IncludesAcTuneUp ? "Yes" : "No")}\n" +
                         $"  - Heating Tune-up: {(agreement.IncludesHeatingTuneUp ? "Yes" : "No")}\n" +
                         $"  - Filter Replacement: {(agreement.IncludesFilterReplacement ? "Yes" : "No")}\n" +
                         $"  - Priority Service: {(agreement.PriorityService ? "Yes" : "No")}";


            if (agreement.NextMaintenanceDue.HasValue)
            {
                details += $"\n\nNext Maintenance: {agreement.NextMaintenanceDue.Value:MMM dd, yyyy}";
            }

            var action = await DisplayActionSheetAsync($"Agreement: {agreement.Name}", "Close", null, "Edit", "View Details");
            
            if (action == "Edit")
            {
                await Shell.Current.GoToAsync($"AddServiceAgreement?id={agreement.Id}");
            }
            else if (action == "View Details")
            {
                await DisplayAlertAsync($"Agreement: {agreement.Name}", details, "OK");
            }
        }
    }

    private async void OnAddAgreementClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AddServiceAgreement");
    }
}

public enum ServiceAgreementFilter
{
    All,
    Active,
    Expiring,
    DueForService
}

public class ServiceAgreementViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string TypeDisplay { get; set; } = string.Empty;
    public string StatusDisplay { get; set; } = string.Empty;
    public decimal AnnualPrice { get; set; }
    public int VisitsUsed { get; set; }
    public int VisitsIncluded { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsExpiringSoon { get; set; }

    public string VisitInfo => $"{VisitsUsed}/{VisitsIncluded} visits";
    
    public string ExpiryInfo => IsExpiringSoon 
        ? $"Expires in {(EndDate - DateTime.Today).Days} days" 
        : (IsActive ? $"Expires {EndDate:MMM yyyy}" : StatusDisplay);

    public string StatusIcon => IsActive ? (IsExpiringSoon ? "?" : "?") : "?";

    public Color StatusColor => IsActive 
        ? (IsExpiringSoon ? Color.FromArgb("#FFF3E0") : Color.FromArgb("#E8F5E9"))
        : Color.FromArgb("#F5F5F5");

    public Color ExpiryColor => IsExpiringSoon 
        ? Color.FromArgb("#FF9800") 
        : Color.FromArgb("#757575");
}
