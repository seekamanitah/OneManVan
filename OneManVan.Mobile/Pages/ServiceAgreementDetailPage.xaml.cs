using Microsoft.EntityFrameworkCore;
using OneManVan.Mobile.Helpers;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;

namespace OneManVan.Mobile.Pages;

[QueryProperty(nameof(AgreementId), "id")]
public partial class ServiceAgreementDetailPage : ContentPage
{
    private readonly OneManVanDbContext _db;
    private ServiceAgreement? _agreement;
    
    public int AgreementId { get; set; }

    public ServiceAgreementDetailPage(OneManVanDbContext dbContext)
    {
        InitializeComponent();
        _db = dbContext;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAgreementAsync();
    }

    private async Task LoadAgreementAsync()
    {
        try
        {
            LoadingIndicator.IsVisible = true;
            
            _agreement = await _db.ServiceAgreements
                .Include(sa => sa.Customer)
                .FirstOrDefaultAsync(sa => sa.Id == AgreementId);

            if (_agreement == null)
            {
                await DisplayAlertAsync("Error", "Service Agreement not found", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            UpdateUI();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load agreement: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
        }
    }

    private void UpdateUI()
    {
        if (_agreement == null) return;


        AgreementNumberLabel.Text = _agreement.AgreementNumber;
        CustomerNameLabel.Text = _agreement.Customer?.Name ?? "Unknown";
        StatusLabel.Text = _agreement.IsActive ? "Active" : "Inactive";
        StatusLabel.TextColor = _agreement.IsActive ? Colors.Green : Colors.Gray;
        
        TypeLabel.Text = _agreement.Type.ToString();
        StartDateLabel.Text = _agreement.StartDate.ToShortDate();
        EndDateLabel.Text = _agreement.EndDate.ToShortDate();
        FrequencyLabel.Text = _agreement.BillingFrequency.ToString();
        PriceLabel.Text = $"${_agreement.AnnualPrice:N2}";
        DescriptionLabel.Text = _agreement.Description ?? "";
    }

    private async void OnEditClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"EditServiceAgreement?id={AgreementId}");
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        var confirm = await DisplayAlertAsync("Delete Agreement", 
            "Are you sure you want to delete this service agreement?", "Delete", "Cancel");

        if (!confirm) return;

        try
        {
            _db.ServiceAgreements.Remove(_agreement!);
            await _db.SaveChangesAsync();
            
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            
            await DisplayAlertAsync("Deleted", "Agreement has been deleted", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to delete: {ex.Message}", "OK");
        }
    }
}
