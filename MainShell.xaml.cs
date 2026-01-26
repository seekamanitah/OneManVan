using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using OneManVan.Pages;
using OneManVan.Services;

namespace OneManVan;

/// <summary>
/// Main application shell with sidebar navigation and status bar.
/// </summary>
public partial class MainShell : Window
{
    private readonly NavigationService _navigation;
    private readonly DispatcherTimer _clockTimer;
    private readonly GlobalSearchService _searchService;
    private readonly DispatcherTimer _searchDebounceTimer;
    private Button? _activeNavButton;

    public MainShell()
    {
        InitializeComponent();

        _navigation = new NavigationService(MainContent);
        _searchService = new GlobalSearchService(App.DbContext);
        
        // Setup search debounce timer
        _searchDebounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _searchDebounceTimer.Tick += SearchDebounceTimer_Tick;
        
        RegisterPages();

        // Setup clock timer
        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _clockTimer.Tick += (s, e) => UpdateClock();
        _clockTimer.Start();

        // Initial status bar update
        UpdateStatusBar();

        // Initialize toast service
        ToastService.Initialize(ToastContainer);
        
        // Initialize drawer service
        DrawerService.Instance.Initialize(GlobalDrawer);

        // Subscribe to navigation requests from other pages
        NavigationRequest.Requested += OnNavigationRequested;

        // Subscribe to UI scale changes
        App.UiScaleService.ScaleChanged += OnUiScaleChanged;
        ApplyCurrentScale();

        // Start on home page
        NavigateTo("Home", NavHome);
        
        // Setup keyboard shortcuts
        KeyDown += MainShell_KeyDown;
    }

    private void OnUiScaleChanged(object? sender, double scale)
    {
        ApplyCurrentScale();
    }

    private void ApplyCurrentScale()
    {
        var scale = App.UiScaleService.CurrentScale;
        ContentScaleTransform.ScaleX = scale;
        ContentScaleTransform.ScaleY = scale;
    }

    private void OnNavigationRequested(object? sender, NavigationRequestArgs e)
    {
        // Map page name to nav button and navigate
        var navButton = e.PageName switch
        {
            "Home" => NavHome,
            "Customers" or "CustomerDataGrid" => NavCustomers,
            "Assets" or "AssetDataGrid" => NavAssets,
            "Products" => NavProducts,
            "Estimates" => NavEstimates,
            "Inventory" => NavInventory,
            "Jobs" => NavSchedule,
            "ServiceAgreements" => NavServiceAgreements,
            "Invoices" => NavInvoices,
            "Reports" => NavReports,
            "Schema" => NavSchema,
            "Settings" => NavSettings,
            "ApiSetupGuide" => NavSettings,
            _ => NavHome
        };

        // Map to actual page name
        var pageName = e.PageName switch
        {
            "Customers" => "CustomerDataGrid",
            "Assets" => "AssetDataGrid",
            _ => e.PageName
        };
        
        NavigateTo(pageName, navButton);
        
        if (!string.IsNullOrEmpty(e.Action))
        {
            SetStatus($"{e.PageName}: {e.Action} mode");
        }
    }

