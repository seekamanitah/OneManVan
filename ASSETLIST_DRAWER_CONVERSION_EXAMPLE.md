# AssetListPage Drawer Conversion - Example Implementation

This demonstrates how to add a drawer to AssetListPage for adding/editing assets inline instead of navigating to a separate page.

---

## Current State (Before)

AssetListPage currently navigates to AddAssetPage when adding a new asset.

---

## Proposed State (After)

AssetListPage will have an embedded drawer that slides in from the right for adding/editing assets.

---

## Implementation Steps

### Step 1: Add Drawer to AssetListPage.xaml

**Add at the END of your page (after the closing tag of your main content):**

```xaml
</Grid>

<!-- Add Asset Drawer - Place this BEFORE the closing ContentPage tag -->
<controls:SlideInDrawer x:Name="AssetDrawer"
                        Title="Add Asset"
                        SaveButtonText="Save Asset"
                        SaveClicked="OnAssetDrawerSaveClicked"
                        CancelClicked="OnAssetDrawerCancelClicked">
    <controls:SlideInDrawer.DrawerContent>
        <VerticalStackLayout Spacing="16">
            <!-- Asset Name/Model -->
            <VerticalStackLayout Spacing="4">
                <Label Text="Asset Name *" 
                       FontSize="12" 
                       TextColor="{AppThemeBinding Light={StaticResource SectionPrimary}, Dark={StaticResource SectionPrimaryDark}}"
                       FontAttributes="Bold"/>
                <Entry x:Name="DrawerAssetNameEntry"
                       Placeholder="e.g., Carrier 24ACC636"
                       BackgroundColor="{AppThemeBinding Light={StaticResource LightInputBackground}, Dark={StaticResource DarkInputBackground}}"/>
            </VerticalStackLayout>

            <!-- Serial Number -->
            <VerticalStackLayout Spacing="4">
                <Label Text="Serial Number *" FontSize="12"/>
                <Entry x:Name="DrawerSerialEntry"
                       Placeholder="Serial number"
                       BackgroundColor="{AppThemeBinding Light={StaticResource LightInputBackground}, Dark={StaticResource DarkInputBackground}}"/>
            </VerticalStackLayout>

            <!-- Equipment Type -->
            <VerticalStackLayout Spacing="4">
                <Label Text="Equipment Type *" FontSize="12"/>
                <Picker x:Name="DrawerEquipmentTypePicker">
                    <Picker.ItemsSource>
                        <x:Array Type="{x:Type x:String}">
                            <x:String>AC Unit</x:String>
                            <x:String>Heat Pump</x:String>
                            <x:String>Furnace</x:String>
                            <x:String>Air Handler</x:String>
                            <x:String>Mini Split</x:String>
                        </x:Array>
                    </Picker.ItemsSource>
                </Picker>
            </VerticalStackLayout>

            <!-- Tonnage -->
            <Grid ColumnDefinitions="*,*" ColumnSpacing="12">
                <VerticalStackLayout Spacing="4">
                    <Label Text="Tonnage" FontSize="12"/>
                    <Entry x:Name="DrawerTonnageEntry"
                           Placeholder="e.g., 3"
                           Keyboard="Numeric"
                           BackgroundColor="{AppThemeBinding Light={StaticResource LightInputBackground}, Dark={StaticResource DarkInputBackground}}"/>
                </VerticalStackLayout>
                <VerticalStackLayout Grid.Column="1" Spacing="4">
                    <Label Text="SEER Rating" FontSize="12"/>
                    <Entry x:Name="DrawerSeerEntry"
                           Placeholder="e.g., 16"
                           Keyboard="Numeric"
                           BackgroundColor="{AppThemeBinding Light={StaticResource LightInputBackground}, Dark={StaticResource DarkInputBackground}}"/>
                </VerticalStackLayout>
            </Grid>

            <!-- Refrigerant -->
            <VerticalStackLayout Spacing="4">
                <Label Text="Refrigerant Type" FontSize="12"/>
                <Picker x:Name="DrawerRefrigerantPicker">
                    <Picker.ItemsSource>
                        <x:Array Type="{x:Type x:String}">
                            <x:String>R-410A</x:String>
                            <x:String>R-22</x:String>
                            <x:String>R-32</x:String>
                            <x:String>R-454B</x:String>
                        </x:Array>
                    </Picker.ItemsSource>
                </Picker>
            </VerticalStackLayout>

            <!-- Installation Date -->
            <VerticalStackLayout Spacing="4">
                <Label Text="Installation Date" FontSize="12"/>
                <DatePicker x:Name="DrawerInstallDatePicker"
                            Format="MMM dd, yyyy"/>
            </VerticalStackLayout>

            <!-- Warranty -->
            <Grid ColumnDefinitions="*,*" ColumnSpacing="12">
                <VerticalStackLayout Spacing="4">
                    <Label Text="Warranty Years" FontSize="12"/>
                    <Entry x:Name="DrawerWarrantyEntry"
                           Placeholder="e.g., 10"
                           Keyboard="Numeric"
                           BackgroundColor="{AppThemeBinding Light={StaticResource LightInputBackground}, Dark={StaticResource DarkInputBackground}}"/>
                </VerticalStackLayout>
                <VerticalStackLayout Grid.Column="1" Spacing="4">
                    <Label Text="Filter Size" FontSize="12"/>
                    <Entry x:Name="DrawerFilterSizeEntry"
                           Placeholder="e.g., 16x25x1"
                           BackgroundColor="{AppThemeBinding Light={StaticResource LightInputBackground}, Dark={StaticResource DarkInputBackground}}"/>
                </VerticalStackLayout>
            </Grid>

            <!-- Notes -->
            <VerticalStackLayout Spacing="4">
                <Label Text="Notes" FontSize="12"/>
                <Editor x:Name="DrawerNotesEditor"
                        Placeholder="Additional notes..."
                        HeightRequest="80"
                        BackgroundColor="{AppThemeBinding Light={StaticResource LightInputBackground}, Dark={StaticResource DarkInputBackground}}"/>
            </VerticalStackLayout>
        </VerticalStackLayout>
    </controls:SlideInDrawer.DrawerContent>
</controls:SlideInDrawer>

</ContentPage>
```

