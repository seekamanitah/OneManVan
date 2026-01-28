using OneManVan.Shared.Services;

namespace OneManVan.Mobile.Pages;

public partial class SetupWizardPage : ContentPage
{
    private readonly TradeConfigurationService _tradeService;
    private TradeInfo? _selectedTrade;

    public SetupWizardPage(TradeConfigurationService tradeService)
    {
        InitializeComponent();
        _tradeService = tradeService;
        LoadTrades();
    }

    private void LoadTrades()
    {
        var trades = _tradeService.GetAvailableTrades().ToList();
        TradeCollection.ItemsSource = trades;
    }

    private void OnTradeSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is TradeInfo trade)
        {
            _selectedTrade = trade;
            
            // Update preview
            var preset = _tradeService.GetDefaultPreset(trade.Type);
            
            AssetLabelPreview.Text = preset.AssetPluralLabel;
            FieldCountPreview.Text = $"{preset.CustomFields.Count} fields";
            TemplateCountPreview.Text = $"{preset.EstimateTemplates.Count} templates";
            
            PreviewFrame.IsVisible = true;
            ContinueButton.IsEnabled = true;
            
            // Update button color based on trade
            ContinueButton.BackgroundColor = Color.FromArgb(preset.PrimaryColor);
            ContinueButton.TextColor = Colors.White;
        }
    }

    private async void OnContinueClicked(object sender, EventArgs e)
    {
        if (_selectedTrade == null)
        {
            await DisplayAlertAsync("Select Trade", "Please select a trade to continue.", "OK");
            return;
        }

        try
        {
            ContinueButton.IsEnabled = false;
            ContinueButton.Text = "Setting up...";

            // Configure the trade
            await _tradeService.SetTradeAsync(_selectedTrade.Type);
            
            // Mark setup complete
            _tradeService.CompleteSetup();

            // Haptic feedback
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

            // Navigate to main app using the new Windows API
            if (Application.Current?.Windows.Count > 0)
            {
                Application.Current.Windows[0].Page = new AppShell();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Setup failed: {ex.Message}", "OK");
            ContinueButton.IsEnabled = true;
            ContinueButton.Text = "Get Started";
        }
    }
}
