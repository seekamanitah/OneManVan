using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using OneManVan.Shared.Models;
using OneManVan.Shared.Services;

namespace OneManVan.Dialogs;

/// <summary>
/// Dialog showing assets that need online registration and allowing user to mark them as registered.
/// </summary>
public partial class UnregisteredAssetsDialog : Window
{
    private readonly EquipmentRegistrationService _registrationService;
    private List<Asset> _assets;

    public UnregisteredAssetsDialog(List<Asset> unregisteredAssets, EquipmentRegistrationService registrationService)
    {
        InitializeComponent();
        
        _assets = unregisteredAssets;
        _registrationService = registrationService;
        
        AssetsListView.ItemsSource = _assets;
    }

    private async void OnRegisterClick(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not Asset asset)
            return;

        // Get registration URL if available
        var registrationUrl = await _registrationService.GetRegistrationUrlAsync(asset.Brand);

        string message;
        if (!string.IsNullOrEmpty(registrationUrl))
        {
            message = $"Registration Website:\n{registrationUrl}\n\n" +
                     $"Would you like to:\n" +
                     $"1. Open the registration website in your browser\n" +
                     $"2. Mark as already registered\n" +
                     $"3. Cancel";
        }
        else
        {
            message = $"No automatic registration URL found for {asset.Brand}.\n\n" +
                     $"Please visit the manufacturer's website to register.\n\n" +
                     $"Mark this asset as registered?";
        }

        // Show dialog with options
        var result = MessageBox.Show(message, "Register Equipment", 
            MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            // Open registration URL in browser
            if (!string.IsNullOrEmpty(registrationUrl))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = registrationUrl,
                        UseShellExecute = true
                    });

                    MessageBox.Show("Registration website opened in your browser.\n\n" +
                                  "After registering, click 'Mark as Registered' below.",
                                  "Registration",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not open browser: {ex.Message}\n\n" +
                                  $"Please manually visit: {registrationUrl}",
                                  "Error",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                }
            }

            // Show mark as registered dialog
            await ShowMarkAsRegisteredDialog(asset);
        }
        else if (result == MessageBoxResult.No)
        {
            // Mark as registered directly
            await ShowMarkAsRegisteredDialog(asset);
        }
    }

    private async System.Threading.Tasks.Task ShowMarkAsRegisteredDialog(Asset asset)
    {
        var dialog = new MarkAsRegisteredDialog(asset, _registrationService)
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true)
        {
            // Remove from list
            _assets.Remove(asset);
            AssetsListView.ItemsSource = null;
            AssetsListView.ItemsSource = _assets;

            MessageBox.Show($"Asset '{asset.Serial}' marked as registered!", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
