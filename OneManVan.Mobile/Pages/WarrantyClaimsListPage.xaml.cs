using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Mobile.Pages;

public partial class WarrantyClaimsListPage : ContentPage
{
    private readonly IDbContextFactory<OneManVanDbContext> _dbFactory;
    private List<WarrantyClaimViewModel> _allClaims = new();
    private string _searchText = string.Empty;
    private bool _isRefreshing;

    public bool IsRefreshing
    {
        get => _isRefreshing;
        set
        {
            _isRefreshing = value;
            OnPropertyChanged();
        }
    }

    public WarrantyClaimsListPage(IDbContextFactory<OneManVanDbContext> dbFactory)
    {
        InitializeComponent();
        _dbFactory = dbFactory;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadClaimsAsync();
    }

    private async Task LoadClaimsAsync()
    {
        try
        {
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;

            await using var db = await _dbFactory.CreateDbContextAsync();

            var claims = await db.WarrantyClaims
                .Include(c => c.Asset)
                    .ThenInclude(a => a!.Customer)
                .OrderByDescending(c => c.ClaimDate)
                .ToListAsync();

            _allClaims = claims.Select(c => new WarrantyClaimViewModel
            {
                Id = c.Id,
                ClaimNumber = c.ClaimNumber ?? $"WC-{c.Id:D6}",
                Status = c.Status,
                AssetDescription = c.Asset != null 
                    ? $"{c.Asset.Brand} {c.Asset.Model}" 
                    : "Unknown Asset",
                CustomerName = c.Asset?.Customer?.DisplayName ?? "Unknown Customer",
                ClaimDate = c.ClaimDate,
                ClaimAmount = c.RepairCost
            }).ToList();

            ApplyFilter();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load warranty claims: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            IsRefreshing = false;
        }
    }

    private void ApplyFilter()
    {
        var filtered = _allClaims;

        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            var search = _searchText.ToLower();
            filtered = filtered.Where(c =>
                (c.ClaimNumber?.ToLower().Contains(search) ?? false) ||
                (c.AssetDescription?.ToLower().Contains(search) ?? false) ||
                (c.CustomerName?.ToLower().Contains(search) ?? false)
            ).ToList();
        }

        ClaimsCollectionView.ItemsSource = filtered;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = e.NewTextValue ?? string.Empty;
        ApplyFilter();
    }

    private void OnSearchPressed(object sender, EventArgs e)
    {
        ApplyFilter();
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadClaimsAsync();
        ClaimsRefreshView.IsRefreshing = false;
    }

    private async void OnClaimSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is WarrantyClaimViewModel claim)
        {
            await Shell.Current.GoToAsync($"WarrantyClaimDetail?id={claim.Id}");
            ((CollectionView)sender).SelectedItem = null;
        }
    }

    private async void OnAddClaimClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AddWarrantyClaim");
    }

    private class WarrantyClaimViewModel
    {
        public int Id { get; set; }
        public string? ClaimNumber { get; set; }
        public ClaimStatus Status { get; set; }
        public string? AssetDescription { get; set; }
        public string? CustomerName { get; set; }
        public DateTime ClaimDate { get; set; }
        public decimal ClaimAmount { get; set; }
    }
}
