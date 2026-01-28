using Microsoft.Extensions.DependencyInjection;
using OneManVan.Mobile.Pages;
using OneManVan.Mobile.Services;
using OneManVan.Shared.Services;

namespace OneManVan.Mobile;

public partial class App : Application
{
	public App(IServiceProvider serviceProvider)
	{
		InitializeComponent();
		ServiceProvider = serviceProvider;
		
		// Apply saved UI scale (load as string to avoid platform issues)
		double uiScale = 1.0;
		if (Preferences.ContainsKey("UIScale"))
		{
			var scaleString = Preferences.Get("UIScale", "1.0");
			if (double.TryParse(scaleString, System.Globalization.NumberStyles.Float, 
				System.Globalization.CultureInfo.InvariantCulture, out var parsedScale))
			{
				uiScale = parsedScale;
			}
		}
		
		if (Resources.ContainsKey("GlobalFontScale"))
		{
			Resources["GlobalFontScale"] = uiScale;
		}
		
		System.Diagnostics.Debug.WriteLine($"App started with UI Scale: {uiScale:F2}");
	}

	public static IServiceProvider ServiceProvider { get; private set; } = null!;

	protected override Window CreateWindow(IActivationState? activationState)
	{
		// Check if setup is complete
		var tradeService = ServiceProvider.GetRequiredService<TradeConfigurationService>();
		
		if (!tradeService.IsSetupComplete)
		{
			// Show setup wizard for first run
			var setupPage = ServiceProvider.GetRequiredService<SetupWizardPage>();
			return new Window(new NavigationPage(setupPage));
		}

		return new Window(new AppShell());
	}
}