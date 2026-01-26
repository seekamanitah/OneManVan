using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Dialogs;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;
using OneManVan.Services;

namespace OneManVan.Pages;

/// <summary>
/// Service Agreements DataGrid page with filtering, CRUD, and detail panel.
/// </summary>
public partial class ServiceAgreementsDataGridPage : UserControl
{
    private readonly OneManVanDbContext _dbContext;
    private List<ServiceAgreement> _allAgreements = [];
    private List<ServiceAgreement> _filteredAgreements = [];

    public ServiceAgreementsDataGridPage(OneManVanDbContext dbContext)
    {
        InitializeComponent();
        _dbContext = dbContext;
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadAgreementsAsync();
    }

    private async Task LoadAgreementsAsync()
    {
        try
        {
            _allAgreements = await _dbContext.ServiceAgreements
                .Include(a => a.Customer)
                .Include(a => a.Site)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            ApplyFilters();
            UpdateStats();
            LastRefreshText.Text = $"Last updated: {DateTime.Now:h:mm tt}";
        }
        catch (Exception ex)
        {
            ToastService.Error($"Failed to load agreements: {ex.Message}");
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allAgreements.AsEnumerable();

        // Search filter
        var searchText = SearchBox.Text?.Trim().ToLower() ?? "";
        if (!string.IsNullOrEmpty(searchText))
        {
            filtered = filtered.Where(a =>
                (a.AgreementNumber?.ToLower().Contains(searchText) ?? false) ||
                (a.Name?.ToLower().Contains(searchText) ?? false) ||
                (a.Customer?.Name?.ToLower().Contains(searchText) ?? false) ||
                (a.Description?.ToLower().Contains(searchText) ?? false));
        }

        // Status filter
        var statusFilter = (StatusFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All Statuses";
        filtered = statusFilter switch
        {
            "Active" => filtered.Where(a => a.IsActive && !a.IsExpiringSoon),
            "Expiring Soon" => filtered.Where(a => a.IsExpiringSoon),
            "Draft" => filtered.Where(a => a.Status == AgreementStatus.Draft),
            "Pending" => filtered.Where(a => a.Status == AgreementStatus.Pending),
            "Expired" => filtered.Where(a => a.IsExpired || a.Status == AgreementStatus.Expired),
            "Cancelled" => filtered.Where(a => a.Status == AgreementStatus.Cancelled),
            _ => filtered
        };

        // Type filter
        var typeFilter = (TypeFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All Types";
        if (typeFilter != "All Types")
        {
            var agreementType = typeFilter switch
            {
                "Basic" => AgreementType.Basic,
                "Standard" => AgreementType.Standard,
                "Premium" => AgreementType.Premium,
                "Annual" => AgreementType.Annual,
                "Semi-Annual" => AgreementType.SemiAnnual,
                "Quarterly" => AgreementType.Quarterly,
                "Custom" => AgreementType.Custom,
                _ => (AgreementType?)null
            };
            if (agreementType.HasValue)
            {
                filtered = filtered.Where(a => a.Type == agreementType.Value);
            }
        }

        // Quick filters
        if (ActiveRadio.IsChecked == true)
        {
            filtered = filtered.Where(a => a.IsActive);
        }
        else if (DueForServiceRadio.IsChecked == true)
        {
            filtered = filtered.Where(a => a.IsActive && a.NextMaintenanceDue <= DateTime.Today.AddDays(30));
        }
        else if (ExpiringRadio.IsChecked == true)
        {
            filtered = filtered.Where(a => a.IsExpiringSoon);
        }

        _filteredAgreements = filtered.ToList();
        AgreementsGrid.ItemsSource = _filteredAgreements;

        // Update clear search button visibility
        ClearSearchButton.Visibility = string.IsNullOrEmpty(searchText) 
            ? Visibility.Collapsed 
            : Visibility.Visible;

        UpdateSubtitle();
    }

    private void UpdateStats()
    {
        var total = _allAgreements.Count;
        var active = _allAgreements.Count(a => a.IsActive);
        var expiring = _allAgreements.Count(a => a.IsExpiringSoon);
        var totalValue = _allAgreements
            .Where(a => a.IsActive)
            .Sum(a => a.AnnualPrice);

        TotalCountText.Text = $"Total: {total} agreements";
        ActiveCountText.Text = $"Active: {active}";
        ExpiringCountText.Text = $"Expiring: {expiring}";
        TotalValueText.Text = $"Annual Value: {totalValue:C0}";
    }

    private void UpdateSubtitle()
    {
        var showing = _filteredAgreements.Count;
        var total = _allAgreements.Count;
        SubtitleText.Text = showing == total
            ? "Maintenance contracts and recurring services"
            : $"Showing {showing} of {total} agreements";
    }

    #region Event Handlers

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Text = "";
    }

    private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded) ApplyFilters();
    }

