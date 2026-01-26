using System.Windows;
using System.Windows.Controls;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;
using OneManVan.Services;

namespace OneManVan.Pages;

/// <summary>
/// Code-behind for the Schema Editor page.
/// </summary>
public partial class SchemaEditorPage : UserControl
{
    private readonly SchemaEditorService _schemaService = new();
    private string _selectedEntityType = string.Empty;
    private SchemaDefinition? _editingField;
    private List<SchemaDefinition> _currentFields = [];

    public SchemaEditorPage()
    {
        InitializeComponent();
        Loaded += SchemaEditorPage_Loaded;
    }

    private async void SchemaEditorPage_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadEntityTypesAsync();
    }

    private async Task LoadEntityTypesAsync()
    {
        var fieldCounts = await _schemaService.GetFieldCountsByEntityTypeAsync();

        var entityTypes = SchemaEntityTypes.All.Select(et => new EntityTypeItem
        {
            Name = et,
            FieldCount = fieldCounts.GetValueOrDefault(et, 0),
            HasFields = fieldCounts.GetValueOrDefault(et, 0) > 0
        }).ToList();

        EntityTypeList.ItemsSource = entityTypes;

        if (entityTypes.Count > 0)
        {
            EntityTypeList.SelectedIndex = 0;
        }
    }

    private async void EntityTypeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (EntityTypeList.SelectedItem is EntityTypeItem item)
        {
            _selectedEntityType = item.Name;
            await LoadFieldsAsync();
            HideEditor();
        }
    }

    private async Task LoadFieldsAsync()
    {
        if (string.IsNullOrEmpty(_selectedEntityType))
        {
            return;
        }

        _currentFields = await _schemaService.GetSchemaDefinitionsAsync(_selectedEntityType);

        SelectedEntityText.Text = $"{_selectedEntityType} Fields";
        FieldCountText.Text = $"{_currentFields.Count} custom field{(_currentFields.Count != 1 ? "s" : "")}";

        FieldsList.ItemsSource = _currentFields;

        EmptyState.Visibility = _currentFields.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void AddField_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedEntityType))
        {
            MessageBox.Show("Please select an entity type first.", "Select Entity Type",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _editingField = null;
        ShowEditor("Add Field");
        ClearEditorInputs();
        FieldTypeCombo.SelectedIndex = 0;
    }

    private void EditField_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int fieldId)
        {
            var field = _currentFields.FirstOrDefault(f => f.Id == fieldId);
            if (field != null)
            {
                _editingField = field;
                ShowEditor("Edit Field");
                PopulateEditor(field);
            }
        }
    }

    private async void DeleteField_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int fieldId)
        {
            var field = _currentFields.FirstOrDefault(f => f.Id == fieldId);
            if (field == null) return;

            var result = MessageBox.Show(
                $"Delete field '{field.DisplayLabel}'?\n\nThis will also delete all stored values for this field.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _schemaService.DeleteSchemaDefinitionAsync(fieldId);
                    await LoadFieldsAsync();
                    await LoadEntityTypesAsync(); // Refresh counts
                    HideEditor();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to delete field: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private async void SaveField_Click(object sender, RoutedEventArgs e)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(FieldNameInput.Text))
        {
            MessageBox.Show("Field Name is required.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            FieldNameInput.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(DisplayLabelInput.Text))
        {
            MessageBox.Show("Display Label is required.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            DisplayLabelInput.Focus();
            return;
        }

        var fieldType = (CustomFieldType)(FieldTypeCombo.SelectedIndex);

        if (fieldType == CustomFieldType.Enum && string.IsNullOrWhiteSpace(EnumOptionsInput.Text))
        {
            MessageBox.Show("Dropdown options are required for Dropdown fields.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            EnumOptionsInput.Focus();
            return;
        }

        try
        {
            var definition = _editingField ?? new SchemaDefinition();

            definition.EntityType = _selectedEntityType;
            definition.FieldName = FieldNameInput.Text.Trim().Replace(" ", "");
            definition.DisplayLabel = DisplayLabelInput.Text.Trim();
            definition.FieldType = fieldType;
            definition.DefaultValue = string.IsNullOrWhiteSpace(DefaultValueInput.Text) ? null : DefaultValueInput.Text.Trim();
            definition.Placeholder = string.IsNullOrWhiteSpace(PlaceholderInput.Text) ? null : PlaceholderInput.Text.Trim();
            definition.Description = string.IsNullOrWhiteSpace(DescriptionInput.Text) ? null : DescriptionInput.Text.Trim();
            definition.IsRequired = IsRequiredCheck.IsChecked == true;

            // Handle enum options
            if (fieldType == CustomFieldType.Enum)
            {
                var options = EnumOptionsInput.Text
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();
                definition.EnumOptions = string.Join(",", options);
            }
            else
            {
                definition.EnumOptions = null;
            }

            // Handle number validation
            if (fieldType == CustomFieldType.Number || fieldType == CustomFieldType.Decimal)
            {
                if (decimal.TryParse(MinValueInput.Text, out decimal minVal))
                    definition.MinValue = minVal;
                else
                    definition.MinValue = null;

                if (decimal.TryParse(MaxValueInput.Text, out decimal maxVal))
                    definition.MaxValue = maxVal;
                else
                    definition.MaxValue = null;
            }
            else
            {
                definition.MinValue = null;
                definition.MaxValue = null;
            }

            // Handle text max length
            if (fieldType == CustomFieldType.Text)
            {
                if (int.TryParse(MaxLengthInput.Text, out int maxLen))
                    definition.MaxLength = maxLen;
                else
                    definition.MaxLength = null;
            }
            else
            {
                definition.MaxLength = null;
            }

            // Save
            if (_editingField == null)
            {
                await _schemaService.CreateSchemaDefinitionAsync(definition);
            }
            else
            {
                await _schemaService.UpdateSchemaDefinitionAsync(definition);
            }

            await LoadFieldsAsync();
            await LoadEntityTypesAsync(); // Refresh counts
            HideEditor();

            MessageBox.Show("Field saved successfully!", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save field: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelEdit_Click(object sender, RoutedEventArgs e)
    {
        HideEditor();
    }

    private void FieldTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FieldTypeCombo.SelectedIndex < 0) return;

        var fieldType = (CustomFieldType)FieldTypeCombo.SelectedIndex;

        // Show/hide type-specific panels
        EnumOptionsPanel.Visibility = fieldType == CustomFieldType.Enum ? Visibility.Visible : Visibility.Collapsed;
        NumberValidationPanel.Visibility = (fieldType == CustomFieldType.Number || fieldType == CustomFieldType.Decimal)
            ? Visibility.Visible : Visibility.Collapsed;
        TextValidationPanel.Visibility = fieldType == CustomFieldType.Text ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ShowEditor(string title)
    {
        EditorTitle.Text = title;
        EditorPanel.Visibility = Visibility.Visible;
        EditorEmptyState.Visibility = Visibility.Collapsed;
    }

    private void HideEditor()
    {
        EditorPanel.Visibility = Visibility.Collapsed;
        EditorEmptyState.Visibility = Visibility.Visible;
        _editingField = null;
    }

    private void ClearEditorInputs()
    {
        FieldNameInput.Text = string.Empty;
        DisplayLabelInput.Text = string.Empty;
        EnumOptionsInput.Text = string.Empty;
        MinValueInput.Text = string.Empty;
        MaxValueInput.Text = string.Empty;
        MaxLengthInput.Text = string.Empty;
        DefaultValueInput.Text = string.Empty;
        PlaceholderInput.Text = string.Empty;
        DescriptionInput.Text = string.Empty;
        IsRequiredCheck.IsChecked = false;
    }

    private void PopulateEditor(SchemaDefinition field)
    {
        FieldNameInput.Text = field.FieldName;
        DisplayLabelInput.Text = field.DisplayLabel;
        FieldTypeCombo.SelectedIndex = (int)field.FieldType;

        if (!string.IsNullOrEmpty(field.EnumOptions))
        {
            EnumOptionsInput.Text = string.Join("\n", field.EnumOptionsList);
        }
        else
        {
            EnumOptionsInput.Text = string.Empty;
        }

        MinValueInput.Text = field.MinValue?.ToString() ?? string.Empty;
        MaxValueInput.Text = field.MaxValue?.ToString() ?? string.Empty;
        MaxLengthInput.Text = field.MaxLength?.ToString() ?? string.Empty;
        DefaultValueInput.Text = field.DefaultValue ?? string.Empty;
        PlaceholderInput.Text = field.Placeholder ?? string.Empty;
        DescriptionInput.Text = field.Description ?? string.Empty;
        IsRequiredCheck.IsChecked = field.IsRequired;
    }
}

/// <summary>
/// View model for entity type list items.
/// </summary>
public class EntityTypeItem
{
    public string Name { get; set; } = string.Empty;
    public int FieldCount { get; set; }
    public bool HasFields { get; set; }
}
