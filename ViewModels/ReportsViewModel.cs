using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using OneManVan.Shared.Models.Enums;
using OneManVan.Services;

namespace OneManVan.ViewModels;

/// <summary>
/// ViewModel for the reports dashboard with KPIs and metrics.
/// </summary>
public class ReportsViewModel : BaseViewModel
{
    private bool _isLoading;
    private string _selectedPeriod = "This Month";
    private DateTime _startDate;
    private DateTime _endDate;

    // Revenue metrics
    private decimal _totalRevenue;
    private decimal _paidRevenue;
    private decimal _outstandingRevenue;
    private decimal _averageJobValue;

    // Job metrics
    private int _totalJobs;
    private int _completedJobs;
    private int _cancelledJobs;
    private decimal _completionRate;

    // Customer metrics
    private int _totalCustomers;
    private int _newCustomers;
    private int _activeCustomers;

    // Asset metrics
    private int _totalAssets;
    private int _expiringWarranties;

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string SelectedPeriod
    {
        get => _selectedPeriod;
        set
        {
            if (SetProperty(ref _selectedPeriod, value))
            {
                UpdateDateRange();
                _ = LoadMetricsAsync();
            }
        }
    }

    public DateTime StartDate
    {
        get => _startDate;
        set => SetProperty(ref _startDate, value);
    }

    public DateTime EndDate
    {
        get => _endDate;
        set => SetProperty(ref _endDate, value);
    }

    // Revenue
    public decimal TotalRevenue
    {
        get => _totalRevenue;
        set => SetProperty(ref _totalRevenue, value);
    }

    public decimal PaidRevenue
    {
        get => _paidRevenue;
        set => SetProperty(ref _paidRevenue, value);
    }

    public decimal OutstandingRevenue
    {
        get => _outstandingRevenue;
        set => SetProperty(ref _outstandingRevenue, value);
    }

    public decimal AverageJobValue
    {
        get => _averageJobValue;
        set => SetProperty(ref _averageJobValue, value);
    }

    // Jobs
    public int TotalJobs
    {
        get => _totalJobs;
        set => SetProperty(ref _totalJobs, value);
    }

    public int CompletedJobs
    {
        get => _completedJobs;
        set => SetProperty(ref _completedJobs, value);
    }

    public int CancelledJobs
    {
        get => _cancelledJobs;
        set => SetProperty(ref _cancelledJobs, value);
    }

    public decimal CompletionRate
    {
        get => _completionRate;
        set => SetProperty(ref _completionRate, value);
    }

    // Customers
    public int TotalCustomers
    {
        get => _totalCustomers;
        set => SetProperty(ref _totalCustomers, value);
    }

    public int NewCustomers
    {
        get => _newCustomers;
        set => SetProperty(ref _newCustomers, value);
    }

    public int ActiveCustomers
    {
        get => _activeCustomers;
        set => SetProperty(ref _activeCustomers, value);
    }

    // Assets
    public int TotalAssets
    {
        get => _totalAssets;
        set => SetProperty(ref _totalAssets, value);
    }

    public int ExpiringWarranties
    {
        get => _expiringWarranties;
        set => SetProperty(ref _expiringWarranties, value);
    }

    // Collections
    public ObservableCollection<string> PeriodOptions { get; } =
    [
        "Today",
        "This Week",
        "This Month",
        "This Quarter",
        "This Year",
        "All Time"
    ];

    public ObservableCollection<RevenueByFuelType> RevenueByFuel { get; } = [];
    public ObservableCollection<JobsByStatus> JobsByStatusData { get; } = [];
    public ObservableCollection<TopCustomer> TopCustomers { get; } = [];

    // Commands
    public ICommand RefreshCommand { get; }
    public ICommand ExportPdfCommand { get; }
    public ICommand ExportCsvCommand { get; }

