using Microsoft.EntityFrameworkCore;
using OneManVan.Mobile.Helpers;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;

namespace OneManVan.Mobile.Pages;

/// <summary>
/// Invoice detail page with payment recording and status management.
/// </summary>
[QueryProperty(nameof(InvoiceId), "id")]
public partial class InvoiceDetailPage : ContentPage
{
    private readonly OneManVanDbContext _db;
    private Invoice? _invoice;
    private int _invoiceId;

    public int InvoiceId
    {
        get => _invoiceId;
        set => _invoiceId = value;
    }

    public InvoiceDetailPage(OneManVanDbContext db)
    {
        InitializeComponent();
        _db = db;
    }

    // Keep backward compatibility with direct navigation
    public InvoiceDetailPage(int invoiceId) : this(
        IPlatformApplication.Current?.Services.GetRequiredService<OneManVanDbContext>()
        ?? throw new InvalidOperationException("DbContext not available"))
    {
        _invoiceId = invoiceId;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_invoiceId > 0)
        {
            await LoadInvoiceAsync();
        }
    }

    private async Task LoadInvoiceAsync()
    {
        try
        {
            ShowLoading("Loading invoice...");

            _invoice = await _db.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.Id == _invoiceId);

            if (_invoice == null)
            {
                await DisplayAlertAsync("Error", "Invoice not found", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            UpdateUI();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load invoice: {ex.Message}", "OK");
        }
        finally
        {
            HideLoading();
        }
    }

    private void UpdateUI()
    {
        if (_invoice == null) return;

        // Invoice number and status
        InvoiceNumberLabel.Text = _invoice.InvoiceNumber;
        UpdateStatusBanner();

        // Customer info
        CustomerName.Text = _invoice.Customer?.Name ?? "Unknown Customer";
        CustomerEmail.Text = _invoice.Customer?.Email ?? "No email";

        // Dates
        InvoiceDateLabel.Text = _invoice.CreatedAt.ToShortDate();
        DueDateLabel.Text = _invoice.DueDate.ToShortDate();

        // Job reference
        JobReferenceLabel.Text = _invoice.JobId.HasValue ? $"J-{_invoice.JobId:D4}" : "N/A";
        PaymentTermsLabel.Text = "Net 30";

        // Amounts
        LaborTotal.Text = $"${_invoice.LaborAmount:F2}";
        PartsTotal.Text = $"${_invoice.PartsAmount:F2}";
        SubTotal.Text = $"${_invoice.SubTotal:F2}";
        TaxTotal.Text = $"${_invoice.TaxAmount:F2}";
        GrandTotal.Text = $"${_invoice.Total:F2}";

        // Payments
        var payments = _invoice.Payments?.ToList() ?? [];
        var totalPaid = payments.Sum(p => p.Amount);
        var balance = _invoice.Total - totalPaid;

        PaymentCountLabel.Text = $"{payments.Count} payment(s)";
        
        if (payments.Any())
        {
            PaymentsCollection.ItemsSource = payments;
            PaymentsCollection.IsVisible = true;
            NoPaymentsLabel.IsVisible = false;
        }
        else
        {
            PaymentsCollection.IsVisible = false;
            NoPaymentsLabel.IsVisible = true;
        }

        BalanceDue.Text = $"${balance:F2}";
        BalanceDueFrame.IsVisible = balance > 0;

        // Notes
        if (!string.IsNullOrWhiteSpace(_invoice.Notes))
        {
            NotesLabel.Text = _invoice.Notes;
            NotesFrame.IsVisible = true;
        }
        else
        {
            NotesFrame.IsVisible = false;
        }

        // Action buttons
        UpdateActionButtons();
    }

    private void UpdateStatusBanner()
    {
        if (_invoice == null) return;

        var (icon, text, subtext, color) = _invoice.Status switch
        {
            InvoiceStatus.Draft => ("", "Draft", "Not yet sent", Color.FromArgb("#6B7280")),
            InvoiceStatus.Sent => ("", "Sent", "Awaiting payment", Color.FromArgb("#FF9800")),
            InvoiceStatus.PartiallyPaid => ("", "Partial Payment", "Balance remaining", Color.FromArgb("#9C27B0")),
            InvoiceStatus.Paid => ("", "Paid", "Payment complete", Color.FromArgb("#4CAF50")),
            InvoiceStatus.Overdue => ("", "Overdue", "Payment past due", Color.FromArgb("#F44336")),
            InvoiceStatus.Cancelled => ("", "Cancelled", "Invoice cancelled", Color.FromArgb("#9E9E9E")),
            InvoiceStatus.Refunded => ("", "Refunded", "Payment refunded", Color.FromArgb("#607D8B")),
            _ => ("", "Unknown", "", Color.FromArgb("#6B7280"))
        };

        StatusIcon.Text = icon;
        StatusText.Text = text;
        StatusSubtext.Text = subtext;
        StatusBanner.BackgroundColor = color;
    }

    private void UpdateActionButtons()
    {
        if (_invoice == null) return;

        var isPaid = _invoice.IsPaid;
        var isCancelled = _invoice.Status == InvoiceStatus.Cancelled;

        RecordPaymentButton.IsVisible = !isPaid && !isCancelled;
        MarkPaidButton.IsVisible = !isPaid && !isCancelled;
        VoidButton.IsVisible = !isCancelled;
    }

    private async void OnEmailInvoiceClicked(object sender, EventArgs e)
    {
        if (_invoice?.Customer?.Email == null)
        {
            await DisplayAlertAsync("Error", "Customer has no email address", "OK");
            return;
        }

        try
        {
            var message = new EmailMessage
            {
                Subject = $"Invoice {_invoice.InvoiceNumber}",
                Body = $"Dear {_invoice.Customer.Name},\n\nPlease find attached invoice {_invoice.InvoiceNumber} for ${_invoice.Total:F2}.\n\nDue Date: {_invoice.DueDate:MMM d, yyyy}\n\nThank you for your business!",
                To = [_invoice.Customer.Email]
            };

            await Email.Default.ComposeAsync(message);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Cannot open email: {ex.Message}", "OK");
        }
    }

    private async void OnShareInvoiceClicked(object sender, EventArgs e)
    {
        if (_invoice == null) return;

        try
        {
            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Title = $"Invoice {_invoice.InvoiceNumber}",
                Text = $"Invoice {_invoice.InvoiceNumber}\nCustomer: {_invoice.Customer?.Name}\nAmount: ${_invoice.Total:F2}\nDue: {_invoice.DueDate:MMM d, yyyy}"
            });
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Cannot share: {ex.Message}", "OK");
        }
    }

    private async void OnRecordPaymentClicked(object sender, EventArgs e)
    {
        if (_invoice == null) return;

        var balance = _invoice.BalanceDue;
        
        var amountStr = await DisplayPromptAsync("Record Payment", 
            $"Enter payment amount (Balance: ${balance:F2}):", 
            "Record", "Cancel", 
            $"{balance:F2}", 
            keyboard: Keyboard.Numeric);

        if (!decimal.TryParse(amountStr, out var amount) || amount <= 0)
            return;

        var methods = new[] { "Cash", "Check", "Credit Card", "ACH/Bank Transfer", "Other" };
        var method = await DisplayActionSheetAsync("Payment Method", "Cancel", null, methods);

        if (method == "Cancel" || method == null)
            return;

        try
        {
            ShowLoading("Recording payment...");

            var payment = new Payment
            {
                InvoiceId = _invoice.Id,
                Amount = amount,
                Method = method switch
                {
                    "Cash" => PaymentMethod.Cash,
                    "Check" => PaymentMethod.Check,
                    "Credit Card" => PaymentMethod.CreditCard,
                    "ACH/Bank Transfer" => PaymentMethod.BankTransfer,
                    _ => PaymentMethod.Other
                },
                PaymentDate = DateTime.Now,
                Notes = $"Recorded via mobile app"
            };

            _db.Payments.Add(payment);

            // Update invoice status
            var totalPaid = (_invoice.Payments?.Sum(p => p.Amount) ?? 0) + amount;
            if (totalPaid >= _invoice.Total)
            {
                _invoice.Status = InvoiceStatus.Paid;
                _invoice.PaidAt = DateTime.Now;
            }
            else
            {
                _invoice.Status = InvoiceStatus.PartiallyPaid;
            }

            await _db.SaveChangesAsync();

            // Reload to get updated payments
            await LoadInvoiceAsync();

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

            await DisplayAlertAsync("Payment Recorded", $"${amount:F2} payment recorded successfully.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to record payment: {ex.Message}", "OK");
        }
        finally
        {
            HideLoading();
        }
    }

    private async void OnMarkPaidClicked(object sender, EventArgs e)
    {
        if (_invoice == null) return;

        var balance = _invoice.BalanceDue;
        
        var confirm = await DisplayAlertAsync("Mark as Paid", 
            $"Record full payment of ${balance:F2}?", 
            "Yes", "No");

        if (!confirm)
            return;

        try
        {
            ShowLoading("Processing...");

            var payment = new Payment
            {
                InvoiceId = _invoice.Id,
                Amount = balance,
                Method = PaymentMethod.Other,
                PaymentDate = DateTime.Now,
                Notes = "Marked as paid via mobile app"
            };

            _db.Payments.Add(payment);
            _invoice.Status = InvoiceStatus.Paid;
            _invoice.PaidAt = DateTime.Now;

            await _db.SaveChangesAsync();

            await LoadInvoiceAsync();

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

            await DisplayAlertAsync("Paid", "Invoice marked as paid.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to update: {ex.Message}", "OK");
        }
        finally
        {
            HideLoading();
        }
    }

    private async void OnVoidInvoiceClicked(object sender, EventArgs e)
    {
        if (_invoice == null) return;

        var reason = await DisplayPromptAsync("Void Invoice", 
            "Enter reason for voiding:", 
            "Void", "Cancel");

        if (string.IsNullOrEmpty(reason))
            return;

        try
        {
            ShowLoading("Voiding invoice...");

            _invoice.Status = InvoiceStatus.Cancelled;
            _invoice.Notes = $"VOIDED: {reason}\n\n{_invoice.Notes}";

            await _db.SaveChangesAsync();

            UpdateUI();

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

            await DisplayAlertAsync("Voided", "Invoice has been voided.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to void: {ex.Message}", "OK");
        }
        finally
        {
            HideLoading();
        }
    }

    private void ShowLoading(string message)
    {
        LoadingText.Text = message;
        LoadingOverlay.IsVisible = true;
    }

    private void HideLoading()
    {
        LoadingOverlay.IsVisible = false;
    }
}
