using Microsoft.EntityFrameworkCore;
using OneManVan.Mobile.Helpers;
using OneManVan.Mobile.Services;
using OneManVan.Mobile.Theme;
using OneManVan.Shared.Data;

namespace OneManVan.Mobile.Pages;

public partial class SettingsPage : ContentPage
{
    private readonly IDbContextFactory<OneManVanDbContext> _dbFactory;
    private readonly MobileBackupService _backupService;
    private readonly CsvExportImportService _csvService;
    private readonly TradeConfigurationService _tradeService;
    private CancellationTokenSource? _cts;

    public SettingsPage(IDbContextFactory<OneManVanDbContext> dbFactory, MobileBackupService backupService, CsvExportImportService csvService, TradeConfigurationService tradeService)
    {
        InitializeComponent();
        _dbFactory = dbFactory;
        _backupService = backupService;
        _csvService = csvService;
        _tradeService = tradeService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        
        try
        {
            LoadTradeInfo();
            await LoadSettingsAsync();
            await LoadDatabaseStatsAsync(_cts.Token);
            await LoadBackupInfoAsync();
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

    private async void OnExportAllDataClicked(object sender, EventArgs e)
    {
        try
        {
            var confirm = await DisplayAlertAsync(
                "Export All Data",
                "This will export ALL your data (Customers, Sites, Assets, Jobs, Estimates, Invoices, Inventory, Products, Service Agreements) to a single file.\n\nContinue?",
                "Export",
                "Cancel");

            if (!confirm) return;

            var result = await _csvService.ExportAllDataAsync();
            if (result.Success)
            {
                try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

                var share = await DisplayAlertAsync(
                    "Export Complete!",
                    $"Exported {result.RecordCount} total records across all categories.\n\nThe file can be opened and edited in Excel. Re-import to update your data.\n\nWould you like to share the file?",
                    "Share",
                    "Done");

                if (share)
                {
                    await _csvService.ShareCsvAsync(result.FilePath!);
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

    private async void OnImportAllDataClicked(object sender, EventArgs e)
    {
        try
        {
            var confirm = await DisplayAlertAsync(
                "Import All Data",
                "This will import data from a full export file. Existing records with matching names will be UPDATED, new records will be ADDED.\n\nThis action cannot be undone. Make sure you have a backup first.\n\nContinue?",
                "Import",
                "Cancel");

            if (!confirm) return;

            // Let user pick a file
            var fileResult = await FilePicker.PickAsync(new PickOptions
            {
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.Android, new[] { "text/csv", "text/comma-separated-values", "application/csv", "*/*" } },
                    { DevicePlatform.iOS, new[] { "public.comma-separated-values-text", "public.text" } },
                    { DevicePlatform.WinUI, new[] { ".csv", ".txt" } }
                }),
                PickerTitle = "Select All Data Export File"
            });

            if (fileResult == null) return;

            var result = await _csvService.ImportAllDataAsync(fileResult.FullPath);

            if (result.Success)
            {
                try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

                var message = $"Import completed!\n\n" +
                    $"Records added: {result.Inserted}\n" +
                    $"Records updated: {result.Updated}\n" +
                    $"Total processed: {result.TotalProcessed}";

                if (result.Errors.Any())
                {
                    message += $"\n\nWarnings: {result.Errors.Count}";
                }

                await DisplayAlertAsync("Import Complete", message, "OK");

                // Refresh database stats
                await LoadDatabaseStatsAsync();
            }
            else
            {
                var errorMsg = result.Errors.Any() 
                    ? string.Join("\n", result.Errors.Take(5)) 
                    : "Unknown error occurred";
                await DisplayAlertAsync("Import Failed", errorMsg, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Import failed: {ex.Message}", "OK");
        }
    }

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
                    await _csvService.ShareCsvAsync(result.FilePath!);
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
                    await _csvService.ShareCsvAsync(result.FilePath!);
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
                    await _csvService.ShareCsvAsync(result.FilePath!);
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
        try
        {
            var result = await _csvService.ExportJobsAsync();
            if (result.Success)
            {
                try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
                
                var share = await DisplayAlertAsync("Export Complete", 
                    $"Exported {result.RecordCount} jobs.\n\nWould you like to share the file?", 
                    "Share", "Done");
                
                if (share)
                {
                    await _csvService.ShareCsvAsync(result.FilePath!);
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

    #endregion

    #region CSV Import Handlers

    private async void OnImportCustomersCsvClicked(object sender, EventArgs e)
    {
        try
        {
            var csvFiles = await _csvService.GetExportedFilesAsync();
            var customerFiles = csvFiles.Where(f => f.FileName.StartsWith("Customers_")).ToList();

            if (!customerFiles.Any())
            {
                await DisplayAlertAsync("No Files", "No customer CSV files found in exports folder.", "OK");
                return;
            }

            var options = customerFiles.Select(f => $"{f.FileName} ({f.CreatedAt:MMM dd})").ToArray();
            var selected = await DisplayActionSheetAsync("Select CSV to Import", "Cancel", null, options);

            if (selected == "Cancel" || selected == null)
                return;

            var selectedIndex = Array.IndexOf(options, selected);
            if (selectedIndex < 0 || selectedIndex >= customerFiles.Count)
                return;

            var file = customerFiles[selectedIndex];
            var result = await _csvService.ImportCustomersAsync(file.FilePath);

            if (result.Success)
            {
                try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
                _cts?.Cancel();
                _cts = new CancellationTokenSource();
                await LoadDatabaseStatsAsync(_cts.Token);
                await DisplayAlertAsync("Import Complete", 
                    $"Inserted: {result.Inserted}\nUpdated: {result.Updated}", 
                    "OK");
            }
            else
            {
                await DisplayAlertAsync("Import Failed", string.Join("\n", result.Errors), "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Import failed: {ex.Message}", "OK");
        }
    }

    private async void OnImportInventoryCsvClicked(object sender, EventArgs e)
    {
        try
        {
            var csvFiles = await _csvService.GetExportedFilesAsync();
            var inventoryFiles = csvFiles.Where(f => f.FileName.StartsWith("Inventory_")).ToList();

            if (!inventoryFiles.Any())
            {
                await DisplayAlertAsync("No Files", "No inventory CSV files found in exports folder.", "OK");
                return;
            }

            var options = inventoryFiles.Select(f => $"{f.FileName} ({f.CreatedAt:MMM dd})").ToArray();
            var selected = await DisplayActionSheetAsync("Select CSV to Import", "Cancel", null, options);

            if (selected == "Cancel" || selected == null)
                return;

            var selectedIndex = Array.IndexOf(options, selected);
            if (selectedIndex < 0 || selectedIndex >= inventoryFiles.Count)
                return;

            var file = inventoryFiles[selectedIndex];
            var result = await _csvService.ImportInventoryAsync(file.FilePath);

            if (result.Success)
            {
                try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
                _cts?.Cancel();
                _cts = new CancellationTokenSource();
                await LoadDatabaseStatsAsync(_cts.Token);
                await DisplayAlertAsync("Import Complete", 
                    $"Inserted: {result.Inserted}\nUpdated: {result.Updated}", 
                    "OK");
            }
            else
            {
                await DisplayAlertAsync("Import Failed", string.Join("\n", result.Errors), "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Import failed: {ex.Message}", "OK");
        }
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
}
