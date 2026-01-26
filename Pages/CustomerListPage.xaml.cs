using System.Windows;
using System.Windows.Controls;
using OneManVan.Services;
using OneManVan.ViewModels;

namespace OneManVan.Pages;

/// <summary>
/// Customer list page with search and details panel.
/// </summary>
public partial class CustomerListPage : UserControl
{
    public CustomerListPage()
    {
        InitializeComponent();
        DataContext = new CustomerViewModel();
        
        // Handle pending action from Quick Actions navigation
        Loaded += CustomerListPage_Loaded;
    }

    private void CustomerListPage_Loaded(object sender, RoutedEventArgs e)
    {
        var pendingAction = NavigationRequest.ConsumePendingAction();
        if (pendingAction?.Action == "Add" && DataContext is CustomerViewModel vm)
        {
            // Trigger add customer mode
            vm.AddCustomerCommand.Execute(null);
        }
    }
}

