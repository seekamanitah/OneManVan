using Microsoft.EntityFrameworkCore;
using OneManVan.Mobile.Helpers;
using OneManVan.Mobile.Services;
using OneManVan.Mobile.Theme;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Services;

namespace OneManVan.Mobile.Pages;

public partial class SettingsPage : ContentPage
{
    private readonly IDbContextFactory<OneManVanDbContext> _dbFactory;
    private readonly MobileBackupService _backupService;
    private readonly CsvExportImportService _csvService;
    private readonly TradeConfigurationService _tradeService;
    private readonly DatabaseConfigService _dbConfigService;
    private readonly DatabaseManagementService _dbManagementService;
    private CancellationTokenSource? _cts;

    public SettingsPage(IDbContextFactory<OneManVanDbContext> dbFactory, MobileBackupService backupService, CsvExportImportService csvService, TradeConfigurationService tradeService)
    {
        InitializeComponent();
        _dbFactory = dbFactory;
        _backupService = backupService;
        _csvService = csvService;
        _tradeService = tradeService;
        
        // Initialize database config service
        var configDir = FileSystem.AppDataDirectory;
        _dbConfigService = new DatabaseConfigService(configDir);
        
        // Initialize database management service
        _dbManagementService = new DatabaseManagementService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        
        try
        {
            LoadTradeInfo();
            LoadDatabaseConfiguration();
            await LoadSettingsAsync();
            await LoadDatabaseStatsAsync(_cts.Token);
            await LoadBackupInfoAsync();
            await LoadDatabaseStatsMobileAsync();
        }
        catch (OperationCanceledException)
        {
            // Page navigated away
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SettingsPage.OnAppearing error: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _cts?.Cancel();
    }

    private void LoadTradeInfo()
    {
        var preset = _tradeService.CurrentPreset;
        
        CurrentTradeIcon.Text = preset.Icon;
        CurrentTradeLabel.Text = preset.Name;
        TradeIconDisplay.Text = preset.Icon;
        TradeNameDisplay.Text = preset.Name;
        TradeDetailDisplay.Text = preset.Description;
        TradeDescriptionLabel.Text = $"{preset.Name} Field Service Management";
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            var isDarkMode = Preferences.Get("DarkMode", false);
            DarkModeSwitch.IsToggled = isDarkMode;

            // Business defaults
            LaborRateEntry.Text = Preferences.Get("LaborRate", "85.00");
            TaxRateEntry.Text = Preferences.Get("TaxRate", "7.0");
            InvoiceDueDaysEntry.Text = Preferences.Get("InvoiceDueDays", "30");

            // Business profile
            CompanyNameEntry.Text = Preferences.Get("CompanyName", "");
            CompanyPhoneEntry.Text = Preferences.Get("CompanyPhone", "");
            CompanyEmailEntry.Text = Preferences.Get("CompanyEmail", "");
            CompanyAddressEntry.Text = Preferences.Get("CompanyAddress", "");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load settings: {ex.Message}", "OK");
        }
    }

    private async void OnSaveProfileClicked(object sender, EventArgs e)
    {
        try
        {
            Preferences.Set("CompanyName", CompanyNameEntry.Text ?? "");
            Preferences.Set("CompanyPhone", CompanyPhoneEntry.Text ?? "");
            Preferences.Set("CompanyEmail", CompanyEmailEntry.Text ?? "");
            Preferences.Set("CompanyAddress", CompanyAddressEntry.Text ?? "");

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            await DisplayAlertAsync("Saved", "Business profile saved successfully.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to save profile: {ex.Message}", "OK");
        }
    }

    private async Task LoadDatabaseStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            
            var customers = await db.Customers.CountAsync(cancellationToken);
            var assets = await db.Assets.CountAsync(cancellationToken);
            var estimates = await db.Estimates.CountAsync(cancellationToken);
            var jobs = await db.Jobs.CountAsync(cancellationToken);
            var invoices = await db.Invoices.CountAsync(cancellationToken);
            var inventory = await db.InventoryItems.CountAsync(cancellationToken);

