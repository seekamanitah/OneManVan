# Drawer System & Settings Persistence - Implementation Guide

**Date:** January 26, 2025  
**Issues:** 
1. Replace popup windows with slide-in drawers for data entry
2. Fix UI Scale setting persistence issue

---

## ? COMPLETED: Slide-In Drawer Control

### Created Files:
1. **OneManVan.Mobile/Controls/SlideInDrawer.xaml** - Reusable drawer UI
2. **OneManVan.Mobile/Controls/SlideInDrawer.xaml.cs** - Drawer logic with animations

### Features:
- ? Smooth slide-in/out animations (300ms)
- ? Semi-transparent overlay with tap-to-close
- ? Configurable title, content, and button text
- ? Event-based Save/Cancel handling
- ? Theme-aware colors (light/dark mode)
- ? Rounded corners with shadow
- ? ScrollView for long content
- ? Fixed action buttons at bottom

### Usage Pattern:

```xaml
<!-- In your page XAML -->
<Grid>
    <!-- Your main content -->
    <VerticalStackLayout>
        <Button Text="Add Customer" Clicked="OnAddCustomerClicked"/>
        <!-- other UI -->
    </VerticalStackLayout>

    <!-- Drawer overlay (last child for Z-order) -->
    <controls:SlideInDrawer x:Name="AddCustomerDrawer"
                            Title="Add New Customer"
                            SaveButtonText="Save Customer"
                            SaveClicked="OnDrawerSaveClicked"
                            CancelClicked="OnDrawerCancelClicked">
        <controls:SlideInDrawer.DrawerContent>
            <!-- Your form content here -->
            <VerticalStackLayout Spacing="16">
                <Entry x:Name="NameEntry" Placeholder="Customer Name"/>
                <Entry x:Name="PhoneEntry" Placeholder="Phone"/>
                <Entry x:Name="EmailEntry" Placeholder="Email"/>
            </VerticalStackLayout>
        </controls:SlideInDrawer.DrawerContent>
    </controls:SlideInDrawer>
</Grid>
```

```csharp
// In your code-behind
private async void OnAddCustomerClicked(object sender, EventArgs e)
{
    // Clear previous data
    NameEntry.Text = "";
    PhoneEntry.Text = "";
    EmailEntry.Text = "";

    // Open drawer
    await AddCustomerDrawer.OpenAsync();
}

private async void OnDrawerSaveClicked(object sender, EventArgs e)
{
    try
    {
        // Validate
        if (string.IsNullOrWhiteSpace(NameEntry.Text))
        {
            await DisplayAlert("Required", "Customer name is required", "OK");
            return;
        }

        // Save to database
        var customer = new Customer
        {
            Name = NameEntry.Text,
            Phone = PhoneEntry.Text,
            Email = EmailEntry.Text
        };

        await using var db = await _dbFactory.CreateDbContextAsync();
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        // Close drawer on success
        await AddCustomerDrawer.CompleteSaveAsync();
        
        // Refresh list
        await LoadCustomersAsync();
    }
    catch (Exception ex)
    {
        await DisplayAlert("Error", ex.Message, "OK");
    }
}

private void OnDrawerCancelClicked(object sender, EventArgs e)
{
    // Drawer auto-closes, just clean up if needed
}
```

---

## ?? TODO: Apply Drawer Pattern to All Data Entry Pages

### Priority 1 - High Traffic Pages
1. **JobListPage** ? Add/Edit Job
2. **CustomerListPage** ? Add/Edit Customer
3. **EstimateListPage** ? Add/Edit Estimate
4. **InvoiceListPage** ? Add/Edit Invoice
5. **AssetListPage** ? Add/Edit Asset

### Priority 2 - Medium Traffic
6. **ProductListPage** ? Add/Edit Product
7. **InventoryListPage** ? Add/Edit Inventory
8. **ServiceAgreementListPage** ? Add/Edit Agreement

### Implementation Steps per Page:

#### Step 1: Update XAML Structure
```xaml
<!-- OLD: Page navigates to separate AddXPage -->
<Button Text="Add" Clicked="OnAddClicked"/>

<!-- NEW: Page has embedded drawer -->
<Grid>
    <VerticalStackLayout>
        <Button Text="Add" Clicked="OnOpenDrawerClicked"/>
        <!-- existing content -->
    </VerticalStackLayout>
    
    <!-- Add drawer at end -->
    <controls:SlideInDrawer x:Name="AddEditDrawer"
                            Title="Add Item"
                            SaveClicked="OnSaveClicked"/>
</Grid>
```

