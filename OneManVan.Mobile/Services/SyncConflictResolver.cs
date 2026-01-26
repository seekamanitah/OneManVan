using System.Reflection;
using System.Text.Json;

namespace OneManVan.Mobile.Services;

/// <summary>
/// Resolves data conflicts between local and server versions.
/// </summary>
public class SyncConflictResolver
{
    /// <summary>
    /// Event raised when user intervention is needed.
    /// </summary>
    public event EventHandler<ConflictResolutionRequestEventArgs>? ResolutionRequested;

    /// <summary>
    /// Compares two versions and identifies conflicting fields.
    /// </summary>
    public ConflictAnalysis AnalyzeConflict<T>(T localVersion, T serverVersion, DateTime localModified, DateTime serverModified)
        where T : class
    {
        var analysis = new ConflictAnalysis
        {
            EntityType = typeof(T).Name,
            LocalModifiedAt = localModified,
            ServerModifiedAt = serverModified
        };

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && !IsIgnoredProperty(p));

        foreach (var prop in properties)
        {
            var localValue = prop.GetValue(localVersion);
            var serverValue = prop.GetValue(serverVersion);

            if (!AreValuesEqual(localValue, serverValue))
            {
                analysis.ConflictingFields.Add(new FieldConflict
                {
                    FieldName = prop.Name,
                    LocalValue = FormatValue(localValue),
                    ServerValue = FormatValue(serverValue),
                    FieldType = prop.PropertyType.Name
                });
            }
        }

        analysis.HasConflicts = analysis.ConflictingFields.Count > 0;
        return analysis;
    }

    /// <summary>
    /// Resolves a conflict using the specified strategy.
    /// </summary>
    public async Task<T?> ResolveAsync<T>(
        T localVersion,
        T serverVersion,
        ConflictAnalysis analysis,
        ConflictResolutionStrategy strategy) where T : class, new()
    {
        return strategy switch
        {
            ConflictResolutionStrategy.ServerWins => serverVersion,
            ConflictResolutionStrategy.ClientWins => localVersion,
            ConflictResolutionStrategy.LastWriteWins => ResolveByTimestamp(localVersion, serverVersion, analysis),
            ConflictResolutionStrategy.MergeChanges => MergeVersions(localVersion, serverVersion, analysis),
            ConflictResolutionStrategy.Manual => await ResolveManuallyAsync(localVersion, serverVersion, analysis),
            _ => serverVersion
        };
    }

    /// <summary>
    /// Merges two versions, keeping non-conflicting changes from both.
    /// </summary>
    public T MergeVersions<T>(T localVersion, T serverVersion, ConflictAnalysis analysis) where T : class, new()
    {
        var merged = new T();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite);

        foreach (var prop in properties)
        {
            var localValue = prop.GetValue(localVersion);
            var serverValue = prop.GetValue(serverVersion);

            var conflict = analysis.ConflictingFields.FirstOrDefault(c => c.FieldName == prop.Name);

            if (conflict != null)
            {
                // For conflicts, use the most recent based on resolution preference
                // Default to server for conflicts when merging
                var valueToUse = conflict.Resolution switch
                {
                    FieldResolution.UseLocal => localValue,
                    FieldResolution.UseServer => serverValue,
                    FieldResolution.Custom => conflict.ResolvedValue,
                    _ => serverValue // Default to server
                };
                prop.SetValue(merged, valueToUse);
            }
            else
            {
                // No conflict - use local if changed from original, else server
                prop.SetValue(merged, localValue ?? serverValue);
            }
        }

        return merged;
    }

    /// <summary>
    /// Shows UI for manual conflict resolution.
    /// </summary>
    public async Task<T?> ResolveManuallyAsync<T>(T localVersion, T serverVersion, ConflictAnalysis analysis) 
        where T : class
    {
        var tcs = new TaskCompletionSource<T?>();

        var args = new ConflictResolutionRequestEventArgs
        {
            Analysis = analysis,
            LocalData = JsonSerializer.Serialize(localVersion),
            ServerData = JsonSerializer.Serialize(serverVersion)
        };

        args.ResolutionCallback = resolution =>
        {
            if (resolution == ConflictResolution.UseClient)
                tcs.TrySetResult(localVersion);
            else if (resolution == ConflictResolution.UseServer)
                tcs.TrySetResult(serverVersion);
            else
                tcs.TrySetResult(null);
        };

        ResolutionRequested?.Invoke(this, args);

        // Wait for user response with timeout
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(5)));

        if (completed == tcs.Task)
            return await tcs.Task;

        // Timeout - default to server
        return serverVersion;
    }

    /// <summary>
    /// Resolves by timestamp - most recent wins.
    /// </summary>
    private T ResolveByTimestamp<T>(T localVersion, T serverVersion, ConflictAnalysis analysis) where T : class
    {
        return analysis.LocalModifiedAt > analysis.ServerModifiedAt
            ? localVersion
            : serverVersion;
    }

    /// <summary>
    /// Checks if two values are equal.
    /// </summary>
    private bool AreValuesEqual(object? value1, object? value2)
    {
        if (value1 == null && value2 == null) return true;
        if (value1 == null || value2 == null) return false;

        // Handle collections
        if (value1 is System.Collections.IEnumerable e1 && value2 is System.Collections.IEnumerable e2)
        {
            var list1 = e1.Cast<object>().ToList();
            var list2 = e2.Cast<object>().ToList();
            return list1.SequenceEqual(list2);
        }

        return value1.Equals(value2);
    }

    /// <summary>
    /// Formats a value for display.
    /// </summary>
    private string FormatValue(object? value)
    {
        if (value == null) return "(empty)";
        if (value is DateTime dt) return dt.ToString("g");
        if (value is decimal d) return d.ToString("N2");
        if (value is bool b) return b ? "Yes" : "No";
        return value.ToString() ?? "(empty)";
    }

    /// <summary>
    /// Checks if a property should be ignored during conflict detection.
    /// </summary>
    private bool IsIgnoredProperty(PropertyInfo prop)
    {
        var ignoredNames = new[]
        {
            "Id", "CreatedAt", "UpdatedAt", "SyncStatus", "LastSyncedAt",
            "RowVersion", "ConcurrencyToken"
        };

        return ignoredNames.Contains(prop.Name) ||
               prop.PropertyType.IsClass && prop.PropertyType != typeof(string);
    }
}

