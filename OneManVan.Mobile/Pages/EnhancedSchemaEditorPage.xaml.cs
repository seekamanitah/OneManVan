using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Controls.Shapes;
using OneManVan.Shared.Services;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;

namespace OneManVan.Mobile.Pages;

public partial class EnhancedSchemaEditorPage : ContentPage
{
    private readonly IDbContextFactory<OneManVanDbContext> _dbFactory;
    private readonly SchemaImportExportService _importExportService;
    private ObservableCollection<CustomFieldDefinition> _fields = [];
    private string _selectedEntityType = "Customer";

    public EnhancedSchemaEditorPage(IDbContextFactory<OneManVanDbContext> dbFactory)
    {
        InitializeComponent();
        _dbFactory = dbFactory;
        
        // Create service with temp context (it will create its own as needed)
        using var tempDb = dbFactory.CreateDbContext();
        _importExportService = new SchemaImportExportService(tempDb);
        
        BindingContext = this;
        EntityTypePicker.SelectedIndex = 0;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadFieldsAsync();
    }

    private async void OnEntityTypeChanged(object? sender, EventArgs e)
    {
        if (EntityTypePicker.SelectedItem is string entityType)
        {
            _selectedEntityType = entityType;
            await LoadFieldsAsync();
        }
    }

    private async Task LoadFieldsAsync()
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            var fields = await db.CustomFieldDefinitions
                .Include(f => f.Choices)
                .Where(f => f.EntityType == _selectedEntityType)
                .OrderBy(f => f.DisplayOrder)
                .ToListAsync();

            _fields = new ObservableCollection<CustomFieldDefinition>(fields);
            FieldsCollection.ItemsSource = _fields;

            EmptyState.IsVisible = !_fields.Any();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load fields: {ex.Message}", "OK");
        }
    }

    private async void OnAddFieldClick(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new FieldEditorPage(_dbFactory, _selectedEntityType, null));
    }

    private async void OnFieldTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border border && border.BindingContext is CustomFieldDefinition field)
        {
            await Navigation.PushAsync(new FieldEditorPage(_dbFactory, _selectedEntityType, field));
        }
    }

    private async void OnMenuClick(object? sender, EventArgs e)
    {
        var action = await DisplayActionSheetAsync("Schema Options", "Cancel", null, "Export Schema", "Import Schema");

        if (action == "Export Schema")
        {
            await ExportSchemaAsync();
        }
        else if (action == "Import Schema")
        {
            await ImportSchemaAsync();
        }
    }

    private async Task ExportSchemaAsync()
    {
        try
        {
            var json = await _importExportService.ExportSchemaAsync(_selectedEntityType);
            var fileName = $"{_selectedEntityType}_Schema_{DateTime.Now:yyyyMMdd}.json";
            var filePath = System.IO.Path.Combine(FileSystem.CacheDirectory, fileName);
            
            await File.WriteAllTextAsync(filePath, json);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Export Schema",
                File = new ShareFile(filePath)
            });

            await DisplayAlertAsync("Export Complete", $"Schema exported for {_selectedEntityType}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Export Failed", ex.Message, "OK");
        }
    }

    private async Task ImportSchemaAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select Schema File",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.iOS, new[] { "public.json" } },
                    { DevicePlatform.Android, new[] { "application/json" } },
                    { DevicePlatform.WinUI, new[] { ".json" } }
                })
            });

            if (result == null)
                return;

            var json = await File.ReadAllTextAsync(result.FullPath);
            
            var replaceExisting = await DisplayAlertAsync(
                "Import Options",
                "Replace existing fields?",
                "Replace All",
                "Keep Existing"
            );

            var importResult = await _importExportService.ImportSchemaAsync(json, replaceExisting);

            if (importResult.Success)
            {
                var message = $"Imported: {importResult.ImportedFields.Count} fields";
                if (importResult.SkippedFields.Any())
                {
                    message += $"\nSkipped: {importResult.SkippedFields.Count} fields";
                }

                await DisplayAlertAsync("Import Complete", message, "OK");
                await LoadFieldsAsync();
            }
            else
            {
                await DisplayAlertAsync("Import Failed", importResult.ErrorMessage, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Import Failed", ex.Message, "OK");
        }
    }
}

/// <summary>
/// Field editor page for creating/editing custom fields.
/// </summary>
public class FieldEditorPage : ContentPage
{
    private readonly IDbContextFactory<OneManVanDbContext> _dbFactory;
    private readonly string _entityType;
    private readonly CustomFieldDefinition? _field;
    private readonly bool _isNew;