    private void MainShell_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        {
            SearchBox.Focus();
            e.Handled = true;
        }
    }

    private void RegisterPages()
    {
        _navigation.RegisterPage("Home", () => new HomePage());
        _navigation.RegisterPage("Customers", () => new CustomerListPage());
        _navigation.RegisterPage("CustomerDataGrid", () => new CustomerDataGridPage());
        _navigation.RegisterPage("Assets", () => new AssetListPage());
        _navigation.RegisterPage("AssetDataGrid", () => new AssetDataGridPage());
        _navigation.RegisterPage("Products", () => new ProductsDataGridPage());
        _navigation.RegisterPage("Estimates", () => new EstimateListPage());
        _navigation.RegisterPage("Inventory", () => new InventoryPage());
        _navigation.RegisterPage("Jobs", () => new JobListPage());
        _navigation.RegisterPage("JobKanban", () => new JobKanbanPage(App.DbContext));
        _navigation.RegisterPage("Calendar", () => new CalendarSchedulingPage(App.DbContext));
        _navigation.RegisterPage("ServiceAgreements", () => new ServiceAgreementsDataGridPage(App.DbContext));
        _navigation.RegisterPage("Invoices", () => new InvoiceListPage());
        _navigation.RegisterPage("Reports", () => new ReportsPage());
        _navigation.RegisterPage("Schema", () => new SchemaEditorPage());
        _navigation.RegisterPage("Settings", () => new SettingsPage());
        _navigation.RegisterPage("ApiSetupGuide", () => new ApiSetupGuidePage());
        _navigation.RegisterPage("TestRunner", () => new TestRunnerPage(App.DbContext));
    }

    private void NavigateTo(string pageName, Button navButton)
    {
        // Update active state
        if (_activeNavButton != null)
        {
            _activeNavButton.Tag = null;
        }
        navButton.Tag = "Active";
        _activeNavButton = navButton;

        // Navigate
        _navigation.NavigateTo(pageName);

        // Update status
        SetStatus($"Navigated to {pageName}");
    }

    private void UpdateStatusBar()
    {
        // Database mode
        DbModeText.Text = "SQLite (Local)";

        // Last backup
        var backupDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OneManVan", "Backups");

        if (Directory.Exists(backupDir))
        {
            var files = Directory.GetFiles(backupDir, "*.json")
                .Concat(Directory.GetFiles(backupDir, "*.zip"))
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTime)
                .FirstOrDefault();

            if (files != null)
            {
                var age = DateTime.Now - files.LastWriteTime;
                LastBackupText.Text = age.TotalHours < 1 
                    ? "Just now" 
                    : age.TotalHours < 24 
                        ? $"{(int)age.TotalHours}h ago" 
                        : $"{(int)age.TotalDays}d ago";
            }
            else
            {
                LastBackupText.Text = "No backup";
            }
        }

        UpdateClock();
    }

    private void UpdateClock()
    {
        CurrentTimeText.Text = DateTime.Now.ToString("h:mm tt");
    }

    public void SetStatus(string message)
    {
        StatusMessage.Text = message;

        // Auto-clear after 3 seconds
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        timer.Tick += (s, e) =>
        {
            timer.Stop();
            StatusMessage.Text = "Ready";
        };
        timer.Start();
    }

    private void NavHome_Click(object sender, RoutedEventArgs e)
    {
        NavigateTo("Home", NavHome);
    }

    private void NavCustomers_Click(object sender, RoutedEventArgs e)
    {
        NavigateTo("CustomerDataGrid", NavCustomers);
    }

    private void NavAssets_Click(object sender, RoutedEventArgs e)
    {
        NavigateTo("AssetDataGrid", NavAssets);
    }

    private void NavProducts_Click(object sender, RoutedEventArgs e)
    {
        NavigateTo("Products", NavProducts);
    }

    private void NavEstimates_Click(object sender, RoutedEventArgs e)
    {
        NavigateTo("Estimates", NavEstimates);
    }

    private void NavInventory_Click(object sender, RoutedEventArgs e)
    {
        NavigateTo("Inventory", NavInventory);
    }

    private void NavSchedule_Click(object sender, RoutedEventArgs e)
    {
        NavigateTo("Jobs", NavSchedule);
    }

    private void NavKanban_Click(object sender, RoutedEventArgs e)
    {
        NavigateTo("JobKanban", NavKanban);
    }

    private void NavCalendar_Click(object sender, RoutedEventArgs e)
    {
        NavigateTo("Calendar", NavCalendar);
    }

    private void NavServiceAgreements_Click(object sender, RoutedEventArgs e)
    {
        NavigateTo("ServiceAgreements", NavServiceAgreements);
    }

    private void NavInvoices_Click(object sender, RoutedEventArgs e)
    {
        NavigateTo("Invoices", NavInvoices);
    }

    private void NavSettings_Click(object sender, RoutedEventArgs e)
    {
        NavigateTo("Settings", NavSettings);
    }

    private void NavReports_Click(object sender, RoutedEventArgs e)
    {
        NavigateTo("Reports", NavReports);
    }

    private void NavSchema_Click(object sender, RoutedEventArgs e)
    {
        NavigateTo("Schema", NavSchema);
    }

    private void NavTestRunner_Click(object sender, RoutedEventArgs e)
    {
        NavigateTo("TestRunner", NavTestRunner);
    }

    private async void NavBackup_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Backup",
            Filter = "JSON Backup|*.json|All Files|*.*",
            FileName = $"OneManVan_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.json"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                await App.BackupService.ExportToJsonAsync(dialog.FileName);
                MessageBox.Show($"Backup saved to:\n{dialog.FileName}", "Backup Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Backup failed: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    #region Search Functionality

    private void SearchBox_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            SearchBox.Text = string.Empty;
            SearchResultsPopup.IsOpen = false;
            return;
        }

        if (e.Key == Key.Enter && SearchResultsList.SelectedItem != null)
        {
            NavigateToSearchResult(SearchResultsList.SelectedItem as SearchResult);
            return;
        }

        if (e.Key == Key.Down && SearchResultsPopup.IsOpen)
        {
            SearchResultsList.Focus();
            if (SearchResultsList.Items.Count > 0)
                SearchResultsList.SelectedIndex = 0;
            return;
        }

        // Debounce the search
        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Start();
    }

    private async void SearchDebounceTimer_Tick(object? sender, EventArgs e)
    {
        _searchDebounceTimer.Stop();
        await PerformSearchAsync();
    }

    private async Task PerformSearchAsync()
    {
        var query = SearchBox.Text?.Trim() ?? string.Empty;

        if (query.Length < 2)
        {
            SearchResultsPopup.IsOpen = false;
            return;
        }

        try
        {
            var results = await _searchService.QuickSearchAsync(query);
            
            if (results.Count > 0)
            {
                SearchResultsList.ItemsSource = results;
                NoResultsText.Visibility = Visibility.Collapsed;
            }
            else
            {
                SearchResultsList.ItemsSource = null;
                NoResultsText.Visibility = Visibility.Visible;
            }
            
            SearchResultsPopup.IsOpen = true;
        }
        catch
        {
            // Silently ignore search errors
        }
    }

    private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
    {
        SearchPlaceholder.Visibility = Visibility.Collapsed;
    }

    private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(SearchBox.Text))
        {
            SearchPlaceholder.Visibility = Visibility.Visible;
        }
        
        // Delay closing popup to allow clicking results
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (!SearchResultsList.IsKeyboardFocusWithin)
            {
                SearchResultsPopup.IsOpen = false;
            }
        }), DispatcherPriority.Background);
    }

    private void SearchResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SearchResultsList.SelectedItem is SearchResult result)
        {
            NavigateToSearchResult(result);
        }
    }

    private void NavigateToSearchResult(SearchResult? result)
    {
        if (result == null) return;

        SearchResultsPopup.IsOpen = false;
        SearchBox.Text = string.Empty;
        SearchPlaceholder.Visibility = Visibility.Visible;

        // Navigate based on entity type
        // Assets and Sites navigate to Customers page since they are viewed within customer details
        switch (result.EntityType)
        {
            case "Customer":
                NavigateTo("Customers", NavCustomers);
                SetStatus($"Navigated to customer: {result.Title}");
                break;
            case "Asset":
                NavigateTo("Customers", NavCustomers);
                SetStatus($"Navigated to asset: {result.Title} (view in customer details)");
                break;
            case "Site":
                NavigateTo("Customers", NavCustomers);
                SetStatus($"Navigated to site: {result.Title} (view in customer details)");
                break;
            case "Estimate":
                NavigateTo("Estimates", NavEstimates);
                SetStatus($"Navigated to estimate: {result.Title}");
                break;
            case "Job":
                NavigateTo("Jobs", NavSchedule);
                SetStatus($"Navigated to job: {result.Title}");
                break;
            case "Invoice":
                NavigateTo("Invoices", NavInvoices);
                SetStatus($"Navigated to invoice: {result.Title}");
                break;
            case "InventoryItem":
                NavigateTo("Inventory", NavInventory);
                SetStatus($"Navigated to inventory: {result.Title}");
                break;
        }
    }

    #endregion
}

