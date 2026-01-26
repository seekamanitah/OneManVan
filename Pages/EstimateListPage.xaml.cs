using System.Windows;
using System.Windows.Controls;
using OneManVan.Services;
using OneManVan.ViewModels;

namespace OneManVan.Pages;

/// <summary>
/// Estimate list page with status filters and inline editing.
/// </summary>
public partial class EstimateListPage : UserControl
{
    public EstimateListPage()
    {
        InitializeComponent();
        DataContext = new EstimateViewModel();
        
        // Handle pending action from Quick Actions navigation
        Loaded += EstimateListPage_Loaded;
    }

    private void EstimateListPage_Loaded(object sender, RoutedEventArgs e)
    {
        var pendingAction = NavigationRequest.ConsumePendingAction();
        if (pendingAction?.Action == "Add" && DataContext is EstimateViewModel vm)
        {
            // Trigger new estimate mode
            vm.AddEstimateCommand.Execute(null);
        }
    }
}

