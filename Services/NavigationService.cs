using System.Windows.Controls;

namespace OneManVan.Services;

/// <summary>
/// Simple navigation service for switching between pages.
/// </summary>
public class NavigationService
{
    private readonly ContentControl _contentHost;
    private readonly Dictionary<string, Func<UserControl>> _pageFactories = [];

    public event EventHandler<string>? Navigated;

    public string CurrentPage { get; private set; } = string.Empty;

    public NavigationService(ContentControl contentHost)
    {
        _contentHost = contentHost;
    }

    public void RegisterPage(string pageName, Func<UserControl> factory)
    {
        _pageFactories[pageName] = factory;
    }

    public void NavigateTo(string pageName)
    {
        if (_pageFactories.TryGetValue(pageName, out var factory))
        {
            var page = factory();
            _contentHost.Content = page;
            CurrentPage = pageName;
            Navigated?.Invoke(this, pageName);
        }
    }

    public void NavigateToHome()
    {
        NavigateTo("Home");
    }
}
