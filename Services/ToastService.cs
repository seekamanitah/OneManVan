using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace OneManVan.Services;

/// <summary>
/// Service for displaying toast notifications in the application.
/// </summary>
public class ToastService
{
    private static Panel? _container;
    private static readonly Queue<ToastMessage> _queue = new();
    private static bool _isShowing;

    /// <summary>
    /// Initializes the toast service with a container panel.
    /// Call this in MainShell after InitializeComponent.
    /// </summary>
    public static void Initialize(Panel container)
    {
        _container = container;
    }

    /// <summary>
    /// Shows a success toast notification.
    /// </summary>
    public static void Success(string message, int durationMs = 3000)
    {
        Show(new ToastMessage(message, ToastType.Success, durationMs));
    }

    /// <summary>
    /// Shows an error toast notification.
    /// </summary>
    public static void Error(string message, int durationMs = 4000)
    {
        Show(new ToastMessage(message, ToastType.Error, durationMs));
    }

    /// <summary>
    /// Shows a warning toast notification.
    /// </summary>
    public static void Warning(string message, int durationMs = 3500)
    {
        Show(new ToastMessage(message, ToastType.Warning, durationMs));
    }

    /// <summary>
    /// Shows an info toast notification.
    /// </summary>
    public static void Info(string message, int durationMs = 3000)
    {
        Show(new ToastMessage(message, ToastType.Info, durationMs));
    }

    private static void Show(ToastMessage toast)
    {
        _queue.Enqueue(toast);
        ProcessQueue();
    }

    private static void ProcessQueue()
    {
        if (_isShowing || _queue.Count == 0 || _container == null)
            return;

        _isShowing = true;
        var toast = _queue.Dequeue();

        Application.Current.Dispatcher.Invoke(() =>
        {
            var toastElement = CreateToastElement(toast);
            _container.Children.Add(toastElement);

            // Animate in
            var slideIn = new DoubleAnimation
            {
                From = 50,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(200)
            };

            toastElement.RenderTransform = new TranslateTransform();
            toastElement.RenderTransform.BeginAnimation(TranslateTransform.YProperty, slideIn);
            toastElement.BeginAnimation(UIElement.OpacityProperty, fadeIn);

            // Auto-dismiss
            var dismissTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(toast.DurationMs)
            };

            dismissTimer.Tick += (s, e) =>
            {
                dismissTimer.Stop();
                DismissToast(toastElement);
            };

            dismissTimer.Start();
        });
    }

    private static void DismissToast(Border toastElement)
    {
        var fadeOut = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(200)
        };

        fadeOut.Completed += (s, e) =>
        {
            _container?.Children.Remove(toastElement);
            _isShowing = false;
            ProcessQueue();
        };

        toastElement.BeginAnimation(UIElement.OpacityProperty, fadeOut);
    }

    private static Border CreateToastElement(ToastMessage toast)
    {
        var (bgColor, icon) = toast.Type switch
        {
            ToastType.Success => ("#a6e3a1", "?"),
            ToastType.Error => ("#f38ba8", "?"),
            ToastType.Warning => ("#f9e2af", "?"),
            ToastType.Info => ("#89b4fa", "?"),
            _ => ("#89b4fa", "?")
        };

        var border = new Border
        {
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgColor)!),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16, 12, 16, 12),
            Margin = new Thickness(0, 0, 0, 10),
            HorizontalAlignment = HorizontalAlignment.Right,
            MaxWidth = 400,
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 10,
                ShadowDepth = 2,
                Opacity = 0.3,
                Color = Colors.Black
            }
        };

        var stack = new StackPanel { Orientation = Orientation.Horizontal };

        var iconText = new TextBlock
        {
            Text = icon,
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e1e2e")!),
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        var messageText = new TextBlock
        {
            Text = toast.Message,
            FontSize = 13,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e1e2e")!),
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        };

        stack.Children.Add(iconText);
        stack.Children.Add(messageText);
        border.Child = stack;

        return border;
    }
}

/// <summary>
/// Types of toast notifications.
/// </summary>
public enum ToastType
{
    Success,
    Error,
    Warning,
    Info
}

/// <summary>
/// Toast message data.
/// </summary>
public record ToastMessage(string Message, ToastType Type, int DurationMs);
