using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using OneManVan.ViewModels;

namespace OneManVan;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly WizardViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new WizardViewModel();
        _viewModel.WizardCompleted += OnWizardCompleted;
        DataContext = _viewModel;
    }

    private void OnWizardCompleted(object? sender, EventArgs e)
    {
        // Open the main shell window
        var mainShell = new MainShell();
        mainShell.Show();
        
        // Close the wizard window
        Close();
    }

    private void LocalRadio_Checked(object sender, RoutedEventArgs e)
    {
        _viewModel.DatabaseMode = "Local";
    }

    private void RemoteRadio_Checked(object sender, RoutedEventArgs e)
    {
        _viewModel.DatabaseMode = "Remote";
    }
}

/// <summary>
/// Converter for step visibility based on current step number.
/// </summary>
public class StepVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int currentStep && parameter is string param)
        {
            // Step number visibility
            if (int.TryParse(param, out int targetStep))
            {
                return currentStep == targetStep ? Visibility.Visible : Visibility.Collapsed;
            }

            // Special parameters
            return param switch
            {
                "notfirst" => currentStep > 1 ? Visibility.Visible : Visibility.Collapsed,
                "notlast" => currentStep < 4 ? Visibility.Visible : Visibility.Collapsed,
                "localcheck" => value?.ToString() == "Local",
                "remotecheck" => value?.ToString() == "Remote",
                "bool" => value is true ? Visibility.Visible : Visibility.Collapsed,
                "notnull" => value != null ? Visibility.Visible : Visibility.Collapsed,
                "required" => value is true ? "Required" : "Optional",
                _ => Visibility.Collapsed
            };
        }

        if (value is bool boolValue && parameter?.ToString() == "bool")
        {
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        if (value is string strValue)
        {
            return parameter?.ToString() switch
            {
                "localcheck" => strValue == "Local",
                "remotecheck" => strValue == "Remote",
                "notnull" => !string.IsNullOrEmpty(strValue) ? Visibility.Visible : Visibility.Collapsed,
                _ => Visibility.Collapsed
            };
        }

        if (parameter?.ToString() == "notnull")
        {
            return value != null ? Visibility.Visible : Visibility.Collapsed;
        }

        if (parameter?.ToString() == "required")
        {
            return value is true ? "Required" : "Optional";
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // This converter is only used for one-way bindings (display purposes)
        return Binding.DoNothing;
    }
}

/// <summary>
/// Converter for step indicator active state (circle color).
/// </summary>
public class StepActiveConverter : IValueConverter
{
    private static readonly SolidColorBrush ActiveBrush = new(Color.FromRgb(0x89, 0xb4, 0xfa));
    private static readonly SolidColorBrush CompletedBrush = new(Color.FromRgb(0xa6, 0xe3, 0xa1));
    private static readonly SolidColorBrush InactiveBrush = new(Color.FromRgb(0x45, 0x47, 0x5a));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int currentStep && parameter is string param && int.TryParse(param, out int targetStep))
        {
            if (currentStep > targetStep)
                return CompletedBrush;
            if (currentStep == targetStep)
                return ActiveBrush;
            return InactiveBrush;
        }

        return InactiveBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // This converter is only used for one-way bindings (display purposes)
        return Binding.DoNothing;
    }
}