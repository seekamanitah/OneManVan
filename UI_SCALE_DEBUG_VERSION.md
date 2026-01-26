# UI Scale Persistence - Debug Version & Testing Guide

**Date:** January 26, 2025  
**Status:** ? IMPROVED FIX WITH DEBUGGING  
**Build:** ? PASSING

---

## ?? New Fix Applied

### Problem with Previous Approach
The flag-based approach wasn't working reliably. Event timing issues could cause the flag to be cleared before the ValueChanged event fired.

### New Solution: Event Handler Detachment
Instead of using a flag, we now:
1. **Remove** the ValueChanged event handler before setting the value
2. **Set** the slider value (no event fires)
3. **Re-attach** the event handler

This guarantees the event won't fire during load.

---

## ?? Debug Logging Added

### What Gets Logged

#### When Loading Settings:
```
=== LoadSettingsAsync START ===
Loading UI Scale from preferences: 1.15 (115%)
Event handler detached
Slider value set to: 1.15
Label updated to: 115%
Event handler re-attached
=== LoadSettingsAsync COMPLETE ===
```

#### When User Changes Slider:
```
UI Scale changed to: 1.20 (120%)
UI Scale saved to preferences: 1.20
UI Scale verified in preferences: 1.20
```

---

## ?? Testing Instructions

### Test 1: Set and Verify Save
1. Open the app
2. Go to Settings ? Appearance
3. Move UI Scale slider to **120%**
4. **Watch the debug output** (in Visual Studio Output window)
5. Should see:
   ```
   UI Scale changed to: 1.20 (120%)
   UI Scale saved to preferences: 1.20
   UI Scale verified in preferences: 1.20
   ```

### Test 2: Navigate Away and Return
1. With slider at 120%, tap back/home
2. Navigate to Customers or another page
3. Navigate back to Settings
4. **Watch the debug output**:
   ```
   === LoadSettingsAsync START ===
   Loading UI Scale from preferences: 1.20 (115%)
   Event handler detached
   Slider value set to: 1.20
   Label updated to: 120%
   Event handler re-attached
   === LoadSettingsAsync COMPLETE ===
   ```
5. **Verify**: Slider should be at 120%

### Test 3: Close and Restart App
1. Close app completely
2. Restart app
3. Go to Settings
4. **Watch debug output** - should load saved value
5. **Verify**: Slider maintains 120%

---

## ?? Troubleshooting with Debug Output

### If Slider Still Resets

**Check Debug Output for:**

#### Scenario A: Not Saving
```
UI Scale changed to: 1.20 (120%)
UI Scale saved to preferences: 1.20
UI Scale verified in preferences: 1.00  ? PROBLEM!
```
**Issue:** Preferences.Set() isn't working  
**Solution:** Platform-specific Preferences issue

#### Scenario B: Not Loading
```
Loading UI Scale from preferences: 1.00 (100%)  ? Should be 1.20!
```
**Issue:** Preferences.Get() returns default  
**Solution:** Check if preferences are being cleared

#### Scenario C: Event Firing During Load
```
=== LoadSettingsAsync START ===
Loading UI Scale from preferences: 1.20
Event handler detached
Slider value set to: 1.20
UI Scale changed to: 1.00 (100%)  ? PROBLEM! Event fired!
```
**Issue:** Event still firing after detachment  
**Solution:** Slider control bug (rare)

---

## ?? Code Changes Summary

### LoadSettingsAsync()
```csharp
// OLD - Flag approach
_isLoadingSettings = true;
UIScaleSlider.Value = uiScale;
_isLoadingSettings = false;

// NEW - Detach/reattach approach
UIScaleSlider.ValueChanged -= OnUIScaleChanged; // Remove handler
UIScaleSlider.Value = uiScale; // Set value (no event)
UIScaleSlider.ValueChanged += OnUIScaleChanged; // Add handler back
```

### OnUIScaleChanged()
```csharp
// Added debug logging
System.Diagnostics.Debug.WriteLine($"UI Scale changed to: {scale:F2}");
Preferences.Set("UIScale", scale);
var savedValue = Preferences.Get("UIScale", 1.0);
System.Diagnostics.Debug.WriteLine($"Verified: {savedValue:F2}");
```

---

## ?? Expected Results

### After This Fix:
? Slider value loads correctly on page appearance  
? Slider value persists when navigating away  
? Slider value persists across app restart  
? Debug output shows exactly what's happening  
? Easy to diagnose if issues persist  

---

## ?? How to View Debug Output

### Visual Studio (Windows)
1. Menu: View ? Output
2. Show output from: **Debug**
3. Run app in debug mode
4. Watch output window while testing

### Visual Studio (Mac)
1. View ? Debug ? Application Output
2. Run in debug mode
3. Filter by "Debug"

### VS Code
1. Run in debug mode
2. Debug Console shows output
3. Filter for "UI Scale"

---

## ?? What to Report Back

If the issue persists after this fix, please provide:

1. **Debug output** when adjusting slider
2. **Debug output** when returning to page
3. **Platform**: Android, iOS, Windows?
4. **Does it reset to**: 100% or another value?
5. **Happens**: Every time or randomly?

---

## ?? Alternative Approaches (If Still Failing)

### Option 1: Use Slider.Minimum/Maximum Binding
Instead of ValueChanged, bind the Value property with TwoWay binding.

### Option 2: Delay Event Registration
Attach the ValueChanged handler after a small delay (100ms).

### Option 3: Use Loaded Event
Only attach handler in the Loaded event of the slider.

### Option 4: Manual Property
Create a custom slider property that doesn't fire events during programmatic changes.

---

## ? Next Steps

1. **Run the app** in debug mode
2. **Test** the three scenarios above
3. **Check debug output** in Output window
4. **Report results**:
   - ? Works now!
   - ? Still failing (include debug output)

---

## ?? Debugging Tips

### View All Preferences
Add this temporary code to see all saved preferences:

```csharp
private void OnDebugPreferencesClicked(object sender, EventArgs e)
{
    var uiScale = Preferences.Get("UIScale", 1.0);
    var darkMode = Preferences.Get("DarkMode", false);
    
    DisplayAlert("Preferences",
        $"UIScale: {uiScale}\n" +
        $"DarkMode: {darkMode}",
        "OK");
}
```

### Clear Preferences
If you want to reset:

```csharp
Preferences.Remove("UIScale");
Preferences.Clear(); // Clears ALL preferences
```

---

## ?? Build Status

**Current:** ? BUILD PASSING  
**SlideInDrawer:** Removed (will re-add later)  
**Debug Logging:** ? Active  
**Ready to Test:** ? YES  

---

*Test this version and check the debug output to see exactly what's happening!*
