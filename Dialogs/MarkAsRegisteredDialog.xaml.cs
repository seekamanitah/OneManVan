using System;
using System.Windows;
using OneManVan.Shared.Models;
using OneManVan.Shared.Services;

namespace OneManVan.Dialogs;

/// <summary>
/// Dialog for marking an asset as registered online with manufacturer.
/// </summary>
public partial class MarkAsRegisteredDialog : Window
{
    private readonly Asset _asset;
    private readonly EquipmentRegistrationService _registrationService;

    public MarkAsRegisteredDialog(Asset asset, EquipmentRegistrationService registrationService)
    {
        InitializeComponent();

        _asset = asset;
        _registrationService = registrationService;

        // Set asset info
        AssetInfoText.Text = $"{asset.Brand} {asset.Model} - Serial: {asset.Serial}";

        // Default to today
        RegistrationDatePicker.SelectedDate = DateTime.Today;
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (!RegistrationDatePicker.SelectedDate.HasValue)
        {
            MessageBox.Show("Please select a registration date", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await _registrationService.MarkAsRegisteredAsync(
                _asset,
                RegistrationDatePicker.SelectedDate.Value,
                string.IsNullOrWhiteSpace(ConfirmationTextBox.Text) ? null : ConfirmationTextBox.Text.Trim());

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
