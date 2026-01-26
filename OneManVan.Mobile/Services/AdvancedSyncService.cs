using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;

namespace OneManVan.Mobile.Services;

/// <summary>
/// Advanced sync service with conflict resolution, delta sync, and background processing.
/// </summary>
public class AdvancedSyncService
{
    private readonly OneManVanDbContext _dbContext;
    private readonly OfflineSyncService _offlineSyncService;
    private readonly string _syncMetadataPath;
    private SyncMetadata _metadata;
    private bool _isSyncing;
    private CancellationTokenSource? _syncCancellation;

    public event EventHandler<SyncEventArgs>? SyncStarted;
    public event EventHandler<SyncEventArgs>? SyncCompleted;
    public event EventHandler<SyncProgressEventArgs>? SyncProgress;
    public event EventHandler<SyncConflictEventArgs>? ConflictDetected;
    public event EventHandler<SyncErrorEventArgs>? SyncError;

    public bool IsSyncing => _isSyncing;
    public bool IsOnline => _offlineSyncService.IsOnline;
    public DateTime? LastSyncTime => _metadata.LastSyncTime;
    public int PendingChanges => _offlineSyncService.PendingCount;

    public AdvancedSyncService(OneManVanDbContext dbContext, OfflineSyncService offlineSyncService)
    {
        _dbContext = dbContext;
        _offlineSyncService = offlineSyncService;
        _syncMetadataPath = Path.Combine(FileSystem.AppDataDirectory, "sync_metadata.json");
        _metadata = LoadMetadata();
    }

    /// <summary>
    /// Performs a full sync with the remote database.
    /// </summary>
    public async Task<SyncResult> FullSyncAsync(ConflictResolutionStrategy strategy = ConflictResolutionStrategy.ServerWins)
    {
        if (_isSyncing)
        {
            return new SyncResult { Success = false, Message = "Sync already in progress" };
        }

        _isSyncing = true;
        _syncCancellation = new CancellationTokenSource();
        var result = new SyncResult();
        var startTime = DateTime.UtcNow;

        try
        {
            SyncStarted?.Invoke(this, new SyncEventArgs { SyncType = "Full" });

            // Step 1: Push local changes
            var pushResult = await PushLocalChangesAsync(strategy, _syncCancellation.Token);
            result.ItemsPushed = pushResult.ItemsProcessed;
            result.PushErrors = pushResult.Errors;

            // Step 2: Pull remote changes
            var pullResult = await PullRemoteChangesAsync(_syncCancellation.Token);
            result.ItemsPulled = pullResult.ItemsProcessed;
            result.PullErrors = pullResult.Errors;

            // Step 3: Resolve any remaining conflicts
            result.ConflictsResolved = await ResolveConflictsAsync(strategy, _syncCancellation.Token);

            // Update metadata
            _metadata.LastSyncTime = DateTime.UtcNow;
            _metadata.LastSyncDuration = DateTime.UtcNow - startTime;
            _metadata.TotalSyncs++;
            await SaveMetadataAsync();

            result.Success = result.PushErrors.Count == 0 && result.PullErrors.Count == 0;
            result.Message = result.Success
                ? $"Sync completed. Pushed {result.ItemsPushed}, pulled {result.ItemsPulled}."
                : $"Sync completed with errors. {result.PushErrors.Count + result.PullErrors.Count} errors.";

            SyncCompleted?.Invoke(this, new SyncEventArgs
            {
                SyncType = "Full",
                Success = result.Success,
                ItemsProcessed = result.ItemsPushed + result.ItemsPulled
            });
        }
        catch (OperationCanceledException)
        {
            result.Success = false;
            result.Message = "Sync was cancelled";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Sync failed: {ex.Message}";
            SyncError?.Invoke(this, new SyncErrorEventArgs { Error = ex, Context = "FullSync" });
        }
        finally
        {
            _isSyncing = false;
            _syncCancellation?.Dispose();
            _syncCancellation = null;
        }

        return result;
    }

