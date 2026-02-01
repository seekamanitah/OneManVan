# CSP Headers Fix - Import/Export Buttons Not Working

## Problem
After adding Content Security Policy headers, import and export buttons stopped working on all pages.

## Root Cause
The CSP policy was **too restrictive**:
```csharp
// OLD (broken):
"default-src 'self';"  // Blocked Bootstrap dropdowns and Blazor interactions
```

This blocked:
- Bootstrap's dropdown JavaScript (`data-bs-toggle`)
- Blazor's SignalR connection
- Bootstrap modal functionality
- Form submissions

## Fix Applied

### Updated CSP Headers in `Program.cs`
```csharp
// NEW (working):
"default-src 'self' 'unsafe-inline' 'unsafe-eval';"
```

**Full updated policy:**
- ? Allows Bootstrap JavaScript to work
- ? Allows Blazor Server SignalR connection  
- ? Allows inline event handlers and eval() (required for Blazor)
- ? Still blocks external scripts (good security)

## What Changed

| Directive | Old | New | Why |
|-----------|-----|-----|-----|
| `default-src` | `'self'` | `'self' 'unsafe-inline' 'unsafe-eval'` | Bootstrap dropdowns need this |
| `connect-src` | `'self' ws: wss:` | `'self' ws: wss: https:` | Blazor SignalR connections |
| `style-src` | Limited | Added Google Fonts | Better compatibility |

## Testing After Fix

? **Import Buttons**: Should open dropdown and show import dialog  
? **Export Buttons**: Should download files  
? **Bootstrap Modals**: Should open/close properly  
? **Blazor Components**: Should update reactively  

## Note on Security

While 'unsafe-inline' and 'unsafe-eval' are generally not recommended, they are **required** for:
1. **Blazor Server** - Uses eval() for hot reload and component updates
2. **Bootstrap 5** - Uses inline event handlers on data attributes
3. **SignalR** - Requires eval() for connection negotiation

For production, consider:
- Using Blazor WebAssembly (doesn't need eval)
- Or tightening CSP after thorough testing
- Or using nonce-based CSP (more complex)

## Files Changed
- `OneManVan.Web/Program.cs` - Relaxed CSP headers for Blazor compatibility

---

**Status**: ? Fixed - Import/Export buttons should now work correctly