**Also add namespace at the top:**
```xaml
<ContentPage xmlns:controls="clr-namespace:OneManVan.Mobile.Controls"
             ...>
```

---

### Step 2: Update AssetListPage.xaml.cs

**Add fields at the top of the class:**
```csharp
private Asset? _editingAsset; // Track if editing existing asset
```

**Replace your current "Add Asset" button handler:**

```csharp
// OLD - Navigates to separate page
private async void OnAddAssetClicked(object sender, EventArgs e)
{
    await Shell.Current.GoToAsync($"AddAssetPage?customerId={_customerId}");
}

// NEW - Opens drawer
private async void OnAddAssetClicked(object sender, EventArgs e)
{
    _editingAsset = null; // Clear edit mode
    
    // Reset form
    DrawerAssetNameEntry.Text = "";
    DrawerSerialEntry.Text = "";
    DrawerEquipmentTypePicker.SelectedIndex = 0;
    DrawerTonnageEntry.Text = "";
    DrawerSeerEntry.Text = "";
    DrawerRefrigerantPicker.SelectedIndex = 0;
    DrawerInstallDatePicker.Date = DateTime.Now;
    DrawerWarrantyEntry.Text = "10";
    DrawerFilterSizeEntry.Text = "";
    DrawerNotesEditor.Text = "";
    
    // Update drawer title
    AssetDrawer.Title = "Add Asset";
    AssetDrawer.SaveButtonText = "Save Asset";
    
    // Open drawer
    await AssetDrawer.OpenAsync();
    
    // Focus first field
    DrawerAssetNameEntry.Focus();
}
```

**Handle asset selection for editing:**

```csharp
private async void OnAssetSelected(object sender, SelectionChangedEventArgs e)
{
    if (e.CurrentSelection.FirstOrDefault() is not Asset asset)
        return;

    // Deselect
    ((CollectionView)sender).SelectedItem = null;

    // Show action sheet
    var action = await DisplayActionSheet(
        $"Asset: {asset.DisplayName}",
        "Cancel",
        null,
        "View Details",
        "Edit",
        "Delete"
    );

    switch (action)
    {
        case "View Details":
            await Shell.Current.GoToAsync($"AssetDetailPage?assetId={asset.Id}");
            break;

        case "Edit":
            await OpenAssetForEdit(asset);
            break;

        case "Delete":
            await DeleteAsset(asset);
            break;
    }
}

private async Task OpenAssetForEdit(Asset asset)
{
    _editingAsset = asset;
    
    // Load data into drawer
    DrawerAssetNameEntry.Text = asset.DisplayName;
    DrawerSerialEntry.Text = asset.Serial;
    DrawerEquipmentTypePicker.SelectedItem = asset.EquipmentType;
    DrawerTonnageEntry.Text = asset.Tonnage?.ToString();
    DrawerSeerEntry.Text = asset.SeerRating?.ToString();
    DrawerRefrigerantPicker.SelectedItem = asset.RefrigerantType;
    DrawerInstallDatePicker.Date = asset.InstallDate ?? DateTime.Now;
    DrawerWarrantyEntry.Text = asset.WarrantyYears?.ToString();
    DrawerFilterSizeEntry.Text = asset.FilterSize;
    DrawerNotesEditor.Text = asset.Notes;
    
    // Update drawer title
    AssetDrawer.Title = "Edit Asset";
    AssetDrawer.SaveButtonText = "Update";
    
    // Open drawer
    await AssetDrawer.OpenAsync();
}
```

**Add drawer event handlers:**

