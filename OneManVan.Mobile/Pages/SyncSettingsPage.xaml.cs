using OneManVan.Mobile.Services;

namespace OneManVan.Mobile.Pages;

/// <summary>
/// Settings page for sync configuration and management.
/// </summary>
public partial class SyncSettingsPage : ContentPage
{
    private readonly AdvancedSyncService? _syncService;
    private readonly OfflineSyncService? _offlineSyncService;

    public SyncSettingsPage(AdvancedSyncService syncService, OfflineSyncService offlineSyncService)
    {
        InitializeComponent();
        _syncService = syncService;
        _offlineSyncService = offlineSyncService;

        if (_syncService != null)
        {
            _syncService.SyncStarted += OnSyncStarted;
            _syncService.SyncCompleted += OnSyncCompleted;
            _syncService.SyncProgress += OnSyncProgress;
        }

        if (_offlineSyncService != null)
        {
            _offlineSyncService.StatusChanged += OnOfflineStatusChanged;
        }

        LoadSettings();
    }

    public SyncSettingsPage()
    {
        InitializeComponent();
        LoadSettings();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshStatus();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (_syncService != null)
        {
            _syncService.SyncStarted -= OnSyncStarted;
            _syncService.SyncCompleted -= OnSyncCompleted;
            _syncService.SyncProgress -= OnSyncProgress;
        }

        if (_offlineSyncService != null)
        {
            _offlineSyncService.StatusChanged -= OnOfflineStatusChanged;
        }
    }

    private void LoadSettings()
    {
        AutoSyncSwitch.IsToggled = Preferences.Get("AutoSyncEnabled", true);
        WifiOnlySwitch.IsToggled = Preferences.Get("SyncWifiOnly", false);

        var interval = Preferences.Get("SyncInterval", 15);
        IntervalPicker.SelectedIndex = interval switch
        {
            5 => 0,
            15 => 1,
            30 => 2,
            60 => 3,
            _ => 4
        };

        var strategy = Preferences.Get("ConflictStrategy", "ServerWins");
        ServerWinsRadio.IsChecked = strategy == "ServerWins";
        ClientWinsRadio.IsChecked = strategy == "ClientWins";
        LastWriteWinsRadio.IsChecked = strategy == "LastWriteWins";
        AskMeRadio.IsChecked = strategy == "Manual";
    }

    private void RefreshStatus()
    {
        var isOnline = _offlineSyncService?.IsOnline ?? Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

        StatusIndicator.BackgroundColor = isOnline ? Color.FromArgb("#4CAF50") : Color.FromArgb("#F44336");
        StatusLabel.Text = isOnline ? "Online" : "Offline";
        StatusLabel.TextColor = isOnline ? Color.FromArgb("#4CAF50") : Color.FromArgb("#F44336");

        if (_syncService != null)
        {
            var stats = _syncService.GetStatistics();
            LastSyncLabel.Text = stats.LastSyncDisplay;
            PendingChangesLabel.Text = stats.PendingChanges.ToString();
            TotalSyncsLabel.Text = stats.TotalSyncs.ToString();
            LastDurationLabel.Text = stats.TotalSyncs > 0 ? stats.DurationDisplay : "-";
        }
        else
        {
            LastSyncLabel.Text = "Never";
            PendingChangesLabel.Text = _offlineSyncService?.PendingCount.ToString() ?? "0";
            TotalSyncsLabel.Text = "0";
            LastDurationLabel.Text = "-";
        }
    }

    private void OnOfflineStatusChanged(object? sender, SyncStatusChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(RefreshStatus);
    }

