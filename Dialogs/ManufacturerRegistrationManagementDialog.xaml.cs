using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;

namespace OneManVan.Dialogs;

/// <summary>
/// Dialog for managing manufacturer registration URLs.
/// Allows admins to configure warranty registration websites.
/// </summary>
public partial class ManufacturerRegistrationManagementDialog : Window
{
    private readonly OneManVanDbContext _dbContext;
    private ObservableCollection<ManufacturerRegistration> _manufacturers = new();

    public ManufacturerRegistrationManagementDialog(OneManVanDbContext dbContext)
    {
        InitializeComponent();
        _dbContext = dbContext;
        LoadManufacturers();
    }

    private async void LoadManufacturers()
    {
        try
        {
            var manufacturers = await _dbContext.ManufacturerRegistrations
                .OrderBy(m => m.DisplayOrder)
                .ThenBy(m => m.BrandName)
                .ToListAsync();

            _manufacturers = new ObservableCollection<ManufacturerRegistration>(manufacturers);
            ManufacturersDataGrid.ItemsSource = _manufacturers;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load manufacturers: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnEditClick(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not ManufacturerRegistration manufacturer)
            return;

        var dialog = new EditManufacturerDialog(manufacturer)
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true)
        {
            // Refresh the grid
            ManufacturersDataGrid.Items.Refresh();
        }
    }

    private void OnTestUrlClick(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not ManufacturerRegistration manufacturer)
            return;

        if (string.IsNullOrWhiteSpace(manufacturer.RegistrationUrl))
        {
            MessageBox.Show("No registration URL configured for this manufacturer.", "No URL",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = manufacturer.RegistrationUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open URL: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnAddClick(object sender, RoutedEventArgs e)
    {
        var newManufacturer = new ManufacturerRegistration
        {
            BrandName = "New Brand",
            IsActive = true,
            DisplayOrder = _manufacturers.Count > 0 ? _manufacturers.Max(m => m.DisplayOrder) + 10 : 10
        };

        var dialog = new EditManufacturerDialog(newManufacturer)
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true)
        {
            _dbContext.ManufacturerRegistrations.Add(newManufacturer);
            _manufacturers.Add(newManufacturer);
        }
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        try
        {
            await _dbContext.SaveChangesAsync();
            MessageBox.Show("Changes saved successfully!", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save changes: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