    private void TypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded) ApplyFilters();
    }

    private void QuickFilter_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded) ApplyFilters();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadAgreementsAsync();
        ToastService.Success("Agreements refreshed");
    }

    private void AgreementsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AgreementsGrid.SelectedItem is ServiceAgreement agreement)
        {
            ShowAgreementDetail(agreement);
        }
        else
        {
            HideAgreementDetail();
        }
    }

    private void AgreementsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (AgreementsGrid.SelectedItem is ServiceAgreement agreement)
        {
            EditAgreement(agreement);
        }
    }

    private void RowMenuButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.ContextMenu != null)
        {
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.IsOpen = true;
        }
    }

    #endregion

    #region CRUD Operations

    private async void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddEditServiceAgreementDialog(_dbContext);
        if (dialog.ShowDialog() == true && dialog.Agreement != null)
        {
            // Generate agreement number
            var lastNumber = await _dbContext.ServiceAgreements
                .OrderByDescending(a => a.Id)
                .Select(a => a.Id)
                .FirstOrDefaultAsync();
            dialog.Agreement.AgreementNumber = ServiceAgreement.GenerateAgreementNumber(lastNumber);

            _dbContext.ServiceAgreements.Add(dialog.Agreement);
            await _dbContext.SaveChangesAsync();
            await LoadAgreementsAsync();
            ToastService.Success("Agreement created successfully");
        }
    }

    private void EditMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (GetSelectedAgreement(sender) is ServiceAgreement agreement)
        {
            EditAgreement(agreement);
        }
    }

    private void EditDetailButton_Click(object sender, RoutedEventArgs e)
    {
        if (AgreementsGrid.SelectedItem is ServiceAgreement agreement)
        {
            EditAgreement(agreement);
        }
    }

    private async void EditAgreement(ServiceAgreement agreement)
    {
        var dialog = new AddEditServiceAgreementDialog(_dbContext, agreement);
        if (dialog.ShowDialog() == true)
        {
            await _dbContext.SaveChangesAsync();
            await LoadAgreementsAsync();
            ToastService.Success("Agreement updated successfully");
        }
    }

    private async void CancelMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (GetSelectedAgreement(sender) is not ServiceAgreement agreement) return;

        var result = MessageBox.Show(
            $"Are you sure you want to cancel agreement '{agreement.Name}'?\n\nThis cannot be undone.",
            "Cancel Agreement",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            var reason = DialogService.ShowInputDialog(
                "Cancellation Reason",
                "Please enter the reason for cancellation:");

            agreement.Status = AgreementStatus.Cancelled;
            agreement.CancelledAt = DateTime.Now;
            agreement.CancellationReason = reason;
            agreement.UpdatedAt = DateTime.Now;

            await _dbContext.SaveChangesAsync();
            await LoadAgreementsAsync();
            ToastService.Success("Agreement cancelled");
        }
    }

    private async void SuspendMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (GetSelectedAgreement(sender) is not ServiceAgreement agreement) return;

        agreement.Status = AgreementStatus.Suspended;
        agreement.UpdatedAt = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        await LoadAgreementsAsync();
        ToastService.Warning("Agreement suspended");
    }

    private async void RenewMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (GetSelectedAgreement(sender) is not ServiceAgreement agreement) return;

        await RenewAgreement(agreement);
    }

    private async void RenewButton_Click(object sender, RoutedEventArgs e)
    {
        if (AgreementsGrid.SelectedItem is ServiceAgreement agreement)
        {
            await RenewAgreement(agreement);
        }
    }

    private async Task RenewAgreement(ServiceAgreement agreement)
    {
        var result = MessageBox.Show(
            $"Renew agreement '{agreement.Name}' for another year?\n\nThis will extend the end date by 1 year and reset visits used.",
            "Renew Agreement",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            agreement.StartDate = agreement.EndDate.AddDays(1);
            agreement.EndDate = agreement.StartDate.AddYears(1);
            agreement.VisitsUsed = 0;
            agreement.Status = AgreementStatus.Active;
            agreement.ActivatedAt = DateTime.Now;
            agreement.UpdatedAt = DateTime.Now;
            agreement.NextMaintenanceDue = agreement.CalculateNextMaintenanceDue();

            await _dbContext.SaveChangesAsync();
            await LoadAgreementsAsync();
            ToastService.Success("Agreement renewed successfully");
        }
    }

    #endregion

    #region Maintenance Scheduling

    private void ScheduleMaintenanceMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (GetSelectedAgreement(sender) is ServiceAgreement agreement)
        {
            ScheduleMaintenance(agreement);
        }
    }

    private void ScheduleDetailButton_Click(object sender, RoutedEventArgs e)
    {
        if (AgreementsGrid.SelectedItem is ServiceAgreement agreement)
        {
            ScheduleMaintenance(agreement);
        }
    }

    private void ScheduleMaintenanceButton_Click(object sender, RoutedEventArgs e)
    {
        if (AgreementsGrid.SelectedItem is ServiceAgreement agreement)
        {
            ScheduleMaintenance(agreement);
        }
    }

    private async void ScheduleMaintenance(ServiceAgreement agreement)
    {
        if (agreement.VisitsRemaining <= 0)
        {
            MessageBox.Show(
                "This agreement has no remaining visits for this contract period.",
                "No Visits Remaining",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        // Create a maintenance job
        var job = new Job
        {
            CustomerId = agreement.CustomerId,
            SiteId = agreement.SiteId,
            Title = $"Scheduled Maintenance - {agreement.Name}",
            Description = $"Scheduled maintenance visit under service agreement {agreement.AgreementNumber}",
            Status = JobStatus.Scheduled,
            ScheduledDate = agreement.NextMaintenanceDue ?? DateTime.Today.AddDays(7),
            EstimatedHours = 2m,
            CreatedAt = DateTime.Now
        };

        _dbContext.Jobs.Add(job);

        // Update agreement
        agreement.VisitsUsed++;
        agreement.LastMaintenanceScheduled = DateTime.Now;
        agreement.NextMaintenanceDue = agreement.CalculateNextMaintenanceDue();
        agreement.UpdatedAt = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        await LoadAgreementsAsync();

        ToastService.Success($"Maintenance job scheduled for {job.ScheduledDate:MMM d, yyyy}");
    }

    #endregion

    #region Detail Panel

    private void ShowAgreementDetail(ServiceAgreement agreement)
    {
        EmptyDetailPanel.Visibility = Visibility.Collapsed;
        AgreementDetailPanel.Visibility = Visibility.Visible;

        // Header
        DetailAgreementNumber.Text = agreement.AgreementNumber ?? "No #";
        DetailAgreementName.Text = agreement.Name;

        // Status
        DetailStatusText.Text = agreement.StatusDisplay;
        DetailStatusBadge.Background = agreement.Status switch
        {
            AgreementStatus.Active when agreement.IsExpiringSoon => FindResource("StatusExpiringSoonBrush") as System.Windows.Media.Brush,
            AgreementStatus.Active => FindResource("StatusActiveBrush") as System.Windows.Media.Brush,
            AgreementStatus.Draft => FindResource("StatusDraftBrush") as System.Windows.Media.Brush,
            AgreementStatus.Pending => FindResource("StatusPendingBrush") as System.Windows.Media.Brush,
            AgreementStatus.Expired => FindResource("StatusExpiredBrush") as System.Windows.Media.Brush,
            AgreementStatus.Cancelled => FindResource("StatusCancelledBrush") as System.Windows.Media.Brush,
            _ => FindResource("StatusDraftBrush") as System.Windows.Media.Brush
        };

        // Customer
        DetailCustomerName.Text = agreement.Customer?.Name ?? "Unknown";
        DetailCustomerContact.Text = agreement.Customer?.Phone ?? "No phone";

        // Term
        DetailStartDate.Text = agreement.StartDate.ToString("MMM d, yyyy");
        DetailEndDate.Text = agreement.EndDate.ToString("MMM d, yyyy");

        // Pricing
        DetailAnnualPrice.Text = agreement.AnnualPrice.ToString("C0");
        DetailBillingFrequency.Text = agreement.BillingFrequency.ToString();
        DetailRepairDiscount.Text = agreement.RepairDiscountPercent > 0 
            ? $"{agreement.RepairDiscountPercent:F0}% off" 
            : "None";

        // Visits
        DetailVisitsProgress.Text = $"{agreement.VisitsUsed} of {agreement.IncludedVisitsPerYear} visits used";
        DetailVisitsBar.Maximum = agreement.IncludedVisitsPerYear;
        DetailVisitsBar.Value = agreement.VisitsUsed;
        DetailVisitsRemaining.Text = $"{agreement.VisitsRemaining} remaining";

        // Services
        DetailServicesPanel.Children.Clear();
        if (agreement.IncludesAcTuneUp)
            AddServiceItem("? AC Tune-Up");
        if (agreement.IncludesHeatingTuneUp)
            AddServiceItem("? Heating Tune-Up");
        if (agreement.IncludesFilterReplacement)
            AddServiceItem("? Filter Replacement");
        if (agreement.IncludesRefrigerantTopOff)
            AddServiceItem($"? Refrigerant Top-Off (up to {agreement.MaxRefrigerantLbsIncluded} lbs)");
        if (agreement.IncludesLimitedPartsCoverage)
            AddServiceItem($"? Parts Coverage (up to {agreement.MaxPartsCoverageAmount:C0})");
        if (agreement.PriorityService)
            AddServiceItem("? Priority Service");
        if (agreement.WaiveTripCharge)
            AddServiceItem("?? Trip Charge Waived");

        // Next maintenance
        if (agreement.NextMaintenanceDue.HasValue)
        {
            DetailNextMaintenanceDate.Text = agreement.NextMaintenanceDue.Value.ToString("MMMM d, yyyy");
            var month = agreement.NextMaintenanceDue.Value.Month;
            DetailNextMaintenanceType.Text = month >= 3 && month <= 5 
                ? "Spring AC Tune-Up" 
                : month >= 9 && month <= 11 
                    ? "Fall Heating Tune-Up" 
                    : "Scheduled Maintenance";
        }
        else
        {
            DetailNextMaintenanceDate.Text = "Not scheduled";
            DetailNextMaintenanceType.Text = "Click to schedule";
        }
    }

    private void AddServiceItem(string text)
    {
        var textBlock = new TextBlock
        {
            Text = text,
            FontSize = 12,
            Margin = new Thickness(0, 2, 0, 2)
        };
        DetailServicesPanel.Children.Add(textBlock);
    }

    private void HideAgreementDetail()
    {
        EmptyDetailPanel.Visibility = Visibility.Visible;
        AgreementDetailPanel.Visibility = Visibility.Collapsed;
    }

    #endregion

    #region Export

    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export Service Agreements",
                Filter = "CSV Files|*.csv",
                FileName = $"ServiceAgreements_{DateTime.Now:yyyyMMdd}.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Agreement #,Customer,Name,Type,Status,Start Date,End Date,Annual Price,Visits/Year,Visits Used,Repair Discount %");

                foreach (var a in _filteredAgreements)
                {
                    sb.AppendLine($"\"{a.AgreementNumber}\",\"{a.Customer?.Name}\",\"{a.Name}\",\"{a.TypeDisplay}\",\"{a.StatusDisplay}\",{a.StartDate:yyyy-MM-dd},{a.EndDate:yyyy-MM-dd},{a.AnnualPrice},{a.IncludedVisitsPerYear},{a.VisitsUsed},{a.RepairDiscountPercent}");
                }

                await File.WriteAllTextAsync(dialog.FileName, sb.ToString());
                ToastService.Success($"Exported to {dialog.FileName}");
            }
        }
        catch (Exception ex)
        {
            ToastService.Error($"Export failed: {ex.Message}");
        }
    }

    #endregion

    #region Helpers

    private ServiceAgreement? GetSelectedAgreement(object sender)
    {
        if (sender is MenuItem menuItem)
        {
            var contextMenu = menuItem.Parent as ContextMenu;
            var button = contextMenu?.PlacementTarget as Button;
            return button?.DataContext as ServiceAgreement;
        }
        return AgreementsGrid.SelectedItem as ServiceAgreement;
    }

    #endregion
}
