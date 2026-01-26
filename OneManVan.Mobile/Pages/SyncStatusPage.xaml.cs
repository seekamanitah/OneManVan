using OneManVan.Mobile.Helpers;
using OneManVan.Mobile.Services;

namespace OneManVan.Mobile.Pages;

/// <summary>
/// Page for viewing sync status, pending changes, and failed syncs.
/// </summary>
public partial class SyncStatusPage : ContentPage
{
    private readonly AdvancedSyncService? _syncService;
    private readonly OfflineSyncService? _offlineSyncService;

    public SyncStatusPage(AdvancedSyncService syncService, OfflineSyncService offlineSyncService)
    {
        InitializeComponent();
        _syncService = syncService;
        _offlineSyncService = offlineSyncService;

        if (_offlineSyncService != null)
            _offlineSyncService.StatusChanged += OnStatusChanged;

        if (_syncService != null)
            _syncService.SyncCompleted += OnSyncCompleted;
    }

    public SyncStatusPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshAll();
    }

    private void RefreshAll()
    {
        RefreshConnectionStatus();
        RefreshPendingList();
        RefreshFailedList();
        RefreshSyncHistory();
        RefreshStorageInfo();
    }

    private void RefreshConnectionStatus()
    {
        var isOnline = _offlineSyncService?.IsOnline ?? Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

        if (isOnline)
        {
            StatusBanner.BackgroundColor = Color.FromArgb("#4CAF50");
            ConnectionStatus.Text = "Online";
            ConnectionSubtext.Text = _offlineSyncService?.HasPendingItems == true
                ? $"{_offlineSyncService.PendingCount} items pending sync"
                : "All changes synced";
        }
        else
        {
            StatusBanner.BackgroundColor = Color.FromArgb("#FF9800");
            ConnectionStatus.Text = "Offline";
            ConnectionSubtext.Text = "Changes will sync when connected";
        }
    }

    private void RefreshPendingList()
    {
        var pending = _offlineSyncService?.GetPendingItems() ?? [];
        var displayItems = pending.Select(p => new PendingItemViewModel(p)).ToList();

        PendingList.ItemsSource = displayItems;
        PendingCount.Text = pending.Count.ToString();
        PendingSubtext.Text = pending.Count == 1 ? "1 item waiting to sync" : $"{pending.Count} items waiting to sync";
    }

    private void RefreshFailedList()
    {
        var pending = _offlineSyncService?.GetPendingItems() ?? [];
        var failed = pending.Where(p => p.RetryCount >= 2).ToList();

        if (failed.Any())
        {
            FailedSection.IsVisible = true;
            FailedList.ItemsSource = failed.Select(f => new PendingItemViewModel(f)).ToList();
            FailedCount.Text = failed.Count.ToString();
        }
        else
        {
            FailedSection.IsVisible = false;
        }
    }

    private void RefreshSyncHistory()
    {
        SyncHistoryList.Children.Clear();

        if (_syncService == null)
        {
            NoHistoryLabel.IsVisible = true;
            return;
        }

        var stats = _syncService.GetStatistics();

        if (stats.TotalSyncs == 0)
        {
            NoHistoryLabel.IsVisible = true;
            return;
        }

        NoHistoryLabel.IsVisible = false;
        AddHistoryEntry("", "Last Sync", stats.LastSyncDisplay, "#1976D2");

        if (stats.LastSyncDuration.TotalSeconds > 0)
            AddHistoryEntry("", "Duration", stats.DurationDisplay, "#757575");

        if (stats.ConflictsResolved > 0)
            AddHistoryEntry("", "Conflicts Resolved", stats.ConflictsResolved.ToString(), "#FF9800");

        if (stats.FailedSyncs > 0)
            AddHistoryEntry("", "Failed Attempts", stats.FailedSyncs.ToString(), "#F44336");

        AddHistoryEntry("", "Total Syncs", stats.TotalSyncs.ToString(), "#4CAF50");
    }

    private void AddHistoryEntry(string icon, string label, string value, string color)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            Padding = new Thickness(0, 4)
        };

        grid.Add(new Label { Text = icon, FontSize = 16, VerticalOptions = LayoutOptions.Center });
        grid.Add(new Label { Text = label, FontSize = 14, TextColor = Color.FromArgb("#333"), Margin = new Thickness(8, 0, 0, 0) }, 1);
        grid.Add(new Label { Text = value, FontSize = 14, TextColor = Color.FromArgb(color), FontAttributes = FontAttributes.Bold }, 2);

        SyncHistoryList.Children.Add(grid);
    }

    private void RefreshStorageInfo()
    {
        try
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "onemanvan.db");
            if (File.Exists(dbPath))
            {
                var dbSize = new FileInfo(dbPath).Length;
                DbSizeLabel.Text = FormatBytes(dbSize);
            }

            var photosDir = Path.Combine(FileSystem.AppDataDirectory, "Photos");
            if (Directory.Exists(photosDir))
            {
                var photoCount = Directory.GetFiles(photosDir).Length;
                PhotoCacheLabel.Text = $"{photoCount} photos";
            }

            var queuePath = Path.Combine(FileSystem.AppDataDirectory, "sync_queue.json");
            if (File.Exists(queuePath))
            {
                var queueSize = new FileInfo(queuePath).Length;
                QueueSizeLabel.Text = FormatBytes(queueSize);
            }
        }
        catch { }
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }

    private void OnStatusChanged(object? sender, SyncStatusChangedEventArgs e) =>
        MainThread.BeginInvokeOnMainThread(RefreshAll);

    private void OnSyncCompleted(object? sender, SyncEventArgs e) =>
        MainThread.BeginInvokeOnMainThread(RefreshAll);

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        if (_syncService != null && _offlineSyncService != null)
            await Navigation.PushAsync(new SyncSettingsPage(_syncService, _offlineSyncService));
        else
            await Navigation.PushAsync(new SyncSettingsPage());
    }

    private async void OnSyncNowClicked(object sender, EventArgs e)
    {
        if (_syncService == null)
        {
            await DisplayAlertAsync("Not Available", "Sync service is not configured.", "OK");
            return;
        }

        SyncNowButton.IsEnabled = false;
        SyncNowButton.Text = "Syncing...";

        try
        {
            var result = await _syncService.DeltaSyncAsync();
            await DisplayAlertAsync("Sync Complete", result.Message, "OK");
        }
        finally
        {
            SyncNowButton.IsEnabled = true;
            SyncNowButton.Text = "?? Sync Now";
            RefreshAll();
        }
    }

    private async void OnViewLogClicked(object sender, EventArgs e)
    {
        if (_syncService == null)
        {
            await DisplayAlertAsync("Sync Log", "No sync data available.", "OK");
            return;
        }

        var stats = _syncService.GetStatistics();
        var log = $"Total Syncs: {stats.TotalSyncs}\n" +
                  $"Last Sync: {stats.LastSyncDisplay}\n" +
                  $"Last Duration: {stats.DurationDisplay}\n" +
                  $"Pending Changes: {stats.PendingChanges}\n" +
                  $"Conflicts Resolved: {stats.ConflictsResolved}\n" +
                  $"Failed Syncs: {stats.FailedSyncs}";

        await DisplayAlertAsync("Sync Log", log, "OK");
    }

    private async void OnRetryItemClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is PendingItemViewModel item)
        {
            await DisplayAlertAsync("Retry", $"Retrying {item.DisplayTitle}...", "OK");
        }
    }

    private async void OnRetryAllClicked(object sender, EventArgs e)
    {
        if (_syncService == null) return;

        var result = await _syncService.FullSyncAsync();
        await DisplayAlertAsync("Retry Complete", result.Message, "OK");
        RefreshAll();
    }

    private async void OnClearQueueClicked(object sender, EventArgs e)
    {
        var confirm = await DisplayAlertAsync("Clear Queue", "This will discard all pending changes. Continue?", "Clear", "Cancel");
        if (!confirm) return;

        if (_offlineSyncService != null)
            await _offlineSyncService.ClearQueueAsync();

        RefreshAll();
    }

    private async void OnExportClicked(object sender, EventArgs e)
    {
        await DisplayAlertAsync("Export", "Export functionality coming soon.", "OK");
    }

    private async void OnForceSyncClicked(object sender, EventArgs e)
    {
        if (_syncService == null)
        {
            await DisplayAlertAsync("Not Available", "Sync service is not configured.", "OK");
            return;
        }

        var confirm = await DisplayAlertAsync("Force Full Sync", "This will sync all data. Continue?", "Yes", "Cancel");
        if (!confirm) return;

        var result = await _syncService.FullSyncAsync();
        await DisplayAlertAsync("Sync Complete", result.Message, "OK");
        RefreshAll();
    }
}

/// <summary>
/// View model for displaying pending sync items.
/// </summary>
public class PendingItemViewModel
{
    private readonly SyncQueueItem _item;

    public PendingItemViewModel(SyncQueueItem item) => _item = item;

    public string OperationIcon => _item.Operation switch
    {
        SyncOperation.Create => "+",
        SyncOperation.Update => "*",
        SyncOperation.Delete => "-",
        SyncOperation.Upload => "^",
        _ => "?"
    };

    public string DisplayTitle => $"{_item.Operation} {_item.EntityType} #{_item.EntityId}";
    public string DisplayTime => _item.QueuedAt.ToLocalTime().ToShortDateTime();
    public bool HasRetries => _item.RetryCount > 0;
    public string RetryText => _item.RetryCount > 0 ? $"Retry {_item.RetryCount}" : "";
    public string? LastError => _item.LastError;
}