#### Step 2: Move Form Content
- Copy form fields from `AddXPage.xaml` to drawer's `DrawerContent`
- Adjust layout for drawer width (380px)
- Ensure scrolling works for long forms

#### Step 3: Update Code-Behind
- Replace navigation with `await DrawerName.OpenAsync()`
- Move save logic from separate page to drawer event
- Handle validation before closing drawer

#### Step 4: Remove Obsolete Pages
- After testing, delete old `AddXPage.xaml/.cs` files
- Update `AppShell.xaml` routing if needed

---

## ?? ISSUE 2: UI Scale Setting Not Persisting

### Problem Analysis:
The UI Scale setting resets when returning to Settings page.

### Root Cause (Likely):
1. **Not loading saved value on page appearance**
2. **Slider not bound to saved preference**
3. **Value saved but not retrieved correctly**

### Investigation Needed:

#### Check if UI Scale feature exists:
```bash
# Search for UI Scale slider
grep -r "ScaleSlider\|FontScale\|UIScale" OneManVan.Mobile/Pages/SettingsPage.xaml
```

#### Current Status:
- ? No UI Scale slider found in current SettingsPage.xaml
- ?? Feature may not be implemented yet

### Solutions:

#### Option A: UI Scale Feature Doesn't Exist Yet
**Add UI Scale feature to SettingsPage:**

```xaml
<!-- Add to Appearance section in SettingsPage.xaml -->
<VerticalStackLayout Spacing="12">
    <Label Text="UI Scale" FontSize="16"/>
    <Label Text="Adjust text and button sizes" 
           FontSize="12" 
           TextColor="{AppThemeBinding Light=#757575, Dark=#B0B0B0}"/>
    
    <Grid ColumnDefinitions="Auto,*,Auto" ColumnSpacing="12">
        <Label Text="Small" FontSize="12" VerticalOptions="Center"/>
        <Slider Grid.Column="1"
                x:Name="UIScaleSlider"
                Minimum="0.85"
                Maximum="1.3"
                Value="1.0"
                ValueChanged="OnUIScaleChanged"/>
        <Label Grid.Column="2" Text="Large" FontSize="12" VerticalOptions="Center"/>
    </Grid>
    
    <Label x:Name="ScaleValueLabel" 
           Text="100%" 
           FontSize="13" 
           HorizontalOptions="Center"
           TextColor="#2196F3"/>
</VerticalStackLayout>
```

```csharp
// In SettingsPage.xaml.cs

private async Task LoadSettingsAsync()
{
    // ... existing code ...
    
    // Load UI Scale
    var uiScale = Preferences.Get("UIScale", 1.0);
    UIScaleSlider.Value = uiScale;
    ScaleValueLabel.Text = $"{(int)(uiScale * 100)}%";
    
    // Apply scale
    Application.Current.Resources["GlobalFontScale"] = uiScale;
}

private void OnUIScaleChanged(object sender, ValueChangedEventArgs e)
{
    var scale = e.NewValue;
    
    // Update label
    ScaleValueLabel.Text = $"{(int)(scale * 100)}%";
    
    // Save to preferences
    Preferences.Set("UIScale", scale);
    
    // Apply immediately
    if (Application.Current != null)
    {
        Application.Current.Resources["GlobalFontScale"] = scale;
    }
    
    // Haptic feedback
    try 
    { 
        HapticFeedback.Default.Perform(HapticFeedbackType.Click); 
    } 
    catch { }
}
```

```xaml
<!-- Add to App.xaml resources -->
<Application.Resources>
    <x:Double x:Key="GlobalFontScale">1.0</x:Double>
</Application.Resources>
```

#### Option B: Feature Exists But Has Bug
**Fix the persistence issue:**

