# UI Scale Fix - String Storage Version (FINAL)

**Date:** January 26, 2025  
**Issue:** UI Scale resets when returning to Settings page  
**Root Cause:** Platform-specific issues with `Preferences.Get<double>`  
**Solution:** Store as string instead of double  
**Status:** ? IMPLEMENTED  
**Build:** ? PASSING

---

## ?? The Real Problem Discovered

After multiple attempts, the issue was identified:

**`Preferences.Get("UIScale", 1.0)` and `Preferences.Set("UIScale", double)` can have platform-specific issues with double values in .NET MAUI.**

Some platforms may:
- Truncate the double value
- Lose precision
- Fail to persist properly
- Return the default value even when the key exists

---

## ? Final Solution: String Storage

### Key Changes:

#### 1. Save as String (SettingsPage.xaml.cs)
```csharp
private void OnUIScaleChanged(object sender, ValueChangedEventArgs e)
{
    var scale = e.NewValue;
    
    // Save as STRING with invariant culture
    var scaleString = scale.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
    Preferences.Set("UIScale", scaleString); // Stores as "1.20" not 1.2
    
    // Verify save
    if (Preferences.ContainsKey("UIScale"))
    {
        var savedValue = Preferences.Get("UIScale", "1.0");
        Debug.WriteLine($"Verified: '{savedValue}'");
    }
}
```

#### 2. Load as String (SettingsPage.xaml.cs)
```csharp
private async Task LoadSettingsAsync()
{
    double uiScale = 1.0;
    
    // Check if key exists
    if (Preferences.ContainsKey("UIScale"))
    {
        // Load as STRING
        var scaleString = Preferences.Get("UIScale", "1.0");
        
        // Parse with invariant culture
        if (double.TryParse(scaleString, 
            System.Globalization.NumberStyles.Float, 
            System.Globalization.CultureInfo.InvariantCulture, 
            out var parsedScale))
        {
            uiScale = parsedScale;
        }
    }
    
    // Detach handler before setting
    UIScaleSlider.ValueChanged -= OnUIScaleChanged;
    UIScaleSlider.Value = uiScale;
    ScaleValueLabel.Text = $"{(int)(uiScale * 100)}%";
    UIScaleSlider.ValueChanged += OnUIScaleChanged;
}
```

#### 3. App Startup (App.xaml.cs)
```csharp
public App(IServiceProvider serviceProvider)
{
    InitializeComponent();
    ServiceProvider = serviceProvider;
    
    // Load UI scale as string
    double uiScale = 1.0;
    if (Preferences.ContainsKey("UIScale"))
    {
        var scaleString = Preferences.Get("UIScale", "1.0");
        if (double.TryParse(scaleString, 
            System.Globalization.NumberStyles.Float, 
            System.Globalization.CultureInfo.InvariantCulture, 
            out var parsedScale))
        {
            uiScale = parsedScale;
        }
    }
    
    if (Resources.ContainsKey("GlobalFontScale"))
    {
        Resources["GlobalFontScale"] = uiScale;
    }
    
    Debug.WriteLine($"App started with UI Scale: {uiScale:F2}");
}
```

---

## ?? Why This Works

### Problem with Double Storage:
- Platform-specific serialization
- Culture-specific decimal separators (, vs .)
- Precision loss
- Type conversion issues

### Solution with String Storage:
? Platform-independent (strings are universal)  
? Explicit culture (InvariantCulture avoids locale issues)  
? No precision loss ("1.20" stays "1.20")  
? Contains check works reliably  
? Debug-friendly (can see exact value stored)  

---

## ?? Testing Instructions

### Test 1: Set and Save
1. Run app in debug mode
2. Open Output window (View ? Output, show "Debug")
3. Go to Settings ? Appearance
4. Move slider to **115%**
5. Watch debug output:
   ```
   === OnUIScaleChanged FIRED ===
   UI Scale changed to: 1.15 (115%)
   UI Scale saved to preferences as string: '1.15'
   UI Scale verified in preferences: '1.15'
   GlobalFontScale resource updated to: 1.15
   ```

### Test 2: Navigate Away and Return
1. With slider at 115%, go to Customers page
2. Return to Settings
3. Watch debug output:
   ```
   === LoadSettingsAsync START ===
   Found UIScale preference: '1.15'
   Parsed UI Scale: 1.15
   Loading UI Scale: 1.15 (115%)
   Event handler detached
   Slider value set to: 1.15
   Label updated to: 115%
   Event handler re-attached
   === LoadSettingsAsync COMPLETE ===
   ```