```csharp
private async void OnAssetDrawerSaveClicked(object sender, EventArgs e)
{
    try
    {
        // Validate
        if (string.IsNullOrWhiteSpace(DrawerAssetNameEntry.Text))
        {
            await DisplayAlert("Required", "Asset name is required", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(DrawerSerialEntry.Text))
        {
            await DisplayAlert("Required", "Serial number is required", "OK");
            return;
        }

        if (DrawerEquipmentTypePicker.SelectedItem == null)
        {
            await DisplayAlert("Required", "Equipment type is required", "OK");
            return;
        }

        await using var db = await _dbFactory.CreateDbContextAsync();

        Asset asset;
        if (_editingAsset != null)
        {
            // Update existing
            asset = await db.Assets.FindAsync(_editingAsset.Id);
            if (asset == null)
            {
                await DisplayAlert("Error", "Asset not found", "OK");
                return;
            }
        }
        else
        {
            // Create new
            asset = new Asset
            {
                CustomerId = _customerId // Assuming you have this from page parameter
            };
            db.Assets.Add(asset);
        }

        // Update properties
        asset.DisplayName = DrawerAssetNameEntry.Text;
        asset.Serial = DrawerSerialEntry.Text;
        asset.EquipmentType = DrawerEquipmentTypePicker.SelectedItem.ToString();
        
        if (double.TryParse(DrawerTonnageEntry.Text, out var tonnage))
            asset.Tonnage = tonnage;
        
        if (int.TryParse(DrawerSeerEntry.Text, out var seer))
            asset.SeerRating = seer;
        
        asset.RefrigerantType = DrawerRefrigerantPicker.SelectedItem?.ToString();
        asset.InstallDate = DrawerInstallDatePicker.Date;
        
        if (int.TryParse(DrawerWarrantyEntry.Text, out var warranty))
            asset.WarrantyYears = warranty;
        
        asset.FilterSize = DrawerFilterSizeEntry.Text;
        asset.Notes = DrawerNotesEditor.Text;

        await db.SaveChangesAsync();

        // Haptic feedback
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

        // Close drawer
        await AssetDrawer.CompleteSaveAsync();

        // Refresh list
        await LoadAssetsAsync();
        
        // Show success message
        var message = _editingAsset != null ? "Asset updated successfully" : "Asset added successfully";
        await DisplayAlert("Success", message, "OK");
    }
    catch (Exception ex)
    {
        await DisplayAlert("Error", $"Failed to save asset: {ex.Message}", "OK");
    }
}

private void OnAssetDrawerCancelClicked(object sender, EventArgs e)
{
    // Drawer auto-closes
    _editingAsset = null;
}

private async Task DeleteAsset(Asset asset)
{
    var confirm = await DisplayAlert(
        "Delete Asset",
        $"Are you sure you want to delete {asset.DisplayName}?",
        "Delete",
        "Cancel"
    );

    if (!confirm)
        return;

    try
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.Assets.Remove(asset);
        await db.SaveChangesAsync();

        await LoadAssetsAsync();
        await DisplayAlert("Success", "Asset deleted", "OK");
    }
    catch (Exception ex)
    {
        await DisplayAlert("Error", $"Failed to delete: {ex.Message}", "OK");
    }
}
```

---

## Benefits of This Approach

### For Users:
? **Faster** - No page navigation delay  
? **Context** - Can still see asset list behind drawer  
? **Familiar** - Modern mobile UX pattern  
? **Dismissable** - Tap outside or press X to cancel  

### For Developers:
? **Cleaner** - All asset CRUD in one file  
? **Maintainable** - Easier to update form fields  
? **Testable** - Better unit test coverage  
? **Flexible** - Easy to add/remove fields  

---

## Testing Checklist

After implementing:

- [ ] Tap "Add Asset" button
- [ ] Verify drawer slides in smoothly
- [ ] Fill out form
- [ ] Tap Save - verify asset is added to list
- [ ] Tap existing asset
- [ ] Choose "Edit" from action sheet
- [ ] Verify drawer opens with existing data
- [ ] Update fields
- [ ] Tap Save - verify asset is updated
- [ ] Test Cancel button
- [ ] Test tap outside drawer to dismiss
- [ ] Test in dark mode
- [ ] Test with long form content (scrolling)

---

## Optional: Keep AddAssetPage as Fallback

If you want to keep the separate page for now:

```csharp
// Keep both options
var action = await DisplayActionSheet(
    "Add Asset",
    "Cancel",
    null,
    "Quick Add (Drawer)",
    "Full Form (Page)"
);

if (action == "Quick Add (Drawer)")
{
    await AssetDrawer.OpenAsync();
}
else if (action == "Full Form (Page)")
{
    await Shell.Current.GoToAsync($"AddAssetPage?customerId={_customerId}");
}
```

---

## After Testing

Once you're happy with the drawer implementation:

1. Remove references to AddAssetPage
2. Update AppShell.xaml routing if needed
3. Delete AddAssetPage.xaml and .cs files
4. Celebrate! ??

---

*This same pattern can be applied to all your list pages: Jobs, Customers, Estimates, Invoices, etc.*
