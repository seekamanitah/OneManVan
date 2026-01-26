# Desktop Emoji Removal Plan

**Issue:** Emojis using HTML entity codes (&#x1F3E0;, &#x1F465;, etc.) are displaying as "??" in the desktop WPF app.

**Root Cause:** WPF doesn't properly render Unicode emoji characters like .NET MAUI does.

**Solution:** Replace all emoji HTML entities with descriptive text labels or use Segoe MDL2 Assets font icons instead.

---

## Files to Fix

### MainShell.xaml - Navigation Buttons
All navigation buttons have emojis that need to be removed/replaced:

**Line ~78:** `NavHome` - &#x1F3E0; ? "Home"  
**Line ~84:** `NavCustomers` - &#x1F465; ? "Customers"  
**Line ~88:** `NavAssets` - &#x1F527; ? "Assets"  
**Line ~92:** `NavProducts` - &#x1F4E6; ? "Products"  
**Line ~96:** `NavEstimates` - &#x1F4DD; ? "Estimates"  
**Line ~100:** `NavInventory` - &#x1F4E6; ? "Inventory"  
**Line ~107:** `NavSchedule` - &#x1F4CB; ? "Jobs"  
**Line ~111:** `NavKanban` - &#x1F5C2; ? "Kanban Board"  
**Line ~115:** `NavCalendar` - &#x1F4C5; ? "Calendar"  
**Line ~119:** `NavServiceAgreements` - &#x1F4C4; ? "Agreements"  
**Line ~123:** `NavInvoices` - &#x1F4B5; ? "Invoices"  
**Line ~127:** `NavReports` - &#x1F4CA; ? "Reports"  
**Line ~137:** `NavSchema` - &#x1F4CB; ? "Schema Editor"  
**Line ~140:** `NavSettings` - &#x2699; ? "Settings"  
**Line ~143:** `NavBackup` - &#x1F4BE; ? "Backup"  
**Line ~146:** `NavTestRunner` - &#x1F9EA; ? "Test Runner"

---

## Replacement Strategy

### Option 1: Simple Text (Recommended)
Remove emojis entirely, keep clean text labels

### Option 2: Use Segoe MDL2 Assets Icons
Use WPF-compatible icon font

---

## Implementation

Will use **Option 1** for consistency with .NET MAUI copilot instructions (no emojis/icons).
