using System.Windows.Controls;
using OneManVan.ViewModels;

namespace OneManVan.Pages;

/// <summary>
/// Reports dashboard page with KPIs and business metrics.
/// </summary>
public partial class ReportsPage : UserControl
{
    public ReportsPage()
    {
        InitializeComponent();
        DataContext = new ReportsViewModel();
    }
}
