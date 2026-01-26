using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using OneManVan.Shared.Models;

namespace OneManVan.Dialogs;

/// <summary>
/// Dialog for selecting an inventory item to add to an estimate.
/// </summary>
public partial class InventoryPickerDialog : Window, INotifyPropertyChanged
{
    private string _searchText = string.Empty;
    private InventoryItem? _selectedItem;
    private readonly List<InventoryItem> _allItems;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                OnPropertyChanged();
                FilterItems();
            }
        }
    }

    public InventoryItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (_selectedItem != value)
            {
                _selectedItem = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelection));
            }
        }
    }

    public ObservableCollection<InventoryItem> FilteredItems { get; } = [];

    public bool HasSelection => SelectedItem != null;
    public bool HasNoItems => FilteredItems.Count == 0;

    /// <summary>
    /// The selected inventory item after dialog closes, or null if cancelled.
    /// </summary>
    public InventoryItem? Result { get; private set; }

    public InventoryPickerDialog(IEnumerable<InventoryItem> items)
    {
        InitializeComponent();
        DataContext = this;
        
        _allItems = items.ToList();
        FilterItems();

        SearchBox.Focus();
    }

    private void FilterItems()
    {
        FilteredItems.Clear();

        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allItems
            : _allItems.Where(i =>
                i.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (i.Sku?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (i.Description?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                i.Category.ToString().Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        foreach (var item in filtered.OrderBy(i => i.Name))
        {
            FilteredItems.Add(item);
        }

        OnPropertyChanged(nameof(HasNoItems));
    }

    private void Select_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedItem != null)
        {
            Result = SelectedItem;
            DialogResult = true;
            Close();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Shows the dialog and returns the selected item, or null if cancelled.
    /// </summary>
    public static InventoryItem? ShowDialog(Window owner, IEnumerable<InventoryItem> items)
    {
        var dialog = new InventoryPickerDialog(items)
        {
            Owner = owner
        };

        return dialog.ShowDialog() == true ? dialog.Result : null;
    }
}