    /// <summary>
    /// Performs an incremental (delta) sync since last sync time.
    /// </summary>
    public async Task<SyncResult> DeltaSyncAsync(ConflictResolutionStrategy strategy = ConflictResolutionStrategy.ServerWins)
    {
        if (!_metadata.LastSyncTime.HasValue)
        {
            // First time sync - do full sync
            return await FullSyncAsync(strategy);
        }

        if (_isSyncing)
        {
            return new SyncResult { Success = false, Message = "Sync already in progress" };
        }

        _isSyncing = true;
        _syncCancellation = new CancellationTokenSource();
        var result = new SyncResult();

        try
        {
            SyncStarted?.Invoke(this, new SyncEventArgs { SyncType = "Delta" });

            // Only sync items modified since last sync
            var since = _metadata.LastSyncTime.Value;

            // Push local changes made since last sync
            var pushResult = await PushChangessSinceAsync(since, strategy, _syncCancellation.Token);
            result.ItemsPushed = pushResult.ItemsProcessed;

            // Pull remote changes since last sync
            var pullResult = await PullChangesSinceAsync(since, _syncCancellation.Token);
            result.ItemsPulled = pullResult.ItemsProcessed;

            _metadata.LastSyncTime = DateTime.UtcNow;
            await SaveMetadataAsync();

            result.Success = true;
            result.Message = $"Delta sync completed. Pushed {result.ItemsPushed}, pulled {result.ItemsPulled}.";

            SyncCompleted?.Invoke(this, new SyncEventArgs
            {
                SyncType = "Delta",
                Success = true,
                ItemsProcessed = result.ItemsPushed + result.ItemsPulled
            });
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Delta sync failed: {ex.Message}";
            SyncError?.Invoke(this, new SyncErrorEventArgs { Error = ex, Context = "DeltaSync" });
        }
        finally
        {
            _isSyncing = false;
        }

        return result;
    }

    /// <summary>
    /// Cancels any ongoing sync operation.
    /// </summary>
    public void CancelSync()
    {
        _syncCancellation?.Cancel();
    }

    /// <summary>
    /// Queues a local change for sync.
    /// </summary>
    public async Task QueueChangeAsync<T>(T entity, SyncOperation operation) where T : class
    {
        var entityType = typeof(T).Name;
        var entityId = GetEntityId(entity);

        // Track the change locally
        var change = new TrackedChange
        {
            EntityType = entityType,
            EntityId = entityId,
            Operation = operation,
            ChangedAt = DateTime.UtcNow,
            Data = JsonSerializer.Serialize(entity)
        };

        _metadata.PendingChanges.Add(change);
        await SaveMetadataAsync();

        // Also queue in offline service
        await _offlineSyncService.QueueOperationAsync(operation, entityType, entityId, entity);
    }

    /// <summary>
    /// Gets sync statistics.
    /// </summary>
    public SyncStatistics GetStatistics()
    {
        return new SyncStatistics
        {
            LastSyncTime = _metadata.LastSyncTime,
            TotalSyncs = _metadata.TotalSyncs,
            PendingChanges = _metadata.PendingChanges.Count,
            LastSyncDuration = _metadata.LastSyncDuration,
            ConflictsResolved = _metadata.ConflictsResolved,
            FailedSyncs = _metadata.FailedSyncs
        };
    }

    #region Private Methods

