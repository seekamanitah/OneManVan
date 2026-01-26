using System.Windows;
using System.Windows.Controls;
using OneManVan.Services;
using OneManVan.ViewModels;

namespace OneManVan.Pages;

/// <summary>
/// Asset list page with hierarchical TreeView and HVAC details panel.
/// </summary>
public partial class AssetListPage : UserControl
{
    private readonly AssetViewModel _viewModel;

    public AssetListPage()
    {
        InitializeComponent();
        _viewModel = new AssetViewModel();
        DataContext = _viewModel;
        
        // Handle pending action from Quick Actions navigation
        Loaded += AssetListPage_Loaded;
    }

    private void AssetListPage_Loaded(object sender, RoutedEventArgs e)
    {
        var pendingAction = NavigationRequest.ConsumePendingAction();
        if (pendingAction?.Action == "Add")
        {
            // Trigger add asset mode
            _viewModel.AddAssetCommand.Execute(null);
        }
    }

    private void TreeViewItem_Selected(object sender, RoutedEventArgs e)
    {
        if (sender is TreeViewItem item && item.DataContext is AssetTreeNode node)
        {
            // Only select if it's an asset node
            if (node.Asset != null)
            {
                _viewModel.SelectedAsset = node.Asset;
            }
            e.Handled = true;
        }
    }
}

