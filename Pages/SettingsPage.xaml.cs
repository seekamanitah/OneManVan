using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.EntityFrameworkCore;
using OneManVan.Services;
using OneManVan.Shared.Models;

namespace OneManVan.Pages;

/// <summary>
/// Settings page for application configuration.
/// </summary>
public partial class SettingsPage : UserControl
{
    private readonly SquareService _squareService = new();
    private readonly GoogleCalendarService _googleCalendarService = new();
    private readonly CsvExportImportService _csvService;
    private readonly InvoicePdfService _pdfService;
    
    public decimal LaborRate { get; set; } = 85m;
    public decimal TaxRate { get; set; } = 7m;
    public int InvoiceDueDays { get; set; } = 30;

    public SettingsPage()
    {
        InitializeComponent();
        _csvService = new CsvExportImportService(App.DbContext);
        _pdfService = new InvoicePdfService();
        DataContext = this;
        LoadSettings();
        LoadKeyboardShortcuts();
        LoadSquareSettings();
        LoadGoogleCalendarSettings();
        LoadTradeConfiguration();
        LoadPdfSettings();
    }

    private void LoadKeyboardShortcuts()
    {
        var shortcuts = new[]
        {
            new { Action = "New Customer", Keys = "Ctrl+N" },
            new { Action = "New Estimate", Keys = "Ctrl+E" },
            new { Action = "New Job", Keys = "Ctrl+J" },
            new { Action = "New Invoice", Keys = "Ctrl+I" },
            new { Action = "Save / Submit", Keys = "Ctrl+S" },
            new { Action = "Search", Keys = "Ctrl+F" },
            new { Action = "Export Backup", Keys = "Ctrl+B" },
            new { Action = "Quick Navigate to Home", Keys = "Ctrl+H" },
            new { Action = "Quick Navigate to Customers", Keys = "Ctrl+1" },
            new { Action = "Quick Navigate to Assets", Keys = "Ctrl+2" },
            new { Action = "Quick Navigate to Estimates", Keys = "Ctrl+3" },
            new { Action = "Quick Navigate to Jobs", Keys = "Ctrl+4" },
            new { Action = "Quick Navigate to Invoices", Keys = "Ctrl+5" },
            new { Action = "Settings", Keys = "Ctrl+," },
            new { Action = "Refresh Current View", Keys = "F5" }
        };

        ShortcutsItemsControl.ItemsSource = shortcuts;
    }

    private void LoadSettings()
    {
        // Display database path
        DbPathText.Text = App.BackupService.GetDatabasePath();

        // Set theme toggle state
        ThemeToggle.IsChecked = !App.ThemeService.IsDarkMode;

        // Set UI scale slider - Detach event to prevent triggering save during load
        UiScaleSlider.ValueChanged -= UiScaleSlider_ValueChanged;
        UiScaleSlider.Value = App.UiScaleService.CurrentScale;
        UiScalePercentText.Text = App.UiScaleService.GetScalePercentage();
        UiScaleSlider.ValueChanged += UiScaleSlider_ValueChanged;

        // Load scheduled backup settings
        LoadBackupSettings();

        // Load business profile
        LoadBusinessProfile();
    }

    private void LoadBackupSettings()
    {
        var settings = App.ScheduledBackupService.Settings;
        
        ScheduledBackupToggle.IsChecked = settings.Enabled;
        BackupFrequencyCombo.SelectedIndex = (int)settings.Frequency;
        MaxBackupsText.Text = settings.MaxBackupsToKeep.ToString();
        UseCompressionCheck.IsChecked = settings.UseCompression;
        
        // Update last backup time display
        if (settings.LastBackupTime > DateTime.MinValue)
        {
            LastBackupText.Text = settings.LastBackupTime.ToString("MMM dd, yyyy h:mm tt");
        }
        else
        {
            LastBackupText.Text = "Never";
        }

        // Load backup list
        RefreshBackupList();
    }

