using Microsoft.Maui.Controls.Shapes;
using OneManVan.Mobile.Services;

namespace OneManVan.Mobile.Controls;

/// <summary>
/// Control that displays sync status and pending item count.
/// Shows online/offline state with visual indicator.
/// </summary>
public class SyncStatusIndicator : ContentView
{
    private readonly Label _iconLabel;
    private readonly Label _textLabel;
    private readonly Label _countLabel;
    private readonly Border _countBadge;
    private OfflineSyncService? _syncService;

    public static readonly BindableProperty IsOnlineProperty =
        BindableProperty.Create(nameof(IsOnline), typeof(bool), typeof(SyncStatusIndicator), true,
            propertyChanged: OnIsOnlineChanged);

    public static readonly BindableProperty PendingCountProperty =
        BindableProperty.Create(nameof(PendingCount), typeof(int), typeof(SyncStatusIndicator), 0,
            propertyChanged: OnPendingCountChanged);

    public static readonly BindableProperty StatusTextProperty =
        BindableProperty.Create(nameof(StatusText), typeof(string), typeof(SyncStatusIndicator), "Online",
            propertyChanged: OnStatusTextChanged);

    public bool IsOnline
    {
        get => (bool)GetValue(IsOnlineProperty);
        set => SetValue(IsOnlineProperty, value);
    }

    public int PendingCount
    {
        get => (int)GetValue(PendingCountProperty);
        set => SetValue(PendingCountProperty, value);
    }

    public string StatusText
    {
        get => (string)GetValue(StatusTextProperty);
        set => SetValue(StatusTextProperty, value);
    }

    public SyncStatusIndicator()
    {
        _iconLabel = new Label
        {
            FontSize = 14,
            VerticalOptions = LayoutOptions.Center
        };

        _textLabel = new Label
        {
            FontSize = 12,
            VerticalOptions = LayoutOptions.Center,
            Margin = new Thickness(5, 0, 0, 0)
        };

        _countLabel = new Label
        {
            FontSize = 10,
            TextColor = Colors.White,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center
        };

        _countBadge = new Border
        {
            BackgroundColor = Color.FromArgb("#F44336"),
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Stroke = Colors.Transparent,
            Padding = new Thickness(5, 2),
            IsVisible = false,
            Margin = new Thickness(5, 0, 0, 0),
            Content = _countLabel
        };

        var layout = new HorizontalStackLayout
        {
            Spacing = 0,
            Children = { _iconLabel, _textLabel, _countBadge }
        };

        Content = layout;
        UpdateVisual();
    }

    /// <summary>
    /// Binds to an OfflineSyncService for automatic updates.
    /// </summary>
    public void BindToSyncService(OfflineSyncService syncService)
    {
        if (_syncService != null)
        {
            _syncService.StatusChanged -= OnSyncStatusChanged;
        }

        _syncService = syncService;
        _syncService.StatusChanged += OnSyncStatusChanged;

        // Initial state
        IsOnline = _syncService.IsOnline;
        PendingCount = _syncService.PendingCount;
    }

    private void OnSyncStatusChanged(object? sender, SyncStatusChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsOnline = e.IsOnline;
            PendingCount = e.PendingCount;
            StatusText = e.Message;
        });
    }

    private static void OnIsOnlineChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is SyncStatusIndicator indicator)
        {
            indicator.UpdateVisual();
        }
    }

    private static void OnPendingCountChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is SyncStatusIndicator indicator)
        {
            indicator.UpdateVisual();
        }
    }

    private static void OnStatusTextChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is SyncStatusIndicator indicator)
        {
            indicator._textLabel.Text = newValue as string ?? "";
        }
    }

    private void UpdateVisual()
    {
        if (IsOnline)
        {
            _iconLabel.Text = "?";
            _textLabel.Text = PendingCount > 0 ? $"Online ({PendingCount} pending)" : "Online";
            _textLabel.TextColor = Color.FromArgb("#4CAF50");
        }
        else
        {
            _iconLabel.Text = "?";
            _textLabel.Text = PendingCount > 0 ? $"Offline ({PendingCount} pending)" : "Offline";
            _textLabel.TextColor = Color.FromArgb("#F44336");
        }

        _countBadge.IsVisible = PendingCount > 0;
        _countLabel.Text = PendingCount.ToString();
    }
}

/// <summary>
/// A simple banner that shows when offline with pending items.
/// </summary>
public class OfflineBanner : ContentView
{
    private readonly Label _messageLabel;
    private readonly Button _syncButton;
    private OfflineSyncService? _syncService;

    public event EventHandler? SyncRequested;

    public OfflineBanner()
    {
        _messageLabel = new Label
        {
            Text = "Working offline",
            TextColor = Colors.White,
            FontSize = 12,
            VerticalOptions = LayoutOptions.Center
        };

        _syncButton = new Button
        {
            Text = "Sync Now",
            BackgroundColor = Colors.Transparent,
            TextColor = Colors.White,
            FontSize = 12,
            HeightRequest = 30,
            Padding = new Thickness(10, 0),
            VerticalOptions = LayoutOptions.Center
        };
        _syncButton.Clicked += (s, e) => SyncRequested?.Invoke(this, EventArgs.Empty);

        var layout = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            Padding = new Thickness(15, 8),
            BackgroundColor = Color.FromArgb("#FF9800")
        };

        layout.Add(_messageLabel, 0, 0);
        layout.Add(_syncButton, 1, 0);

        Content = layout;
        IsVisible = false;
    }

    /// <summary>
    /// Binds to an OfflineSyncService for automatic updates.
    /// </summary>
    public void BindToSyncService(OfflineSyncService syncService)
    {
        if (_syncService != null)
        {
            _syncService.StatusChanged -= OnSyncStatusChanged;
        }

        _syncService = syncService;
        _syncService.StatusChanged += OnSyncStatusChanged;

        UpdateBanner();
    }

    private void OnSyncStatusChanged(object? sender, SyncStatusChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(UpdateBanner);
    }

    private void UpdateBanner()
    {
        if (_syncService == null) return;

        var showBanner = !_syncService.IsOnline || _syncService.HasPendingItems;
        IsVisible = showBanner;

        if (_syncService.IsOnline && _syncService.HasPendingItems)
        {
            BackgroundColor = Color.FromArgb("#4CAF50");
            _messageLabel.Text = $"{_syncService.PendingCount} items ready to sync";
            _syncButton.IsVisible = true;
        }
        else if (!_syncService.IsOnline)
        {
            BackgroundColor = Color.FromArgb("#FF9800");
            _messageLabel.Text = _syncService.HasPendingItems
                ? $"Offline - {_syncService.PendingCount} items pending"
                : "Working offline";
            _syncButton.IsVisible = false;
        }
    }
}
