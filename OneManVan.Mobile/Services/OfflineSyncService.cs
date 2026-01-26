using System.Text.Json;

namespace OneManVan.Mobile.Services;

/// <summary>
/// Service for managing offline sync queue.
/// Queues operations when offline and syncs when connectivity is restored.
/// </summary>
public class OfflineSyncService
{
    private readonly string _queueFilePath;
    private readonly List<SyncQueueItem> _pendingItems = [];
    private bool _isOnline = true;

    public event EventHandler<SyncStatusChangedEventArgs>? StatusChanged;
    public event EventHandler<SyncProgressEventArgs>? SyncProgress;

    public bool IsOnline => _isOnline;
    public int PendingCount => _pendingItems.Count;
    public bool HasPendingItems => _pendingItems.Count > 0;

    public OfflineSyncService()
    {
        _queueFilePath = Path.Combine(FileSystem.AppDataDirectory, "sync_queue.json");
        LoadQueue();
        
        // Monitor connectivity
        Connectivity.ConnectivityChanged += OnConnectivityChanged;
        _isOnline = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        var wasOnline = _isOnline;
        _isOnline = e.NetworkAccess == NetworkAccess.Internet;

        if (_isOnline && !wasOnline && HasPendingItems)
        {
            // Connection restored - trigger sync
            StatusChanged?.Invoke(this, new SyncStatusChangedEventArgs
            {
                IsOnline = true,
                Message = $"Connection restored. {PendingCount} items pending sync.",
                PendingCount = PendingCount
            });
        }
        else if (!_isOnline && wasOnline)
        {
            StatusChanged?.Invoke(this, new SyncStatusChangedEventArgs
            {
                IsOnline = false,
                Message = "Working offline. Changes will sync when connected.",
                PendingCount = PendingCount
            });
        }
    }

    /// <summary>
    /// Queues an operation for sync.
    /// </summary>
    public async Task QueueOperationAsync(SyncOperation operation, string entityType, int entityId, object? data = null)
    {
        var item = new SyncQueueItem
        {
            Id = Guid.NewGuid().ToString(),
            Operation = operation,
            EntityType = entityType,
            EntityId = entityId,
            Data = data != null ? JsonSerializer.Serialize(data) : null,
            QueuedAt = DateTime.UtcNow,
            RetryCount = 0
        };

        _pendingItems.Add(item);
        await SaveQueueAsync();

        StatusChanged?.Invoke(this, new SyncStatusChangedEventArgs
        {
            IsOnline = _isOnline,
            Message = $"Operation queued. {PendingCount} items pending.",
            PendingCount = PendingCount
        });
    }

    /// <summary>
    /// Processes the sync queue.
    /// </summary>
    public async Task ProcessQueueAsync(Func<SyncQueueItem, Task<bool>> syncHandler)
    {
        if (!_isOnline || !HasPendingItems) return;

        var processed = 0;
        var failed = 0;
        var itemsToProcess = _pendingItems.ToList();

        foreach (var item in itemsToProcess)
        {
            try
            {
                SyncProgress?.Invoke(this, new SyncProgressEventArgs
                {
                    Current = processed + 1,
                    Total = itemsToProcess.Count,
                    CurrentItem = item
                });

                var success = await syncHandler(item);

                if (success)
                {
                    _pendingItems.Remove(item);
                    processed++;
                }
                else
                {
                    item.RetryCount++;
                    item.LastError = "Sync handler returned false";
                    failed++;
                }
            }
            catch (Exception ex)
            {
                item.RetryCount++;
                item.LastError = ex.Message;
                failed++;

                // Remove items that have failed too many times
                if (item.RetryCount >= 3)
                {
                    _pendingItems.Remove(item);
                    // Log permanent failure
                }
            }
        }

        await SaveQueueAsync();

        StatusChanged?.Invoke(this, new SyncStatusChangedEventArgs
        {
            IsOnline = _isOnline,
            Message = failed > 0 
                ? $"Sync completed. {processed} synced, {failed} failed."
                : $"Sync completed. {processed} items synced.",
            PendingCount = PendingCount
        });
    }

    /// <summary>
    /// Clears all pending items from the queue.
    /// </summary>
    public async Task ClearQueueAsync()
    {
        _pendingItems.Clear();
        await SaveQueueAsync();

        StatusChanged?.Invoke(this, new SyncStatusChangedEventArgs
        {
            IsOnline = _isOnline,
            Message = "Sync queue cleared.",
            PendingCount = 0
        });
    }

    /// <summary>
    /// Gets all pending items.
    /// </summary>
    public IReadOnlyList<SyncQueueItem> GetPendingItems() => _pendingItems.AsReadOnly();

    private void LoadQueue()
    {
        try
        {
            if (File.Exists(_queueFilePath))
            {
                var json = File.ReadAllText(_queueFilePath);
                var items = JsonSerializer.Deserialize<List<SyncQueueItem>>(json);
                if (items != null)
                {
                    _pendingItems.AddRange(items);
                }
            }
        }
        catch
        {
            // Ignore load errors - start with empty queue
        }
    }

    private async Task SaveQueueAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_pendingItems);
            await File.WriteAllTextAsync(_queueFilePath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }
}

/// <summary>
/// Types of sync operations.
/// </summary>
public enum SyncOperation
{
    Create,
    Update,
    Delete,
    Upload
}

/// <summary>
/// Item in the sync queue.
/// </summary>
public class SyncQueueItem
{
    public string Id { get; set; } = "";
    public SyncOperation Operation { get; set; }
    public string EntityType { get; set; } = "";
    public int EntityId { get; set; }
    public string? Data { get; set; }
    public DateTime QueuedAt { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
}

/// <summary>
/// Event args for sync status changes.
/// </summary>
public class SyncStatusChangedEventArgs : EventArgs
{
    public bool IsOnline { get; set; }
    public string Message { get; set; } = "";
    public int PendingCount { get; set; }
}

/// <summary>
/// Event args for sync progress.
/// </summary>
public class SyncProgressEventArgs : EventArgs
{
    public int Current { get; set; }
    public int Total { get; set; }
    public SyncQueueItem? CurrentItem { get; set; }
}
