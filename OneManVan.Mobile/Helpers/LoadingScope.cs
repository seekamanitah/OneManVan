namespace OneManVan.Mobile.Helpers;

/// <summary>
/// IDisposable helper for managing loading indicators in a clean, exception-safe way.
/// Usage: using (new LoadingScope(LoadingIndicator)) { await LoadAsync(); }
/// </summary>
public class LoadingScope : IDisposable
{
    private readonly ActivityIndicator? _indicator;
    private readonly VisualElement? _overlay;
    
    /// <summary>
    /// Create a loading scope with an ActivityIndicator
    /// </summary>
    public LoadingScope(ActivityIndicator indicator)
    {
        _indicator = indicator ?? throw new ArgumentNullException(nameof(indicator));
        _indicator.IsVisible = true;
        _indicator.IsRunning = true;
    }
    
    /// <summary>
    /// Create a loading scope with a loading overlay element
    /// </summary>
    public LoadingScope(VisualElement overlay)
    {
        _overlay = overlay ?? throw new ArgumentNullException(nameof(overlay));
        _overlay.IsVisible = true;
    }
    
    /// <summary>
    /// Create a loading scope with both indicator and overlay
    /// </summary>
    public LoadingScope(ActivityIndicator indicator, VisualElement overlay)
    {
        _indicator = indicator ?? throw new ArgumentNullException(nameof(indicator));
        _overlay = overlay;
        
        _indicator.IsVisible = true;
        _indicator.IsRunning = true;
        
        if (_overlay != null)
        {
            _overlay.IsVisible = true;
        }
    }

    public void Dispose()
    {
        if (_indicator != null)
        {
            _indicator.IsVisible = false;
            _indicator.IsRunning = false;
        }
        
        if (_overlay != null)
        {
            _overlay.IsVisible = false;
        }
    }
}
