using System.Windows;
using System.Windows.Controls;

namespace OneManVan.Controls;

/// <summary>
/// Dashboard widget showing assets that need online registration for warranty.
/// </summary>
public partial class UnregisteredAssetsWidget : UserControl
{
    public static readonly DependencyProperty UnregisteredCountProperty =
        DependencyProperty.Register(nameof(UnregisteredCount), typeof(int), typeof(UnregisteredAssetsWidget),
            new PropertyMetadata(0, OnUnregisteredCountChanged));

    public static readonly DependencyProperty ShowDetailsProperty =
        DependencyProperty.Register(nameof(ShowDetails), typeof(bool), typeof(UnregisteredAssetsWidget),
            new PropertyMetadata(false));

    public int UnregisteredCount
    {
        get => (int)GetValue(UnregisteredCountProperty);
        set => SetValue(UnregisteredCountProperty, value);
    }

    public bool ShowDetails
    {
        get => (bool)GetValue(ShowDetailsProperty);
        set => SetValue(ShowDetailsProperty, value);
    }

    public event EventHandler? ViewAllClicked;
    public event EventHandler? RegisterNowClicked;

    public UnregisteredAssetsWidget()
    {
        InitializeComponent();
    }

    private static void OnUnregisteredCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UnregisteredAssetsWidget widget)
        {
            var count = (int)e.NewValue;
            widget.UpdateVisibility(count);
        }
    }

    private void UpdateVisibility(int count)
    {
        // Only show widget if there are unregistered assets
        Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnViewAllClick(object sender, RoutedEventArgs e)
    {
        ViewAllClicked?.Invoke(this, EventArgs.Empty);
    }

    private void OnRegisterNowClick(object sender, RoutedEventArgs e)
    {
        RegisterNowClicked?.Invoke(this, EventArgs.Empty);
    }
}
