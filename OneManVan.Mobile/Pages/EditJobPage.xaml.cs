using Microsoft.EntityFrameworkCore;
using OneManVan.Mobile.Extensions;
using OneManVan.Mobile.Helpers;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Mobile.Pages;

[QueryProperty(nameof(JobId), "id")]
public partial class EditJobPage : ContentPage
{
    private readonly OneManVanDbContext _db;
    private Job? _job;
    
    public int JobId { get; set; }

    public EditJobPage(OneManVanDbContext db)
    {
        InitializeComponent();
        _db = db;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadJobAsync();
    }

    private async Task LoadJobAsync()
    {
        try
        {
            using (new LoadingScope(LoadingIndicator))
            {
                _job = await _db.Jobs
                    .Include(j => j.Customer)
                    .FirstOrDefaultAsync(j => j.Id == JobId);

                if (_job == null)
                {
                    await DisplayAlertAsync("Error", "Job not found", "OK");
                    await Shell.Current.GoToAsync("..");
                    return;
                }

                TitleEntry.Text = _job.Title;
                DescriptionEditor.Text = _job.Description;
                ScheduledDatePicker.Date = _job.ScheduledDate ?? DateTime.Today;
                EstimatedHoursEntry.Text = _job.EstimatedHours.ToString();
                PriorityPicker.SelectedIndex = (int)_job.Priority;
                StatusPicker.SelectedIndex = (int)_job.Status;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"EditJob load error: {ex}");
            await DisplayAlertAsync("Unable to Load", 
                "Failed to load job data. Please try again.", "OK");
            await Shell.Current.GoToAsync("..");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // Validate data is loaded
        if (_job == null)
        {
            await DisplayAlertAsync("Cannot Save", 
                "Job data not loaded. Please go back and try again.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(TitleEntry.Text))
        {
            await DisplayAlertAsync("Required", "Job title is required", "OK");
            return;
        }

        // Use PageExtensions.SaveWithFeedbackAsync for clean save pattern
        await this.SaveWithFeedbackAsync(async () =>
        {
            _job.Title = TitleEntry.Text.Trim();
            _job.Description = DescriptionEditor.Text?.Trim();
            _job.ScheduledDate = ScheduledDatePicker.Date;
            _job.EstimatedHours = decimal.Parse(EstimatedHoursEntry.Text ?? "0");
            _job.Priority = (JobPriority)PriorityPicker.SelectedIndex;
            _job.Status = (JobStatus)StatusPicker.SelectedIndex;
            _job.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
        }, 
        successMessage: "Job updated successfully");
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
