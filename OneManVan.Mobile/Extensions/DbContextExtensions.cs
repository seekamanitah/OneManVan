using Microsoft.EntityFrameworkCore;

namespace OneManVan.Mobile.Extensions;

/// <summary>
/// Extension methods for database operations with retry logic
/// </summary>
public static class DbContextExtensions
{
    /// <summary>
    /// Saves changes with automatic retry for transient failures
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="maxRetries">Maximum number of retry attempts (default: 3)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of entities saved</returns>
    public static async Task<int> SaveChangesWithRetryAsync(
        this DbContext context,
        int maxRetries = 3,
        CancellationToken cancellationToken = default)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (attempt < maxRetries && IsTransientError(ex))
            {
                // Exponential backoff: 1s, 2s, 4s
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                System.Diagnostics.Debug.WriteLine(
                    $"SaveChanges attempt {attempt} failed (transient error), retrying in {delay.TotalSeconds}s...");
                
                await Task.Delay(delay, cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex) when (attempt < maxRetries)
            {
                // Handle concurrency conflicts
                System.Diagnostics.Debug.WriteLine(
                    $"SaveChanges attempt {attempt} failed (concurrency conflict), retrying...");
                
                // Reload entities from database
                foreach (var entry in context.ChangeTracker.Entries())
                {
                    await entry.ReloadAsync(cancellationToken);
                }
                
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        // Final attempt without catch
        return await context.SaveChangesAsync(cancellationToken);
    }

    private static bool IsTransientError(DbUpdateException exception)
    {
        // Check for common transient error indicators
        var message = exception.InnerException?.Message ?? exception.Message;
        
        return message.Contains("database is locked", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("deadlock", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("connection", StringComparison.OrdinalIgnoreCase);
    }
}