    private Entry _displayNameEntry = null!;
    private Picker _fieldTypePicker = null!;
    private Switch _requiredSwitch = null!;
    private Switch _readOnlySwitch = null!;
    private Entry _defaultValueEntry = null!;
    private Editor _helpTextEditor = null!;
    private Entry _validationRegexEntry = null!;
    private Entry _minValueEntry = null!;
    private Entry _maxValueEntry = null!;
    private Entry _minLengthEntry = null!;
    private Entry _maxLengthEntry = null!;
    private Entry _groupNameEntry = null!;
    private VerticalStackLayout _choicesSection = null!;
    private ObservableCollection<CustomFieldChoice> _choices = [];

    public FieldEditorPage(IDbContextFactory<OneManVanDbContext> dbFactory, string entityType, CustomFieldDefinition? field)
    {
        _dbFactory = dbFactory;
        _entityType = entityType;
        _field = field;
        _isNew = field == null;

        Title = _isNew ? "Add Field" : "Edit Field";
        BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Dark 
            ? Color.FromArgb("#121212") 
            : Color.FromArgb("#F5F5F5");

        BuildUI();
        
        if (!_isNew && _field != null)
        {
            LoadField();
        }
    }

    private void BuildUI()
    {
        var scrollView = new ScrollView();
        var mainStack = new VerticalStackLayout
        {
            Padding = 16,
            Spacing = 16
        };

        // Display Name
        mainStack.Add(CreateLabel("Display Name", true));
        _displayNameEntry = new Entry
        {
            Placeholder = "Enter field name",
            BackgroundColor = Colors.White
        };
        mainStack.Add(_displayNameEntry);

        // Field Type
        mainStack.Add(CreateLabel("Field Type", true));
        _fieldTypePicker = new Picker
        {
            ItemsSource = new List<string> { "Text", "TextArea", "Number", "Decimal", "Date", "DateTime", "Checkbox", "Dropdown", "MultiSelect", "Radio", "Email", "Phone", "Url", "Currency" },
            BackgroundColor = Colors.White
        };
        _fieldTypePicker.SelectedIndexChanged += OnFieldTypeChanged;
        mainStack.Add(_fieldTypePicker);

        // Required & Read Only
        var switchStack = new HorizontalStackLayout { Spacing = 20 };
        _requiredSwitch = new Switch();
        switchStack.Add(new Label { Text = "Required", VerticalOptions = LayoutOptions.Center });
        switchStack.Add(_requiredSwitch);
        
        _readOnlySwitch = new Switch();
        switchStack.Add(new Label { Text = "Read Only", VerticalOptions = LayoutOptions.Center, Margin = new Thickness(20, 0, 0, 0) });
        switchStack.Add(_readOnlySwitch);
        mainStack.Add(switchStack);

        // Default Value
        mainStack.Add(CreateLabel("Default Value"));
        _defaultValueEntry = new Entry { Placeholder = "Optional", BackgroundColor = Colors.White };
        mainStack.Add(_defaultValueEntry);

        // Help Text
        mainStack.Add(CreateLabel("Help Text"));
        _helpTextEditor = new Editor { Placeholder = "Optional hint for users", HeightRequest = 80, BackgroundColor = Colors.White };
        mainStack.Add(_helpTextEditor);

        // Validation Section
        var validationFrame = new Border
        {
            BackgroundColor = Colors.LightGray.WithAlpha(0.3f),
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Padding = 12,
            Stroke = Colors.Transparent
        };
        var validationStack = new VerticalStackLayout { Spacing = 12 };
        validationStack.Add(CreateLabel("Validation Rules", false, 16, true));

        validationStack.Add(CreateLabel("Regex Pattern"));
        _validationRegexEntry = new Entry { Placeholder = "Optional regex", BackgroundColor = Colors.White };
        validationStack.Add(_validationRegexEntry);

        var numberValidation = new VerticalStackLayout { Spacing = 8 };
        var numberGrid = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition(), new ColumnDefinition() }, ColumnSpacing = 12 };
        var minStack = new VerticalStackLayout { Spacing = 4 };
        minStack.Add(CreateLabel("Min Value", false, 12));
        _minValueEntry = new Entry { Keyboard = Keyboard.Numeric, BackgroundColor = Colors.White };
        minStack.Add(_minValueEntry);
        numberGrid.Add(minStack, 0, 0);
        
        var maxStack = new VerticalStackLayout { Spacing = 4 };
        maxStack.Add(CreateLabel("Max Value", false, 12));
        _maxValueEntry = new Entry { Keyboard = Keyboard.Numeric, BackgroundColor = Colors.White };
        maxStack.Add(_maxValueEntry);
        numberGrid.Add(maxStack, 1, 0);
        validationStack.Add(numberGrid);

        var lengthValidation = new VerticalStackLayout { Spacing = 8 };
        var lengthGrid = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition(), new ColumnDefinition() }, ColumnSpacing = 12 };
        var minLenStack = new VerticalStackLayout { Spacing = 4 };
        minLenStack.Add(CreateLabel("Min Length", false, 12));
        _minLengthEntry = new Entry { Keyboard = Keyboard.Numeric, BackgroundColor = Colors.White };
        minLenStack.Add(_minLengthEntry);
        lengthGrid.Add(minLenStack, 0, 0);
        
        var maxLenStack = new VerticalStackLayout { Spacing = 4 };
        maxLenStack.Add(CreateLabel("Max Length", false, 12));
        _maxLengthEntry = new Entry { Keyboard = Keyboard.Numeric, BackgroundColor = Colors.White };
        maxLenStack.Add(_maxLengthEntry);
        lengthGrid.Add(maxLenStack, 1, 0);
        validationStack.Add(lengthGrid);

        validationFrame.Content = validationStack;
        mainStack.Add(validationFrame);

        // Field Group
        mainStack.Add(CreateLabel("Field Group (Optional)"));
        _groupNameEntry = new Entry { Placeholder = "e.g., Contact Information", BackgroundColor = Colors.White };
        mainStack.Add(_groupNameEntry);

        // Choices Section
        _choicesSection = new VerticalStackLayout { Spacing = 12, IsVisible = false };
        _choicesSection.Add(CreateLabel("Dropdown Choices", false, 16, true));
        
        var addChoiceBtn = new Button
        {
            Text = "+ Add Choice",
            BackgroundColor = Color.FromArgb("#9C27B0"),
            TextColor = Colors.White,
            CornerRadius = 8
        };
        addChoiceBtn.Clicked += OnAddChoiceClick;
        _choicesSection.Add(addChoiceBtn);
        mainStack.Add(_choicesSection);

        // Save Button
        var saveBtn = new Button
        {
            Text = _isNew ? "Create Field" : "Save Changes",
            BackgroundColor = Color.FromArgb("#4CAF50"),
            TextColor = Colors.White,
            CornerRadius = 8,
            Padding = new Thickness(0, 12),
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 20, 0, 0)
        };
        saveBtn.Clicked += OnSaveClick;
        mainStack.Add(saveBtn);

        scrollView.Content = mainStack;
        Content = scrollView;
    }

    private Label CreateLabel(string text, bool required = false, double fontSize = 14, bool bold = false)
    {
        return new Label
        {
            Text = required ? $"{text} *" : text,
            FontSize = fontSize,
            FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
            TextColor = required ? Color.FromArgb("#D32F2F") : (Application.Current?.RequestedTheme == AppTheme.Dark ? Colors.White : Colors.Black)
        };
    }

    private void LoadField()
    {
        if (_field == null) return;

        _displayNameEntry.Text = _field.DisplayName;
        _fieldTypePicker.SelectedItem = _field.FieldType;
        _requiredSwitch.IsToggled = _field.IsRequired;
        _readOnlySwitch.IsToggled = _field.IsReadOnly;
        _defaultValueEntry.Text = _field.DefaultValue;
        _helpTextEditor.Text = _field.HelpText;
        _validationRegexEntry.Text = _field.ValidationRegex;
        _minValueEntry.Text = _field.MinValue?.ToString();
        _maxValueEntry.Text = _field.MaxValue?.ToString();
        _minLengthEntry.Text = _field.MinLength?.ToString();
        _maxLengthEntry.Text = _field.MaxLength?.ToString();
        _groupNameEntry.Text = _field.GroupName;

        if (_field.FieldType == "Dropdown" || _field.FieldType == "MultiSelect" || _field.FieldType == "Radio")
        {
            _choices = new ObservableCollection<CustomFieldChoice>(_field.Choices.OrderBy(c => c.DisplayOrder));
            UpdateChoicesUI();
        }
    }

    private void OnFieldTypeChanged(object? sender, EventArgs e)
    {
        var fieldType = _fieldTypePicker.SelectedItem as string;
        _choicesSection.IsVisible = fieldType == "Dropdown" || fieldType == "MultiSelect" || fieldType == "Radio";
    }

    private void OnAddChoiceClick(object? sender, EventArgs e)
    {
        var choice = new CustomFieldChoice
        {
            DisplayText = "New Choice",
            Value = "new_choice",
            DisplayOrder = _choices.Count
        };
        _choices.Add(choice);
        UpdateChoicesUI();
    }

    private void UpdateChoicesUI()
    {
        // Clear existing choices UI (except header and add button)
        while (_choicesSection.Children.Count > 2)
        {
            _choicesSection.Children.RemoveAt(2);
        }

        foreach (var choice in _choices)
        {
            var choiceFrame = new Border
            {
                BackgroundColor = Colors.White,
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                Padding = 12,
                Stroke = Colors.LightGray
            };

            var choiceStack = new VerticalStackLayout { Spacing = 8 };
            
            var displayEntry = new Entry { Text = choice.DisplayText, Placeholder = "Display Text" };
            displayEntry.TextChanged += (s, e) => choice.DisplayText = e.NewTextValue;
            choiceStack.Add(displayEntry);

            var valueEntry = new Entry { Text = choice.Value, Placeholder = "Value" };
            valueEntry.TextChanged += (s, e) => choice.Value = e.NewTextValue;
            choiceStack.Add(valueEntry);

            var bottomStack = new HorizontalStackLayout { Spacing = 12 };
            var defaultSwitch = new Switch { IsToggled = choice.IsDefault };
            defaultSwitch.Toggled += (s, e) => choice.IsDefault = e.Value;
            bottomStack.Add(new Label { Text = "Default", VerticalOptions = LayoutOptions.Center });
            bottomStack.Add(defaultSwitch);

            var deleteBtn = new Button
            {
                Text = "Delete",
                BackgroundColor = Color.FromArgb("#F44336"),
                TextColor = Colors.White,
                CornerRadius = 4,
                Padding = new Thickness(12, 4),
                HorizontalOptions = LayoutOptions.End
            };
            deleteBtn.Clicked += (s, e) =>
            {
                _choices.Remove(choice);
                UpdateChoicesUI();
            };
            bottomStack.Add(deleteBtn);

            choiceStack.Add(bottomStack);
            choiceFrame.Content = choiceStack;
            _choicesSection.Add(choiceFrame);
        }
    }

    private async void OnSaveClick(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_displayNameEntry.Text))
        {
            await DisplayAlertAsync("Validation", "Display Name is required", "OK");
            return;
        }

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            var field = _field ?? new CustomFieldDefinition
            {
                EntityType = _entityType,
                DisplayOrder = await db.CustomFieldDefinitions.CountAsync(f => f.EntityType == _entityType)
            };

            field.DisplayName = _displayNameEntry.Text;
            field.FieldName = _displayNameEntry.Text.Replace(" ", "");
            field.FieldType = _fieldTypePicker.SelectedItem as string ?? "Text";
            field.IsRequired = _requiredSwitch.IsToggled;
            field.IsReadOnly = _readOnlySwitch.IsToggled;
            field.DefaultValue = _defaultValueEntry.Text;
            field.HelpText = _helpTextEditor.Text;
            field.ValidationRegex = _validationRegexEntry.Text;
            field.GroupName = _groupNameEntry.Text;

            if (decimal.TryParse(_minValueEntry.Text, out var minVal))
                field.MinValue = minVal;
            if (decimal.TryParse(_maxValueEntry.Text, out var maxVal))
                field.MaxValue = maxVal;
            if (int.TryParse(_minLengthEntry.Text, out var minLen))
                field.MinLength = minLen;
            if (int.TryParse(_maxLengthEntry.Text, out var maxLen))
                field.MaxLength = maxLen;

            if (_isNew)
            {
                db.CustomFieldDefinitions.Add(field);
            }

            // Save choices
            if (field.FieldType == "Dropdown" || field.FieldType == "MultiSelect" || field.FieldType == "Radio")
            {
                var existingChoices = await db.CustomFieldChoices
                    .Where(c => c.FieldDefinitionId == field.Id)
                    .ToListAsync();
                db.CustomFieldChoices.RemoveRange(existingChoices);

                foreach (var choice in _choices)
                {
                    choice.FieldDefinitionId = field.Id;
                    db.CustomFieldChoices.Add(choice);
                }
            }

            await db.SaveChangesAsync();
            await DisplayAlertAsync("Success", "Field saved successfully!", "OK");
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to save field: {ex.Message}", "OK");
        }
    }
}
