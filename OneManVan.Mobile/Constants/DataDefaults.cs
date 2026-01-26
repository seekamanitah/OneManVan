namespace OneManVan.Mobile.Constants;

/// <summary>
/// Database and data-related constants.
/// </summary>
public static class DataDefaults
{
    // Database retry settings
    public const int MaxRetryAttempts = 3;
    public const int RetryDelayMilliseconds = 1000;
    
    // Query timeouts
    public const int DefaultQueryTimeoutSeconds = 30;
    public const int LongRunningQueryTimeoutSeconds = 60;
    
    // Batch sizes
    public const int DefaultBatchSize = 50;
    public const int MaxBatchSize = 500;
    
    // Sync settings
    public const int SyncIntervalMinutes = 15;
    public const int SyncRetryDelayMinutes = 5;
    public const int MaxSyncFailures = 3;
    
    // Cache settings
    public const int CacheDurationMinutes = 60;
    public const int CustomerCacheDurationMinutes = 30;
}
