using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using OneManVan.Shared.Services;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;

namespace OneManVan.Pages;

public partial class EnhancedSchemaEditorPage : UserControl
{
    private readonly OneManVanDbContext _dbContext;
    private readonly SchemaImportExportService _importExportService;
    private ObservableCollection<CustomFieldDefinition> _fields = [];
    private ObservableCollection<CustomFieldChoice> _currentChoices = [];
    private CustomFieldDefinition? _selectedField;
    private bool _isNewField;

    public EnhancedSchemaEditorPage()
    {
        InitializeComponent();
        _dbContext = App.DbContext;
        _importExportService = new SchemaImportExportService(_dbContext);
        
        Loaded += OnPageLoaded;
    }

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        await LoadEntityTypesAsync();
        await LoadFieldTypesAsync();
    }

    private async Task LoadEntityTypesAsync()
    {
        var entityTypes = new List<string>
        {
            "Customer",
            "Asset",
            "Job",
            "Invoice",
            "Estimate",
            "Product",
            "InventoryItem",
            "ServiceAgreement"
        };

        EntityTypeCombo.ItemsSource = entityTypes;
        EntityTypeCombo.SelectedIndex = 0;
    }

    private Task LoadFieldTypesAsync()
    {
        var fieldTypes = new List<string>
        {
            "Text",
            "TextArea",
            "Number",
            "Decimal",
            "Date",
            "DateTime",
            "Checkbox",
            "Dropdown",
            "MultiSelect",
            "Radio",
            "Email",
            "Phone",
            "Url",
            "Currency"
        };

        FieldTypeCombo.ItemsSource = fieldTypes;
        return Task.CompletedTask;
    }

    private async void EntityTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (EntityTypeCombo.SelectedItem is string entityType)
        {
            await LoadFieldsAsync(entityType);
            await LoadGroupNamesAsync(entityType);
        }
    }

    private async Task LoadGroupNamesAsync(string entityType)
    {
        try
        {
            var groupNames = await _dbContext.CustomFieldDefinitions
                .Where(f => f.EntityType == entityType && !string.IsNullOrEmpty(f.GroupName))
                .Select(f => f.GroupName)
                .Distinct()
                .ToListAsync();

            GroupNameCombo.ItemsSource = groupNames;
        }
        catch
        {
            // Silently handle - not critical
        }
    }

    private async Task LoadFieldsAsync(string entityType)
    {
        try
        {
            _fields = new ObservableCollection<CustomFieldDefinition>(
                await _dbContext.CustomFieldDefinitions
                    .Include(f => f.Choices)
                    .Where(f => f.EntityType == entityType)
                    .OrderBy(f => f.DisplayOrder)
                    .ToListAsync()
            );

            FieldsList.ItemsSource = _fields;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load fields: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnFieldClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.DataContext is CustomFieldDefinition field)
        {
            LoadFieldForEditing(field);
        }
    }

    private void OnEditFieldClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is CustomFieldDefinition field)
        {
            LoadFieldForEditing(field);
        }
    }

    private void LoadFieldForEditing(CustomFieldDefinition field)
    {
        _selectedField = field;
        _isNewField = false;

        // Show editor panel
        EditorScrollViewer.Visibility = Visibility.Visible;
        EmptyState.Visibility = Visibility.Collapsed;

        // Load field properties
        DisplayNameTextBox.Text = field.DisplayName;
        FieldTypeCombo.SelectedItem = field.FieldType;
        IsRequiredCheckBox.IsChecked = field.IsRequired;
        IsReadOnlyCheckBox.IsChecked = field.IsReadOnly;
        DefaultValueTextBox.Text = field.DefaultValue;
        HelpTextTextBox.Text = field.HelpText;

        // Load validation rules
        ValidationRegexTextBox.Text = field.ValidationRegex;
        MinValueTextBox.Text = field.MinValue?.ToString();
        MaxValueTextBox.Text = field.MaxValue?.ToString();
        MinLengthTextBox.Text = field.MinLength?.ToString();
        MaxLengthTextBox.Text = field.MaxLength?.ToString();

        // Load group name
        GroupNameCombo.Text = field.GroupName;

        // Load choices if dropdown/multiselect
        LoadChoices(field);
        UpdateValidationVisibility(field.FieldType);
    }

    private void LoadChoices(CustomFieldDefinition field)
    {
        if (field.FieldType == "Dropdown" || field.FieldType == "MultiSelect" || field.FieldType == "Radio")
        {
            _currentChoices = new ObservableCollection<CustomFieldChoice>(
                field.Choices.OrderBy(c => c.DisplayOrder)
            );
            ChoicesList.ItemsSource = _currentChoices;
            ChoicesSection.Visibility = Visibility.Visible;
        }
        else
        {
            ChoicesSection.Visibility = Visibility.Collapsed;
        }
    }

    private void FieldTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FieldTypeCombo.SelectedItem is string fieldType)
        {
            UpdateValidationVisibility(fieldType);
        }
    }

    private void UpdateValidationVisibility(string fieldType)
    {
        // Show choices section for dropdown types
        ChoicesSection.Visibility = (fieldType == "Dropdown" || fieldType == "MultiSelect" || fieldType == "Radio")
            ? Visibility.Visible
            : Visibility.Collapsed;

        // Show number validation for number/decimal types
        NumberValidationSection.Visibility = (fieldType == "Number" || fieldType == "Decimal" || fieldType == "Currency")
            ? Visibility.Visible
            : Visibility.Collapsed;

        // Show length validation for text types
        LengthValidationSection.Visibility = (fieldType == "Text" || fieldType == "TextArea" || fieldType == "Email" || fieldType == "Phone" || fieldType == "Url")
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void OnAddFieldClick(object sender, RoutedEventArgs e)
    {
        if (EntityTypeCombo.SelectedItem is not string entityType)
        {
            MessageBox.Show("Please select an entity type first", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _selectedField = new CustomFieldDefinition
        {
            EntityType = entityType,
            FieldType = "Text",
            DisplayOrder = _fields.Count
        };
        _isNewField = true;

        // Show editor panel
        EditorScrollViewer.Visibility = Visibility.Visible;
        EmptyState.Visibility = Visibility.Collapsed;

        // Clear form
        DisplayNameTextBox.Text = string.Empty;
        FieldTypeCombo.SelectedIndex = 0;
        IsRequiredCheckBox.IsChecked = false;
        IsReadOnlyCheckBox.IsChecked = false;
        DefaultValueTextBox.Text = string.Empty;
        HelpTextTextBox.Text = string.Empty;
        _currentChoices.Clear();
        ChoicesSection.Visibility = Visibility.Collapsed;
    }

    private async void OnSaveFieldClick(object sender, RoutedEventArgs e)
    {
        if (_selectedField == null) return;

        if (string.IsNullOrWhiteSpace(DisplayNameTextBox.Text))
        {
            MessageBox.Show("Display Name is required", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            _selectedField.DisplayName = DisplayNameTextBox.Text;
            _selectedField.FieldName = DisplayNameTextBox.Text.Replace(" ", "");
            _selectedField.FieldType = FieldTypeCombo.SelectedItem as string ?? "Text";
            _selectedField.IsRequired = IsRequiredCheckBox.IsChecked == true;
            _selectedField.IsReadOnly = IsReadOnlyCheckBox.IsChecked == true;
            _selectedField.DefaultValue = DefaultValueTextBox.Text;
            _selectedField.HelpText = HelpTextTextBox.Text;
            _selectedField.GroupName = GroupNameCombo.Text;

            // Save validation rules
            _selectedField.ValidationRegex = ValidationRegexTextBox.Text;
            
            if (decimal.TryParse(MinValueTextBox.Text, out var minVal))
                _selectedField.MinValue = minVal;
            else
                _selectedField.MinValue = null;

            if (decimal.TryParse(MaxValueTextBox.Text, out var maxVal))
                _selectedField.MaxValue = maxVal;
            else
                _selectedField.MaxValue = null;

            if (int.TryParse(MinLengthTextBox.Text, out var minLen))
                _selectedField.MinLength = minLen;
            else
                _selectedField.MinLength = null;

            if (int.TryParse(MaxLengthTextBox.Text, out var maxLen))
                _selectedField.MaxLength = maxLen;
            else
                _selectedField.MaxLength = null;

            if (_isNewField)
            {
                _dbContext.CustomFieldDefinitions.Add(_selectedField);
                _fields.Add(_selectedField);
            }

            // Save choices
            if (_selectedField.FieldType == "Dropdown" || _selectedField.FieldType == "MultiSelect" || _selectedField.FieldType == "Radio")
            {
                // Remove old choices
                var existingChoices = await _dbContext.CustomFieldChoices
                    .Where(c => c.FieldDefinitionId == _selectedField.Id)
                    .ToListAsync();
                _dbContext.CustomFieldChoices.RemoveRange(existingChoices);

                // Add new choices
                foreach (var choice in _currentChoices)
                {
                    choice.FieldDefinitionId = _selectedField.Id;
                    _dbContext.CustomFieldChoices.Add(choice);
                }
            }

            await _dbContext.SaveChangesAsync();
            MessageBox.Show("Field saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

            // Reload
            if (EntityTypeCombo.SelectedItem is string entityType)
            {
                await LoadFieldsAsync(entityType);
                await LoadGroupNamesAsync(entityType);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save field: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnAddChoiceClick(object sender, RoutedEventArgs e)
    {
        var newChoice = new CustomFieldChoice
        {
            DisplayText = "New Choice",
            Value = "new_choice",
            DisplayOrder = _currentChoices.Count
        };
        _currentChoices.Add(newChoice);
    }

    private void OnDeleteChoiceClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is CustomFieldChoice choice)
        {
            _currentChoices.Remove(choice);
        }
    }

    private async void OnDeleteFieldClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is CustomFieldDefinition field)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to delete the field '{field.DisplayName}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _dbContext.CustomFieldDefinitions.Remove(field);
                    await _dbContext.SaveChangesAsync();
                    _fields.Remove(field);

                    MessageBox.Show("Field deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to delete field: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private void OnImportClick(object sender, RoutedEventArgs e)
    {
        var openDialog = new OpenFileDialog
        {
            Title = "Import Schema",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            DefaultExt = ".json"
        };

        if (openDialog.ShowDialog() == true)
        {
            _ = ImportSchemaAsync(openDialog.FileName);
        }
    }

    private async Task ImportSchemaAsync(string filePath)
    {
        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            
            var result = MessageBox.Show(
                "Replace existing fields?\n\nYes = Replace all fields\nNo = Keep existing and add new\nCancel = Cancel import",
                "Import Options",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Cancel)
                return;

            var replaceExisting = result == MessageBoxResult.Yes;
            var importResult = await _importExportService.ImportSchemaAsync(json, replaceExisting);

            if (importResult.Success)
            {
                var message = $"Import complete!\n\nImported: {importResult.ImportedFields.Count} fields";
                if (importResult.SkippedFields.Any())
                {
                    message += $"\nSkipped: {importResult.SkippedFields.Count} fields (already exist)";
                }

                MessageBox.Show(message, "Import Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Reload fields
                if (EntityTypeCombo.SelectedItem is string entityType)
                {
                    await LoadFieldsAsync(entityType);
                }
            }
            else
            {
                MessageBox.Show($"Import failed: {importResult.ErrorMessage}", "Import Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to import schema: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnExportClick(object sender, RoutedEventArgs e)
    {
        if (EntityTypeCombo.SelectedItem is not string entityType)
        {
            MessageBox.Show("Please select an entity type to export", "Export",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var saveDialog = new SaveFileDialog
        {
            Title = "Export Schema",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            DefaultExt = ".json",
            FileName = $"{entityType}_Schema_{DateTime.Now:yyyyMMdd}.json"
        };

        if (saveDialog.ShowDialog() == true)
        {
            _ = ExportSchemaAsync(entityType, saveDialog.FileName);
        }
    }

    private async Task ExportSchemaAsync(string entityType, string filePath)
    {
        try
        {
            var json = await _importExportService.ExportSchemaAsync(entityType);
            await File.WriteAllTextAsync(filePath, json);

            MessageBox.Show($"Schema exported successfully to:\n{filePath}", "Export Complete",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to export schema: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
