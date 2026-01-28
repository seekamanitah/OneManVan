using Microsoft.EntityFrameworkCore;
using OneManVan.Mobile.Services;
using OneManVan.Shared.Data;
using OneManVan.Shared.Models;
using OneManVan.Shared.Models.Enums;
using OneManVan.Shared.Services;

namespace OneManVan.Mobile.Pages;

public partial class SchemaEditorPage : ContentPage
{
    private readonly IDbContextFactory<OneManVanDbContext> _dbFactory;
    private readonly TradeConfigurationService _tradeService;
    private CancellationTokenSource? _cts;

    public SchemaEditorPage(IDbContextFactory<OneManVanDbContext> dbFactory, TradeConfigurationService tradeService)
    {
        InitializeComponent();
        _dbFactory = dbFactory;
        _tradeService = tradeService;

        EntityTypePicker.SelectedIndex = 0;
        FieldTypePicker.SelectedIndex = 0;
        FieldTypePicker.SelectedIndexChanged += OnFieldTypeChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        
        var preset = _tradeService.CurrentPreset;
        TradeLabel.Text = $"{preset.Name} Trade Configuration";
        
        try
        {
            await LoadSchemaDefinitionsAsync(_cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Page navigated away
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SchemaEditorPage.OnAppearing error: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _cts?.Cancel();
    }

    private void OnFieldTypeChanged(object? sender, EventArgs e)
    {
        EnumOptionsStack.IsVisible = FieldTypePicker.SelectedItem?.ToString() == "Enum";
    }

    private async Task LoadSchemaDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            
            var definitions = await db.SchemaDefinitions
                .Where(s => s.IsActive)
                .OrderBy(s => s.DisplayOrder)
                .ThenBy(s => s.DisplayLabel)
                .ToListAsync(cancellationToken);

            // Group by entity type
            var assetFields = definitions.Where(d => d.EntityType == "Asset").ToList();
            var customerFields = definitions.Where(d => d.EntityType == "Customer").ToList();
            var siteFields = definitions.Where(d => d.EntityType == "Site").ToList();
            var jobFields = definitions.Where(d => d.EntityType == "Job").ToList();
            var estimateFields = definitions.Where(d => d.EntityType == "Estimate").ToList();
            var invoiceFields = definitions.Where(d => d.EntityType == "Invoice").ToList();

            if (!cancellationToken.IsCancellationRequested)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    AssetFieldsCollection.ItemsSource = assetFields;
                    AssetFieldCount.Text = $"({assetFields.Count})";

                    CustomerFieldsCollection.ItemsSource = customerFields;
                    CustomerFieldCount.Text = $"({customerFields.Count})";

                    SiteFieldsCollection.ItemsSource = siteFields;
                    SiteFieldCount.Text = $"({siteFields.Count})";

                    JobFieldsCollection.ItemsSource = jobFields;
                    JobFieldCount.Text = $"({jobFields.Count})";

                    EstimateFieldsCollection.ItemsSource = estimateFields;
                    EstimateFieldCount.Text = $"({estimateFields.Count})";

                    InvoiceFieldsCollection.ItemsSource = invoiceFields;
                    InvoiceFieldCount.Text = $"({invoiceFields.Count})";

                    var total = definitions.Count;
                    SyncInfoLabel.Text = total > 0
                        ? $"{total} custom field{(total != 1 ? "s" : "")} defined - Syncs with desktop"
                        : "No custom fields defined yet";
                });
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                await DisplayAlertAsync("Error", $"Failed to load schema: {ex.Message}", "OK");
            }
        }
    }

    private async void OnAddFieldClicked(object sender, EventArgs e)
    {
        // Validate
        if (EntityTypePicker.SelectedItem == null)
        {
            await DisplayAlertAsync("Required", "Please select an entity type.", "OK");
            return;
        }

        if (FieldTypePicker.SelectedItem == null)
        {
            await DisplayAlertAsync("Required", "Please select a field type.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(FieldNameEntry.Text))
        {
            await DisplayAlertAsync("Required", "Field name is required.", "OK");
            FieldNameEntry.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(DisplayLabelEntry.Text))
        {
            await DisplayAlertAsync("Required", "Display label is required.", "OK");
            DisplayLabelEntry.Focus();
            return;
        }

        var fieldTypeStr = FieldTypePicker.SelectedItem.ToString()!;
        if (fieldTypeStr == "Enum" && string.IsNullOrWhiteSpace(EnumOptionsEntry.Text))
        {
            await DisplayAlertAsync("Required", "Enum options are required.", "OK");
            EnumOptionsEntry.Focus();
            return;
        }

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            var entityType = EntityTypePicker.SelectedItem.ToString()!;
            var fieldName = FieldNameEntry.Text.Trim().Replace(" ", "");

            // Check for duplicate
            var exists = await db.SchemaDefinitions.AnyAsync(s => 
                s.EntityType == entityType && s.FieldName == fieldName && s.IsActive);

            if (exists)
            {
                await DisplayAlertAsync("Duplicate", $"A field named '{fieldName}' already exists for {entityType}.", "OK");
                return;
            }

            // Parse field type enum
            var fieldType = fieldTypeStr switch
            {
                "Text" => CustomFieldType.Text,
                "Number" => CustomFieldType.Number,
                "Decimal" => CustomFieldType.Decimal,
                "Boolean" => CustomFieldType.Boolean,
                "Date" => CustomFieldType.Date,
                "Enum" => CustomFieldType.Enum,
                _ => CustomFieldType.Text
            };

            var definition = new SchemaDefinition
            {
                EntityType = entityType,
                FieldName = fieldName,
                DisplayLabel = DisplayLabelEntry.Text.Trim(),
                FieldType = fieldType,
                IsRequired = RequiredSwitch.IsToggled,
                Description = HelpTextEntry.Text?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                DisplayOrder = await db.SchemaDefinitions.CountAsync(s => s.EntityType == entityType) + 1
            };

            if (fieldType == CustomFieldType.Enum)
            {
                definition.EnumOptions = EnumOptionsEntry.Text?.Trim();
            }

            db.SchemaDefinitions.Add(definition);
            await db.SaveChangesAsync();

            // Clear form
            FieldNameEntry.Text = string.Empty;
            DisplayLabelEntry.Text = string.Empty;
            HelpTextEntry.Text = string.Empty;
            EnumOptionsEntry.Text = string.Empty;
            RequiredSwitch.IsToggled = false;

            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            await LoadSchemaDefinitionsAsync(_cts.Token);
            await DisplayAlertAsync("Added", $"Custom field '{definition.DisplayLabel}' added to {entityType}.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to add field: {ex.Message}", "OK");
        }
    }

    private async void OnDeleteFieldSwipe(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.BindingContext is SchemaDefinition field)
        {
            var confirm = await DisplayAlertAsync(
                "Delete Field",
                $"Delete '{field.DisplayLabel}' from {field.EntityType}?\n\nExisting data using this field will be preserved but hidden.",
                "Delete", "Cancel");

            if (!confirm) return;

            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync();
                var dbField = await db.SchemaDefinitions.FindAsync(field.Id);
                if (dbField != null)
                {
                    dbField.IsActive = false;
                    dbField.ModifiedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }

                try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

                _cts?.Cancel();
                _cts = new CancellationTokenSource();
                await LoadSchemaDefinitionsAsync(_cts.Token);
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"Failed to delete: {ex.Message}", "OK");
            }
        }
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        await LoadSchemaDefinitionsAsync(_cts.Token);
        RefreshViewControl.IsRefreshing = false;
    }
}
