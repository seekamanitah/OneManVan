using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;

namespace OneManVan.Mobile.Pages;

public partial class SiteListPage : ContentPage
{
    private readonly IDbContextFactory<OneManVanDbContext> _dbFactory;
    private List<SiteViewModel> _allSites = new();
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

    public SiteListPage(IDbContextFactory<OneManVanDbContext> dbFactory)
    {
        InitializeComponent();
        _dbFactory = dbFactory;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadSitesAsync();
    }

    private async Task LoadSitesAsync()
    {
        try
        {
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;

            await using var db = await _dbFactory.CreateDbContextAsync();

            var sites = await db.Sites
                .Include(s => s.Customer)
                .Include(s => s.Assets)
                .OrderBy(s => s.Customer!.DisplayName)
                .ThenBy(s => s.SiteName)
                .ToListAsync();


            _allSites = sites.Select(s => new SiteViewModel
            {
                Id = s.Id,
                Name = s.SiteName ?? "Unnamed Site",
                CustomerName = s.Customer?.DisplayName ?? "Unknown Customer",
                CustomerId = s.CustomerId,
                FullAddress = FormatAddress(s),
                AssetCount = s.Assets?.Count ?? 0,
                ContactPhone = s.SiteContactPhone,
                HasContactPhone = !string.IsNullOrWhiteSpace(s.SiteContactPhone),
                IsPrimary = s.IsPrimary,
                Latitude = (double?)s.Latitude,
                Longitude = (double?)s.Longitude,
                Address = s.Address,
                City = s.City,
                State = s.State,
                ZipCode = s.ZipCode
            }).ToList();

            ApplyFilter();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load sites: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            IsRefreshing = false;
        }
    }

    private string FormatAddress(Site site)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(site.Address))
            parts.Add(site.Address);
        if (!string.IsNullOrWhiteSpace(site.City))
            parts.Add(site.City);
        if (!string.IsNullOrWhiteSpace(site.State))
            parts.Add(site.State);
        if (!string.IsNullOrWhiteSpace(site.ZipCode))
            parts.Add(site.ZipCode);
        
        return parts.Count > 0 ? string.Join(", ", parts) : "No address";
    }

    private void ApplyFilter()
    {
        var filtered = _allSites;

        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            var search = _searchText.ToLower();
            filtered = filtered.Where(s =>
                (s.Name?.ToLower().Contains(search) ?? false) ||
                (s.CustomerName?.ToLower().Contains(search) ?? false) ||
                (s.FullAddress?.ToLower().Contains(search) ?? false)
            ).ToList();
        }

        SitesCollectionView.ItemsSource = filtered;
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
        await LoadSitesAsync();
        SitesRefreshView.IsRefreshing = false;
    }

    private async void OnSiteSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is SiteViewModel site)
        {
            await Shell.Current.GoToAsync($"SiteDetail?id={site.Id}");
            ((CollectionView)sender).SelectedItem = null;
        }
    }

    private async void OnAddSiteClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AddSite");
    }

    private async void OnNavigateClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is SiteViewModel site)
        {
            try
            {
                // Try to open in maps app
                if (site.Latitude.HasValue && site.Longitude.HasValue)
                {
                    var location = new Location(site.Latitude.Value, site.Longitude.Value);
                    await Map.Default.OpenAsync(location, new MapLaunchOptions
                    {
                        Name = site.Name,
                        NavigationMode = NavigationMode.Driving
                    });
                }
                else if (!string.IsNullOrWhiteSpace(site.FullAddress))
                {
                    await Map.Default.OpenAsync(new Placemark
                    {
                        AdminArea = site.State,
                        CountryName = "USA",
                        Locality = site.City,
                        PostalCode = site.ZipCode,
                        Thoroughfare = site.Address
                    }, new MapLaunchOptions
                    {
                        Name = site.Name,
                        NavigationMode = NavigationMode.Driving
                    });
                }
                else
                {
                    await DisplayAlert("No Address", "This site does not have an address set.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Could not open maps: {ex.Message}", "OK");
            }
        }
    }

    private class SiteViewModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? CustomerName { get; set; }
        public int CustomerId { get; set; }
        public string? FullAddress { get; set; }
        public int AssetCount { get; set; }
        public string? ContactPhone { get; set; }
        public bool HasContactPhone { get; set; }
        public bool IsPrimary { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
    }
}
