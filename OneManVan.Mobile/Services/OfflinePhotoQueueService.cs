using System.Text.Json;

namespace OneManVan.Mobile.Services;

/// <summary>
/// Manages offline photo queue with auto-retry for syncing photos when connection is available.
/// </summary>
public class OfflinePhotoQueueService
{
    private readonly string _queuePath;
    private readonly string _photosPath;
    private const string QueueFileName = "photo_queue.json";
    private List<QueuedPhoto> _queue = [];
    private bool _isSyncing;
    private CancellationTokenSource? _syncCts;

    public event EventHandler<PhotoSyncProgressEventArgs>? SyncProgressChanged;
    public event EventHandler<PhotoSyncCompletedEventArgs>? SyncCompleted;

    public int PendingCount => _queue.Count(p => p.Status == QueueStatus.Pending);
    public int FailedCount => _queue.Count(p => p.Status == QueueStatus.Failed);
    public bool IsSyncing => _isSyncing;

    public OfflinePhotoQueueService()
    {
        _queuePath = Path.Combine(FileSystem.AppDataDirectory, "Queue");
        _photosPath = Path.Combine(FileSystem.AppDataDirectory, "Photos");
        
        Directory.CreateDirectory(_queuePath);
        Directory.CreateDirectory(_photosPath);
        
        LoadQueue();
    }

    #region Queue Management

    /// <summary>
    /// Adds a photo to the offline queue.
    /// </summary>
    public async Task<string> QueuePhotoAsync(
        int jobId,
        string photoType,
        Stream photoStream,
        double? latitude = null,
        double? longitude = null)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var fileName = $"job_{jobId}_{timestamp}_{photoType}.jpg";
        var filePath = Path.Combine(_photosPath, fileName);

        // Save photo locally
        using (var fileStream = File.Create(filePath))
        {
            await photoStream.CopyToAsync(fileStream);
        }

        // Add to queue
        var queuedPhoto = new QueuedPhoto
        {
            Id = Guid.NewGuid().ToString(),
            JobId = jobId,
            PhotoType = photoType,
            LocalPath = filePath,
            FileName = fileName,
            QueuedAt = DateTime.Now,
            Latitude = latitude,
            Longitude = longitude,
            Status = QueueStatus.Pending,
            RetryCount = 0
        };

        _queue.Add(queuedPhoto);
        await SaveQueueAsync();

