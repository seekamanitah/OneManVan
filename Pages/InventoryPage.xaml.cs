using System;
using System.Windows;
using System.Windows.Controls;
using OneManVan.Services;
using OneManVan.Shared.Models;
using OneManVan.ViewModels;

namespace OneManVan.Pages;

/// <summary>
/// Inventory management page with HVAC compatibility filtering.
/// </summary>
public partial class InventoryPage : UserControl
{
    private InventoryViewModel? _viewModel;
    
    public InventoryPage()
    {
        InitializeComponent();
        _viewModel = new InventoryViewModel();
        DataContext = _viewModel;
    }
    
    private async void OnAddInventoryClick(object sender, RoutedEventArgs e)
    {
        // Close any open drawer first
        await DrawerService.Instance.CloseDrawerAsync();
        
        // Small delay to allow previous drawer to fully close
        await System.Threading.Tasks.Task.Delay(150);
        
        var formContent = new Controls.InventoryFormContent();
        
        await DrawerService.Instance.OpenDrawerAsync(
            title: "Add Inventory Item",
            content: formContent,
            saveButtonText: "Add Item",
            onSave: async () =>
            {
                if (!formContent.Validate())
                {
                    MessageBox.Show("Item name is required", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    var item = formContent.GetInventoryItem();
                    App.DbContext.InventoryItems.Add(item);
                    await App.DbContext.SaveChangesAsync();
                    
                    await DrawerService.Instance.CompleteDrawerAsync();
                    
                    // Refresh ViewModel
                    if (_viewModel?.LoadItemsCommand.CanExecute(null) == true)
                    {
                        _viewModel.LoadItemsCommand.Execute(null);
                    }
                    
                    ToastService.Success($"Item '{item.Name}' added!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to add item: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        );
    }
    
    private async void OnInventoryItemSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is InventoryItem item)
        {
            // Close any open drawer first
            await DrawerService.Instance.CloseDrawerAsync();
            
            // Small delay to allow previous drawer to fully close
            await System.Threading.Tasks.Task.Delay(150);
            
            var formContent = new Controls.InventoryFormContent();
            formContent.LoadInventoryItem(item);
            
            await DrawerService.Instance.OpenDrawerAsync(
                title: "Edit Inventory Item",
                content: formContent,
                saveButtonText: "Save Changes",
                onSave: async () =>
                {
                    if (!formContent.Validate())
                    {
                        MessageBox.Show("Item name is required", "Validation Error",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    try
                    {
                        var updated = formContent.GetInventoryItem();
                        item.Name = updated.Name;
                        item.Sku = updated.Sku;
                        item.Category = updated.Category;
                        item.QuantityOnHand = updated.QuantityOnHand;
                        item.Cost = updated.Cost;
                        item.Price = updated.Price;
                        item.Location = updated.Location;
                        item.Description = updated.Description;
                        
                        await App.DbContext.SaveChangesAsync();
                        await DrawerService.Instance.CompleteDrawerAsync();
                        
                        // Refresh ViewModel
                        if (_viewModel?.LoadItemsCommand.CanExecute(null) == true)
                        {
                            _viewModel.LoadItemsCommand.Execute(null);
                        }
                        
                        ToastService.Success($"Item '{item.Name}' updated!");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to update item: {ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            );
        }
    }
}