    private void OnSyncStarted(object? sender, SyncEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            SyncOverlay.IsVisible = true;
            SyncStatusText.Text = $"Starting {e.SyncType} sync...";
            SyncProgressText.Text = "Preparing...";
            SyncProgressBar.Progress = 0;
        });
    }

    private void OnSyncCompleted(object? sender, SyncEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            SyncProgressBar.Progress = 1;
            SyncStatusText.Text = e.Success ? "Sync Complete!" : "Sync Failed";
            await Task.Delay(1000);
            SyncOverlay.IsVisible = false;
            RefreshStatus();

            if (e.Success)
            {
                try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            }
        });
    }

    private void OnSyncProgress(object? sender, SyncProgressEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var progress = e.Total > 0 ? (double)e.Current / e.Total : 0;
            SyncProgressBar.Progress = progress;
            SyncProgressText.Text = $"{e.Current} / {e.Total} items";
            SyncStatusText.Text = $"Syncing {e.CurrentItem?.EntityType ?? "data"}...";
        });
    }

    private async void OnSyncNowClicked(object sender, EventArgs e)
    {
        if (_syncService == null)
        {
            await DisplayAlertAsync("Not Available", "Sync service is not configured.", "OK");
            return;
        }

        if (_syncService.IsSyncing)
        {
            await DisplayAlertAsync("In Progress", "A sync is already in progress.", "OK");
            return;
        }

        var strategy = GetSelectedStrategy();
        var result = await _syncService.DeltaSyncAsync(strategy);

        if (!result.Success)
        {
            await DisplayAlertAsync("Sync Result", result.Message, "OK");
        }
    }

    private async void OnFullSyncClicked(object sender, EventArgs e)
    {
        if (_syncService == null)
        {
            await DisplayAlertAsync("Not Available", "Sync service is not configured.", "OK");
            return;
        }

        var confirm = await DisplayAlertAsync("Full Sync", "This will sync all data, which may take longer. Continue?", "Yes", "Cancel");
        if (!confirm) return;

        var strategy = GetSelectedStrategy();
        var result = await _syncService.FullSyncAsync(strategy);

        if (!result.Success)
        {
            await DisplayAlertAsync("Sync Result", result.Message, "OK");
        }
    }

    private async void OnDeltaSyncClicked(object sender, EventArgs e)
    {
        if (_syncService == null)
        {
            await DisplayAlertAsync("Not Available", "Sync service is not configured.", "OK");
            return;
        }

        var strategy = GetSelectedStrategy();
        var result = await _syncService.DeltaSyncAsync(strategy);
        await DisplayAlertAsync("Sync Result", result.Message, "OK");
    }

    private void OnCancelSyncClicked(object sender, EventArgs e)
    {
        _syncService?.CancelSync();
        SyncOverlay.IsVisible = false;
    }

    private void OnAutoSyncToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Set("AutoSyncEnabled", e.Value);
    }

    private void OnWifiOnlyToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Set("SyncWifiOnly", e.Value);
    }

    private void OnIntervalChanged(object sender, EventArgs e)
    {
        var interval = IntervalPicker.SelectedIndex switch
        {
            0 => 5,
            1 => 15,
            2 => 30,
            3 => 60,
            _ => 0
        };
        Preferences.Set("SyncInterval", interval);
    }

    private async void OnViewPendingClicked(object sender, EventArgs e)
    {
        var pending = _offlineSyncService?.GetPendingItems();

        if (pending == null || !pending.Any())
        {
            await DisplayAlertAsync("Pending Changes", "No pending changes to sync.", "OK");
            return;
        }

        var summary = string.Join("\n", pending.Take(10).Select(p =>
            $"• {p.Operation} {p.EntityType} #{p.EntityId}"));

        if (pending.Count > 10)
        {
            summary += $"\n... and {pending.Count - 10} more";
        }

        await DisplayAlertAsync($"Pending Changes ({pending.Count})", summary, "OK");
    }

    private async void OnViewLogClicked(object sender, EventArgs e)
    {
        if (_syncService == null)
        {
            await DisplayAlertAsync("Sync Log", "No sync history available.", "OK");
            return;
        }

        var stats = _syncService.GetStatistics();
        var log = $"Total Syncs: {stats.TotalSyncs}\n" +
                  $"Last Sync: {stats.LastSyncDisplay}\n" +
                  $"Last Duration: {stats.DurationDisplay}\n" +
                  $"Conflicts Resolved: {stats.ConflictsResolved}\n" +
                  $"Failed Syncs: {stats.FailedSyncs}";

        await DisplayAlertAsync("Sync Log", log, "OK");
    }

    private async void OnClearQueueClicked(object sender, EventArgs e)
    {
        var confirm = await DisplayAlertAsync("Clear Queue", "This will discard all pending changes. They will not be synced. Continue?", "Clear", "Cancel");
        if (!confirm) return;

        if (_offlineSyncService != null)
        {
            await _offlineSyncService.ClearQueueAsync();
        }

        RefreshStatus();
        await DisplayAlertAsync("Cleared", "Sync queue has been cleared.", "OK");
    }

    private ConflictResolutionStrategy GetSelectedStrategy()
    {
        if (ClientWinsRadio.IsChecked) return ConflictResolutionStrategy.ClientWins;
        if (LastWriteWinsRadio.IsChecked) return ConflictResolutionStrategy.LastWriteWins;
        if (AskMeRadio.IsChecked) return ConflictResolutionStrategy.Manual;
        return ConflictResolutionStrategy.ServerWins;
    }
}
