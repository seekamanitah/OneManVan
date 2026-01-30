# Dark Mode Implementation Guide

**Feature:** Complete dark/light theme toggle for OneManVan Web UI  
**Date:** 2025-01-28  
**Status:** ? Implemented

---

## ?? What Was Added

### 1. **ThemeToggle Component**
- **File:** `OneManVan.Web/Components/Shared/ThemeToggle.razor`
- **Location:** Top right header, left of "About" link
- **Icons:** 
  - ?? Moon icon = Light mode (click to go dark)
  - ?? Sun icon = Dark mode (click to go light)
- **Behavior:** Smooth animated transition on hover/click

### 2. **Theme Manager JavaScript**
- **File:** `OneManVan.Web/wwwroot/js/theme.js`
- **Features:**
  - LocalStorage persistence (remembers your choice)
  - System preference detection (auto-detects OS dark mode)
  - Instant theme application (no flash of unstyled content)
  - System theme change listener

### 3. **Dark Theme Styles**
- **File:** `OneManVan.Web/wwwroot/css/theme-dark.css`
- **Coverage:** Complete styling for all components:
  - Cards, tables, forms, buttons
  - Alerts, badges, modals, dropdowns
  - Sidebar, header, navigation
  - Dashboard metrics and cards

---

## ?? How It Works

### Theme Storage
```javascript
localStorage.setItem('theme', 'dark');  // Saves preference
localStorage.getItem('theme');          // Retrieves preference
```

### Theme Application
```html
<html data-bs-theme="dark" class="dark-theme">
  <!-- Bootstrap 5 dark mode + custom dark styles -->
</html>
```

### Component Integration
```razor
<ThemeToggle />  <!-- Place anywhere, typically in header -->
```

---

## ?? User Experience

### Initial Load
1. **Has saved preference?** ? Apply saved theme
2. **No preference?** ? Detect system theme (dark/light)
3. **Apply theme instantly** ? No flash or flicker

### Toggling Theme
1. Click sun/moon icon in top right
2. Theme switches immediately with smooth transition
3. Preference saved to localStorage
4. Works across all pages (persists on navigation)

### System Theme Sync
- If user hasn't set preference
- Theme auto-switches when OS changes (light ? dark)
- Once user clicks toggle, manual preference takes priority

---

## ?? Color Palette

### Light Theme (Default)
| Element | Color |
|---------|-------|
| Background | `#ffffff` |
| Surface | `#f8f9fa` |
| Text | `#212529` |
| Sidebar | `#3b82f6` |
| Border | `#dee2e6` |

### Dark Theme
| Element | Color |
|---------|-------|
| Background | `#1a1d23` |
| Surface | `#242830` |
| Text | `#e9ecef` |
| Sidebar | `#2563eb` (darker blue) |
| Border | `#3a3f47` |

---

## ?? Files Added/Modified

### New Files
```
OneManVan.Web/
??? Components/
?   ??? Shared/
?       ??? ThemeToggle.razor          ? Theme toggle component
??? wwwroot/
?   ??? css/
?   ?   ??? theme-dark.css             ? Dark theme styles
?   ??? js/
?       ??? theme.js                   ? Theme manager script
```

### Modified Files
```
OneManVan.Web/
??? Components/
?   ??? App.razor                      ? Added CSS/JS references
?   ??? _Imports.razor                 ? Added Shared namespace
?   ??? Layout/
?       ??? MainLayout.razor           ? Added ThemeToggle to header
```

---

## ?? Testing Checklist

### Basic Functionality
- [ ] Theme toggle appears in top right (left of About link)
- [ ] Click toggle ? theme switches
- [ ] Icon changes (moon ? sun)
- [ ] Refresh page ? theme persists
- [ ] Navigate to different page ? theme persists

### Theme Application
- [ ] Dashboard looks good in dark mode
- [ ] Cards have proper contrast
- [ ] Forms are readable
- [ ] Tables are styled correctly
- [ ] Alerts/badges visible
- [ ] Modals work in dark mode
- [ ] Sidebar colors adjusted