    private async Task<SyncOperationResult> PushLocalChangesAsync(ConflictResolutionStrategy strategy, CancellationToken ct)
    {
        var result = new SyncOperationResult();

        await _offlineSyncService.ProcessQueueAsync(async item =>
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                // Simulate push to remote (in real app, this would be HTTP API call)
                // For local-only mode, we just clear the queue
                result.ItemsProcessed++;

                SyncProgress?.Invoke(this, new SyncProgressEventArgs
                {
                    Current = result.ItemsProcessed,
                    Total = _offlineSyncService.PendingCount,
                    CurrentItem = item
                });

                return true;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"{item.EntityType} {item.EntityId}: {ex.Message}");
                return false;
            }
        });

        return result;
    }

    private async Task<SyncOperationResult> PullRemoteChangesAsync(CancellationToken ct)
    {
        var result = new SyncOperationResult();

        // In a real implementation, this would pull from remote API
        // For local-only mode, nothing to pull
        await Task.Delay(100, ct); // Simulate network delay

        return result;
    }

    private async Task<SyncOperationResult> PushChangessSinceAsync(DateTime since, ConflictResolutionStrategy strategy, CancellationToken ct)
    {
        var result = new SyncOperationResult();

        // Get entities modified since last sync using CreatedAt as the timestamp
        var modifiedCustomers = await _dbContext.Customers
            .Where(c => c.CreatedAt > since)
            .ToListAsync(ct);

        result.ItemsProcessed += modifiedCustomers.Count;

        var modifiedAssets = await _dbContext.Assets
            .Where(a => a.CreatedAt > since || (a.InstallDate.HasValue && a.InstallDate > since))
            .ToListAsync(ct);

        result.ItemsProcessed += modifiedAssets.Count;

        return result;
    }

    private async Task<SyncOperationResult> PullChangesSinceAsync(DateTime since, CancellationToken ct)
    {
        var result = new SyncOperationResult();

        // In a real implementation, this would pull from remote API
        await Task.Delay(50, ct);

        return result;
    }

    private async Task<int> ResolveConflictsAsync(ConflictResolutionStrategy strategy, CancellationToken ct)
    {
        var conflictsResolved = 0;

        foreach (var conflict in _metadata.Conflicts.ToList())
        {
            ct.ThrowIfCancellationRequested();

            var resolved = strategy switch
            {
                ConflictResolutionStrategy.ServerWins => await ResolveServerWinsAsync(conflict),
                ConflictResolutionStrategy.ClientWins => await ResolveClientWinsAsync(conflict),
                ConflictResolutionStrategy.Manual => await ResolveManualAsync(conflict),
                ConflictResolutionStrategy.MergeChanges => await ResolveMergeAsync(conflict),
                _ => false
            };

            if (resolved)
            {
                _metadata.Conflicts.Remove(conflict);
                conflictsResolved++;
            }
        }

        _metadata.ConflictsResolved += conflictsResolved;
        return conflictsResolved;
    }

    private Task<bool> ResolveServerWinsAsync(SyncConflict conflict)
    {
        // Server version takes precedence
        // In real implementation, apply server data to local
        return Task.FromResult(true);
    }

    private Task<bool> ResolveClientWinsAsync(SyncConflict conflict)
    {
        // Client version takes precedence
        // In real implementation, push client data to server
        return Task.FromResult(true);
    }

    private async Task<bool> ResolveManualAsync(SyncConflict conflict)
    {
        // Notify user for manual resolution
        var args = new SyncConflictEventArgs { Conflict = conflict };
        ConflictDetected?.Invoke(this, args);

        // Wait for user decision (would be async in real implementation)
        await Task.Delay(100);

        return args.Resolution != ConflictResolution.Pending;
    }

    private Task<bool> ResolveMergeAsync(SyncConflict conflict)
    {
        // Attempt to merge non-conflicting fields
        // In real implementation, compare field by field
        return Task.FromResult(true);
    }

    private int GetEntityId<T>(T entity)
    {
        var idProperty = typeof(T).GetProperty("Id");
        if (idProperty != null)
        {
            var value = idProperty.GetValue(entity);
            if (value is int intValue)
                return intValue;
        }
        return 0;
    }

    private SyncMetadata LoadMetadata()
    {
        try
        {
            if (File.Exists(_syncMetadataPath))
            {
                var json = File.ReadAllText(_syncMetadataPath);
                return JsonSerializer.Deserialize<SyncMetadata>(json) ?? new SyncMetadata();
            }
        }
        catch { }

        return new SyncMetadata();
    }

    private async Task SaveMetadataAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_metadata);
            await File.WriteAllTextAsync(_syncMetadataPath, json);
        }
        catch { }
    }

    #endregion
}