            if (!cancellationToken.IsCancellationRequested)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    CustomerCountLabel.Text = customers.ToString();
                    AssetCountLabel.Text = assets.ToString();
                    EstimateCountLabel.Text = estimates.ToString();
                    JobCountLabel.Text = jobs.ToString();
                    InvoiceCountLabel.Text = invoices.ToString();
                    InventoryCountLabel.Text = inventory.ToString();
                });
            }

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "onemanvan.db");
            if (File.Exists(dbPath))
            {
                var fileInfo = new FileInfo(dbPath);
                var sizeKb = fileInfo.Length / 1024.0;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    DbSizeLabel.Text = sizeKb > 1024 
                        ? $"{sizeKb / 1024:N1} MB" 
                        : $"{sizeKb:N0} KB";
                });
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                await DisplayAlertAsync("Error", $"Failed to load stats: {ex.Message}", "OK");
            }
        }
    }

    private async Task LoadBackupInfoAsync()
    {
        try
        {
            var backups = await _backupService.GetAvailableBackupsAsync();
            var backupList = backups.ToList();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                BackupCountLabel.Text = $"{backupList.Count} backup(s)";
                
                if (backupList.Any())
                {
                    var latest = backupList.OrderByDescending(b => b.CreatedAt).First();
                    LastBackupLabel.Text = latest.CreatedAt.ToShortDateTime();
                }
                else
                {
                    LastBackupLabel.Text = "Never";
                }
            });
        }
        catch
        {
            // Ignore backup info errors
        }
    }

    #region Trade Configuration

    private async void OnChangeTradeClicked(object sender, EventArgs e)
    {
        var trades = _tradeService.GetAvailableTrades().ToList();
        var options = trades.Select(t => $"{t.Icon} {t.Name}").ToArray();
        
        var selected = await DisplayActionSheetAsync("Select Trade", "Cancel", null, options);
        
        if (selected == "Cancel" || selected == null)
            return;

        var selectedIndex = Array.IndexOf(options, selected);
        if (selectedIndex < 0 || selectedIndex >= trades.Count)
            return;

        var trade = trades[selectedIndex];
        
        var confirm = await DisplayAlertAsync(
            "Change Trade",
            $"Switch to {trade.Name}?\n\nThis will update custom fields and templates. Your existing data will remain but may display differently.",
            "Change", "Cancel");

        if (!confirm)
            return;

        try
        {
            await _tradeService.SetTradeAsync(trade.Type, keepExistingData: true);
            LoadTradeInfo();
            
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            
            await DisplayAlertAsync("Trade Changed", $"Switched to {trade.Name}. Custom fields and templates updated.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to change trade: {ex.Message}", "OK");
        }
    }

    private async void OnEditSchemaClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("SchemaEditor");
    }

    #endregion

    private void OnDarkModeToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Set("DarkMode", e.Value);
        Application.Current!.UserAppTheme = e.Value ? AppTheme.Dark : AppTheme.Light;
    }

    private async void OnSaveDefaultsClicked(object sender, EventArgs e)
    {
        try
        {
            if (decimal.TryParse(LaborRateEntry.Text, out var laborRate))
                Preferences.Set("LaborRate", laborRate.ToString("N2"));

            if (decimal.TryParse(TaxRateEntry.Text, out var taxRate))
                Preferences.Set("TaxRate", taxRate.ToString("N1"));

            if (int.TryParse(InvoiceDueDaysEntry.Text, out var dueDays))
                Preferences.Set("InvoiceDueDays", dueDays.ToString());

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

            await DisplayAlertAsync("Saved", "Business defaults have been saved.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to save: {ex.Message}", "OK");
        }
    }

    private async void OnExportBackupClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await _backupService.CreateBackupAsync();
            
            if (result.Success)
            {
                try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

                await LoadBackupInfoAsync();
                await DisplayAlertAsync("Backup Created", 
                    $"Backup saved successfully.\n{result.RecordCount} records exported.\n\nLocation: {result.FilePath}", 
                    "OK");
            }
            else
            {
                await DisplayAlertAsync("Backup Failed", result.Message, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to create backup: {ex.Message}", "OK");
        }
    }

    private async void OnImportBackupClicked(object sender, EventArgs e)
    {
        try
        {
            var backups = await _backupService.GetAvailableBackupsAsync();
            var backupList = backups.OrderByDescending(b => b.CreatedAt).ToList();

            if (!backupList.Any())
            {
                await DisplayAlertAsync("No Backups", "No backup files found to import.", "OK");
                return;
            }

            var options = backupList.Select(b => $"{b.FileName} ({b.CreatedAt:MMM dd, HH:mm})").ToArray();
            var selected = await DisplayActionSheetAsync("Select Backup to Import", "Cancel", null, options);

            if (selected == "Cancel" || selected == null)
                return;

            var selectedIndex = Array.IndexOf(options, selected);
            if (selectedIndex < 0 || selectedIndex >= backupList.Count)
                return;

            var importMode = await DisplayActionSheetAsync(
                "Import Mode",
                "Cancel",
                null,
                "Merge (update existing, add new)",
                "Replace All (clear and restore)");

            if (importMode == "Cancel" || importMode == null)
                return;

            var useMerge = importMode.StartsWith("Merge");

            var backup = backupList[selectedIndex];
            var result = await _backupService.RestoreBackupAsync(backup.FilePath, useMerge);

            if (result.Success)
            {
                try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

                _cts?.Cancel();
                _cts = new CancellationTokenSource();
                await LoadDatabaseStatsAsync(_cts.Token);
                await DisplayAlertAsync("Import Complete", result.Message ?? "Data restored successfully.", "OK");
            }
            else
            {
                await DisplayAlertAsync("Import Failed", result.Message, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to import backup: {ex.Message}", "OK");
        }
    }

    private async void OnShareBackupClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await _backupService.CreateAndShareBackupAsync();

            if (result.Success)
            {
                await LoadBackupInfoAsync();
            }
            else
            {
                await DisplayAlertAsync("Error", result.Message, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to share backup: {ex.Message}", "OK");
        }
    }

    #region Full Data Export/Import

    // TODO: Re-implement with shared CsvExportImportService
    /*
    private async void OnExportAllDataClicked(object sender, EventArgs e)
    {
        await DisplayAlertAsync("Coming Soon", "Full data export is being re-implemented. Use individual exports for now.", "OK");
    }

    private async void OnImportAllDataClicked(object sender, EventArgs e)
    {
        await DisplayAlertAsync("Coming Soon", "Full data import is being re-implemented. Use individual imports for now.", "OK");
    }
    */

    #endregion

    #region CSV Export Handlers

    private async void OnExportCustomersCsvClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await _csvService.ExportCustomersAsync();
            if (result.Success)
            {
                try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
                
                var share = await DisplayAlertAsync("Export Complete", 
                    $"Exported {result.RecordCount} customers.\n\nWould you like to share the file?", 
                    "Share", "Done");
                
                if (share)
                {
                    // TODO: Re-implement file sharing
                    // await _csvService.ShareCsvAsync(result.FilePath!);
                }
            }
            else
            {
                await DisplayAlertAsync("Export Failed", result.Message, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Export failed: {ex.Message}", "OK");
        }
    }

    private async void OnExportInventoryCsvClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await _csvService.ExportInventoryAsync();
            if (result.Success)
            {
                try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
                
                var share = await DisplayAlertAsync("Export Complete", 
                    $"Exported {result.RecordCount} inventory items.\n\nWould you like to share the file?", 
                    "Share", "Done");
                
                if (share)
                {
                    // TODO: Re-implement file sharing
                    // await _csvService.ShareCsvAsync(result.FilePath!);
                }
            }
            else
            {
                await DisplayAlertAsync("Export Failed", result.Message, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Export failed: {ex.Message}", "OK");
        }
    }

    private async void OnExportAssetsCsvClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await _csvService.ExportAssetsAsync();
            if (result.Success)
            {
                try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
                
                var share = await DisplayAlertAsync("Export Complete", 
                    $"Exported {result.RecordCount} assets.\n\nWould you like to share the file?", 
                    "Share", "Done");
                
                if (share)
                {
                    // TODO: Re-implement file sharing
                    // await _csvService.ShareCsvAsync(result.FilePath!);
                }
            }
            else
            {
                await DisplayAlertAsync("Export Failed", result.Message, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Export failed: {ex.Message}", "OK");
        }
    }

    private async void OnExportJobsCsvClicked(object sender, EventArgs e)
    {
        // TODO: Re-implement ExportJobsAsync
        await DisplayAlertAsync("Coming Soon", "Job export is being re-implemented.", "OK");
    }

    #endregion

    #region CSV Import Handlers

    private async void OnImportCustomersCsvClicked(object sender, EventArgs e)
    {
        // TODO: Re-implement with GetExportedFilesAsync or file picker
        await DisplayAlertAsync("Coming Soon", "CSV import is being re-implemented.", "OK");
    }

    private async void OnImportInventoryCsvClicked(object sender, EventArgs e)
    {
        // TODO: Re-implement with GetExportedFilesAsync or file picker
        await DisplayAlertAsync("Coming Soon", "CSV import is being re-implemented.", "OK");
    }

    #endregion

    private async void OnClearDataClicked(object sender, EventArgs e)
    {
        var confirm1 = await DisplayAlertAsync(
            "Clear All Data",
            "This will permanently delete ALL data including customers, assets, jobs, invoices, and inventory. This action cannot be undone!",
            "Continue", "Cancel");

        if (!confirm1)
            return;

        var confirm2 = await DisplayPromptAsync(
            "Confirm Delete",
            "Type 'DELETE' to confirm:",
            placeholder: "DELETE");

        if (confirm2?.ToUpper() != "DELETE")
        {
            await DisplayAlertAsync("Cancelled", "Data was not cleared.", "OK");
            return;
        }

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            // Clear all tables in order
            db.Payments.RemoveRange(db.Payments);
            db.Invoices.RemoveRange(db.Invoices);
            db.TimeEntries.RemoveRange(db.TimeEntries);
            db.Jobs.RemoveRange(db.Jobs);
            db.InventoryLogs.RemoveRange(db.InventoryLogs);
            db.InventoryItems.RemoveRange(db.InventoryItems);
            db.EstimateLines.RemoveRange(db.EstimateLines);
            db.Estimates.RemoveRange(db.Estimates);
            db.CustomFields.RemoveRange(db.CustomFields);
            db.Assets.RemoveRange(db.Assets);
            db.Sites.RemoveRange(db.Sites);
            db.Customers.RemoveRange(db.Customers);

            await db.SaveChangesAsync();

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            await LoadDatabaseStatsAsync(_cts.Token);
            await DisplayAlertAsync("Data Cleared", "All data has been deleted.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to clear data: {ex.Message}", "OK");
        }
    }

    private async void OnRunTestsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("TestRunner");
    }

    private async void OnViewSchemaClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("SchemaEditor");
    }

    #region Database Configuration

    private void LoadDatabaseConfiguration()
    {
        try
        {
            var config = _dbConfigService.LoadConfiguration();

            DatabaseTypePicker.SelectedIndex = config.Type == DatabaseType.SQLite ? 0 : 1;

            if (config.Type == DatabaseType.SqlServer)
            {
                ServerAddressMobileEntry.Text = config.ServerAddress ?? "";
                ServerPortMobileEntry.Text = config.ServerPort.ToString();
                DatabaseNameMobileEntry.Text = config.DatabaseName;
                UsernameMobileEntry.Text = config.Username ?? "";
                PasswordMobileEntry.Text = config.Password ?? "";
                TrustCertificateMobileCheck.IsChecked = config.TrustServerCertificate;
            }

            UpdateCurrentConfigMobile();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load database configuration: {ex.Message}");
        }
    }

    private void OnDatabaseTypeChanged(object sender, EventArgs e)
    {
        var isLocal = DatabaseTypePicker.SelectedIndex == 0;
        SqlServerMobilePanel.IsVisible = !isLocal;
        LocalModeDescription.IsVisible = isLocal;
        RemoteModeDescription.IsVisible = !isLocal;
        UpdateCurrentConfigMobile();
    }

    private async void OnTestConnectionMobileClicked(object sender, EventArgs e)
    {
        ConnectionStatusMobileLabel.IsVisible = false;

        var config = BuildConfigFromMobileUI();
        
        var validation = config.Validate();
        if (!validation.IsValid)
        {
            ConnectionStatusMobileLabel.Text = $"? {validation.ErrorMessage}";
            ConnectionStatusMobileLabel.TextColor = Colors.Red;
            ConnectionStatusMobileLabel.IsVisible = true;
            return;
        }

        var button = sender as Button;
        if (button == null) return;
        var originalText = button.Text;
        button.Text = "Testing...";
        button.IsEnabled = false;

        try
        {
            // For SQLite, test file access
            if (config.Type == DatabaseType.SQLite)
            {
                var connectionString = config.GetConnectionString(FileSystem.AppDataDirectory);
                var optionsBuilder = new DbContextOptionsBuilder<OneManVanDbContext>();
                optionsBuilder.UseSqlite(connectionString);

                await using var context = new OneManVanDbContext(optionsBuilder.Options);
                var canConnect = await context.Database.CanConnectAsync();

                if (canConnect)
                {
                    ConnectionStatusMobileLabel.Text = "? SQLite database accessible!";
                    ConnectionStatusMobileLabel.TextColor = Colors.Green;
                }
                else
                {
                    ConnectionStatusMobileLabel.Text = "? Unable to access SQLite database";
                    ConnectionStatusMobileLabel.TextColor = Colors.Red;
                }
            }
            else
            {
                // For SQL Server, just show validation success
                ConnectionStatusMobileLabel.Text = "? Configuration is valid. Save and restart to connect.";
                ConnectionStatusMobileLabel.TextColor = Colors.Green;
            }

            ConnectionStatusMobileLabel.IsVisible = true;
        }
        catch (Exception ex)
        {
            ConnectionStatusMobileLabel.Text = $"? Test failed: {ex.Message}";
            ConnectionStatusMobileLabel.TextColor = Colors.Red;
            ConnectionStatusMobileLabel.IsVisible = true;
        }
        finally
        {
            button.Text = originalText;
            button.IsEnabled = true;
        }
    }

    private async void OnSaveDatabaseConfigMobileClicked(object sender, EventArgs e)
    {
        var newConfig = BuildConfigFromMobileUI();
        
        var validation = newConfig.Validate();
        if (!validation.IsValid)
        {
            await DisplayAlertAsync("Validation Error", validation.ErrorMessage, "OK");
            return;
        }

        if (!_dbConfigService.HasConfigurationChanged(newConfig))
        {
            await DisplayAlertAsync("No Changes", "Configuration has not changed.", "OK");
            return;
        }

        var confirm = await DisplayAlertAsync(
            "Restart Required",
            "Changing database configuration requires restarting the app. Continue?",
            "Restart",
            "Cancel");

        if (!confirm) return;

        try
        {
            _dbConfigService.SaveConfiguration(newConfig);

            // Display success message
            await DisplayAlertAsync("Configuration Saved", 
                "Database configuration saved. The app will restart now.", 
                "OK");

            // Restart app
            Application.Current?.Quit();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to save: {ex.Message}", "OK");
        }
    }

    private void OnShowPasswordMobileClicked(object sender, EventArgs e)
    {
        PasswordMobileEntry.IsPassword = !PasswordMobileEntry.IsPassword;
        ShowPasswordMobileButton.Text = PasswordMobileEntry.IsPassword ? "Show" : "Hide";
    }

    private DatabaseConfig BuildConfigFromMobileUI()
    {
        var config = new DatabaseConfig();

        if (DatabaseTypePicker.SelectedIndex == 1) // SQL Server
        {
            config.Type = DatabaseType.SqlServer;
            config.ServerAddress = ServerAddressMobileEntry.Text?.Trim();
            config.ServerPort = int.TryParse(ServerPortMobileEntry.Text, out var port) ? port : 1433;
            config.DatabaseName = DatabaseNameMobileEntry.Text?.Trim() ?? "OneManVanFSM";
            config.Username = UsernameMobileEntry.Text?.Trim();
            config.Password = PasswordMobileEntry.Text;
            config.TrustServerCertificate = TrustCertificateMobileCheck.IsChecked;
        }
        else
        {
            config.Type = DatabaseType.SQLite;
        }

        return config;
    }

    private void UpdateCurrentConfigMobile()
    {
        try
        {
            var config = _dbConfigService.GetCurrentConfiguration();
            CurrentConfigMobileLabel.Text = 
                $"{config.Type}\n{config.GetDisplayConnectionString(FileSystem.AppDataDirectory)}";
        }
        catch (Exception ex)
        {
            CurrentConfigMobileLabel.Text = $"Error: {ex.Message}";
        }
    }

    #endregion

    #region Database Management (Mobile)

    private async Task LoadDatabaseStatsMobileAsync()
    {
        try
        {
            await using var context = await _dbFactory.CreateDbContextAsync();
            var stats = await _dbManagementService.GetDatabaseStatsAsync(context);
            
            DatabaseStatsMobileLabel.Text = $"Customers: {stats.CustomerCount} | Companies: {stats.CompanyCount}\n" +
                                            $"Assets: {stats.AssetCount} | Products: {stats.ProductCount}\n" +
                                            $"Jobs: {stats.JobCount} | Estimates: {stats.EstimateCount}\n" +
                                            $"Invoices: {stats.InvoiceCount} | Service Agreements: {stats.ServiceAgreementCount}\n" +
                                            $"Total Records: {stats.TotalRecords}";
        }
        catch (Exception ex)
        {
            DatabaseStatsMobileLabel.Text = $"Error: {ex.Message}";
        }
    }

    private async void OnRefreshDatabaseStatsMobileClicked(object sender, EventArgs e)
    {
        await LoadDatabaseStatsMobileAsync();
        await DisplayAlertAsync("Success", "Statistics refreshed!", "OK");
    }

    private async void OnSeedDemoDataMobileClicked(object sender, EventArgs e)
    {
        var confirm = await DisplayAlertAsync(
            "Seed Demo Data",
            "This will add demo data:\n\n" +
            "• 5 Customers\n" +
            "• 3 Companies\n" +
            "• 4 Assets\n" +
            "• 4 Products\n" +
            "• 3 Jobs\n" +
            "• 2 Estimates\n" +
            "• 2 Invoices\n" +
            "• 1 Service Agreement\n\n" +
            "Continue?",
            "Yes",
            "Cancel");

        if (!confirm) return;

        try
        {
            var button = sender as Button;
            if (button != null)
            {
                button.Text = "Seeding...";
                button.IsEnabled = false;
            }

            await using var context = await _dbFactory.CreateDbContextAsync();
            var success = await _dbManagementService.SeedDemoDataAsync(context);

            if (button != null)
            {
                button.Text = "Seed Demo Data";
                button.IsEnabled = true;
            }

            if (success)
            {
                await DisplayAlertAsync("Success", "Demo data seeded successfully!", "OK");
                await LoadDatabaseStatsMobileAsync();
            }
            else
            {
                await DisplayAlertAsync("Information", "Failed to seed. Database may already contain data.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to seed: {ex.Message}", "OK");
        }
    }

    private async void OnClearAllDataMobileClicked(object sender, EventArgs e)
    {
        var confirm = await DisplayAlertAsync(
            "Clear All Data",
            "WARNING: This will delete ALL data!\n\n" +
            "This includes customers, companies, assets, products, jobs, estimates, invoices, and service agreements.\n\n" +
            "THIS CANNOT BE UNDONE!\n\n" +
            "Continue?",
            "Yes, Delete All",
            "Cancel");

        if (!confirm) return;

        try
        {
            var button = sender as Button;
            if (button != null)
            {
                button.Text = "Clearing...";
                button.IsEnabled = false;
            }

            await using var context = await _dbFactory.CreateDbContextAsync();
            var success = await _dbManagementService.ClearAllDataAsync(context);

            if (button != null)
            {
                button.Text = "Clear All Data";
                button.IsEnabled = true;
            }

            if (success)
            {
                await DisplayAlertAsync("Success", "All data cleared successfully!", "OK");
                await LoadDatabaseStatsMobileAsync();
            }
            else
            {
                await DisplayAlertAsync("Error", "Failed to clear data.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to clear: {ex.Message}", "OK");
        }
    }

    private async void OnResetDatabaseMobileClicked(object sender, EventArgs e)
    {
        var confirm = await DisplayAlertAsync(
            "Reset Database",
            "CRITICAL WARNING!\n\n" +
            "This will COMPLETELY ERASE the database and recreate it fresh.\n\n" +
            "ALL DATA WILL BE LOST!\n\n" +
            "THIS CANNOT BE UNDONE!\n\n" +
            "Are you absolutely sure?",
            "Yes, Reset",
            "Cancel");

        if (!confirm) return;

        // Final confirmation
        var finalConfirm = await DisplayAlertAsync(
            "Final Warning",
            "This is your FINAL WARNING!\n\n" +
            "The database will be completely erased.\n\n" +
            "Continue?",
            "Yes, I'm Sure",
            "Cancel");

        if (!finalConfirm) return;

        try
        {
            var button = sender as Button;
            if (button != null)
            {
                button.Text = "Resetting...";
                button.IsEnabled = false;
            }

            await using var context = await _dbFactory.CreateDbContextAsync();
            var success = await _dbManagementService.ResetDatabaseAsync(context);

            if (success)
            {
                await DisplayAlertAsync("Success", 
                    "Database reset successfully!\n\nThe app will now restart.", 
                    "OK");
                
                // Restart app
                Application.Current?.Quit();
            }
            else
            {
            if (button != null)
            {
                button.Text = "Reset Database";
                button.IsEnabled = true;
            }
                await DisplayAlertAsync("Error", "Failed to reset database.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to reset: {ex.Message}", "OK");
        }
    }

    #endregion
}
