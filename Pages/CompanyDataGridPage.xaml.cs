using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using OneManVan.Controls;
using OneManVan.Services;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;

namespace OneManVan.Pages;

public partial class CompanyDataGridPage : UserControl
{
    private readonly OneManVanDbContext _dbContext;
    private List<Company> _allCompanies = [];

    public CompanyDataGridPage()
    {
        InitializeComponent();
        _dbContext = App.DbContext;
        
        Loaded += OnPageLoaded;
    }

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        await LoadCompaniesAsync();
    }

    private async System.Threading.Tasks.Task LoadCompaniesAsync()
    {
        try
        {
            _allCompanies = await _dbContext.Companies
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            CompanyDataGrid.ItemsSource = _allCompanies;
            CountTextBlock.Text = $"({_allCompanies.Count})";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load companies: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = SearchTextBox.Text.ToLower();
        
        if (string.IsNullOrWhiteSpace(searchText))
        {
            CompanyDataGrid.ItemsSource = _allCompanies;
        }
        else
        {
            var filtered = _allCompanies.Where(c =>
                (c.Name?.ToLower().Contains(searchText) ?? false) ||
                (c.Industry?.ToLower().Contains(searchText) ?? false) ||
                (c.Email?.ToLower().Contains(searchText) ?? false) ||
                (c.Phone?.ToLower().Contains(searchText) ?? false)
            ).ToList();
            
            CompanyDataGrid.ItemsSource = filtered;
        }
    }

    private void OnAddClick(object sender, RoutedEventArgs e)
    {
        var formContent = new CompanyFormContent(_dbContext);
        
        _ = DrawerService.Instance.OpenDrawerAsync(
            title: "Add Company",
            content: formContent,
            saveButtonText: "Save",
            onSave: async () =>
            {
                if (!formContent.Validate())
                {
                    MessageBox.Show("Please fill in required fields", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    var company = formContent.GetCompany();
                    _dbContext.Companies.Add(company);
                    await _dbContext.SaveChangesAsync();
                    await LoadCompaniesAsync();
                    await DrawerService.Instance.CompleteDrawerAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to add company: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        );
    }

    private void CompanyDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (CompanyDataGrid.SelectedItem is Company company)
        {
            OpenEditDrawer(company);
        }
    }

    private void OpenEditDrawer(Company company)
    {
        var formContent = new CompanyFormContent(_dbContext);
        formContent.LoadCompany(company);

        _ = DrawerService.Instance.OpenDrawerAsync(
            title: "Edit Company",
            content: formContent,
            saveButtonText: "Save",
            onSave: async () =>
            {
                if (!formContent.Validate())
                {
                    MessageBox.Show("Please fill in required fields", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    var updatedCompany = formContent.GetCompany();
                    company.Name = updatedCompany.Name;
                    company.LegalName = updatedCompany.LegalName;
                    company.CompanyType = updatedCompany.CompanyType;
                    company.Industry = updatedCompany.Industry;
                    company.TaxId = updatedCompany.TaxId;
                    company.Website = updatedCompany.Website;
                    company.Phone = updatedCompany.Phone;
                    company.Email = updatedCompany.Email;
                    company.ContactCustomerId = updatedCompany.ContactCustomerId;
                    company.BillingAddress = updatedCompany.BillingAddress;
                    company.BillingCity = updatedCompany.BillingCity;
                    company.BillingState = updatedCompany.BillingState;
                    company.BillingZipCode = updatedCompany.BillingZipCode;
                    company.Notes = updatedCompany.Notes;
                    company.UpdatedAt = DateTime.UtcNow;

                    await _dbContext.SaveChangesAsync();
                    await LoadCompaniesAsync();
                    await DrawerService.Instance.CompleteDrawerAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to update company: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        );
    }
}
