using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;
using OneManVan.Services;

namespace OneManVan.ViewModels;

/// <summary>
/// ViewModel for invoice management with payment tracking.
/// </summary>
public class InvoiceViewModel : BaseViewModel
{
    private readonly SchemaEditorService _schemaService = new();
    private readonly SquareService _squareService = new();
    
    private ObservableCollection<Invoice> _invoices = [];
    private Invoice? _selectedInvoice;
    private string _searchText = string.Empty;
    private InvoiceStatus? _statusFilter;
    private bool _isLoading;
    private bool _isEditing;
    private bool _isRecordingPayment;

    // Payment entry fields
    private decimal _paymentAmount;
    private PaymentMethod _paymentMethod = PaymentMethod.Cash;
    private string _paymentReference = string.Empty;
    private string _paymentNotes = string.Empty;

    public ObservableCollection<Invoice> Invoices
    {
        get => _invoices;
        set => SetProperty(ref _invoices, value);
    }

    public Invoice? SelectedInvoice
    {
        get => _selectedInvoice;
        set
        {
            if (SetProperty(ref _selectedInvoice, value))
            {
                LoadInvoiceDetails();
                OnPropertyChanged(nameof(HasSelectedInvoice));
                OnPropertyChanged(nameof(CanRecordPayment));
                OnPropertyChanged(nameof(CanSendInvoice));
            }
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                _ = LoadInvoicesAsync();
            }
        }
    }

    public InvoiceStatus? StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (SetProperty(ref _statusFilter, value))
            {
                _ = LoadInvoicesAsync();
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    public bool IsRecordingPayment
    {
        get => _isRecordingPayment;
        set => SetProperty(ref _isRecordingPayment, value);
    }

    public bool HasSelectedInvoice => SelectedInvoice != null;
    public bool CanRecordPayment => SelectedInvoice != null && 
        SelectedInvoice.Status != InvoiceStatus.Paid && 
        SelectedInvoice.Status != InvoiceStatus.Cancelled &&
        SelectedInvoice.Status != InvoiceStatus.Refunded;
    public bool CanSendInvoice => SelectedInvoice?.Status == InvoiceStatus.Draft;
    
    /// <summary>
    /// Whether Square payments are available.
    /// </summary>
    public bool IsSquareAvailable => _squareService.IsAvailable;

    // Payment entry properties
    public decimal PaymentAmount
    {
        get => _paymentAmount;
        set => SetProperty(ref _paymentAmount, value);
    }

    public PaymentMethod PaymentMethod
    {
        get => _paymentMethod;
        set => SetProperty(ref _paymentMethod, value);
    }

    public string PaymentReference
    {
        get => _paymentReference;
        set => SetProperty(ref _paymentReference, value);
    }

    public string PaymentNotes
    {
        get => _paymentNotes;
        set => SetProperty(ref _paymentNotes, value);
    }

    // Collections
    public ObservableCollection<Payment> InvoicePayments { get; } = [];
    public static InvoiceStatus[] StatusOptions => Enum.GetValues<InvoiceStatus>();
    public static PaymentMethod[] PaymentMethods => Enum.GetValues<PaymentMethod>();
    
    // Custom fields
    public ObservableCollection<SchemaDefinition> CustomFieldDefinitions { get; } = [];
    public ObservableCollection<CustomFieldValue> CustomFieldValues { get; } = [];
    public bool HasCustomFields => CustomFieldDefinitions.Count > 0;

    // Stats
    public decimal TotalOutstanding => Invoices.Where(i => 
        i.Status != InvoiceStatus.Paid && 
        i.Status != InvoiceStatus.Cancelled &&
        i.Status != InvoiceStatus.Refunded).Sum(i => i.BalanceDue);
    public int OverdueCount => Invoices.Count(i => i.IsOverdue);
    public decimal PaidThisMonth => Invoices
        .Where(i => i.PaidAt?.Month == DateTime.UtcNow.Month && i.PaidAt?.Year == DateTime.UtcNow.Year)
        .Sum(i => i.AmountPaid);

    // Commands
    public ICommand LoadInvoicesCommand { get; }
    public ICommand SendInvoiceCommand { get; }
    public ICommand RecordPaymentCommand { get; }
    public ICommand SavePaymentCommand { get; }
    public ICommand CancelPaymentCommand { get; }
    public ICommand MarkPaidCommand { get; }
    public ICommand CancelInvoiceCommand { get; }
    public ICommand ClearFilterCommand { get; }
    public ICommand PrintInvoiceCommand { get; }
    public ICommand ProcessSquarePaymentCommand { get; }

    public InvoiceViewModel()
    {
        LoadInvoicesCommand = new AsyncRelayCommand(LoadInvoicesAsync);
        SendInvoiceCommand = new AsyncRelayCommand(SendInvoiceAsync, () => CanSendInvoice);
        RecordPaymentCommand = new RelayCommand(StartRecordPayment, () => CanRecordPayment);
        SavePaymentCommand = new AsyncRelayCommand(SavePaymentAsync);
        CancelPaymentCommand = new RelayCommand(CancelPayment);
        MarkPaidCommand = new AsyncRelayCommand(MarkPaidAsync, () => CanRecordPayment);
        CancelInvoiceCommand = new AsyncRelayCommand(CancelInvoiceAsync, () => HasSelectedInvoice);
        ClearFilterCommand = new RelayCommand(() => StatusFilter = null);
        PrintInvoiceCommand = new RelayCommand(PrintInvoice, () => HasSelectedInvoice);
        ProcessSquarePaymentCommand = new AsyncRelayCommand(ProcessSquarePaymentAsync, () => CanRecordPayment && IsSquareAvailable);

        _ = LoadInvoicesAsync();
        _ = LoadCustomFieldDefinitionsAsync();
    }
    
    private async Task LoadCustomFieldDefinitionsAsync()
    {
        try
        {
            var definitions = await _schemaService.GetSchemaDefinitionsAsync("Invoice");
            CustomFieldDefinitions.Clear();
            foreach (var def in definitions)
            {
                CustomFieldDefinitions.Add(def);
            }
            OnPropertyChanged(nameof(HasCustomFields));
        }
        catch
        {
            // Silently fail - custom fields are optional
        }
    }

    private void PrintInvoice()
    {
        if (SelectedInvoice == null) return;
        
        try
        {
            PrintService.PrintInvoice(SelectedInvoice);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to print invoice: {ex.Message}", "Print Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadInvoicesAsync()
    {
        IsLoading = true;

        try
        {
            var query = App.DbContext.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Payments)
                .AsNoTracking();

            if (StatusFilter.HasValue)
            {
                query = query.Where(i => i.Status == StatusFilter.Value);
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                query = query.Where(i =>
                    i.InvoiceNumber.ToLower().Contains(searchLower) ||
                    i.Customer.Name.ToLower().Contains(searchLower));
            }

            var invoices = await query
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();

            // Update overdue status
            foreach (var invoice in invoices)
            {
                if (invoice.IsOverdue && invoice.Status == InvoiceStatus.Sent)
                {
                    invoice.UpdateStatus();
                }
            }

            Invoices = new ObservableCollection<Invoice>(invoices);

            OnPropertyChanged(nameof(TotalOutstanding));
            OnPropertyChanged(nameof(OverdueCount));
            OnPropertyChanged(nameof(PaidThisMonth));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load invoices: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void LoadInvoiceDetails()
    {
        InvoicePayments.Clear();

        if (SelectedInvoice == null) return;

        foreach (var payment in SelectedInvoice.Payments.OrderByDescending(p => p.PaymentDate))
        {
            InvoicePayments.Add(payment);
        }
    }

    private async Task SendInvoiceAsync()
    {
        if (SelectedInvoice == null || SelectedInvoice.Status != InvoiceStatus.Draft) return;

        try
        {
            var invoice = await App.DbContext.Invoices.FindAsync(SelectedInvoice.Id);
            if (invoice != null)
            {
                invoice.Status = InvoiceStatus.Sent;
                invoice.SentAt = DateTime.UtcNow;
                await App.DbContext.SaveChangesAsync();

                MessageBox.Show($"Invoice {invoice.InvoiceNumber} marked as sent.", "Invoice Sent",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadInvoicesAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to send invoice: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void StartRecordPayment()
    {
        if (SelectedInvoice == null) return;

        PaymentAmount = SelectedInvoice.BalanceDue;
        PaymentMethod = PaymentMethod.Cash;
        PaymentReference = string.Empty;
        PaymentNotes = string.Empty;
        IsRecordingPayment = true;
    }

    private void CancelPayment()
    {
        IsRecordingPayment = false;
    }

    private async Task SavePaymentAsync()
    {
        if (SelectedInvoice == null || PaymentAmount <= 0)
        {
            MessageBox.Show("Please enter a valid payment amount.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var invoice = await App.DbContext.Invoices
                .Include(i => i.Payments)
                .FirstAsync(i => i.Id == SelectedInvoice.Id);

            var payment = new Payment
            {
                InvoiceId = invoice.Id,
                Amount = PaymentAmount,
                Method = PaymentMethod,
                ReferenceNumber = string.IsNullOrWhiteSpace(PaymentReference) ? null : PaymentReference.Trim(),
                Notes = string.IsNullOrWhiteSpace(PaymentNotes) ? null : PaymentNotes.Trim(),
                PaymentDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            // Calculate processing fee for card payments (simplified - typically 2.9% + $0.30)
            if (PaymentMethod == PaymentMethod.CreditCard || PaymentMethod == PaymentMethod.DebitCard)
            {
                payment.ProcessingFee = Math.Round(PaymentAmount * 0.029m + 0.30m, 2);
            }
            payment.CalculateNetAmount();

            App.DbContext.Payments.Add(payment);

            invoice.AmountPaid += PaymentAmount;
            invoice.UpdateStatus();

            await App.DbContext.SaveChangesAsync();

            MessageBox.Show($"Payment of ${PaymentAmount:N2} recorded.", "Payment Recorded",
                MessageBoxButton.OK, MessageBoxImage.Information);

            IsRecordingPayment = false;
            await LoadInvoicesAsync();
            SelectedInvoice = Invoices.FirstOrDefault(i => i.Id == invoice.Id);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to record payment: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task MarkPaidAsync()
    {
        if (SelectedInvoice == null) return;

        var result = MessageBox.Show(
            $"Mark invoice {SelectedInvoice.InvoiceNumber} as fully paid?",
            "Confirm Payment",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            var invoice = await App.DbContext.Invoices.FindAsync(SelectedInvoice.Id);
            if (invoice != null)
            {
                // Record remaining balance as payment
                var remainingBalance = invoice.Total - invoice.AmountPaid;
                if (remainingBalance > 0)
                {
                    var payment = new Payment
                    {
                        InvoiceId = invoice.Id,
                        Amount = remainingBalance,
                        Method = PaymentMethod.Cash,
                        Notes = "Marked as paid",
                        PaymentDate = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    };
                    App.DbContext.Payments.Add(payment);
                    invoice.AmountPaid = invoice.Total;
                }

                invoice.Status = InvoiceStatus.Paid;
                invoice.PaidAt = DateTime.UtcNow;

                await App.DbContext.SaveChangesAsync();

                MessageBox.Show("Invoice marked as paid.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadInvoicesAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to mark paid: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task CancelInvoiceAsync()
    {
        if (SelectedInvoice == null) return;

        var result = MessageBox.Show(
            $"Cancel invoice {SelectedInvoice.InvoiceNumber}?\n\nThis cannot be undone.",
            "Confirm Cancel",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            var invoice = await App.DbContext.Invoices.FindAsync(SelectedInvoice.Id);
            if (invoice != null)
            {
                invoice.Status = InvoiceStatus.Cancelled;
                await App.DbContext.SaveChangesAsync();

                MessageBox.Show("Invoice cancelled.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadInvoicesAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to cancel invoice: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Process a payment through Square using the secure payment dialog.
    /// </summary>
    private async Task ProcessSquarePaymentAsync()
    {
        if (SelectedInvoice == null || !_squareService.IsAvailable) return;

        // Show the Square payment dialog
        var result = Dialogs.SquarePaymentDialog.ShowPaymentDialog(
            System.Windows.Application.Current.MainWindow,
            SelectedInvoice.BalanceDue,
            SelectedInvoice.InvoiceNumber
        );

        if (result == null || !result.Success)
        {
            // User cancelled or payment failed
            return;
        }

        IsLoading = true;

        try
        {
            var invoice = await App.DbContext.Invoices
                .Include(i => i.Payments)
                .FirstAsync(i => i.Id == SelectedInvoice.Id);

            var paymentAmount = invoice.Total - invoice.AmountPaid;
            
            // Record the successful Square payment
            var payment = new Payment
            {
                InvoiceId = invoice.Id,
                Amount = paymentAmount,
                Method = PaymentMethod.CreditCard,
                ReferenceNumber = result.TransactionId,
                Notes = $"Paid via Square ({result.CardBrand} •••• {result.Last4})",
                PaymentDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                ProcessingFee = Math.Round(paymentAmount * 0.029m + 0.30m, 2)
            };
            payment.CalculateNetAmount();

            App.DbContext.Payments.Add(payment);

            invoice.AmountPaid += paymentAmount;
            invoice.UpdateStatus();

            await App.DbContext.SaveChangesAsync();

            MessageBox.Show(
                $"Payment processed successfully!\n\n" +
                $"Amount: ${paymentAmount:N2}\n" +
                $"Card: {result.CardBrand} •••• {result.Last4}\n" +
                $"Transaction: {result.TransactionId}",
                "Payment Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            await LoadInvoicesAsync();
            SelectedInvoice = Invoices.FirstOrDefault(i => i.Id == invoice.Id);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to record payment: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
