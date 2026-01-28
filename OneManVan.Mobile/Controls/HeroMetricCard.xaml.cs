namespace OneManVan.Mobile.Controls;

/// <summary>
/// Hero metric card for dashboard showing key business metrics.
/// </summary>
public partial class HeroMetricCard : ContentView
{
    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(nameof(Value), typeof(string), typeof(HeroMetricCard), "0",
            propertyChanged: OnValueChanged);

    public static readonly BindableProperty LabelProperty =
        BindableProperty.Create(nameof(Label), typeof(string), typeof(HeroMetricCard), "Metric",
            propertyChanged: OnLabelChanged);

    public static readonly BindableProperty TrendProperty =
        BindableProperty.Create(nameof(Trend), typeof(string), typeof(HeroMetricCard), null,
            propertyChanged: OnTrendChanged);

    public static readonly BindableProperty TrendColorProperty =
        BindableProperty.Create(nameof(TrendColor), typeof(Color), typeof(HeroMetricCard), Colors.Green,
            propertyChanged: OnTrendColorChanged);

    public static readonly BindableProperty ValueColorProperty =
        BindableProperty.Create(nameof(ValueColor), typeof(Color), typeof(HeroMetricCard), 
            Color.FromArgb("#2196F3"),
            propertyChanged: OnValueColorChanged);

    public HeroMetricCard()
    {
        InitializeComponent();
    }

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Trend
    {
        get => (string)GetValue(TrendProperty);
        set => SetValue(TrendProperty, value);
    }

    public Color TrendColor
    {
        get => (Color)GetValue(TrendColorProperty);
        set => SetValue(TrendColorProperty, value);
    }

    public Color ValueColor
    {
        get => (Color)GetValue(ValueColorProperty);
        set => SetValue(ValueColorProperty, value);
    }

    private static void OnValueChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (HeroMetricCard)bindable;
        control.ValueLabel.Text = newValue?.ToString() ?? "0";
    }

    private static void OnLabelChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (HeroMetricCard)bindable;
        control.LabelText.Text = newValue?.ToString() ?? "Metric";
    }

    private static void OnTrendChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (HeroMetricCard)bindable;
        var trend = newValue?.ToString();
        
        if (string.IsNullOrWhiteSpace(trend))
        {
            control.TrendContainer.IsVisible = false;
        }
        else
        {
            control.TrendContainer.IsVisible = true;
            control.TrendLabel.Text = trend;
        }
    }

    private static void OnTrendColorChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (HeroMetricCard)bindable;
        control.TrendLabel.TextColor = (Color)newValue;
    }

    private static void OnValueColorChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (HeroMetricCard)bindable;
        control.ValueLabel.TextColor = (Color)newValue;
    }

    /// <summary>
    /// Animate value change with smooth counter animation.
    /// </summary>
    public async Task AnimateValueAsync(string newValue)
    {
        // Simple fade animation for now
        await ValueLabel.FadeTo(0.3, 150);
        Value = newValue;
        await ValueLabel.FadeTo(1, 150);
    }
}
