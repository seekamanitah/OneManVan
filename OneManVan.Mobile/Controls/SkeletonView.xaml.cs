namespace OneManVan.Mobile.Controls;

/// <summary>
/// Loading skeleton placeholder for list items.
/// Shows animated placeholders while data is loading.
/// </summary>
public partial class SkeletonView : ContentView
{
    private bool _isAnimating;

    public SkeletonView()
    {
        InitializeComponent();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        
        if (Handler != null)
        {
            StartPulseAnimation();
        }
        else
        {
            StopPulseAnimation();
        }
    }

    private async void StartPulseAnimation()
    {
        _isAnimating = true;
        
        while (_isAnimating && Handler != null)
        {
            await this.FadeToAsync(0.5, 800, Easing.CubicInOut);
            await this.FadeToAsync(1.0, 800, Easing.CubicInOut);
        }
    }

    private void StopPulseAnimation()
    {
        _isAnimating = false;
    }
}