/// <summary>
/// Result of conflict analysis.
/// </summary>
public class ConflictAnalysis
{
    public string EntityType { get; set; } = "";
    public DateTime LocalModifiedAt { get; set; }
    public DateTime ServerModifiedAt { get; set; }
    public bool HasConflicts { get; set; }
    public List<FieldConflict> ConflictingFields { get; set; } = [];

    public string Summary => HasConflicts
        ? $"{ConflictingFields.Count} conflicting field(s) in {EntityType}"
        : $"No conflicts in {EntityType}";
}

/// <summary>
/// Details about a single field conflict.
/// </summary>
public class FieldConflict
{
    public string FieldName { get; set; } = "";
    public string LocalValue { get; set; } = "";
    public string ServerValue { get; set; } = "";
    public string FieldType { get; set; } = "";
    public FieldResolution Resolution { get; set; } = FieldResolution.Pending;
    public object? ResolvedValue { get; set; }

    public string DisplayName => FormatFieldName(FieldName);

    private static string FormatFieldName(string name)
    {
        // Convert PascalCase to Title Case with spaces
        return string.Concat(name.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
    }
}

/// <summary>
/// Resolution for a single field.
/// </summary>
public enum FieldResolution
{
    Pending,
    UseLocal,
    UseServer,
    Custom
}

/// <summary>
/// Event args for conflict resolution request.
/// </summary>
public class ConflictResolutionRequestEventArgs : EventArgs
{
    public ConflictAnalysis? Analysis { get; set; }
    public string LocalData { get; set; } = "";
    public string ServerData { get; set; } = "";
    public Action<ConflictResolution>? ResolutionCallback { get; set; }
}
