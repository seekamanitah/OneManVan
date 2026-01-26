using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;
using OneManVan.Services;

namespace OneManVan.Pages;

/// <summary>
/// DataGrid-based customer management page with sorting, filtering, and pagination.
/// </summary>
public partial class CustomerDataGridPage : UserControl
{
    private List<Customer> _allCustomers = [];
    private ICollectionView? _customersView;
    private int _currentPage = 1;
    private int _pageSize = 50;
    private int _totalPages = 1;
    private string _searchText = string.Empty;
    private CustomerStatus? _statusFilter = null;
    private CustomerType? _typeFilter = null;
    private readonly CsvExportService _exportService = new();

    public CustomerDataGridPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await LoadCustomersAsync();
    }

    private async Task LoadCustomersAsync()
    {
        LoadingOverlay.Visibility = Visibility.Visible;

        try
        {
            _allCustomers = await App.DbContext.Customers
                .Include(c => c.Sites)
                .Include(c => c.Assets)
                .OrderBy(c => c.Name)
                .AsNoTracking()
                .ToListAsync();

            ApplyFiltersAndPagination();
            UpdateStats();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load customers: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
        }
    }

    private void ApplyFiltersAndPagination()
    {
        // Guard against calls during InitializeComponent before controls are created
        if (CustomerGrid == null || PageInfo == null) return;

        var filtered = _allCustomers.AsEnumerable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            var search = _searchText.ToLowerInvariant();
            filtered = filtered.Where(c =>
                c.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (c.CompanyName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (c.Email?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (c.Phone?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (c.CustomerNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (c.Notes?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        // Apply status filter
        if (_statusFilter.HasValue)
        {
            filtered = filtered.Where(c => c.Status == _statusFilter.Value);
        }

        // Apply type filter
        if (_typeFilter.HasValue)
        {
            filtered = filtered.Where(c => c.CustomerType == _typeFilter.Value);
        }

        var filteredList = filtered.ToList();

        // Calculate pagination
        var totalItems = filteredList.Count;
        _totalPages = _pageSize > 0 ? (int)Math.Ceiling(totalItems / (double)_pageSize) : 1;
        _currentPage = Math.Min(_currentPage, Math.Max(1, _totalPages));

        // Apply pagination
        var pagedData = _pageSize > 0
            ? filteredList.Skip((_currentPage - 1) * _pageSize).Take(_pageSize).ToList()
            : filteredList;

        // Update DataGrid
        _customersView = CollectionViewSource.GetDefaultView(pagedData);
        CustomerGrid.ItemsSource = _customersView;

        // Update count text
        CustomerCountText.Text = $"{totalItems:N0} customers found";

        // Update pagination UI
        UpdatePaginationUI(totalItems, filteredList.Count);
    }

    private void UpdatePaginationUI(int totalItems, int filteredCount)
    {
        var start = _pageSize > 0 ? ((_currentPage - 1) * _pageSize) + 1 : 1;
        var end = _pageSize > 0 ? Math.Min(_currentPage * _pageSize, filteredCount) : filteredCount;

        if (filteredCount == 0)
        {
            PageInfo.Text = "No customers to display";
        }
        else
        {
            PageInfo.Text = $"Showing {start:N0}-{end:N0} of {filteredCount:N0} customers";
        }

        CurrentPageText.Text = $"Page {_currentPage} of {_totalPages}";

        // Enable/disable navigation buttons
        FirstPageBtn.IsEnabled = _currentPage > 1;
        PrevPageBtn.IsEnabled = _currentPage > 1;
        NextPageBtn.IsEnabled = _currentPage < _totalPages;
        LastPageBtn.IsEnabled = _currentPage < _totalPages;
    }

    private void UpdateStats()
    {
        TotalCount.Text = _allCustomers.Count.ToString("N0");
        ActiveCount.Text = _allCustomers.Count(c => c.Status == CustomerStatus.Active).ToString("N0");
        VIPCount.Text = _allCustomers.Count(c => c.Status == CustomerStatus.VIP || c.IsVip).ToString("N0");
        OutstandingBalance.Text = _allCustomers.Sum(c => c.AccountBalance).ToString("C2");
        LifetimeRevenue.Text = _allCustomers.Sum(c => c.LifetimeRevenue).ToString("C2");
    }

    #region Event Handlers

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = SearchBox.Text;
        SearchPlaceholder.Visibility = string.IsNullOrEmpty(_searchText) ? Visibility.Visible : Visibility.Collapsed;
        _currentPage = 1;
        ApplyFiltersAndPagination();
    }

    private void OnStatusFilterClick(object sender, RoutedEventArgs e)
    {
        // Reset all filter buttons
        FilterAll.IsChecked = false;
        FilterActive.IsChecked = false;
        FilterVIP.IsChecked = false;
        FilterLead.IsChecked = false;
        FilterDelinquent.IsChecked = false;

        // Set the clicked filter
        if (sender is ToggleButton btn)
        {
            btn.IsChecked = true;

            _statusFilter = btn.Name switch
            {
                "FilterActive" => CustomerStatus.Active,
                "FilterVIP" => CustomerStatus.VIP,
                "FilterLead" => CustomerStatus.Lead,
                "FilterDelinquent" => CustomerStatus.Delinquent,
                _ => null
            };
        }

        _currentPage = 1;
        ApplyFiltersAndPagination();
    }

    private void OnTypeFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TypeFilter.SelectedItem is ComboBoxItem item)
        {
            var content = item.Content?.ToString() ?? "";
            _typeFilter = content switch
            {
                string s when s.Contains("Residential") => CustomerType.Residential,
                string s when s.Contains("Commercial") => CustomerType.Commercial,
                string s when s.Contains("Property Manager") => CustomerType.PropertyManager,
                string s when s.Contains("Government") => CustomerType.Government,
                _ => null
            };
        }

        _currentPage = 1;
        ApplyFiltersAndPagination();
    }

    private void OnDataGridSorting(object sender, DataGridSortingEventArgs e)
    {
        // Let default sorting handle it
    }

    private void OnRowDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (CustomerGrid.SelectedItem is Customer customer)
        {
            NavigateToCustomerDetail(customer);
        }
    }
    
    private void OnCustomerSelected(object sender, SelectionChangedEventArgs e)
    {
        if (CustomerGrid.SelectedItem is Customer customer)
        {
            // Open drawer for editing
            var formContent = new Controls.CustomerFormContent();
            formContent.LoadCustomer(customer);
            
            _ = DrawerService.Instance.OpenDrawerAsync(
                title: "Edit Customer",
                content: formContent,
                saveButtonText: "Save Changes",
                onSave: async () =>
                {
                    if (!formContent.Validate())
                    {
                        MessageBox.Show("Customer name is required", "Validation Error",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    try
                    {
                        var updated = formContent.GetCustomer();
                        customer.Name = updated.Name;
                        customer.CompanyName = updated.CompanyName;
                        customer.Email = updated.Email;
                        customer.Phone = updated.Phone;
                        customer.CustomerType = updated.CustomerType;
                        customer.Notes = updated.Notes;
                        
                        await App.DbContext.SaveChangesAsync();
                        await DrawerService.Instance.CompleteDrawerAsync();
                        await LoadCustomersAsync();
                        
                        ToastService.Success($"Customer '{customer.Name}' updated!");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to update customer: {ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            );
        }
    }

    private void OnAddCustomerClick(object sender, RoutedEventArgs e)
    {
        var formContent = new Controls.CustomerFormContent();
        
        _ = DrawerService.Instance.OpenDrawerAsync(
            title: "Add Customer",
            content: formContent,
            saveButtonText: "Save Customer",
            onSave: async () =>
            {
                if (!formContent.Validate())
                {
                    MessageBox.Show("Customer name is required", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    var customer = formContent.GetCustomer();
                    App.DbContext.Customers.Add(customer);
                    await App.DbContext.SaveChangesAsync();
                    
                    await DrawerService.Instance.CompleteDrawerAsync();
                    await LoadCustomersAsync();
                    
                    ToastService.Success($"Customer '{customer.Name}' added successfully!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save customer: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        );
    }

    private async void OnExportClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                FileName = $"Customers_{DateTime.Now:yyyyMMdd}"
            };

            if (dialog.ShowDialog() == true)
            {
                var data = _allCustomers.Select(c => new
                {
                    c.CustomerNumber,
                    c.Name,
                    c.CompanyName,
                    Type = c.CustomerTypeDisplay,
                    Status = c.StatusDisplay,
                    c.Email,
                    c.Phone,
                    Sites = c.Sites.Count,
                    Assets = c.Assets.Count,
                    AccountBalance = c.AccountBalance,
                    LifetimeRevenue = c.LifetimeRevenue,
                    c.CreatedAt
                }).ToList();

                await _exportService.ExportToCsvAsync(data, dialog.FileName);
                MessageBox.Show($"Exported {data.Count} customers to {dialog.FileName}", "Export Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnCallClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Customer customer && !string.IsNullOrEmpty(customer.Phone))
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = $"tel:{customer.Phone}",
                    UseShellExecute = true
                });
            }
            catch
            {
                Clipboard.SetText(customer.Phone);
                MessageBox.Show($"Phone number copied: {customer.Phone}", "Call Customer",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    private void OnEmailClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Customer customer && !string.IsNullOrEmpty(customer.Email))
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = $"mailto:{customer.Email}",
                    UseShellExecute = true
                });
            }
            catch
            {
                Clipboard.SetText(customer.Email);
                MessageBox.Show($"Email copied: {customer.Email}", "Email Customer",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    private void OnEditClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Customer customer)
        {
            // Open edit dialog with the customer
            var dialog = new Dialogs.AddEditCustomerDialog(customer);
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true && dialog.SavedCustomer != null)
            {
                _ = LoadCustomersAsync();
            }
        }
    }

    private async void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Customer customer)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to delete '{customer.Name}'?\n\nThis will also delete all associated sites and assets.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var dbCustomer = await App.DbContext.Customers.FindAsync(customer.Id);
                    if (dbCustomer != null)
                    {
                        App.DbContext.Customers.Remove(dbCustomer);
                        await App.DbContext.SaveChangesAsync();
                        await LoadCustomersAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to delete customer: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    #endregion

    #region Pagination

    private void OnPageSizeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PageSizeCombo.SelectedItem is ComboBoxItem item)
        {
            var value = item.Content?.ToString();
            _pageSize = value == "All" ? 0 : int.Parse(value ?? "50");
            _currentPage = 1;
            ApplyFiltersAndPagination();
        }
    }

    private void OnFirstPageClick(object sender, RoutedEventArgs e)
    {
        _currentPage = 1;
        ApplyFiltersAndPagination();
    }

    private void OnPrevPageClick(object sender, RoutedEventArgs e)
    {
        if (_currentPage > 1)
        {
            _currentPage--;
            ApplyFiltersAndPagination();
        }
    }

    private void OnNextPageClick(object sender, RoutedEventArgs e)
    {
        if (_currentPage < _totalPages)
        {
            _currentPage++;
            ApplyFiltersAndPagination();
        }
    }

    private void OnLastPageClick(object sender, RoutedEventArgs e)
    {
        _currentPage = _totalPages;
        ApplyFiltersAndPagination();
    }

    #endregion

    private void NavigateToCustomerDetail(Customer customer)
    {
        // Use navigation request service to navigate
        NavigationRequest.Navigate("Customers", $"Edit customer: {customer.Name}");
    }
}