```csharp
// Ensure LoadSettingsAsync is called in OnAppearing
protected override async void OnAppearing()
{
    base.OnAppearing();
    
    _cts?.Cancel();
    _cts = new CancellationTokenSource();
    
    try
    {
        LoadTradeInfo();
        await LoadSettingsAsync(); // ? Make sure this is called
        await LoadDatabaseStatsAsync(_cts.Token);
        await LoadBackupInfoAsync();
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
    }
}

private async Task LoadSettingsAsync()
{
    try
    {
        // Dark mode (already working)
        var isDarkMode = Preferences.Get("DarkMode", false);
        DarkModeSwitch.IsToggled = isDarkMode;

        // UI Scale - ADD THIS
        var uiScale = Preferences.Get("UIScale", 1.0);
        if (UIScaleSlider != null) // Check control exists
        {
            UIScaleSlider.Value = uiScale;
        }

        // Business defaults (already working)
        LaborRateEntry.Text = Preferences.Get("LaborRate", "85.00");
        // ... rest of existing code ...
    }
    catch (Exception ex)
    {
        await DisplayAlertAsync("Error", $"Failed to load settings: {ex.Message}", "OK");
    }
}
```

---

## ?? Implementation Checklist

### Phase 1: Drawer System (Priority 1 Pages)
- [ ] Test SlideInDrawer control on a simple page
- [ ] Convert JobListPage to use drawer
- [ ] Convert CustomerListPage to use drawer
- [ ] Convert EstimateListPage to use drawer
- [ ] Convert InvoiceListPage to use drawer
- [ ] Convert AssetListPage to use drawer

### Phase 2: Drawer System (Priority 2 Pages)
- [ ] Convert ProductListPage
- [ ] Convert InventoryListPage
- [ ] Convert ServiceAgreementListPage

### Phase 3: UI Scale Feature
- [ ] Determine if feature exists
- [ ] If not: Add UI Scale slider to SettingsPage
- [ ] If yes: Fix persistence bug
- [ ] Test scale changes persist across navigation
- [ ] Verify scale applies to all pages

### Phase 4: Cleanup
- [ ] Remove obsolete AddX/EditX pages
- [ ] Update navigation routes
- [ ] Test all workflows
- [ ] Update user documentation

---

## ?? Drawer Design Guidelines

### Width: 380px
- Optimal for tablets and phones in landscape
- Narrows to 90% screen width on small phones

### Animation: 300ms
- Fast enough to feel responsive
- Slow enough to be smooth

### Overlay: 50% opacity black
- Clear visual separation
- Tap to dismiss

### Header: Colored bar
- Blue for add operations
- Purple for config/settings
- Green for completion actions

### Content: Scrollable
- Accommodates long forms
- Padding: 20px for comfortable spacing

### Actions: Fixed bottom
- Always visible
- Cancel (left) and Save (right)
- Save button is primary (bold, colored)

---

## ?? Benefits of Drawer Pattern

### User Experience
? **Faster** - No page navigation delay
? **Context** - Can see previous content behind drawer
? **Familiar** - Modern mobile UX pattern
? **Dismissable** - Tap outside to cancel

### Developer Experience
? **Reusable** - One drawer control for all forms
? **Maintainable** - Form logic stays in parent page
? **Testable** - Easier to unit test without navigation
? **Flexible** - Easy to customize per use case

### Performance
? **Lighter** - No page creation/destruction overhead
? **Animated** - Smooth transitions
? **Memory** - Better resource management

---

## ?? Next Steps

1. **Immediate**: 
   - Test SlideInDrawer on one page (e.g., CustomerListPage)
   - Verify it works as expected
   - Check if UI Scale feature exists

2. **Short-term**:
   - Apply drawer to all Priority 1 pages
   - Fix UI Scale persistence if needed
   - Gather user feedback

3. **Long-term**:
   - Complete Priority 2 pages
   - Remove obsolete pages
   - Document pattern for future pages

---

## ?? Estimated Timeline

- **Drawer Implementation**: 1-2 hours per page
- **UI Scale Fix**: 30 minutes to 1 hour
- **Testing**: 2-3 hours total
- **Total**: ~15-20 hours for complete rollout

---

## ? Questions to Answer

1. Does UI Scale feature currently exist in SettingsPage?
2. If yes, where is the slider control? (not found in current XAML)
3. Which page should we convert first as a proof-of-concept?
4. Should we keep Add pages as fallback or remove completely?

---

*Ready to implement! Start with investigating UI Scale, then convert one page as a test.*