        return filePath;
    }

    /// <summary>
    /// Gets all queued photos for a job.
    /// </summary>
    public List<QueuedPhoto> GetPhotosForJob(int jobId)
    {
        return _queue.Where(p => p.JobId == jobId).OrderByDescending(p => p.QueuedAt).ToList();
    }

    /// <summary>
    /// Gets all pending photos in the queue.
    /// </summary>
    public List<QueuedPhoto> GetPendingPhotos()
    {
        return _queue.Where(p => p.Status == QueueStatus.Pending || p.Status == QueueStatus.Failed)
                     .OrderBy(p => p.QueuedAt)
                     .ToList();
    }

    /// <summary>
    /// Marks a photo as synced.
    /// </summary>
    public async Task MarkAsSyncedAsync(string photoId, string? remoteUrl = null)
    {
        var photo = _queue.FirstOrDefault(p => p.Id == photoId);
        if (photo != null)
        {
            photo.Status = QueueStatus.Synced;
            photo.SyncedAt = DateTime.Now;
            photo.RemoteUrl = remoteUrl;
            await SaveQueueAsync();
        }
    }

    /// <summary>
    /// Marks a photo as failed with error message.
    /// </summary>
    public async Task MarkAsFailedAsync(string photoId, string errorMessage)
    {
        var photo = _queue.FirstOrDefault(p => p.Id == photoId);
        if (photo != null)
        {
            photo.Status = QueueStatus.Failed;
            photo.LastError = errorMessage;
            photo.RetryCount++;
            photo.LastAttemptAt = DateTime.Now;
            await SaveQueueAsync();
        }
    }

    /// <summary>
    /// Removes synced photos older than specified days.
    /// </summary>
    public async Task CleanupOldSyncedPhotosAsync(int olderThanDays = 7)
    {
        var cutoff = DateTime.Now.AddDays(-olderThanDays);
        var toRemove = _queue.Where(p => 
            p.Status == QueueStatus.Synced && 
            p.SyncedAt.HasValue && 
            p.SyncedAt.Value < cutoff).ToList();

        foreach (var photo in toRemove)
        {
            _queue.Remove(photo);
            // Optionally delete local file if synced
            // File.Delete(photo.LocalPath);
        }

        await SaveQueueAsync();
    }

    /// <summary>
    /// Retries a failed photo.
    /// </summary>
    public async Task RetryPhotoAsync(string photoId)
    {
        var photo = _queue.FirstOrDefault(p => p.Id == photoId);
        if (photo != null)
        {
            photo.Status = QueueStatus.Pending;
            photo.LastError = null;
            await SaveQueueAsync();
        }
    }

    /// <summary>
    /// Retries all failed photos.
    /// </summary>
    public async Task RetryAllFailedAsync()
    {
        foreach (var photo in _queue.Where(p => p.Status == QueueStatus.Failed))
        {
            photo.Status = QueueStatus.Pending;
            photo.LastError = null;
        }
        await SaveQueueAsync();
    }

    #endregion

    #region Auto-Sync

    /// <summary>
    /// Starts the auto-sync process.
    /// </summary>
    public async Task StartAutoSyncAsync(Func<QueuedPhoto, Task<bool>> uploadFunc)
    {
        if (_isSyncing) return;

        _isSyncing = true;
        _syncCts = new CancellationTokenSource();

        var pending = GetPendingPhotos();
        var total = pending.Count;
        var synced = 0;
        var failed = 0;

        try
        {
            foreach (var photo in pending)
            {
                if (_syncCts.Token.IsCancellationRequested) break;

                try
                {
                    // Check if file exists
                    if (!File.Exists(photo.LocalPath))
                    {
                        await MarkAsFailedAsync(photo.Id, "File not found");
                        failed++;
                        continue;
                    }

                    // Attempt upload
                    var success = await uploadFunc(photo);
                    
                    if (success)
                    {
                        await MarkAsSyncedAsync(photo.Id);
                        synced++;
                    }
                    else
                    {
                        await MarkAsFailedAsync(photo.Id, "Upload failed");
                        failed++;
                    }
                }
                catch (Exception ex)
                {
                    await MarkAsFailedAsync(photo.Id, ex.Message);
                    failed++;
                }

                SyncProgressChanged?.Invoke(this, new PhotoSyncProgressEventArgs
                {
                    Current = synced + failed,
                    Total = total,
                    CurrentPhoto = photo.FileName
                });
            }
        }
        finally
        {
            _isSyncing = false;
            _syncCts?.Dispose();
            _syncCts = null;

            SyncCompleted?.Invoke(this, new PhotoSyncCompletedEventArgs
            {
                SyncedCount = synced,
                FailedCount = failed,
                TotalProcessed = synced + failed
            });
        }
    }

    /// <summary>
    /// Stops the auto-sync process.
    /// </summary>
    public void StopAutoSync()
    {
        _syncCts?.Cancel();
    }

    #endregion

    #region Persistence

    private void LoadQueue()
    {
        try
        {
            var queueFile = Path.Combine(_queuePath, QueueFileName);
            if (File.Exists(queueFile))
            {
                var json = File.ReadAllText(queueFile);
                _queue = JsonSerializer.Deserialize<List<QueuedPhoto>>(json) ?? [];
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load photo queue: {ex.Message}");
            _queue = [];
        }
    }

    private async Task SaveQueueAsync()
    {
        try
        {
            var queueFile = Path.Combine(_queuePath, QueueFileName);
            var json = JsonSerializer.Serialize(_queue, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(queueFile, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save photo queue: {ex.Message}");
        }
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Gets queue statistics.
    /// </summary>
    public QueueStatistics GetStatistics()
    {
        return new QueueStatistics
        {
            TotalPhotos = _queue.Count,
            PendingPhotos = _queue.Count(p => p.Status == QueueStatus.Pending),
            SyncedPhotos = _queue.Count(p => p.Status == QueueStatus.Synced),
            FailedPhotos = _queue.Count(p => p.Status == QueueStatus.Failed),
            TotalSizeBytes = _queue.Where(p => File.Exists(p.LocalPath))
                                   .Sum(p => new FileInfo(p.LocalPath).Length)
        };
    }

    #endregion
}

#region Models

public class QueuedPhoto
{
    public string Id { get; set; } = "";
    public int JobId { get; set; }
    public string PhotoType { get; set; } = "Photo";
    public string LocalPath { get; set; } = "";
    public string FileName { get; set; } = "";
    public string? RemoteUrl { get; set; }
    public DateTime QueuedAt { get; set; }
    public DateTime? SyncedAt { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public QueueStatus Status { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
}

public enum QueueStatus
{
    Pending,
    Syncing,
    Synced,
    Failed
}

public class QueueStatistics
{
    public int TotalPhotos { get; set; }
    public int PendingPhotos { get; set; }
    public int SyncedPhotos { get; set; }
    public int FailedPhotos { get; set; }
    public long TotalSizeBytes { get; set; }
    
    public string TotalSizeDisplay => TotalSizeBytes switch
    {
        < 1024 => $"{TotalSizeBytes} B",
        < 1024 * 1024 => $"{TotalSizeBytes / 1024.0:F1} KB",
        _ => $"{TotalSizeBytes / (1024.0 * 1024.0):F1} MB"
    };
}

public class PhotoSyncProgressEventArgs : EventArgs
{
    public int Current { get; set; }
    public int Total { get; set; }
    public string? CurrentPhoto { get; set; }
    public double Progress => Total > 0 ? (double)Current / Total : 0;
}

public class PhotoSyncCompletedEventArgs : EventArgs
{
    public int SyncedCount { get; set; }
    public int FailedCount { get; set; }
    public int TotalProcessed { get; set; }
}

#endregion
