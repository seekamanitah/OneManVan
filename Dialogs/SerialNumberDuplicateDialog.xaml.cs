using System.Windows;
using OneManVan.Shared.Services;

namespace OneManVan.Dialogs;

/// <summary>
/// Dialog shown when a duplicate serial number is detected.
/// </summary>
public partial class SerialNumberDuplicateDialog : Window
{
    private readonly SerialNumberValidationResult _result;

    public SerialNumberDuplicateDialog(SerialNumberValidationResult result, string serialNumber)
    {
        InitializeComponent();
        _result = result;

        // Set serial number
        SerialNumberText.Text = $"Serial Number: {serialNumber}";

        // Set warning message
        WarningMessageText.Text = result.Message;

        // Set existing item details
        TypeText.Text = result.DuplicateType ?? "Unknown";
        DescriptionText.Text = result.DuplicateDescription ?? "No description";

        // Show customer if asset
        if (result.DuplicateType == "Asset" && !string.IsNullOrEmpty(result.DuplicateCustomer))
        {
            CustomerLabel.Visibility = Visibility.Visible;
            CustomerText.Visibility = Visibility.Visible;
            CustomerText.Text = result.DuplicateCustomer;
        }
    }

    private void OnViewClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