    public ReportsViewModel()
    {
        RefreshCommand = new AsyncRelayCommand(LoadMetricsAsync);
        ExportPdfCommand = new AsyncRelayCommand(ExportToPdfAsync);
        ExportCsvCommand = new AsyncRelayCommand(ExportToCsvAsync);

        UpdateDateRange();
        _ = LoadMetricsAsync();
    }

    private async Task ExportToPdfAsync()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export Report to PDF",
            Filter = "PDF Files|*.pdf|All Files|*.*",
            FileName = $"OneManVan_Report_{SelectedPeriod.Replace(" ", "")}_{DateTime.Now:yyyyMMdd}.pdf"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                IsLoading = true;
                var pdfService = new PdfExportService();
                await pdfService.ExportReportToPdfAsync(this, dialog.FileName);
                
                System.Windows.MessageBox.Show(
                    $"Report exported successfully!\n\n{dialog.FileName}",
                    "Export Complete",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Failed to export PDF: {ex.Message}",
                    "Export Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    private async Task ExportToCsvAsync()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export Report to CSV",
            Filter = "CSV Files|*.csv|All Files|*.*",
            FileName = $"OneManVan_Report_{SelectedPeriod.Replace(" ", "")}_{DateTime.Now:yyyyMMdd}.csv"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                IsLoading = true;
                var csvService = new CsvExportService();
                await csvService.ExportReportToCsvAsync(this, dialog.FileName);
                
                System.Windows.MessageBox.Show(
                    $"Report exported successfully!\n\n{dialog.FileName}",
                    "Export Complete",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Failed to export CSV: {ex.Message}",
                    "Export Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    private void UpdateDateRange()
    {
        var now = DateTime.UtcNow;
        EndDate = now;

        StartDate = SelectedPeriod switch
        {
            "Today" => now.Date,
            "This Week" => now.AddDays(-(int)now.DayOfWeek).Date,
            "This Month" => new DateTime(now.Year, now.Month, 1),
            "This Quarter" => new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1),
            "This Year" => new DateTime(now.Year, 1, 1),
            "All Time" => DateTime.MinValue,
            _ => new DateTime(now.Year, now.Month, 1)
        };
    }

    private async Task LoadMetricsAsync()
    {
        IsLoading = true;

        try
        {
            // Revenue metrics from invoices
            var invoices = await App.DbContext.Invoices
                .Where(i => StartDate == DateTime.MinValue || i.InvoiceDate >= StartDate)
                .Where(i => i.InvoiceDate <= EndDate)
                .AsNoTracking()
                .ToListAsync();

            TotalRevenue = invoices.Sum(i => i.Total);
            PaidRevenue = invoices.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.Total);
            OutstandingRevenue = invoices
                .Where(i => i.Status != InvoiceStatus.Paid && 
                           i.Status != InvoiceStatus.Cancelled &&
                           i.Status != InvoiceStatus.Refunded)
                .Sum(i => i.Total - i.AmountPaid);

            // Job metrics
            var jobs = await App.DbContext.Jobs
                .Where(j => StartDate == DateTime.MinValue || j.CreatedAt >= StartDate)
                .Where(j => j.CreatedAt <= EndDate)
                .AsNoTracking()
                .ToListAsync();

            TotalJobs = jobs.Count;
            CompletedJobs = jobs.Count(j => j.Status == JobStatus.Completed || j.Status == JobStatus.Closed);
            CancelledJobs = jobs.Count(j => j.Status == JobStatus.Cancelled);
            CompletionRate = TotalJobs > 0 ? (decimal)CompletedJobs / TotalJobs * 100 : 0;
            AverageJobValue = CompletedJobs > 0 
                ? jobs.Where(j => j.Status == JobStatus.Completed || j.Status == JobStatus.Closed).Average(j => j.Total) 
                : 0;

            // Customer metrics
            TotalCustomers = await App.DbContext.Customers.CountAsync();
            NewCustomers = await App.DbContext.Customers
                .CountAsync(c => StartDate == DateTime.MinValue || c.CreatedAt >= StartDate);

            // Active customers (had a job in period)
            var customerIdsWithJobs = jobs.Select(j => j.CustomerId).Distinct().ToHashSet();
            ActiveCustomers = customerIdsWithJobs.Count;

            // Asset metrics
            TotalAssets = await App.DbContext.Assets.CountAsync();
            
            var assets = await App.DbContext.Assets
                .Where(a => a.WarrantyStartDate != null)
                .AsNoTracking()
                .ToListAsync();
            
            ExpiringWarranties = assets.Count(a => 
                a.DaysUntilWarrantyExpires.HasValue && 
                a.DaysUntilWarrantyExpires.Value <= 90 &&
                a.DaysUntilWarrantyExpires.Value >= 0);

            // Revenue by fuel type
            await LoadRevenueByFuelTypeAsync();

            // Jobs by status
            await LoadJobsByStatusAsync();

            // Top customers
            await LoadTopCustomersAsync();
        }
        catch
        {
            // Silently handle errors in dashboard
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadRevenueByFuelTypeAsync()
    {
        RevenueByFuel.Clear();

        var jobsWithAssets = await App.DbContext.Jobs
            .Include(j => j.Asset)
            .Where(j => j.AssetId != null)
            .Where(j => j.Status == JobStatus.Completed || j.Status == JobStatus.Closed)
            .Where(j => StartDate == DateTime.MinValue || j.CreatedAt >= StartDate)
            .Where(j => j.CreatedAt <= EndDate)
            .AsNoTracking()
            .ToListAsync();

        var grouped = jobsWithAssets
            .GroupBy(j => j.Asset?.FuelType ?? FuelType.Unknown)
            .Select(g => new RevenueByFuelType
            {
                FuelType = g.Key.ToString(),
                Revenue = g.Sum(j => j.Total),
                JobCount = g.Count()
            })
            .OrderByDescending(r => r.Revenue);

        foreach (var item in grouped)
        {
            RevenueByFuel.Add(item);
        }
    }

    private async Task LoadJobsByStatusAsync()
    {
        JobsByStatusData.Clear();

        var jobs = await App.DbContext.Jobs
            .Where(j => StartDate == DateTime.MinValue || j.CreatedAt >= StartDate)
            .Where(j => j.CreatedAt <= EndDate)
            .AsNoTracking()
            .ToListAsync();

        var grouped = jobs
            .GroupBy(j => j.Status)
            .Select(g => new JobsByStatus
            {
                Status = g.Key.ToString(),
                Count = g.Count()
            })
            .OrderByDescending(j => j.Count);

        foreach (var item in grouped)
        {
            JobsByStatusData.Add(item);
        }
    }

    private async Task LoadTopCustomersAsync()
    {
        TopCustomers.Clear();

        var jobs = await App.DbContext.Jobs
            .Include(j => j.Customer)
            .Where(j => j.Status == JobStatus.Completed || j.Status == JobStatus.Closed)
            .Where(j => StartDate == DateTime.MinValue || j.CreatedAt >= StartDate)
            .Where(j => j.CreatedAt <= EndDate)
            .AsNoTracking()
            .ToListAsync();

        var grouped = jobs
            .GroupBy(j => new { j.CustomerId, j.Customer.Name })
            .Select(g => new TopCustomer
            {
                Name = g.Key.Name,
                Revenue = g.Sum(j => j.Total),
                JobCount = g.Count()
            })
            .OrderByDescending(c => c.Revenue)
            .Take(5);

        foreach (var item in grouped)
        {
            TopCustomers.Add(item);
        }
    }
}

public class RevenueByFuelType
{
    public string FuelType { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int JobCount { get; set; }
}

public class JobsByStatus
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class TopCustomer
{
    public string Name { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int JobCount { get; set; }
}
