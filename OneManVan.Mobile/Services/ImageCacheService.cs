using System.Collections.Concurrent;

namespace OneManVan.Mobile.Services;

/// <summary>
/// Service for caching image sources to improve performance and reduce disk I/O.
/// Uses weak references to allow garbage collection when memory is needed.
/// </summary>
public class ImageCacheService
{
    private readonly ConcurrentDictionary<string, WeakReference<ImageSource>> _cache = new();
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    /// <summary>
    /// Gets a cached image or loads it from disk if not cached.
    /// </summary>
    public ImageSource GetOrLoadImage(string path)
    {
        if (string.IsNullOrEmpty(path))
            return ImageSource.FromFile("placeholder.png");

        // Try to get from cache
        if (_cache.TryGetValue(path, out var weakRef) && 
            weakRef.TryGetTarget(out var cachedImage))
        {
            return cachedImage;
        }

        // Load from disk
        try
        {
            var image = File.Exists(path) 
                ? ImageSource.FromFile(path) 
                : ImageSource.FromFile("placeholder.png");

            // Cache with weak reference
            _cache[path] = new WeakReference<ImageSource>(image);
            return image;
        }
        catch
        {
            return ImageSource.FromFile("placeholder.png");
        }
    }

    /// <summary>
    /// Asynchronously loads an image with caching.
    /// </summary>
    public async Task<ImageSource> GetOrLoadImageAsync(string path)
    {
        await _loadLock.WaitAsync();
        try
        {
            return GetOrLoadImage(path);
        }
        finally
        {
            _loadLock.Release();
        }
    }

    /// <summary>
    /// Preloads multiple images in the background.
    /// </summary>
    public async Task PreloadImagesAsync(IEnumerable<string> paths)
    {
        var tasks = paths.Select(path => Task.Run(() => GetOrLoadImage(path)));
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Removes a specific image from cache.
    /// </summary>
    public void InvalidateCache(string path)
    {
        _cache.TryRemove(path, out _);
    }

    /// <summary>
    /// Clears all cached images (e.g., on low memory warning).
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
    }

    /// <summary>
    /// Gets cache statistics for monitoring.
    /// </summary>
    public (int Total, int Active) GetCacheStats()
    {
        var total = _cache.Count;
        var active = _cache.Count(kvp => kvp.Value.TryGetTarget(out _));
        return (total, active);
    }
}
