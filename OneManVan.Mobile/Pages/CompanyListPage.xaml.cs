using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using System.Collections.ObjectModel;

namespace OneManVan.Mobile.Pages;

public partial class CompanyListPage : ContentPage
{
    private readonly OneManVanDbContext _db;
    private ObservableCollection<Company> _companies = [];
    private List<Company> _allCompanies = [];

    public CompanyListPage(OneManVanDbContext db)
    {
        InitializeComponent();
        _db = db;
        CompaniesCollection.ItemsSource = _companies;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadCompaniesAsync();
    }

    private async Task LoadCompaniesAsync()
    {
        try
        {
            _allCompanies = await _db.Companies
                .OrderBy(c => c.Name)
                .ToListAsync();

            _companies.Clear();
            foreach (var company in _allCompanies)
            {
                _companies.Add(company);
            }

            // Update title with count
            Title = $"Companies ({_companies.Count})";
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load companies: {ex.Message}", "OK");
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = e.NewTextValue?.ToLower() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(searchText))
        {
            // Show all companies
            _companies.Clear();
            foreach (var company in _allCompanies)
            {
                _companies.Add(company);
            }
        }
        else
        {
            // Filter companies
            var filtered = _allCompanies.Where(c =>
                c.Name.ToLower().Contains(searchText) ||
                (c.LegalName?.ToLower().Contains(searchText) ?? false) ||
                (c.Industry?.ToLower().Contains(searchText) ?? false) ||
                (c.Phone?.Contains(searchText) ?? false) ||
                (c.Email?.ToLower().Contains(searchText) ?? false)
            ).ToList();

            _companies.Clear();
            foreach (var company in filtered)
            {
                _companies.Add(company);
            }
        }

        Title = $"Companies ({_companies.Count})";
    }

    private async void OnCompanySelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Company company)
        {
            await Shell.Current.GoToAsync($"CompanyDetail?companyId={company.Id}");
            
            // Clear selection
            CompaniesCollection.SelectedItem = null;
        }
    }

    private async void OnAddCompanyClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AddCompany");
    }
}
