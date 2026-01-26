using Microsoft.EntityFrameworkCore;
using OneManVan.Mobile.Services;
using OneManVan.Mobile.Theme;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Mobile.Pages;

public partial class InvoiceListPage : ContentPage
{
    private readonly IDbContextFactory<OneManVanDbContext> _dbFactory;
    private readonly QuickAddCustomerService _quickAddCustomer;
    private CancellationTokenSource? _cts;
    private List<Invoice> _allInvoices = [];
    private InvoiceStatus? _statusFilter;

    public InvoiceListPage(IDbContextFactory<OneManVanDbContext> dbFactory, QuickAddCustomerService quickAddCustomer)
    {
        InitializeComponent();
        _dbFactory = dbFactory;
        _quickAddCustomer = quickAddCustomer;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        
        try
        {
            await LoadInvoicesAsync(_cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Page navigated away
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"InvoiceListPage.OnAppearing error: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _cts?.Cancel();
    }

    private async Task LoadInvoicesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            
            // PERF: AsNoTracking + removed Payments include (not displayed in list)
            _allInvoices = await db.Invoices
                .AsNoTracking()
                .Include(i => i.Customer)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync(cancellationToken);

            foreach (var invoice in _allInvoices)
            {
                invoice.UpdateStatus();
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                ApplyFilters();
                UpdateStats();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                await DisplayAlertAsync("Error", $"Failed to load invoices: {ex.Message}", "OK");
            }
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allInvoices.AsEnumerable();

        if (_statusFilter.HasValue)
        {
            if (_statusFilter == InvoiceStatus.Overdue)
            {
                filtered = filtered.Where(i => i.IsOverdue);
            }
            else
            {
                filtered = filtered.Where(i => i.Status == _statusFilter.Value);
            }
        }

        InvoiceCollection.ItemsSource = filtered.ToList();
    }

    private void UpdateStats()
    {
        var outstanding = _allInvoices
            .Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled)
            .Sum(i => i.BalanceDue);

        var overdueCount = _allInvoices.Count(i => i.IsOverdue);

        var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var paidThisMonth = _allInvoices
            .Where(i => i.PaidAt.HasValue && i.PaidAt.Value >= startOfMonth)
            .Sum(i => i.AmountPaid);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            OutstandingLabel.Text = $"${outstanding:N0}";
            OverdueCountLabel.Text = overdueCount.ToString();
            PaidMonthLabel.Text = $"${paidThisMonth:N0}";
        });
    }

    private void OnFilterClicked(object sender, EventArgs e)
    {
        ResetFilterStyles();

        if (sender is Button button)
        {
            button.BackgroundColor = AppColors.Primary;
            button.TextColor = AppColors.TextOnDark;

            _statusFilter = button.Text switch
            {
                string s when s.Contains("Draft") => InvoiceStatus.Draft,
                string s when s.Contains("Sent") => InvoiceStatus.Sent,
                string s when s.Contains("Overdue") => InvoiceStatus.Overdue,
                string s when s.Contains("Paid") => InvoiceStatus.Paid,
                _ => null
            };
        }

        ApplyFilters();
    }

    private void ResetFilterStyles()
    {
        AllFilter.BackgroundColor = AppColors.PrimarySurface;
        AllFilter.TextColor = AppColors.Primary;
        DraftFilter.BackgroundColor = AppColors.PrimarySurface;
        DraftFilter.TextColor = AppColors.Primary;
        SentFilter.BackgroundColor = AppColors.PrimarySurface;
        SentFilter.TextColor = AppColors.Primary;
        OverdueFilter.BackgroundColor = AppColors.ErrorSurface;
        OverdueFilter.TextColor = AppColors.Error;
        PaidFilter.BackgroundColor = AppColors.SuccessSurface;
        PaidFilter.TextColor = AppColors.Success;
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        await LoadInvoicesAsync(_cts.Token);
        RefreshViewControl.IsRefreshing = false;
    }

    private async void OnInvoiceSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Invoice invoice)
        {
            InvoiceCollection.SelectedItem = null;
            await Shell.Current.GoToAsync($"InvoiceDetail?id={invoice.Id}");
        }
    }

    private async Task RecordPaymentAsync(Invoice invoice)
    {
        if (invoice.IsPaid)
        {
            await DisplayAlertAsync("Already Paid", "This invoice has already been paid in full.", "OK");
            return;
        }

        var amountStr = await DisplayPromptAsync(
            "Record Payment",
            $"Enter payment amount (Balance: ${invoice.BalanceDue:N2}):",
            initialValue: invoice.BalanceDue.ToString("N2"),
            keyboard: Keyboard.Numeric);

        if (string.IsNullOrWhiteSpace(amountStr) || !decimal.TryParse(amountStr, out var amount) || amount <= 0)
            return;

        var methods = new[] { "Cash", "Check", "Credit Card", "Debit Card", "Bank Transfer", "Other" };
        var method = await DisplayActionSheetAsync("Payment Method", "Cancel", null, methods);

        if (method == "Cancel" || method == null)
            return;

        var paymentMethod = method switch
        {
            "Cash" => PaymentMethod.Cash,
            "Check" => PaymentMethod.Check,
            "Credit Card" => PaymentMethod.CreditCard,
            "Debit Card" => PaymentMethod.DebitCard,
            "Bank Transfer" => PaymentMethod.BankTransfer,
            _ => PaymentMethod.Other
        };

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            var dbInvoice = await db.Invoices.FindAsync(invoice.Id);
            if (dbInvoice != null)
            {
                var payment = new Payment
                {
                    InvoiceId = invoice.Id,
                    Amount = amount,
                    Method = paymentMethod,
                    PaymentDate = DateTime.UtcNow,
                    Notes = "Recorded from mobile app"
                };

                dbInvoice.AmountPaid += amount;
                dbInvoice.UpdateStatus();

                db.Payments.Add(payment);
                await db.SaveChangesAsync();
            }

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            await LoadInvoicesAsync(_cts.Token);

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

            await DisplayAlertAsync("Payment Recorded", 
                $"${amount:N2} payment recorded.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to record payment: {ex.Message}", "OK");
        }
    }

    private async Task SendInvoiceAsync(Invoice invoice)
    {
        if (invoice.Status != InvoiceStatus.Draft)
        {
            await DisplayAlertAsync("Cannot Send", "Only draft invoices can be sent.", "OK");
            return;
        }

        var confirm = await DisplayAlertAsync("Send Invoice", $"Mark invoice {invoice.InvoiceNumber} as sent?", "Send", "Cancel");
        if (!confirm) return;

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var dbInvoice = await db.Invoices.FindAsync(invoice.Id);
            if (dbInvoice != null)
            {
                dbInvoice.Status = InvoiceStatus.Sent;
                dbInvoice.SentAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            await LoadInvoicesAsync(_cts.Token);

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            await DisplayAlertAsync("Sent", "Invoice marked as sent.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to update invoice: {ex.Message}", "OK");
        }
    }

    private async void OnSendSwipe(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.BindingContext is Invoice invoice)
        {
            await SendInvoiceAsync(invoice);
        }
    }

    private async void OnPaySwipe(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.BindingContext is Invoice invoice)
        {
            await RecordPaymentAsync(invoice);
        }
    }

    #region Add Invoice

    private async void OnAddInvoiceClicked(object sender, EventArgs e)
    {
        var options = new[] { "New Invoice (Full Form)", "From Completed Job", "From Accepted Estimate" };
        var choice = await DisplayActionSheetAsync("Create Invoice", "Cancel", null, options);

        switch (choice)
        {
            case "New Invoice (Full Form)":
                await Shell.Current.GoToAsync("AddInvoice");
                break;
            case "From Completed Job":
                await CreateInvoiceFromJobAsync();
                break;
            case "From Accepted Estimate":
                await CreateInvoiceFromEstimateAsync();
                break;
        }
    }

    private async Task CreateInvoiceFromJobAsync()
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            var completedJobs = await db.Jobs
                .Include(j => j.Customer)
                .Where(j => j.Status == JobStatus.Completed && 
                           !db.Invoices.Any(i => i.JobId == j.Id))
                .OrderByDescending(j => j.CompletedAt)
                .Take(20)
                .ToListAsync();

            if (!completedJobs.Any())
            {
                await DisplayAlertAsync("No Jobs", "No completed jobs found without invoices.", "OK");
                return;
            }

            var jobOptions = completedJobs.Select(j => $"{j.Title} - {j.Customer?.Name} (${j.Total:N2})").ToArray();
            var selected = await DisplayActionSheetAsync("Select Job", "Cancel", null, jobOptions);

            if (string.IsNullOrEmpty(selected) || selected == "Cancel") return;

            var selectedIndex = Array.IndexOf(jobOptions, selected);
            var job = completedJobs[selectedIndex];

            var today = DateTime.Today;
            var invoiceCount = await db.Invoices.CountAsync(i => i.InvoiceDate >= today.AddDays(-today.Day + 1)) + 1;
            var invoiceNumber = $"INV-{today:yyyyMM}-{invoiceCount:D4}";

            var taxRate = decimal.Parse(Preferences.Get("TaxRate", "7.0"));
            var subTotal = job.LaborTotal + job.PartsTotal;
            var taxAmount = subTotal * (taxRate / 100);
            var total = subTotal + taxAmount;

            var invoice = new Invoice
            {
                CustomerId = job.CustomerId,
                JobId = job.Id,
                InvoiceNumber = invoiceNumber,
                Status = InvoiceStatus.Draft,
                Notes = job.WorkPerformed ?? job.Title,
                LaborAmount = job.LaborTotal,
                PartsAmount = job.PartsTotal,
                SubTotal = subTotal,
                TaxRate = taxRate,
                TaxAmount = taxAmount,
                Total = total,
                InvoiceDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(30),
                CreatedAt = DateTime.UtcNow
            };

            var dbJob = await db.Jobs.FindAsync(job.Id);
            if (dbJob != null)
            {
                dbJob.Status = JobStatus.Closed;
                dbJob.UpdatedAt = DateTime.UtcNow;
            }

            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            await LoadInvoicesAsync(_cts.Token);

            await DisplayAlertAsync("Invoice Created", $"Invoice {invoiceNumber} created.\nTotal: ${invoice.Total:N2}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to create invoice: {ex.Message}", "OK");
        }
    }

    private async Task CreateInvoiceFromEstimateAsync()
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            var acceptedEstimates = await db.Estimates
                .Include(e => e.Customer)
                .Include(e => e.Lines)
                .Where(e => e.Status == EstimateStatus.Accepted)
                .OrderByDescending(e => e.AcceptedAt)
                .Take(20)
                .ToListAsync();

            if (!acceptedEstimates.Any())
            {
                await DisplayAlertAsync("No Estimates", "No accepted estimates found.", "OK");
                return;
            }

            var estimateOptions = acceptedEstimates.Select(e => $"{e.Title} - {e.Customer?.Name} (${e.Total:N2})").ToArray();
            var selected = await DisplayActionSheetAsync("Select Estimate", "Cancel", null, estimateOptions);

            if (string.IsNullOrEmpty(selected) || selected == "Cancel") return;

            var selectedIndex = Array.IndexOf(estimateOptions, selected);
            var estimate = acceptedEstimates[selectedIndex];

            var today = DateTime.Today;
            var invoiceCount = await db.Invoices.CountAsync(i => i.InvoiceDate >= today.AddDays(-today.Day + 1)) + 1;
            var invoiceNumber = $"INV-{today:yyyyMM}-{invoiceCount:D4}";

            var invoice = new Invoice
            {
                CustomerId = estimate.CustomerId,
                EstimateId = estimate.Id,
                InvoiceNumber = invoiceNumber,
                Status = InvoiceStatus.Draft,
                Notes = estimate.Description ?? estimate.Title,
                LaborAmount = estimate.SubTotal * 0.7m,
                PartsAmount = estimate.SubTotal * 0.3m,
                SubTotal = estimate.SubTotal,
                TaxRate = estimate.TaxRate,
                TaxAmount = estimate.TaxAmount,
                Total = estimate.Total,
                InvoiceDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(30),
                CreatedAt = DateTime.UtcNow
            };

            var dbEstimate = await db.Estimates.FindAsync(estimate.Id);
            if (dbEstimate != null)
            {
                dbEstimate.Status = EstimateStatus.Converted;
            }

            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            await LoadInvoicesAsync(_cts.Token);

            await DisplayAlertAsync("Invoice Created", $"Invoice {invoiceNumber} created.\nTotal: ${invoice.Total:N2}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to create invoice: {ex.Message}", "OK");
        }
    }

    #endregion
}
