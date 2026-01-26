using Microsoft.EntityFrameworkCore;
using OneManVan.Mobile.Helpers;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Mobile.Pages;

[QueryProperty(nameof(EstimateId), "id")]
public partial class EstimateDetailPage : ContentPage
{
    private readonly OneManVanDbContext _db;
    private Estimate? _estimate;
    
    public int EstimateId { get; set; }

    public EstimateDetailPage(OneManVanDbContext db)
    {
        InitializeComponent();
        _db = db;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadEstimateAsync();
    }

    private async Task LoadEstimateAsync()
    {
        try
        {
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            _estimate = await _db.Estimates
                .Include(e => e.Customer)
                .Include(e => e.Site)
                .Include(e => e.Lines)
                .FirstOrDefaultAsync(e => e.Id == EstimateId);

            if (_estimate == null)
            {
                await DisplayAlertAsync("Error", "Estimate not found", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            UpdateUI();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load estimate: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    private void UpdateUI()
    {
        if (_estimate == null) return;

        // Header
        EstimateNumberLabel.Text = $"EST-{_estimate.Id:D4}";
        // StatusBadge is a Border - update via child label or just set background
        StatusBadge.BackgroundColor = _estimate.Status switch
        {
            EstimateStatus.Draft => Color.FromArgb("#FFF3CD"),
            EstimateStatus.Sent => Color.FromArgb("#CCE5FF"),
            EstimateStatus.Accepted => Color.FromArgb("#D1E7DD"),
            EstimateStatus.Declined => Color.FromArgb("#F8D7DA"),
            EstimateStatus.Converted => Color.FromArgb("#D1E7DD"),
            _ => Color.FromArgb("#E9ECEF")
        };

        // Customer Info
        CustomerNameLabel.Text = _estimate.Customer?.Name ?? "Unknown";
        CustomerEmailLabel.Text = _estimate.Customer?.Email ?? "";
        CustomerPhoneLabel.Text = _estimate.Customer?.Phone ?? "";

        // Details
        TitleLabel.Text = _estimate.Title;
        DescriptionLabel.Text = _estimate.Description;
        DateLabel.Text = _estimate.CreatedAt.ToShortDate();
        ValidUntilLabel.Text = _estimate.ExpiresAt.ToShortDate("N/A");

        // Line Items
        LineItemsCollection.ItemsSource = _estimate.Lines;

        // Totals
        SubtotalLabel.Text = $"${_estimate.SubTotal:N2}";
        TaxLabel.Text = $"${_estimate.TaxAmount:N2}";
        TotalLabel.Text = $"${_estimate.Total:N2}";

        // Action buttons based on status
        UpdateActionButtons();
    }

    private void UpdateActionButtons()
    {
        if (_estimate == null) return;

        EditButton.IsVisible = _estimate.Status == EstimateStatus.Draft;
        SendButton.IsVisible = _estimate.Status == EstimateStatus.Draft;
        AcceptButton.IsVisible = _estimate.Status == EstimateStatus.Sent;
        RejectButton.IsVisible = _estimate.Status == EstimateStatus.Sent;
        ConvertToJobButton.IsVisible = _estimate.Status == EstimateStatus.Accepted;
    }

    private async void OnEditClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"EditEstimate?id={EstimateId}");
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        var confirm = await DisplayAlertAsync("Send Estimate", 
            "Mark this estimate as sent to customer?", "Send", "Cancel");

        if (!confirm) return;

        try
        {
            _estimate!.Status = EstimateStatus.Sent;
            _estimate.SentAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            
            await LoadEstimateAsync();
            await DisplayAlertAsync("Success", "Estimate marked as sent", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to send estimate: {ex.Message}", "OK");
        }
    }

    private async void OnAcceptClicked(object sender, EventArgs e)
    {
        var confirm = await DisplayAlertAsync("Accept Estimate", 
            "Mark this estimate as accepted by customer?", "Accept", "Cancel");

        if (!confirm) return;

        try
        {
            _estimate!.Status = EstimateStatus.Accepted;
            _estimate.AcceptedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            
            await LoadEstimateAsync();
            await DisplayAlertAsync("Success", "Estimate marked as accepted", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to accept estimate: {ex.Message}", "OK");
        }
    }

    private async void OnRejectClicked(object sender, EventArgs e)
    {
        var reason = await DisplayPromptAsync("Decline Estimate", 
            "Enter decline reason (optional):", placeholder: "Customer declined");

        try
        {
            _estimate!.Status = EstimateStatus.Declined;
            _estimate.Notes = $"{_estimate.Notes}\n\nDeclined: {reason}".Trim();
            await _db.SaveChangesAsync();

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            
            await LoadEstimateAsync();
            await DisplayAlertAsync("Success", "Estimate marked as declined", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to reject estimate: {ex.Message}", "OK");
        }
    }

    private async void OnConvertToJobClicked(object sender, EventArgs e)
    {
        var confirm = await DisplayAlertAsync("Convert to Job", 
            $"Convert this estimate to a job?\n\nTotal: ${_estimate!.Total:N2}", 
            "Convert", "Cancel");

        if (!confirm) return;

        try
        {
            var job = new Job
            {
                EstimateId = _estimate.Id,
                CustomerId = _estimate.CustomerId,
                SiteId = _estimate.SiteId,
                AssetId = _estimate.AssetId,
                Title = _estimate.Title,
                Description = _estimate.Description,
                Status = JobStatus.Draft,
                Priority = JobPriority.Normal,
                EstimatedHours = _estimate.Lines.Sum(l => l.Quantity),
                LaborTotal = _estimate.Lines.Where(l => l.Type == LineItemType.Labor).Sum(l => l.Total),
                PartsTotal = _estimate.Lines.Where(l => l.Type != LineItemType.Labor).Sum(l => l.Total),
                SubTotal = _estimate.SubTotal,
                TaxRate = _estimate.TaxRate,
                TaxAmount = _estimate.TaxAmount,
                Total = _estimate.Total,
                CreatedAt = DateTime.UtcNow
            };

            await _db.Jobs.AddAsync(job);
            
            _estimate.Status = EstimateStatus.Converted;
            await _db.SaveChangesAsync();

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

            await DisplayAlertAsync("Success", "Estimate converted to job!", "OK");
            await Shell.Current.GoToAsync($"JobDetail?id={job.Id}");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to convert estimate: {ex.Message}", "OK");
        }
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        var confirm = await DisplayAlertAsync("Delete Estimate", 
            "Are you sure you want to delete this estimate?", "Delete", "Cancel");

        if (!confirm) return;

        try
        {
            _db.Estimates.Remove(_estimate!);
            await _db.SaveChangesAsync();

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            
            await DisplayAlertAsync("Deleted", "Estimate has been deleted", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to delete estimate: {ex.Message}", "OK");
        }
    }
}
