using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.EntityFrameworkCore;
using OneManVan.Services;
using OneManVan.Shared.Models;
using OneManVan.Shared.Services;

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
    private readonly DatabaseConfigService _databaseConfigService;
    private readonly DatabaseManagementService _databaseManagementService;
    
    public decimal LaborRate { get; set; } = 85m;
    public decimal TaxRate { get; set; } = 7m;
    public int InvoiceDueDays { get; set; } = 30;

    public SettingsPage()
    {
        InitializeComponent();
        
        // Use shared CsvExportImportService with ISettingsStorage
        var settingsStorage = new DesktopSettingsStorage();
        _csvService = new Shared.Services.CsvExportImportService(App.DbContext, settingsStorage);
        _pdfService = new InvoicePdfService();
        
        // Initialize database config service
        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OneManVan");
        _databaseConfigService = new DatabaseConfigService(configDir);
        
        // Initialize database management service
        _databaseManagementService = new DatabaseManagementService();
        
        DataContext = this;
        LoadSettings();
        LoadKeyboardShortcuts();
        LoadSquareSettings();
        LoadGoogleCalendarSettings();
        LoadTradeConfiguration();
        LoadPdfSettings();
        LoadDatabaseConfiguration();
        _ = LoadDatabaseStatsAsync();
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
                    $"Backup created successfully!\n\nFile: {result.FilePath}\nRecords: {result.RecordCount}\n\n{result.Message}",
                    "Backup Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"Backup failed: {result.Message}", "Error",
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
                        $"Records imported: {importResult.RecordCount}\n" +
                        $"Timestamp: {importResult.Timestamp:yyyy-MM-dd HH:mm:ss}";

                    if (!string.IsNullOrEmpty(importResult.Message))
                    {
                        message += $"\n\n{importResult.Message}";
                    }

                    MessageBox.Show(message, "Import Complete",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Import failed:\n{importResult.Message}", "Import Error",
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

    #region Database Configuration

    private void LoadDatabaseConfiguration()
    {
        try
        {
            var config = _databaseConfigService.LoadConfiguration();

            // Set UI values
            if (config.Type == DatabaseType.SQLite)
            {
                SqliteRadio.IsChecked = true;
                SqlServerPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                SqlServerRadio.IsChecked = true;
                SqlServerPanel.Visibility = Visibility.Visible;
                
                ServerAddressBox.Text = config.ServerAddress ?? "localhost";
                ServerPortBox.Text = config.ServerPort.ToString();
                DatabaseNameBox.Text = config.DatabaseName;
                UsernameBox.Text = config.Username ?? "";
                PasswordBox.Password = config.Password ?? "";
                TrustCertificateCheck.IsChecked = config.TrustServerCertificate;
                EncryptCheck.IsChecked = config.Encrypt;
            }

            UpdateCurrentConfigDisplay();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load database configuration: {ex.Message}");
        }
    }

    private void OnDatabaseTypeChanged(object sender, RoutedEventArgs e)
    {
        // Null check - controls may not be initialized yet during InitializeComponent
        if (SqlServerRadio == null || SqlServerPanel == null)
            return;

        if (SqlServerRadio.IsChecked == true)
        {
            SqlServerPanel.Visibility = Visibility.Visible;
        }
        else
        {
            SqlServerPanel.Visibility = Visibility.Collapsed;
        }

        UpdateCurrentConfigDisplay();
    }

    private async void OnTestConnectionClicked(object sender, RoutedEventArgs e)
    {
        ConnectionStatusText.Visibility = Visibility.Collapsed;

        // Build config from UI
        var testConfig = BuildConfigFromUI();
        
        // Validate first
        var validation = testConfig.Validate();
        if (!validation.IsValid)
        {
            ConnectionStatusText.Text = $"? {validation.ErrorMessage}";
            ConnectionStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
            ConnectionStatusText.Visibility = Visibility.Visible;
            return;
        }

        // Show loading
        var button = sender as Button;
        if (button == null) return;
        var originalContent = button?.Content;
        button.Content = "Testing...";
        button.IsEnabled = false;

        try
        {
            // For SQLite, test file access
            if (testConfig.Type == DatabaseType.SQLite)
            {
                var baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "OneManVan");
                var connectionString = testConfig.GetConnectionString(baseDir);
                
                var optionsBuilder = new DbContextOptionsBuilder<Shared.Data.OneManVanDbContext>();
                optionsBuilder.UseSqlite(connectionString);
                
                using var context = new Shared.Data.OneManVanDbContext(optionsBuilder.Options);
                var canConnect = await context.Database.CanConnectAsync();
                
                if (canConnect)
                {
                    ConnectionStatusText.Text = "? SQLite database accessible!";
                    ConnectionStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
                }
                else
                {
                    ConnectionStatusText.Text = "? Unable to access SQLite database";
                    ConnectionStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                }
            }
            else
            {
                // For SQL Server, just show validation success (actual connection test requires SQL Server package)
                ConnectionStatusText.Text = "? Configuration is valid. Save and restart to connect to SQL Server.";
                ConnectionStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
            }
            
            ConnectionStatusText.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            ConnectionStatusText.Text = $"? Connection failed: {ex.Message}";
            ConnectionStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
            ConnectionStatusText.Visibility = Visibility.Visible;
        }
        finally
        {
            button.Content = originalContent;
            button.IsEnabled = true;
        }
    }

    private void OnSaveDatabaseConfigClicked(object sender, RoutedEventArgs e)
    {
        var newConfig = BuildConfigFromUI();
        
        // Validate
        var validation = newConfig.Validate();
        if (!validation.IsValid)
        {
            MessageBox.Show(validation.ErrorMessage, "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Check if changed
        if (!_databaseConfigService.HasConfigurationChanged(newConfig))
        {
            MessageBox.Show("No changes detected.", "Information", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Confirm restart
        var result = MessageBox.Show(
            "Changing the database configuration requires restarting the application.\n\n" +
            "Any unsaved changes will be lost. Continue?",
            "Restart Required",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            // Save configuration
            _databaseConfigService.SaveConfiguration(newConfig);

            // Restart application
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(exePath))
            {
                Process.Start(exePath);
                Application.Current.Shutdown();
            }
            else
            {
                MessageBox.Show("Configuration saved. Please restart the application manually.", 
                    "Restart Required", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save configuration: {ex.Message}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnResetDatabaseConfigClicked(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Reset database configuration to default (Local SQLite)?",
            "Confirm Reset",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _databaseConfigService.ResetToDefault();
            LoadDatabaseConfiguration();
        }
    }

    private void OnShowPasswordClicked(object sender, RoutedEventArgs e)
    {
        // Toggle password visibility
        if (ShowPasswordButton.Content.ToString() == "Show")
        {
            // Create TextBox to show password
            var textBox = new TextBox
            {
                Text = PasswordBox.Password,
                IsReadOnly = true,
                Background = PasswordBox.Background,
                Foreground = PasswordBox.Foreground,
                BorderBrush = PasswordBox.BorderBrush
            };
            
            // Replace PasswordBox temporarily
            var parent = PasswordBox.Parent as Grid;
            var index = parent.Children.IndexOf(PasswordBox);
            parent.Children.RemoveAt(index);
            parent.Children.Insert(index, textBox);
            
            ShowPasswordButton.Content = "Hide";
            ShowPasswordButton.Tag = textBox;
        }
        else
        {
            // Restore PasswordBox
            var textBox = ShowPasswordButton.Tag as TextBox;
            var parent = textBox.Parent as Grid;
            var index = parent.Children.IndexOf(textBox);
            parent.Children.RemoveAt(index);
            parent.Children.Insert(index, PasswordBox);
            
            ShowPasswordButton.Content = "Show";
            ShowPasswordButton.Tag = null;
        }
    }

    private DatabaseConfig BuildConfigFromUI()
    {
        var config = new DatabaseConfig();

        if (SqlServerRadio.IsChecked == true)
        {
            config.Type = DatabaseType.SqlServer;
            config.ServerAddress = ServerAddressBox.Text.Trim();
            config.ServerPort = int.TryParse(ServerPortBox.Text, out var port) ? port : 1433;
            config.DatabaseName = DatabaseNameBox.Text.Trim();
            config.Username = UsernameBox.Text.Trim();
            config.Password = PasswordBox.Password;
            config.TrustServerCertificate = TrustCertificateCheck.IsChecked == true;
            config.Encrypt = EncryptCheck.IsChecked == true;
        }
        else
        {
            config.Type = DatabaseType.SQLite;
            config.SqliteFilePath = "OneManVan.db";
        }

        return config;
    }

    private void UpdateCurrentConfigDisplay()
    {
        try
        {
            var config = _databaseConfigService.GetCurrentConfiguration();
            var baseDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OneManVan");
            
            CurrentConfigText.Text = $"Type: {config.Type}\n" +
                                     $"Connection: {config.GetDisplayConnectionString(baseDir)}";
        }
        catch (Exception ex)
        {
            CurrentConfigText.Text = $"Error: {ex.Message}";
        }
    }

    #endregion

    #region Database Management

    private async Task LoadDatabaseStatsAsync()
    {
        try
        {
            var stats = await _databaseManagementService.GetDatabaseStatsAsync(App.DbContext);
            
            DatabaseStatsText.Text = $"Customers: {stats.CustomerCount} | " +
                                     $"Companies: {stats.CompanyCount} | " +
                                     $"Assets: {stats.AssetCount} | " +
                                     $"Products: {stats.ProductCount}\n" +
                                     $"Jobs: {stats.JobCount} | " +
                                     $"Estimates: {stats.EstimateCount} | " +
                                     $"Invoices: {stats.InvoiceCount} | " +
                                     $"Service Agreements: {stats.ServiceAgreementCount}\n" +
                                     $"Total Records: {stats.TotalRecords}";
        }
        catch (Exception ex)
        {
            DatabaseStatsText.Text = $"Error loading stats: {ex.Message}";
        }
    }

    private async void OnRefreshDatabaseStatsClicked(object sender, RoutedEventArgs e)
    {
        await LoadDatabaseStatsAsync();
    }

    private async void OnSeedDemoDataClicked(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "This will add demo data to your database:\n\n" +
            "• 5 Customers\n" +
            "• 3 Companies\n" +
            "• 4 Assets\n" +
            "• 4 Products\n" +
            "• 3 Jobs\n" +
            "• 2 Estimates\n" +
            "• 2 Invoices\n" +
            "• 1 Service Agreement\n\n" +
            "Continue?",
            "Seed Demo Data",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            var button = sender as Button;
            if (button == null) return;
            var originalContent = button?.Content;
            button.Content = "Seeding...";
            button.IsEnabled = false;

            var success = await _databaseManagementService.SeedDemoDataAsync(App.DbContext);

            if (success)
            {
                MessageBox.Show("Demo data seeded successfully!", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadDatabaseStatsAsync();
            }
            else
            {
                MessageBox.Show("Failed to seed demo data. Database may already contain data.", 
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            button.Content = originalContent;
            button.IsEnabled = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error seeding demo data: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnClearAllDataClicked(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "?? WARNING: This will delete ALL data from the database!\n\n" +
            "This includes:\n" +
            "• All customers and companies\n" +
            "• All assets and products\n" +
            "• All jobs, estimates, and invoices\n" +
            "• All service agreements\n\n" +
            "The database structure will be preserved.\n\n" +
            "THIS CANNOT BE UNDONE!\n\n" +
            "Are you ABSOLUTELY SURE you want to continue?",
            "Clear All Data - FINAL WARNING",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            var button = sender as Button;
            if (button == null) return;
            var originalContent = button?.Content;
            button.Content = "Clearing...";
            button.IsEnabled = false;

            var success = await _databaseManagementService.ClearAllDataAsync(App.DbContext);

            if (success)
            {
                MessageBox.Show("All data cleared successfully!", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadDatabaseStatsAsync();
            }
            else
            {
                MessageBox.Show("Failed to clear data.", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

            button.Content = originalContent;
            button.IsEnabled = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error clearing data: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnResetDatabaseClicked(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "?? CRITICAL WARNING: This will COMPLETELY ERASE the database!\n\n" +
            "This will:\n" +
            "• Delete ALL data (customers, companies, assets, etc.)\n" +
            "• Delete the entire database structure\n" +
            "• Recreate a fresh, empty database\n\n" +
            "THIS CANNOT BE UNDONE!\n\n" +
            "Are you ABSOLUTELY SURE you want to continue?",
            "Reset Database - CRITICAL WARNING",
            MessageBoxButton.YesNo,
            MessageBoxImage.Stop);

        if (result != MessageBoxResult.Yes)
            return;

        // Second confirmation
        var confirmResult = MessageBox.Show(
            "This is your FINAL WARNING!\n\n" +
            "Type 'YES' in the next dialog to confirm database reset.",
            "Final Confirmation Required",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);

        if (confirmResult != MessageBoxResult.OK)
            return;

        try
        {
            var button = sender as Button;
            if (button == null) return;
            var originalContent = button?.Content;
            button.Content = "Resetting...";
            button.IsEnabled = false;

            var success = await _databaseManagementService.ResetDatabaseAsync(App.DbContext);

            if (success)
            {
                MessageBox.Show(
                    "Database reset successfully!\n\n" +
                    "The application will now restart.",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Restart application
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                {
                    System.Diagnostics.Process.Start(exePath);
                    Application.Current.Shutdown();
                }
            }
            else
            {
                MessageBox.Show("Failed to reset database.", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

            button.Content = originalContent;
            button.IsEnabled = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error resetting database: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion
}


