using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using OneManVan.Services;

namespace OneManVan.Pages;

/// <summary>
/// API Setup Guide page with instructions for Square and Google Calendar integration.
/// </summary>
public partial class ApiSetupGuidePage : UserControl
{
    public ApiSetupGuidePage()
    {
        InitializeComponent();
    }

    private void OpenSquareDashboard_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://developer.squareup.com/apps");
    }

    private void OpenGoogleCloud_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://console.cloud.google.com/");
    }

    private void OpenApiLibrary_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://console.cloud.google.com/apis/library/calendar-json.googleapis.com");
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        // Navigate to settings page
        NavigationRequest.Navigate("Settings");
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open browser: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
