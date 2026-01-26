using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using OneManVan.Dialogs;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;
using OneManVan.Shared.Models.Enums;
using OneManVan.Services;

namespace OneManVan.Pages;

/// <summary>
/// DataGrid-based product catalog page with filtering, search, and detail panel.
/// </summary>
public partial class ProductsDataGridPage : UserControl
{
    private List<Product> _allProducts = [];
    private ICollectionView? _productsView;
    private int _currentPage = 1;
    private int _pageSize = 50;
    private int _totalPages = 1;
    private string _searchText = string.Empty;
    private ProductCategory? _categoryFilter = null;
    private string? _manufacturerFilter = null;
    private Product? _selectedProduct = null;
    private readonly CsvExportService _exportService = new();
    private readonly ProductDocumentService _documentService = new();

    public ProductsDataGridPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await LoadProductsAsync();
    }

    private async Task LoadProductsAsync()
    {
        LoadingOverlay.Visibility = Visibility.Visible;

        try
        {
            _allProducts = await App.DbContext.Products
                .Include(p => p.Documents)
                .OrderBy(p => p.Manufacturer)
                .ThenBy(p => p.ModelNumber)
                .AsNoTracking()
                .ToListAsync();

            // Populate manufacturer filter
            PopulateManufacturerFilter();

            ApplyFiltersAndPagination();
            UpdateStats();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load products: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
        }
    }

    private void PopulateManufacturerFilter()
    {
        ManufacturerFilter.Items.Clear();
        ManufacturerFilter.Items.Add(new ComboBoxItem { Content = "All Manufacturers", IsSelected = true });

        var manufacturers = _allProducts
            .Select(p => p.Manufacturer)
            .Distinct()
            .OrderBy(m => m)
            .ToList();

        foreach (var manufacturer in manufacturers)
        {
            ManufacturerFilter.Items.Add(new ComboBoxItem { Content = manufacturer });
        }
    }

    private void ApplyFiltersAndPagination()
    {
        // Guard against calls during InitializeComponent before controls are created
        if (ProductGrid == null || PageInfo == null) return;

        var filtered = _allProducts.AsEnumerable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            var search = _searchText.ToLowerInvariant();
            filtered = filtered.Where(p =>
                p.ModelNumber.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.Manufacturer.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (p.ProductName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.ProductNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.Tags?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        // Apply category filter
        if (_categoryFilter.HasValue)
        {
            filtered = filtered.Where(p => p.Category == _categoryFilter.Value);
        }

        // Apply manufacturer filter
        if (!string.IsNullOrEmpty(_manufacturerFilter))
        {
            filtered = filtered.Where(p => p.Manufacturer == _manufacturerFilter);
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
        _productsView = CollectionViewSource.GetDefaultView(pagedData);
        ProductGrid.ItemsSource = _productsView;

        // Update count text
        ProductCountText.Text = $"{totalItems:N0} products in catalog";

        // Update pagination UI
        UpdatePaginationUI(filteredList.Count);
    }

    private void UpdatePaginationUI(int filteredCount)
    {
        var start = _pageSize > 0 ? ((_currentPage - 1) * _pageSize) + 1 : 1;
        var end = _pageSize > 0 ? Math.Min(_currentPage * _pageSize, filteredCount) : filteredCount;

        if (filteredCount == 0)
        {
            PageInfo.Text = "No products to display";
        }
        else
        {
            PageInfo.Text = $"Showing {start:N0}-{end:N0} of {filteredCount:N0} products";
        }

        CurrentPageText.Text = $"Page {_currentPage} of {_totalPages}";

        FirstPageBtn.IsEnabled = _currentPage > 1;
        PrevPageBtn.IsEnabled = _currentPage > 1;
        NextPageBtn.IsEnabled = _currentPage < _totalPages;
        LastPageBtn.IsEnabled = _currentPage < _totalPages;
    }

    private void UpdateStats()
    {
        TotalCount.Text = _allProducts.Count.ToString("N0");
        ActiveCount.Text = _allProducts.Count(p => p.IsActive && !p.IsDiscontinued).ToString("N0");
        DiscontinuedCount.Text = _allProducts.Count(p => p.IsDiscontinued).ToString("N0");
        ManufacturerCount.Text = _allProducts.Select(p => p.Manufacturer).Distinct().Count().ToString("N0");
    }

    private void UpdateDetailPanel(Product? product)
    {
        _selectedProduct = product;

        if (product == null)
        {
            NoSelectionPanel.Visibility = Visibility.Visible;
            ProductDetailsPanel.Visibility = Visibility.Collapsed;
            return;
        }

        NoSelectionPanel.Visibility = Visibility.Collapsed;
        ProductDetailsPanel.Visibility = Visibility.Visible;

        // Header
        DetailCategoryIcon.Text = GetCategoryIcon(product.Category);
        DetailModelNumber.Text = product.ModelNumber;
        DetailManufacturer.Text = product.Manufacturer;
        DetailProductName.Text = product.ProductName ?? "";

        // Specifications
        DetailCapacity.Text = product.CapacityDisplay;
        DetailEfficiency.Text = string.IsNullOrEmpty(product.EfficiencyDisplay) ? "-" : product.EfficiencyDisplay;
        DetailRefrigerant.Text = string.IsNullOrEmpty(product.RefrigerantDisplay) ? "-" : product.RefrigerantDisplay;
        DetailElectrical.Text = string.IsNullOrEmpty(product.ElectricalDisplay) ? "-" : product.ElectricalDisplay;
        DetailDimensions.Text = string.IsNullOrEmpty(product.DimensionsDisplay) ? "-" : product.DimensionsDisplay;
        DetailWeight.Text = product.WeightLbs.HasValue ? $"{product.WeightLbs:N0} lbs" : "-";
        DetailFilterSize.Text = product.FilterSize ?? "-";
        DetailMinCircuit.Text = product.MinCircuitAmpacity.HasValue ? $"{product.MinCircuitAmpacity:N0}A" : "-";

        // Pricing
        DetailMsrp.Text = product.Msrp.HasValue ? $"${product.Msrp:N2}" : "-";
        DetailCost.Text = product.WholesaleCost.HasValue ? $"${product.WholesaleCost:N2}" : "-";
        DetailSellPrice.Text = product.SuggestedSellPrice.HasValue ? $"${product.SuggestedSellPrice:N2}" : "-";
        DetailMargin.Text = product.ProfitMargin.HasValue ? $"{product.ProfitMargin:N1}%" : "-";
        DetailLaborHours.Text = product.LaborHoursEstimate.HasValue ? $"{product.LaborHoursEstimate:N1} hours" : "-";

        // Warranty
        DetailWarranty.Text = string.IsNullOrEmpty(product.WarrantyDisplay) ? "No warranty information" : product.WarrantyDisplay;
        RegistrationRequiredPanel.Visibility = product.RegistrationRequired ? Visibility.Visible : Visibility.Collapsed;

        // Documents
        UpdateDocumentsList(product);

        // Links
        var hasLinks = !string.IsNullOrEmpty(product.ManufacturerUrl) || !string.IsNullOrEmpty(product.VideoUrl);
        LinksHeader.Visibility = hasLinks ? Visibility.Visible : Visibility.Collapsed;
        LinksPanel.Visibility = hasLinks ? Visibility.Visible : Visibility.Collapsed;
        ManufacturerUrlButton.Visibility = !string.IsNullOrEmpty(product.ManufacturerUrl) ? Visibility.Visible : Visibility.Collapsed;
        VideoUrlButton.Visibility = !string.IsNullOrEmpty(product.VideoUrl) ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateDocumentsList(Product product)
    {
        DocumentsList.Children.Clear();

        if (product.Documents.Count == 0)
        {
            DocumentsList.Children.Add(new TextBlock
            {
                Text = "No documents attached",
                FontSize = 12,
                Foreground = (System.Windows.Media.Brush)FindResource("SubtextBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 10)
            });
            return;
        }

        foreach (var doc in product.Documents.OrderBy(d => d.DocumentType))
        {
            var docPanel = new Border
            {
                Background = (System.Windows.Media.Brush)FindResource("Surface2Brush"),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 0, 5),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = doc
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var iconText = new TextBlock
            {
                Text = GetDocumentIcon(doc.DocumentType),
                FontSize = 18,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            Grid.SetColumn(iconText, 0);

            var infoPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            infoPanel.Children.Add(new TextBlock
            {
                Text = doc.FileName,
                FontSize = 12,
                FontWeight = FontWeights.Medium,
                Foreground = (System.Windows.Media.Brush)FindResource("TextBrush"),
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            infoPanel.Children.Add(new TextBlock
            {
                Text = $"{doc.DocumentTypeDisplay} � {doc.FileSizeDisplay}",
                FontSize = 10,
                Foreground = (System.Windows.Media.Brush)FindResource("SubtextBrush")
            });
            Grid.SetColumn(infoPanel, 1);

            var openButton = new Button
            {
                Content = "",
                ToolTip = "Open",
                Padding = new Thickness(6, 4, 6, 4),
                FontSize = 12,
                Background = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = doc
            };
            openButton.Click += OnOpenDocumentClick;
            Grid.SetColumn(openButton, 2);

            grid.Children.Add(iconText);
            grid.Children.Add(infoPanel);
            grid.Children.Add(openButton);

            docPanel.Child = grid;
            docPanel.MouseLeftButtonUp += (s, e) =>
            {
                if (s is Border border && border.Tag is ProductDocument clickedDoc)
                {
                    _documentService.OpenDocument(clickedDoc);
                }
            };

            DocumentsList.Children.Add(docPanel);
        }
    }

    #region Event Handlers

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = SearchBox.Text;
        SearchPlaceholder.Visibility = string.IsNullOrEmpty(_searchText) ? Visibility.Visible : Visibility.Collapsed;
        _currentPage = 1;
        ApplyFiltersAndPagination();
    }

    private void OnCategoryFilterClick(object sender, RoutedEventArgs e)
    {
        FilterAll.IsChecked = false;
        FilterAC.IsChecked = false;
        FilterHeatPump.IsChecked = false;
        FilterFurnace.IsChecked = false;
        FilterMiniSplit.IsChecked = false;

        if (sender is ToggleButton btn)
        {
            btn.IsChecked = true;

            _categoryFilter = btn.Name switch
            {
                "FilterAC" => ProductCategory.AirConditioner,
                "FilterHeatPump" => ProductCategory.HeatPump,
                "FilterFurnace" => ProductCategory.GasFurnace,
                "FilterMiniSplit" => ProductCategory.MiniSplit,
                _ => null
            };
        }

        _currentPage = 1;
        ApplyFiltersAndPagination();
    }

    private void OnManufacturerFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ManufacturerFilter.SelectedItem is ComboBoxItem item)
        {
            var content = item.Content?.ToString() ?? "";
            _manufacturerFilter = content == "All Manufacturers" ? null : content;
        }

        _currentPage = 1;
        ApplyFiltersAndPagination();
    }

    private void OnDataGridSorting(object sender, DataGridSortingEventArgs e)
    {
        // Let default sorting handle it
    }

    private void OnProductSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProductGrid.SelectedItem is Product product)
        {
            UpdateDetailPanel(product);
            
            // Open drawer for editing
            var formContent = new Controls.ProductFormContent();
            formContent.LoadProduct(product);
            
            _ = DrawerService.Instance.OpenDrawerAsync(
                title: "Edit Product",
                content: formContent,
                saveButtonText: "Save Changes",
                onSave: async () =>
                {
                    if (!formContent.Validate())
                    {
                        MessageBox.Show("Manufacturer and Model Number are required", "Validation Error",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    try
                    {
                        var updated = formContent.GetProduct();
                        product.Manufacturer = updated.Manufacturer;
                        product.ModelNumber = updated.ModelNumber;
                        product.ProductName = updated.ProductName;
                        product.Category = updated.Category;
                        product.WholesaleCost = updated.WholesaleCost;
                        product.SuggestedSellPrice = updated.SuggestedSellPrice;
                        product.Description = updated.Description;
                        
                        await App.DbContext.SaveChangesAsync();
                        await DrawerService.Instance.CompleteDrawerAsync();
                        await LoadProductsAsync();
                        
                        ToastService.Success($"Product '{product.ModelNumber}' updated!");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to update product: {ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            );
        }
        else
        {
            UpdateDetailPanel(null);
        }
    }

    private void OnRowDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (ProductGrid.SelectedItem is Product product)
        {
            OnEditProductClick(sender, new RoutedEventArgs());
        }
    }

    private void OnAddProductClick(object sender, RoutedEventArgs e)
    {
        var formContent = new Controls.ProductFormContent();
        
        _ = DrawerService.Instance.OpenDrawerAsync(
            title: "Add Product",
            content: formContent,
            saveButtonText: "Save Product",
            onSave: async () =>
            {
                if (!formContent.Validate())
                {
                    MessageBox.Show("Manufacturer and Model Number are required", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    var product = formContent.GetProduct();
                    App.DbContext.Products.Add(product);
                    await App.DbContext.SaveChangesAsync();
                    
                    await DrawerService.Instance.CompleteDrawerAsync();
                    await LoadProductsAsync();
                    
                    ToastService.Success($"Product '{product.ModelNumber}' added successfully!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save product: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        );
    }

    private void OnEditProductClick(object sender, RoutedEventArgs e)
    {
        if (_selectedProduct == null) return;

        var dialog = new AddEditProductDialog(_selectedProduct)
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() == true && dialog.SavedProduct != null)
        {
            _ = LoadProductsAsync();
        }
    }

    private async void OnDeleteProductClick(object sender, RoutedEventArgs e)
    {
        if (_selectedProduct == null) return;

        var result = MessageBox.Show(
            $"Are you sure you want to delete '{_selectedProduct.Manufacturer} {_selectedProduct.ModelNumber}'?\n\nThis will also delete all attached documents.",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                var dbProduct = await App.DbContext.Products.FindAsync(_selectedProduct.Id);
                if (dbProduct != null)
                {
                    // Delete associated document files
                    foreach (var doc in _selectedProduct.Documents)
                    {
                        _documentService.DeleteDocumentFile(doc);
                    }

                    App.DbContext.Products.Remove(dbProduct);
                    await App.DbContext.SaveChangesAsync();
                    await LoadProductsAsync();
                    UpdateDetailPanel(null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete product: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void OnExportClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                FileName = $"Products_{DateTime.Now:yyyyMMdd}"
            };

            if (dialog.ShowDialog() == true)
            {
                var data = _allProducts.Select(p => new
                {
                    p.ProductNumber,
                    p.Manufacturer,
                    p.ModelNumber,
                    p.ProductName,
                    Category = p.CategoryDisplay,
                    Capacity = p.CapacityDisplay,
                    Efficiency = p.EfficiencyDisplay,
                    Refrigerant = p.RefrigerantDisplay,
                    p.Voltage,
                    p.Amperage,
                    p.MinCircuitAmpacity,
                    p.Msrp,
                    p.WholesaleCost,
                    p.SuggestedSellPrice,
                    p.LaborHoursEstimate,
                    p.PartsWarrantyYears,
                    p.CompressorWarrantyYears,
                    p.IsActive,
                    p.IsDiscontinued,
                    DocumentCount = p.Documents.Count
                }).ToList();

                await _exportService.ExportToCsvAsync(data, dialog.FileName);
                MessageBox.Show($"Exported {data.Count} products to {dialog.FileName}", "Export Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnImportClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                Title = "Import Products from CSV"
            };

            if (dialog.ShowDialog() == true)
            {
                // TODO: Implement CSV import
                MessageBox.Show("CSV import will be implemented in a future update.", "Coming Soon",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Import failed: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnAddToEstimateClick(object sender, RoutedEventArgs e)
    {
        if (_selectedProduct == null) return;

        // TODO: Implement add to estimate workflow
        MessageBox.Show($"Add '{_selectedProduct.ModelNumber}' to estimate workflow will be implemented.", 
            "Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnCreateAssetClick(object sender, RoutedEventArgs e)
    {
        if (_selectedProduct == null) return;

        // TODO: Implement create asset from product workflow
        MessageBox.Show($"Create asset from '{_selectedProduct.ModelNumber}' workflow will be implemented.", 
            "Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void OnUploadDocumentClick(object sender, RoutedEventArgs e)
    {
        if (_selectedProduct == null) return;

        var dialog = new OpenFileDialog
        {
            Filter = "PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*",
            Title = "Upload Product Document",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                foreach (var filePath in dialog.FileNames)
                {
                    var doc = await _documentService.UploadDocumentAsync(_selectedProduct.Id, filePath);
                    _selectedProduct.Documents.Add(doc);
                }

                await App.DbContext.SaveChangesAsync();
                UpdateDocumentsList(_selectedProduct);

                MessageBox.Show($"Uploaded {dialog.FileNames.Length} document(s) successfully.", "Upload Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Upload failed: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void OnOpenDocumentClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ProductDocument doc)
        {
            _documentService.OpenDocument(doc);
        }
    }

    private void OnManufacturerUrlClick(object sender, RoutedEventArgs e)
    {
        if (_selectedProduct?.ManufacturerUrl != null)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _selectedProduct.ManufacturerUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open URL: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void OnVideoUrlClick(object sender, RoutedEventArgs e)
    {
        if (_selectedProduct?.VideoUrl != null)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _selectedProduct.VideoUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open URL: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    #endregion

    #region Helper Methods

    private static string GetCategoryIcon(ProductCategory category) => category switch
    {
        ProductCategory.AirConditioner => "",
        ProductCategory.HeatPump => "",
        ProductCategory.GasFurnace or ProductCategory.OilFurnace or ProductCategory.ElectricFurnace => "",
        ProductCategory.Boiler => "",
        ProductCategory.Thermostat => "",
        ProductCategory.Accessories => "",
        _ => ""
    };

    private static string GetDocumentIcon(ProductDocumentType docType) => docType switch
    {
        ProductDocumentType.Brochure => "",
        ProductDocumentType.Manual => "",
        ProductDocumentType.SpecSheet => "",
        ProductDocumentType.InstallGuide => "",
        ProductDocumentType.WiringDiagram => "?",
        ProductDocumentType.WarrantyInfo => "",
        ProductDocumentType.ServiceBulletin => "",
        _ => ""
    };

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
}
