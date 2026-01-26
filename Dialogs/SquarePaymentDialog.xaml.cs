using System.Windows;
using OneManVan.Services;

namespace OneManVan.Dialogs;

/// <summary>
/// Dialog for processing Square payments.
/// Note: Full WebView2 integration would provide secure card entry.
/// This simplified version simulates payment for development/testing.
/// </summary>
public partial class SquarePaymentDialog : Window
{
    private readonly SquareService _squareService = new();
    private readonly decimal _amount;
    private readonly string _invoiceNumber;

    /// <summary>
    /// Result of the payment attempt.
    /// </summary>
    public SquarePaymentResult? PaymentResult { get; private set; }

    public SquarePaymentDialog(decimal amount, string invoiceNumber)
    {
        InitializeComponent();
        _amount = amount;
        _invoiceNumber = invoiceNumber;
        
        AmountText.Text = $"${amount:N2}";
        
        // Check if Square is configured
        if (!_squareService.IsAvailable)
        {
            StatusText.Text = "Square is not configured.\nGo to Settings to set up Square payments.";
            ProcessButton.IsEnabled = false;
        }
    }

    private async void Process_Click(object sender, RoutedEventArgs e)
    {
        ProcessButton.IsEnabled = false;
        CancelButton.IsEnabled = false;
        StatusText.Text = "Processing payment...";

        // Simulate a payment (in production, this would use Square Web Payments SDK)
        var amountCents = (long)(_amount * 100);
        var result = await _squareService.CreatePaymentAsync(
            amountCents,
            $"simulated_nonce_{Guid.NewGuid():N}",
            _invoiceNumber,
            $"Payment for invoice {_invoiceNumber}"
        );

        if (result.Success)
        {
            PaymentResult = result;
            DialogResult = true;
            Close();
        }
        else
        {
            StatusText.Text = $"Payment failed: {result.ErrorMessage}";
            ProcessButton.IsEnabled = true;
            CancelButton.IsEnabled = true;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// Shows the payment dialog and returns the result.
    /// </summary>
    public static SquarePaymentResult? ShowPaymentDialog(Window owner, decimal amount, string invoiceNumber)
    {
        var dialog = new SquarePaymentDialog(amount, invoiceNumber)
        {
            Owner = owner
        };

        return dialog.ShowDialog() == true ? dialog.PaymentResult : null;
    }
}
