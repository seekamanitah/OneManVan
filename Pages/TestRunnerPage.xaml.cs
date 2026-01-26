using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using OneManVan.Shared.Data;
using OneManVan.Services;

namespace OneManVan.Pages;

/// <summary>
/// Test runner page for executing comprehensive tests.
/// </summary>
public partial class TestRunnerPage : UserControl
{
    private readonly OneManVanDbContext _dbContext;
    private readonly TestRunnerService _testRunner;
    private readonly TestDataSeederService _seeder;

    public TestRunnerPage(OneManVanDbContext dbContext)
    {
        InitializeComponent();
        _dbContext = dbContext;
        _testRunner = new TestRunnerService(dbContext);
        _seeder = new TestDataSeederService(dbContext);
    }

    private async void OnSeedDataClicked(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "This will add test data to the database. Continue?",
            "Seed Test Data",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            ShowLoading("Seeding test data...");
            await _seeder.SeedAllTestDataAsync();
            ResultsTextBox.Text = "? Test data seeded successfully!\n\n" +
                "Created:\n" +
                "- 10 Customers\n" +
                "- 20+ Sites\n" +
                "- 30+ Assets\n" +
                "- 50 Inventory Items\n" +
                "- 10 Estimates\n" +
                "- 15 Jobs\n" +
                "- 10 Invoices\n" +
                "- 5 Service Agreements\n" +
                "- 20 Products";
        }
        catch (Exception ex)
        {
            ResultsTextBox.Text = $"? Error seeding data: {ex.Message}";
        }
        finally
        {
            HideLoading();
        }
    }

    private async void OnClearDataClicked(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "?? WARNING: This will DELETE ALL DATA from the database!\n\n" +
            "This action cannot be undone. Are you sure?",
            "Clear All Data",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            ShowLoading("Clearing all data...");
            await _seeder.ClearAllDataAsync();
            ResultsTextBox.Text = "? All data cleared successfully!";
            UpdateCounts(0, 0);
        }
        catch (Exception ex)
        {
            ResultsTextBox.Text = $"? Error clearing data: {ex.Message}";
        }
        finally
        {
            HideLoading();
        }
    }

    private async void OnRunStage1Clicked(object sender, RoutedEventArgs e)
    {
        try
        {
            ShowLoading("Running Stage 1: Database & Models tests...");
            var report = await _testRunner.RunStage1TestsAsync();
            ResultsTextBox.Text = report;
            ParseAndUpdateCounts(report);
        }
        catch (Exception ex)
        {
            ResultsTextBox.Text = $"? Error running tests: {ex.Message}\n\n{ex.StackTrace}";
        }
        finally
        {
            HideLoading();
        }
    }

    private async void OnRunStage2Clicked(object sender, RoutedEventArgs e)
    {
        try
        {
            ShowLoading("Running Stage 2: CRUD Operations tests...");
            var report = await _testRunner.RunStage2TestsAsync();
            ResultsTextBox.Text = report;
            ParseAndUpdateCounts(report);
        }
        catch (Exception ex)
        {
            ResultsTextBox.Text = $"? Error running tests: {ex.Message}\n\n{ex.StackTrace}";
        }
        finally
        {
            HideLoading();
        }
    }

    private void OnRunStage3Clicked(object sender, RoutedEventArgs e)
    {
        try
        {
            ShowLoading("Running Stage 3: UI & Theme tests...");
            var report = _testRunner.RunStage3Tests();
            ResultsTextBox.Text = report;
            ParseAndUpdateCounts(report);
        }
        catch (Exception ex)
        {
            ResultsTextBox.Text = $"? Error running tests: {ex.Message}\n\n{ex.StackTrace}";
        }
        finally
        {
            HideLoading();
        }
    }

    private void OnRunStage4Clicked(object sender, RoutedEventArgs e)
    {
        try
        {
            ShowLoading("Running Stage 4: ViewModel tests...");
            var report = _testRunner.RunStage4Tests();
            ResultsTextBox.Text = report;
            ParseAndUpdateCounts(report);
        }
        catch (Exception ex)
        {
            ResultsTextBox.Text = $"? Error running tests: {ex.Message}\n\n{ex.StackTrace}";
        }
        finally
        {
            HideLoading();
        }
    }

    private async void OnRunAllTestsClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            ShowLoading("Running all tests...");
            var report = await _testRunner.RunAllTestsAsync();
            ResultsTextBox.Text = report;
            ParseAndUpdateCounts(report);
        }
        catch (Exception ex)
        {
            ResultsTextBox.Text = $"? Error running tests: {ex.Message}\n\n{ex.StackTrace}";
        }
        finally
        {
            HideLoading();
        }
    }

    private void OnCopyResultsClicked(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(ResultsTextBox.Text);
        MessageBox.Show("Results copied to clipboard!", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ShowLoading(string message)
    {
        LoadingText.Text = message;
        LoadingOverlay.Visibility = Visibility.Visible;
        SeedDataButton.IsEnabled = false;
        ClearDataButton.IsEnabled = false;
    }

    private void HideLoading()
    {
        LoadingOverlay.Visibility = Visibility.Collapsed;
        SeedDataButton.IsEnabled = true;
        ClearDataButton.IsEnabled = true;
    }

    private void ParseAndUpdateCounts(string report)
    {
        // Parse passed/failed counts from report
        var passMatch = Regex.Match(report, @"\*\*Passed\*\*:\s*(\d+)");
        var failMatch = Regex.Match(report, @"\*\*Failed\*\*:\s*(\d+)");

        int passed = passMatch.Success ? int.Parse(passMatch.Groups[1].Value) : 0;
        int failed = failMatch.Success ? int.Parse(failMatch.Groups[1].Value) : 0;

        UpdateCounts(passed, failed);
    }

    private void UpdateCounts(int passed, int failed)
    {
        PassedCountLabel.Text = passed.ToString();
        FailedCountLabel.Text = failed.ToString();
        TotalCountLabel.Text = (passed + failed).ToString();
    }
}