### Edge Cases
- [ ] Clear localStorage ? uses system preference
- [ ] Change OS theme (no saved pref) ? auto-switches
- [ ] Change OS theme (saved pref) ? ignores OS, uses saved
- [ ] Works on mobile/tablet
- [ ] Smooth transitions (no jarring changes)

---

## ?? Customization

### Change Dark Theme Colors
Edit `OneManVan.Web/wwwroot/css/theme-dark.css`:
```css
.dark-theme {
    --background-color: #your-color;
    --surface-color: #your-color;
    --text-color: #your-color;
    /* ... */
}
```

### Move Toggle Location
Edit `OneManVan.Web/Components/Layout/MainLayout.razor`:
```razor
<!-- Place ThemeToggle wherever you want -->
<ThemeToggle />
```

### Change Icons
Edit `OneManVan.Web/Components/Shared/ThemeToggle.razor`:
```razor
@if (isDarkMode)
{
    <i class="bi bi-your-icon"></i>  <!-- Light mode icon -->
}
else
{
    <i class="bi bi-your-icon"></i>  <!-- Dark mode icon -->
}
```

---

## ?? Implementation Details

### Why Load theme.js in <head>?
```html
<head>
    ...
    <script src="js/theme.js"></script>  <!-- Load BEFORE body renders -->
</head>
```
**Reason:** Prevents "flash of unstyled content" (FOUC). Theme applies **instantly** before Blazor renders.

### Why Use Both `data-bs-theme` and `.dark-theme`?
```html
<html data-bs-theme="dark" class="dark-theme">
```
- `data-bs-theme="dark"` ? **Bootstrap 5 dark mode** (automatic dark styling)
- `.dark-theme` ? **Custom dark styles** (our additional styling)

### Why LocalStorage vs Cookies?
- ? **LocalStorage:** Persists forever, no server overhead, faster
- ? **Cookies:** Sent with every request, limited size, expires

---

## ?? Next Steps (Optional Enhancements)

### 1. **Auto-Switch by Time**
```javascript
// Auto dark mode at night (8 PM - 6 AM)
const hour = new Date().getHours();
if (hour >= 20 || hour < 6) {
    window.themeManager.setTheme(true);
}
```

### 2. **More Theme Options**
Add additional themes (not just dark/light):
- High contrast
- Sepia/warm
- Custom brand themes

### 3. **Per-Page Theme**
Allow different themes for different sections:
```razor
<div data-bs-theme="dark">  <!-- This section always dark -->
    ...
</div>
```

### 4. **Theme Preview**
Show preview before applying:
```razor
<button @onclick="PreviewTheme">Preview Dark Mode</button>
```

---

## ?? Browser Support

| Browser | Support | Notes |
|---------|---------|-------|
| Chrome 90+ | ? Full | All features work |
| Firefox 88+ | ? Full | All features work |
| Safari 14+ | ? Full | All features work |
| Edge 90+ | ? Full | All features work |
| IE 11 | ? None | Not supported (deprecated) |

---

## ?? Troubleshooting

### Theme Not Persisting
**Problem:** Theme resets on refresh  
**Solution:** Check browser allows localStorage (private mode may block)

### Flash of White on Load
**Problem:** Page shows light theme briefly before switching to dark  
**Solution:** Ensure `theme.js` loads in `<head>` before body

### Icons Not Showing
**Problem:** Moon/sun icons are squares  
**Solution:** Bootstrap Icons not loaded - check `<link rel="stylesheet" href="bootstrap-icons.css" />`

### Some Components Still Light
**Problem:** Custom components not themed  
**Solution:** Add `.dark-theme` styles to `theme-dark.css`

---

## ? Summary

**What you get:**
- ?? Working dark/light mode toggle
- ?? Automatic persistence (remembers your choice)
- ?? Beautiful dark theme for all components
- ?? Smooth transitions and animations
- ?? Works on all devices
- ? Instant theme application (no FOUC)
- ?? System theme detection

**Files created:** 3  
**Files modified:** 3  
**Total lines:** ~400 lines (CSS, JS, Razor)

**Ready to use!** Just restart your app and click the theme toggle in the top right corner! ??
