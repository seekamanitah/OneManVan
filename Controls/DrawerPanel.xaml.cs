using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace OneManVan.Controls;

/// <summary>
/// Responsive slide-in drawer panel for desktop WPF app.
/// Adapts to window size and provides smooth animations.
/// </summary>
public partial class DrawerPanel : UserControl
{
    private const double AnimationDuration = 0.3; // seconds
    private TaskCompletionSource<bool>? _closeTaskSource;
    
    public event EventHandler? SaveClicked;
    public event EventHandler? CancelClicked;
    public event EventHandler? Closed;

    public DrawerPanel()
    {
        InitializeComponent();
        SizeChanged += OnSizeChanged;
    }

    #region Properties

    public string Title
    {
        get => HeaderTitle.Text;
        set => HeaderTitle.Text = value;
    }

    public string SaveButtonText
    {
        get => SaveButton.Content?.ToString() ?? "Save";
        set => SaveButton.Content = value;
    }

    public string CancelButtonText
    {
        get => CancelButton.Content?.ToString() ?? "Cancel";
        set => CancelButton.Content = value;
    }

    public UIElement? Content
    {
        get => DrawerContent.Content as UIElement;
        set => DrawerContent.Content = value;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Opens the drawer with slide-in animation
    /// </summary>
    public async Task OpenAsync()
    {
        _closeTaskSource = new TaskCompletionSource<bool>();
        
        // Make visible
        Visibility = Visibility.Visible;
        
        // Adjust drawer width based on window size
        UpdateDrawerWidth();
        
        // Animate overlay fade in
        var overlayAnimation = new DoubleAnimation
        {
            From = 0,
            To = 0.5,
            Duration = TimeSpan.FromSeconds(AnimationDuration),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        
        // Animate drawer slide in
        var drawerTransform = DrawerBorder.RenderTransform as TranslateTransform;
        if (drawerTransform == null)
        {
            drawerTransform = new TranslateTransform();
            DrawerBorder.RenderTransform = drawerTransform;
        }
        
        var drawerAnimation = new DoubleAnimation
        {
            From = DrawerBorder.ActualWidth > 0 ? DrawerBorder.ActualWidth : 500,
            To = 0,
            Duration = TimeSpan.FromSeconds(AnimationDuration),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        
        Overlay.BeginAnimation(OpacityProperty, overlayAnimation);
        drawerTransform.BeginAnimation(TranslateTransform.XProperty, drawerAnimation);
        
        await Task.Delay(TimeSpan.FromSeconds(AnimationDuration));
    }

    /// <summary>
    /// Closes the drawer with slide-out animation
    /// </summary>
    public async Task<bool> CloseAsync(bool result = false)
    {
        // Animate overlay fade out
        var overlayAnimation = new DoubleAnimation
        {
            From = 0.5,
            To = 0,
            Duration = TimeSpan.FromSeconds(AnimationDuration),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        
        // Animate drawer slide out
        var drawerTransform = DrawerBorder.RenderTransform as TranslateTransform;
        if (drawerTransform == null)
        {
            drawerTransform = new TranslateTransform();
            DrawerBorder.RenderTransform = drawerTransform;
        }
        
        var drawerAnimation = new DoubleAnimation
        {
            From = 0,
            To = DrawerBorder.ActualWidth > 0 ? DrawerBorder.ActualWidth : 500,
            Duration = TimeSpan.FromSeconds(AnimationDuration),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        
        Overlay.BeginAnimation(OpacityProperty, overlayAnimation);
        drawerTransform.BeginAnimation(TranslateTransform.XProperty, drawerAnimation);
        
        await Task.Delay(TimeSpan.FromSeconds(AnimationDuration));
        
        // Hide after animation
        Visibility = Visibility.Collapsed;
        
        _closeTaskSource?.TrySetResult(result);
        Closed?.Invoke(this, EventArgs.Empty);
        
        return result;
    }

    /// <summary>
    /// Call this after successful save to close the drawer
    /// </summary>
    public async Task CompleteSaveAsync()
    {
        await CloseAsync(true);
    }

    /// <summary>
    /// Waits for the drawer to close and returns the result
    /// </summary>
    public Task<bool> WaitForResultAsync()
    {
        return _closeTaskSource?.Task ?? Task.FromResult(false);
    }

    #endregion

    #region Event Handlers

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateDrawerWidth();
    }

    private void UpdateDrawerWidth()
    {
        // Responsive drawer width based on window size
        var window = Window.GetWindow(this);
        if (window == null) return;
        
        var windowWidth = window.ActualWidth;
        
        // Use 40% of window width, minimum 400px, maximum 600px
        var drawerWidth = Math.Max(400, Math.Min(600, windowWidth * 0.4));
        
        // But never more than 90% of window width
        drawerWidth = Math.Min(drawerWidth, windowWidth * 0.9);
        
        DrawerBorder.Width = drawerWidth;
        DrawerBorder.MaxWidth = windowWidth * 0.9;
    }

    private async void OnOverlayClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        await CloseAsync(false);
    }

    private async void OnCloseClick(object sender, RoutedEventArgs e)
    {
        await CloseAsync(false);
    }

    private async void OnCancelClick(object sender, RoutedEventArgs e)
    {
        CancelClicked?.Invoke(this, EventArgs.Empty);
        await CloseAsync(false);
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        SaveClicked?.Invoke(this, EventArgs.Empty);
        // Don't auto-close - let parent handle validation and closing
    }

    #endregion
}