#region Supporting Types

/// <summary>
/// Conflict resolution strategies.
/// </summary>
public enum ConflictResolutionStrategy
{
    ServerWins,
    ClientWins,
    Manual,
    MergeChanges,
    LastWriteWins
}

/// <summary>
/// Conflict resolution outcomes.
/// </summary>
public enum ConflictResolution
{
    Pending,
    UseServer,
    UseClient,
    Merged,
    Skipped
}

/// <summary>
/// Result of a sync operation.
/// </summary>
public class SyncResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public int ItemsPushed { get; set; }
    public int ItemsPulled { get; set; }
    public int ConflictsResolved { get; set; }
    public List<string> PushErrors { get; set; } = [];
    public List<string> PullErrors { get; set; } = [];
}

/// <summary>
/// Internal sync operation result.
/// </summary>
public class SyncOperationResult
{
    public int ItemsProcessed { get; set; }
    public List<string> Errors { get; set; } = [];
}

/// <summary>
/// Sync event arguments.
/// </summary>
public class SyncEventArgs : EventArgs
{
    public string SyncType { get; set; } = "";
    public bool Success { get; set; }
    public int ItemsProcessed { get; set; }
}

/// <summary>
/// Sync error event arguments.
/// </summary>
public class SyncErrorEventArgs : EventArgs
{
    public Exception? Error { get; set; }
    public string Context { get; set; } = "";
}

/// <summary>
/// Sync conflict event arguments.
/// </summary>
public class SyncConflictEventArgs : EventArgs
{
    public SyncConflict? Conflict { get; set; }
    public ConflictResolution Resolution { get; set; } = ConflictResolution.Pending;
}

/// <summary>
/// Represents a sync conflict.
/// </summary>
public class SyncConflict
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EntityType { get; set; } = "";
    public int EntityId { get; set; }
    public string? LocalData { get; set; }
    public string? ServerData { get; set; }
    public DateTime LocalModifiedAt { get; set; }
    public DateTime ServerModifiedAt { get; set; }
    public List<string> ConflictingFields { get; set; } = [];
}

/// <summary>
/// Tracked change for sync.
/// </summary>
public class TrackedChange
{
    public string EntityType { get; set; } = "";
    public int EntityId { get; set; }
    public SyncOperation Operation { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Data { get; set; }
}

/// <summary>
/// Sync metadata for persistence.
/// </summary>
public class SyncMetadata
{
    public DateTime? LastSyncTime { get; set; }
    public TimeSpan LastSyncDuration { get; set; }
    public int TotalSyncs { get; set; }
    public int ConflictsResolved { get; set; }
    public int FailedSyncs { get; set; }
    public List<TrackedChange> PendingChanges { get; set; } = [];
    public List<SyncConflict> Conflicts { get; set; } = [];
}

/// <summary>
/// Sync statistics for display.
/// </summary>
public class SyncStatistics
{
    public DateTime? LastSyncTime { get; set; }
    public TimeSpan LastSyncDuration { get; set; }
    public int TotalSyncs { get; set; }
    public int PendingChanges { get; set; }
    public int ConflictsResolved { get; set; }
    public int FailedSyncs { get; set; }

    public string LastSyncDisplay => LastSyncTime.HasValue
        ? LastSyncTime.Value.ToLocalTime().ToString("MMM d, h:mm tt")
        : "Never";

    public string DurationDisplay => LastSyncDuration.TotalSeconds < 60
        ? $"{LastSyncDuration.TotalSeconds:N1}s"
        : $"{LastSyncDuration.TotalMinutes:N1}m";
}

#endregion
