using System.Windows.Input;

namespace OneManVan.Mobile.Controls;

/// <summary>
/// Reusable empty state view component for list pages.
/// Shows a friendly message with an optional action button.
/// </summary>
public partial class EmptyStateView : ContentView
{
    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(string), typeof(EmptyStateView), "");

    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(EmptyStateView), "No items yet");

    public static readonly BindableProperty MessageProperty =
        BindableProperty.Create(nameof(Message), typeof(string), typeof(EmptyStateView), "Get started by adding your first item");

    public static readonly BindableProperty ButtonTextProperty =
        BindableProperty.Create(nameof(ButtonText), typeof(string), typeof(EmptyStateView), "Get Started");

    public static readonly BindableProperty ButtonCommandProperty =
        BindableProperty.Create(nameof(ButtonCommand), typeof(ICommand), typeof(EmptyStateView));

    public static readonly BindableProperty ShowButtonProperty =
        BindableProperty.Create(nameof(ShowButton), typeof(bool), typeof(EmptyStateView), true);

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public string ButtonText
    {
        get => (string)GetValue(ButtonTextProperty);
        set => SetValue(ButtonTextProperty, value);
    }

    public ICommand ButtonCommand
    {
        get => (ICommand)GetValue(ButtonCommandProperty);
        set => SetValue(ButtonCommandProperty, value);
    }

    public bool ShowButton
    {
        get => (bool)GetValue(ShowButtonProperty);
        set => SetValue(ShowButtonProperty, value);
    }

    public EmptyStateView()
    {
        InitializeComponent();
    }
}