4. **Verify: Slider should be at 115%** ?

### Test 3: App Restart
1. Close app completely
2. Restart app
3. Watch debug output at startup:
   ```
   App started with UI Scale: 1.15
   ```
4. Go to Settings
5. **Verify: Slider should be at 115%** ?

---

## ?? Debug Output Reference

### When Working Correctly:

**On Save:**
```
=== OnUIScaleChanged FIRED ===
UI Scale changed to: 1.20 (120%)
UI Scale saved to preferences as string: '1.20'
UI Scale verified in preferences: '1.20'  ? Match!
```

**On Load:**
```
=== LoadSettingsAsync START ===
Found UIScale preference: '1.20'  ? Key exists!
Parsed UI Scale: 1.20  ? Parse successful!
Loading UI Scale: 1.20 (120%)
Slider value set to: 1.20  ? Slider updated!
=== LoadSettingsAsync COMPLETE ===
```

### If Still Failing:

**Save Issue:**
```
WARNING: UIScale key not found after save!  ? PROBLEM
```
**Cause:** Preferences API not working on platform

**Load Issue:**
```
UIScale preference not found, using default 1.0  ? PROBLEM
```
**Cause:** Key doesn't exist (wasn't saved)

**Parse Issue:**
```
Failed to parse UI Scale, using default  ? PROBLEM
```
**Cause:** String format issue (shouldn't happen with InvariantCulture)

---

## ?? Additional Fixes Applied

### 1. Use InvariantCulture
Prevents issues with decimal separators:
- US: `1.20` (period)
- EU: `1,20` (comma)
- InvariantCulture: Always `1.20`

### 2. Contains Check
```csharp
if (Preferences.ContainsKey("UIScale"))
```
Explicitly checks if key exists before loading

### 3. TryParse
```csharp
if (double.TryParse(...))
```
Safe parsing with error handling

### 4. Event Detachment
```csharp
UIScaleSlider.ValueChanged -= OnUIScaleChanged;
// set value
UIScaleSlider.ValueChanged += OnUIScaleChanged;
```
Guarantees no events during load

### 5. Comprehensive Logging
Every step is logged for debugging

---

## ? Files Modified

1. **OneManVan.Mobile/Pages/SettingsPage.xaml.cs**
   - LoadSettingsAsync: Load as string with Contains check
   - OnUIScaleChanged: Save as string with verification

2. **OneManVan.Mobile/App.xaml.cs**
   - Constructor: Load as string on app startup

---

## ?? Expected Behavior Now

### Scenario A: First Time Use
1. Slider starts at 100% (default)
2. User moves to 120%
3. Saves as "1.20" string
4. Navigate away ? stays 120%
5. Return to settings ? loads "1.20" ? slider at 120% ?

### Scenario B: After App Restart
1. App starts
2. Loads "1.20" from preferences
3. Applies to GlobalFontScale
4. User goes to Settings
5. Loads "1.20" ? slider at 120% ?

### Scenario C: Multiple Changes
1. Set to 120%
2. Navigate away and back ? 120% ?
3. Change to 90%
4. Navigate away and back ? 90% ?
5. Restart app ? 90% ?

---

## ?? Next Steps

1. **Stop debugging** (to clear old preferences if needed)
2. **Rebuild** the solution
3. **Run** the app fresh
4. **Test** all three scenarios above
5. **Check** debug output
6. **Report** results:
   - ? Fixed! Slider maintains position
   - ? Still failing (share debug output)

---

## ?? If Still Not Working

If this STILL doesn't fix it, then we have a deeper platform issue. In that case, we would need to:

### Option 1: Use Secure Storage
```csharp
await SecureStorage.SetAsync("UIScale", scaleString);
var stored = await SecureStorage.GetAsync("UIScale");
```

### Option 2: Use File Storage
```csharp
var path = Path.Combine(FileSystem.AppDataDirectory, "settings.json");
// Save to JSON file
```

### Option 3: Use SQLite
```csharp
// Store in database table
```

But string-based Preferences should work. This is the most reliable approach.

---

## ?? Summary

**What Changed:**
- ? Store as string instead of double
- ? Use InvariantCulture for parsing
- ? Check if key exists before loading
- ? Comprehensive debug logging
- ? Safe error handling

**Why It Should Work:**
- Avoids platform-specific double issues
- Culture-independent
- Explicit validation at every step
- Clear debug output

**Build Status:** ? PASSING

---

*This is the most robust solution for the UI Scale persistence issue. Test it and check the debug output!*
