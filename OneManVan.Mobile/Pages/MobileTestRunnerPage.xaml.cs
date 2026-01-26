using OneManVan.Mobile.Services;
using OneManVan.Shared.Data;

namespace OneManVan.Mobile.Pages;

public partial class MobileTestRunnerPage : ContentPage
{
    private readonly MobileTestRunnerService _testRunner;

    public MobileTestRunnerPage(OneManVanDbContext db)
    {
        InitializeComponent();
        _testRunner = new MobileTestRunnerService(db);
    }

    private async void OnRunTestsClicked(object sender, EventArgs e)
    {
        try
        {
            // Show loading state
            RunButton.IsEnabled = false;
            RunButton.Text = "Running...";
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            StatusLabel.Text = "Running tests...";
            ReportEditor.Text = "";

            // Run tests
            var result = await _testRunner.RunAllTestsAsync();

            // Update UI with results
            PassedLabel.Text = result.PassCount.ToString();
            FailedLabel.Text = result.FailCount.ToString();
            PassRateLabel.Text = $"{result.PassRate:F0}%";
            
            // Color code pass rate
            PassRateLabel.TextColor = result.PassRate >= 90 
                ? Color.FromArgb("#4CAF50") 
                : result.PassRate >= 70 
                    ? Color.FromArgb("#FF9800")
                    : Color.FromArgb("#F44336");

            ReportEditor.Text = result.Report;

            StatusLabel.Text = result.AllPassed 
                ? "All tests passed!" 
                : $"{result.FailCount} test(s) failed";
            StatusLabel.TextColor = result.AllPassed 
                ? Color.FromArgb("#4CAF50") 
                : Color.FromArgb("#F44336");

            // Haptic feedback
            try
            {
                if (result.AllPassed)
                    HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                else
                    HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
            }
            catch { }
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.Message}";
            StatusLabel.TextColor = Color.FromArgb("#F44336");
            ReportEditor.Text = $"Test execution failed:\n\n{ex}";
        }
        finally
        {
            RunButton.IsEnabled = true;
            RunButton.Text = "Run All Tests";
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }
}