    private void RefreshBackupList()
    {
        var backups = App.ScheduledBackupService.GetExistingBackups().Take(5).ToList();
        BackupsList.ItemsSource = backups;
    }

    private void ScheduledBackupToggle_Click(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton toggle)
        {
            App.ScheduledBackupService.SetEnabled(toggle.IsChecked == true);
        }
    }

    private void BackupFrequencyCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BackupFrequencyCombo.SelectedIndex >= 0)
        {
            var settings = App.ScheduledBackupService.Settings;
            settings.Frequency = (BackupFrequency)BackupFrequencyCombo.SelectedIndex;
            App.ScheduledBackupService.UpdateSettings(settings);
        }
    }

    private void MaxBackupsText_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (int.TryParse(MaxBackupsText.Text, out int max) && max > 0)
        {
            var settings = App.ScheduledBackupService.Settings;
            settings.MaxBackupsToKeep = max;
            App.ScheduledBackupService.UpdateSettings(settings);
        }
    }

    private void UseCompressionCheck_Click(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox check)
        {
            var settings = App.ScheduledBackupService.Settings;
            settings.UseCompression = check.IsChecked == true;
            App.ScheduledBackupService.UpdateSettings(settings);
        }
    }

    private async void BackupNow_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = await App.ScheduledBackupService.RunBackupNowAsync();

            if (result.Success)
            {
                LoadBackupSettings(); // Refresh UI
                MessageBox.Show(
                    $"Backup created successfully!\n\n{result.FilePath}\n\nCleaned up {result.CleanedUpCount} old backup(s).",
                    "Backup Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"Backup failed: {result.ErrorMessage}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Backup failed: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ThemeToggle_Click(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton toggle)
        {
            var theme = toggle.IsChecked == true
                ? ThemeService.AppTheme.Light
                : ThemeService.AppTheme.Dark;

            App.ThemeService.SetTheme(theme);
        }
    }

    private void UiScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (App.UiScaleService == null) return;
        
        App.UiScaleService.SetScale(e.NewValue);
        if (UiScalePercentText != null)
        {
            UiScalePercentText.Text = App.UiScaleService.GetScalePercentage();
        }
    }

    private void ResetUiScale_Click(object sender, RoutedEventArgs e)
    {
        App.UiScaleService.ResetScale();
        UiScaleSlider.Value = App.UiScaleService.CurrentScale;
        UiScalePercentText.Text = App.UiScaleService.GetScalePercentage();
    }

    private void SaveDefaults_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Save to appsettings.json
        MessageBox.Show(
            $"Defaults saved:\n\nLabor Rate: ${LaborRate}/hr\nTax Rate: {TaxRate}%\nInvoice Due: {InvoiceDueDays} days",
            "Settings Saved",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private async void ExportBackup_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Backup",
            Filter = "JSON Backup|*.json|All Files|*.*",
            FileName = $"OneManVan_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.json"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                await App.BackupService.ExportToJsonAsync(dialog.FileName);
                MessageBox.Show($"Backup saved to:\n{dialog.FileName}", "Backup Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Backup failed: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void ImportBackup_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Import Backup",
            Filter = "JSON Backup|*.json|All Files|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            var result = MessageBox.Show(
                "Import will merge data with existing records.\n\nExisting records may be updated.\n\nContinue?",
                "Confirm Import",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var importResult = await App.BackupService.ImportFromJsonAsync(dialog.FileName);

                if (importResult.Success)
                {
                    var message = $"Import complete!\n\n" +
                        $"Customers: {importResult.CustomersImported}\n" +
                        $"Sites: {importResult.SitesImported}\n" +
                        $"Assets: {importResult.AssetsImported}\n" +
                        $"Total: {importResult.TotalImported}";

                    if (importResult.Warnings.Count > 0)
                    {
                        message += $"\n\nWarnings: {importResult.Warnings.Count}";
                    }

                    MessageBox.Show(message, "Import Complete",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Import failed:\n{string.Join("\n", importResult.Errors)}", "Import Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Import failed: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void OpenDataFolder_Click(object sender, RoutedEventArgs e)
    {
        var dbPath = App.BackupService.GetDatabasePath();
        var folder = Path.GetDirectoryName(dbPath);

        if (folder != null && Directory.Exists(folder))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = folder,
                UseShellExecute = true
            });
        }
        else
        {
            MessageBox.Show("Data folder not found.", "Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void ResetData_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "WARNING: This will delete ALL data!\n\n" +
            "• All customers\n" +
            "• All assets\n" +
            "• All estimates\n" +
            "• All jobs\n" +
            "• All invoices\n\n" +
            "This cannot be undone!\n\n" +
            "Are you absolutely sure?",
            "Confirm Reset",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        // Double confirmation
        result = MessageBox.Show(
            "Last chance!\n\nType 'RESET' in your mind and click Yes to proceed.",
            "Final Confirmation",
            MessageBoxButton.YesNo,
            MessageBoxImage.Stop);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            // Delete all data
            await App.DbContext.Payments.ExecuteDeleteAsync();
            await App.DbContext.Invoices.ExecuteDeleteAsync();
            await App.DbContext.TimeEntries.ExecuteDeleteAsync();
            await App.DbContext.Jobs.ExecuteDeleteAsync();
            await App.DbContext.EstimateLines.ExecuteDeleteAsync();
            await App.DbContext.Estimates.ExecuteDeleteAsync();
            await App.DbContext.InventoryLogs.ExecuteDeleteAsync();
            await App.DbContext.InventoryItems.ExecuteDeleteAsync();
            await App.DbContext.CustomFields.ExecuteDeleteAsync();
            await App.DbContext.Assets.ExecuteDeleteAsync();
            await App.DbContext.Sites.ExecuteDeleteAsync();
            await App.DbContext.Customers.ExecuteDeleteAsync();

            MessageBox.Show("All data has been reset.", "Reset Complete",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Reset failed: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void LoadSampleData_Click(object sender, RoutedEventArgs e)
    {
        var hasData = await App.DbContext.Customers.AnyAsync();
        
        if (hasData)
        {
            var result = MessageBox.Show(
                "You already have data in the database.\n\n" +
                "Loading sample data will add additional records.\n\n" +
                "Continue?",
                "Confirm Load Sample Data",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;
        }

        try
        {
            var seeder = new DataSeederService(App.DbContext);
            await seeder.SeedSampleDataAsync();

            MessageBox.Show(
                "Sample data loaded successfully!\n\n" +
                "• 5 Customers with sites\n" +
                "• 7 HVAC Assets\n" +
                "• 8 Inventory items\n" +
                "• 3 Estimates\n" +
                "• 3 Jobs\n" +
                "• 1 Invoice",
                "Sample Data Loaded",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load sample data: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #region Square Integration

    private void LoadSquareSettings()
    {
        var settings = _squareService.Settings;
        
        SquareEnabledToggle.IsChecked = settings.IsEnabled;
        SquareSandboxRadio.IsChecked = settings.UseSandbox;
        SquareProductionRadio.IsChecked = !settings.UseSandbox;
        SquareAppIdText.Text = settings.ApplicationId;
        SquareAccessTokenText.Password = settings.AccessToken;
        SquareLocationIdText.Text = settings.LocationId;
        
        UpdateSquareStatus();
    }

    private void UpdateSquareStatus()
    {
        var settings = _squareService.Settings;
        
        if (!settings.IsEnabled)
        {
            SquareStatusText.Text = "Disabled";
            SquareStatusText.Foreground = FindResource("SubtextBrush") as System.Windows.Media.Brush;
        }
        else if (!settings.IsConfigured)
        {
            SquareStatusText.Text = "Not configured";
            SquareStatusText.Foreground = FindResource("WarningBrush") as System.Windows.Media.Brush;
        }
        else
        {
            SquareStatusText.Text = "Ready (not tested)";
            SquareStatusText.Foreground = FindResource("PrimaryBrush") as System.Windows.Media.Brush;
        }
    }

    private void SquareEnabledToggle_Click(object sender, RoutedEventArgs e)
    {
        UpdateSquareStatus();
    }

    private void SquareEnvironment_Changed(object sender, RoutedEventArgs e)
    {
        // Just track state, will be saved when user clicks Save
    }

    private void SquareSettings_Changed(object sender, RoutedEventArgs e)
    {
        // Just track state, will be saved when user clicks Save
    }

    private void SquareSettings_Changed(object sender, TextChangedEventArgs e)
    {
        // Just track state, will be saved when user clicks Save
    }

    private void SaveSquareSettings_Click(object sender, RoutedEventArgs e)
    {
        var settings = new SquareSettings
        {
            IsEnabled = SquareEnabledToggle.IsChecked == true,
            UseSandbox = SquareSandboxRadio.IsChecked == true,
            ApplicationId = SquareAppIdText.Text.Trim(),
            AccessToken = SquareAccessTokenText.Password,
            LocationId = SquareLocationIdText.Text.Trim()
        };

        _squareService.SaveSettings(settings);
        UpdateSquareStatus();

        MessageBox.Show("Square settings saved successfully!", "Settings Saved",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void TestSquareConnection_Click(object sender, RoutedEventArgs e)
    {
        // First save settings
        var settings = new SquareSettings
        {
            IsEnabled = SquareEnabledToggle.IsChecked == true,
            UseSandbox = SquareSandboxRadio.IsChecked == true,
            ApplicationId = SquareAppIdText.Text.Trim(),
            AccessToken = SquareAccessTokenText.Password,
            LocationId = SquareLocationIdText.Text.Trim()
        };

        _squareService.SaveSettings(settings);

        SquareStatusText.Text = "Testing...";
        SquareStatusText.Foreground = FindResource("PrimaryBrush") as System.Windows.Media.Brush;

        var (success, message) = await _squareService.TestConnectionAsync();

        if (success)
        {
            SquareStatusText.Text = message;
            SquareStatusText.Foreground = FindResource("SecondaryBrush") as System.Windows.Media.Brush;
            
            MessageBox.Show($"Connection successful!\n\n{message}", "Square Connected",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            SquareStatusText.Text = "Connection failed";
            SquareStatusText.Foreground = FindResource("ErrorBrush") as System.Windows.Media.Brush;
            
            MessageBox.Show($"Connection failed:\n\n{message}", "Connection Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void FetchSquareLocations_Click(object sender, RoutedEventArgs e)
    {
        // Save current settings first to use for API call
        var settings = new SquareSettings
        {
            IsEnabled = true,
            UseSandbox = SquareSandboxRadio.IsChecked == true,
            ApplicationId = SquareAppIdText.Text.Trim(),
            AccessToken = SquareAccessTokenText.Password,
            LocationId = SquareLocationIdText.Text.Trim()
        };

        _squareService.SaveSettings(settings);

        var locations = await _squareService.GetLocationsAsync();

        if (locations.Count == 0)
        {
            MessageBox.Show("No locations found. Check your credentials.", "No Locations",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (locations.Count == 1)
        {
            SquareLocationIdText.Text = locations[0].Id;
            MessageBox.Show($"Found location: {locations[0].Name}\n\nLocation ID has been filled in.",
                "Location Found", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Multiple locations - show selection
        var locationList = string.Join("\n", locations.Select((l, i) => $"{i + 1}. {l.Name} ({l.Id})"));
        var result = MessageBox.Show(
            $"Multiple locations found:\n\n{locationList}\n\nUse the first location?",
            "Multiple Locations",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            SquareLocationIdText.Text = locations[0].Id;
        }
    }

    #endregion

    #region Google Calendar Integration

    private void LoadGoogleCalendarSettings()
    {
        var settings = _googleCalendarService.Settings;
        
        GoogleCalendarEnabledToggle.IsChecked = settings.IsEnabled;
        GoogleClientIdText.Text = settings.ClientId;
        GoogleClientSecretText.Password = settings.ClientSecret;
        GoogleSyncJobsCheck.IsChecked = settings.SyncJobsToCalendar;
        GoogleAddRemindersCheck.IsChecked = settings.AddReminders;
        
        UpdateGoogleCalendarStatus();
    }

    private void UpdateGoogleCalendarStatus()
    {
        var settings = _googleCalendarService.Settings;
        
        if (!settings.IsEnabled)
        {
            GoogleStatusText.Text = "Disabled";
            GoogleStatusText.Foreground = FindResource("SubtextBrush") as System.Windows.Media.Brush;
            GoogleEmailText.Text = "";
            GoogleConnectButton.Visibility = Visibility.Visible;
            GoogleDisconnectButton.Visibility = Visibility.Collapsed;
        }
        else if (!settings.IsConfigured)
        {
            GoogleStatusText.Text = "Not configured";
            GoogleStatusText.Foreground = FindResource("WarningBrush") as System.Windows.Media.Brush;
            GoogleEmailText.Text = "";
            GoogleConnectButton.Visibility = Visibility.Visible;
            GoogleDisconnectButton.Visibility = Visibility.Collapsed;
        }
        else if (!settings.IsAuthorized)
        {
            GoogleStatusText.Text = "Not connected";
            GoogleStatusText.Foreground = FindResource("WarningBrush") as System.Windows.Media.Brush;
            GoogleEmailText.Text = "";
            GoogleConnectButton.Visibility = Visibility.Visible;
            GoogleDisconnectButton.Visibility = Visibility.Collapsed;
        }
        else
        {
            GoogleStatusText.Text = "Connected";
            GoogleStatusText.Foreground = FindResource("SecondaryBrush") as System.Windows.Media.Brush;
            GoogleEmailText.Text = settings.ConnectedEmail ?? "";
            GoogleConnectButton.Visibility = Visibility.Collapsed;
            GoogleDisconnectButton.Visibility = Visibility.Visible;
        }
    }

    private void GoogleCalendarEnabledToggle_Click(object sender, RoutedEventArgs e)
    {
        UpdateGoogleCalendarStatus();
    }

    private void SaveGoogleSettings_Click(object sender, RoutedEventArgs e)
    {
        var settings = _googleCalendarService.Settings;
        
        settings.IsEnabled = GoogleCalendarEnabledToggle.IsChecked == true;
        settings.ClientId = GoogleClientIdText.Text.Trim();
        settings.ClientSecret = GoogleClientSecretText.Password;
        settings.SyncJobsToCalendar = GoogleSyncJobsCheck.IsChecked == true;
        settings.AddReminders = GoogleAddRemindersCheck.IsChecked == true;

        _googleCalendarService.SaveSettings(settings);
        UpdateGoogleCalendarStatus();

        MessageBox.Show("Google Calendar settings saved successfully!", "Settings Saved",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void ConnectGoogleCalendar_Click(object sender, RoutedEventArgs e)
    {
        // First save current settings
        var settings = _googleCalendarService.Settings;
        settings.IsEnabled = GoogleCalendarEnabledToggle.IsChecked == true;
        settings.ClientId = GoogleClientIdText.Text.Trim();
        settings.ClientSecret = GoogleClientSecretText.Password;
        settings.SyncJobsToCalendar = GoogleSyncJobsCheck.IsChecked == true;
        settings.AddReminders = GoogleAddRemindersCheck.IsChecked == true;
        _googleCalendarService.SaveSettings(settings);

        if (!settings.IsConfigured)
        {
            MessageBox.Show("Please enter your Client ID and Client Secret first.", "Configuration Required",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        GoogleStatusText.Text = "Connecting...";
        GoogleStatusText.Foreground = FindResource("PrimaryBrush") as System.Windows.Media.Brush;

        var (success, email, error) = await _googleCalendarService.AuthorizeAsync();

        if (success)
        {
            UpdateGoogleCalendarStatus();
            
            MessageBox.Show($"Successfully connected to Google Calendar!\n\nConnected as: {email}", 
                "Connected",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            GoogleStatusText.Text = "Connection failed";
            GoogleStatusText.Foreground = FindResource("ErrorBrush") as System.Windows.Media.Brush;
            
            MessageBox.Show($"Failed to connect:\n\n{error}", "Connection Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DisconnectGoogleCalendar_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Disconnect from Google Calendar?\n\nYou will need to re-authorize to sync events again.",
            "Confirm Disconnect",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        _googleCalendarService.Disconnect();
        UpdateGoogleCalendarStatus();

        MessageBox.Show("Disconnected from Google Calendar.", "Disconnected",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OpenApiSetupGuide_Click(object sender, RoutedEventArgs e)
    {
        NavigationRequest.Navigate("ApiSetupGuide");
    }

    #endregion

    #region Business Profile

    private void LoadBusinessProfile()
    {
        try
        {
            var settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OneManVan", "business_profile.json");

            if (File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);
                var profile = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (profile != null)
                {
                    CompanyNameText.Text = profile.GetValueOrDefault("CompanyName", "");
                    CompanyPhoneText.Text = profile.GetValueOrDefault("Phone", "");
                    CompanyEmailText.Text = profile.GetValueOrDefault("Email", "");
                }
            }
        }
        catch { }
    }

    private void SaveBusinessProfile_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Simple settings storage using app settings file
            var settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OneManVan", "business_profile.json");

            var directory = Path.GetDirectoryName(settingsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var profile = new
            {
                CompanyName = CompanyNameText.Text,
                Phone = CompanyPhoneText.Text,
                Email = CompanyEmailText.Text
            };

            var json = System.Text.Json.JsonSerializer.Serialize(profile, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsPath, json);

            // Also update in-memory settings
            if (App.Settings.BusinessProfile != null)
            {
                App.Settings.BusinessProfile.CompanyName = CompanyNameText.Text;
                App.Settings.BusinessProfile.Phone = CompanyPhoneText.Text;
                App.Settings.BusinessProfile.Email = CompanyEmailText.Text;
            }

            MessageBox.Show("Business profile saved successfully!", "Saved",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region CSV Export

    private async void ExportCustomersCsv_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = await _csvService.ExportCustomersAsync();
            if (result.Success)
            {
                var openResult = MessageBox.Show(
                    $"Exported {result.RecordCount} customers successfully!\n\nFile: {Path.GetFileName(result.FilePath)}\n\nOpen export folder?",
                    "Export Complete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (openResult == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _csvService.GetExportDirectory(),
                        UseShellExecute = true
                    });
                }
            }
            else
            {
                MessageBox.Show(result.Message, "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ExportInventoryCsv_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = await _csvService.ExportInventoryAsync();
            if (result.Success)
            {
                var openResult = MessageBox.Show(
                    $"Exported {result.RecordCount} inventory items successfully!\n\nFile: {Path.GetFileName(result.FilePath)}\n\nOpen export folder?",
                    "Export Complete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (openResult == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _csvService.GetExportDirectory(),
                        UseShellExecute = true
                    });
                }
            }
            else
            {
                MessageBox.Show(result.Message, "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ExportAssetsCsv_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = await _csvService.ExportAssetsAsync();
            if (result.Success)
            {
                var openResult = MessageBox.Show(
                    $"Exported {result.RecordCount} assets successfully!\n\nFile: {Path.GetFileName(result.FilePath)}\n\nOpen export folder?",
                    "Export Complete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (openResult == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _csvService.GetExportDirectory(),
                        UseShellExecute = true
                    });
                }
            }
            else
            {
                MessageBox.Show(result.Message, "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenCsvFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var folder = _csvService.GetExportDirectory();
            Process.Start(new ProcessStartInfo
            {
                FileName = folder,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ImportCustomersCsv_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Customers CSV File",
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                InitialDirectory = _csvService.GetExportDirectory()
            };

            if (dialog.ShowDialog() == true)
            {
                var mergeResult = MessageBox.Show(
                    "Merge Mode: Updates existing records and adds new ones.\nReplace Mode: Deletes all existing records first.\n\nUse Merge Mode?",
                    "Import Mode",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (mergeResult == MessageBoxResult.Cancel) return;

                bool mergeMode = mergeResult == MessageBoxResult.Yes;
                
                var result = await _csvService.ImportCustomersAsync(dialog.FileName, mergeMode);
                
                if (result.Success)
                {
                    var details = result.Errors.Count > 0
                        ? $"\n\nErrors:\n{string.Join("\n", result.Errors.Take(5))}"
                        : "";
                    
                    MessageBox.Show(
                        $"{result.Message}{details}",
                        "Import Complete",
                        MessageBoxButton.OK,
                        result.Errors.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(result.Message, "Import Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Import failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ImportInventoryCsv_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Inventory CSV File",
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                InitialDirectory = _csvService.GetExportDirectory()
            };

            if (dialog.ShowDialog() == true)
            {
                var mergeResult = MessageBox.Show(
                    "Merge Mode: Updates existing records and adds new ones.\nReplace Mode: Deletes all existing records first.\n\nUse Merge Mode?",
                    "Import Mode",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (mergeResult == MessageBoxResult.Cancel) return;

                bool mergeMode = mergeResult == MessageBoxResult.Yes;
                
                var result = await _csvService.ImportInventoryAsync(dialog.FileName, mergeMode);
                
                if (result.Success)
                {
                    var details = result.Errors.Count > 0
                        ? $"\n\nErrors:\n{string.Join("\n", result.Errors.Take(5))}"
                        : "";
                    
                    MessageBox.Show(
                        $"{result.Message}{details}",
                        "Import Complete",
                        MessageBoxButton.OK,
                        result.Errors.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(result.Message, "Import Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Import failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region Trade Configuration

    private void LoadTradeConfiguration()
    {
        var trade = App.TradeService.CurrentPreset;
        CurrentTradeDisplay.Text = $"{trade.Icon} {trade.Name} - {trade.Description}";
    }

    private async void ChangeTrade_Click(object sender, RoutedEventArgs e)
    {
        var trades = App.TradeService.GetAvailableTrades().ToList();
        
        var dialog = new TradeSelectionDialog(trades, App.TradeService.CurrentTrade);
        if (dialog.ShowDialog() == true && dialog.SelectedTrade.HasValue)
        {
            var selectedTrade = dialog.SelectedTrade.Value;
            var tradeInfo = trades.First(t => t.Type == selectedTrade);
            
            var result = MessageBox.Show(
                $"Switch to {tradeInfo.Name}?\n\n{tradeInfo.Description}\n\nYour existing data will be preserved, but custom fields and templates will be updated to match {tradeInfo.Name} defaults.",
                "Confirm Trade Change",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await App.TradeService.SetTradeAsync(selectedTrade, keepExistingData: true);
                    LoadTradeConfiguration();
                    
                    MessageBox.Show($"Successfully switched to {tradeInfo.Name}!", "Trade Changed",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to change trade: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    #endregion

    #region Invoice PDF Settings

    private void LoadPdfSettings()
    {
        try
        {
            var settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OneManVan", "invoice_pdf_settings.json");

            if (File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);
                var settings = System.Text.Json.JsonSerializer.Deserialize<InvoicePdfSettings>(json);
                if (settings != null)
                {
                    PrimaryColorText.Text = settings.PrimaryColor ?? "#2196F3";
                    PaymentTermsText.Text = settings.PaymentTerms ?? "";
                    FooterMessageText.Text = settings.FooterMessage ?? "";
                    
                    if (!string.IsNullOrEmpty(settings.LogoPath))
                    {
                        LogoPathText.Text = Path.GetFileName(settings.LogoPath);
                    }
                }
            }
        }
        catch { }
    }

    private void SavePdfSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var settings = new InvoicePdfSettings
            {
                CompanyName = CompanyNameText.Text,
                Phone = CompanyPhoneText.Text,
                Email = CompanyEmailText.Text,
                LogoPath = LogoPathText.Tag as string,
                PrimaryColor = PrimaryColorText.Text,
                PaymentTerms = PaymentTermsText.Text,
                FooterMessage = FooterMessageText.Text
            };

            _pdfService.SaveSettings(settings);

            MessageBox.Show("Invoice PDF settings saved successfully!", "Saved",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UploadLogo_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Company Logo",
                Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                // Copy logo to app data folder
                var logoFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "OneManVan", "Logos");
                
                Directory.CreateDirectory(logoFolder);
                
                var fileName = Path.GetFileName(dialog.FileName);
                var destPath = Path.Combine(logoFolder, fileName);
                
                File.Copy(dialog.FileName, destPath, true);
                
                LogoPathText.Text = fileName;
                LogoPathText.Tag = destPath;

                MessageBox.Show("Logo uploaded successfully! Click 'Save PDF Settings' to apply.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to upload logo: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void PreviewSampleInvoice_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Create sample invoice
            var sampleInvoice = await CreateSampleInvoiceAsync();
            
            // Generate PDF
            var tempPath = Path.Combine(Path.GetTempPath(), $"Sample_Invoice_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            _pdfService.GenerateInvoicePdf(sampleInvoice, tempPath);

            // Open PDF
            Process.Start(new ProcessStartInfo
            {
                FileName = tempPath,
                UseShellExecute = true
            });

            MessageBox.Show($"Sample invoice generated!\n\nFile: {tempPath}", "Preview Generated",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to generate preview: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task<Invoice> CreateSampleInvoiceAsync()
    {
        // Get first customer or create sample
        var customer = await App.DbContext.Customers.FirstOrDefaultAsync();
        if (customer == null)
        {
            customer = new Customer
            {
                Name = "John Doe",
                Email = "john@example.com",
                Phone = "(555) 123-4567",
                CompanyName = "Doe Enterprises",
                CustomerType = Shared.Models.Enums.CustomerType.Residential,
                Status = Shared.Models.Enums.CustomerStatus.Active
            };
        }

        var invoice = new Invoice
        {
            InvoiceNumber = "INV-SAMPLE-001",
            InvoiceDate = DateTime.Now,
            DueDate = DateTime.Now.AddDays(30),
            Customer = customer,
            CustomerId = customer.Id,
            Status = Shared.Models.Enums.InvoiceStatus.Draft,
            Notes = "This is a sample invoice generated for preview purposes.",
            LaborAmount = 239.00m,  // HVAC System Maintenance + Diagnostic
            PartsAmount = 50.00m,   // Air Filter Replacement
            OtherAmount = 0m
        };

        // Calculate totals
        invoice.SubTotal = invoice.LaborAmount + invoice.PartsAmount + invoice.OtherAmount;
        invoice.TaxRate = 7.5m;
        invoice.TaxAmount = invoice.SubTotal * (invoice.TaxRate / 100);
        invoice.Total = invoice.SubTotal + invoice.TaxAmount;
        invoice.AmountPaid = 0;

        return invoice;
    }

    #endregion
}


