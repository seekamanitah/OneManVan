using System;
using System.Windows;
using System.Windows.Controls;
using OneManVan.Services;
using OneManVan.Shared.Models;
using OneManVan.ViewModels;

namespace OneManVan.Pages;

/// <summary>
/// Invoice list page with payment tracking.
/// </summary>
public partial class InvoiceListPage : UserControl
{
    private InvoiceViewModel? _viewModel;
    
    public InvoiceListPage()
    {
        InitializeComponent();
        _viewModel = new InvoiceViewModel();
        DataContext = _viewModel;
        
        Loaded += OnPageLoaded;
    }
    
    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        // Check if there's a quick action to add invoice
        var pendingAction = NavigationRequest.ConsumePendingAction();
        if (pendingAction?.Action == "Add")
        {
            OnAddInvoiceClick(this, new RoutedEventArgs());
        }
    }
    
    private void OnAddInvoiceClick(object sender, RoutedEventArgs e)
    {
        var formContent = new Controls.InvoiceFormContent(App.DbContext);
        
        _ = DrawerService.Instance.OpenDrawerAsync(
            title: "Add Invoice",
            content: formContent,
            saveButtonText: "Create Invoice",
            onSave: async () =>
            {
                if (!formContent.Validate())
                {
                    MessageBox.Show("Customer, Invoice Date, and Due Date are required", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    var invoice = formContent.GetInvoice();
                    App.DbContext.Invoices.Add(invoice);
                    await App.DbContext.SaveChangesAsync();
                    
                    await DrawerService.Instance.CompleteDrawerAsync();
                    
                    // Refresh the ViewModel
                    if (_viewModel!.LoadInvoicesCommand.CanExecute(null))
                    {
                        _viewModel.LoadInvoicesCommand.Execute(null);
                    }
                    
                    ToastService.Success($"Invoice created successfully!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save invoice: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        );
    }
    
    private void OnInvoiceSelected(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is Invoice invoice)
        {
            var formContent = new Controls.InvoiceFormContent(App.DbContext);
            formContent.LoadInvoice(invoice);
            
            _ = DrawerService.Instance.OpenDrawerAsync(
                title: "Edit Invoice",
                content: formContent,
                saveButtonText: "Save Changes",
                onSave: async () =>
                {
                    if (!formContent.Validate())
                    {
                        MessageBox.Show("Customer, Invoice Date, and Due Date are required", "Validation Error",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    try
                    {
                        var updated = formContent.GetInvoice();
                        invoice.CustomerId = updated.CustomerId;
                        invoice.InvoiceDate = updated.InvoiceDate;
                        invoice.DueDate = updated.DueDate;
                        invoice.LaborAmount = updated.LaborAmount;
                        invoice.PartsAmount = updated.PartsAmount;
                        invoice.SubTotal = updated.SubTotal;
                        invoice.TaxRate = updated.TaxRate;
                        invoice.TaxAmount = updated.TaxAmount;
                        invoice.Total = updated.Total;
                        invoice.Notes = updated.Notes;
                        
                        await App.DbContext.SaveChangesAsync();
                        await DrawerService.Instance.CompleteDrawerAsync();
                        
                        // Refresh the ViewModel
                        if (_viewModel!.LoadInvoicesCommand.CanExecute(null))
                        {
                            _viewModel.LoadInvoicesCommand.Execute(null);
                        }
                        
                        ToastService.Success("Invoice updated!");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to update invoice: {ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            );
        }
    }
}

