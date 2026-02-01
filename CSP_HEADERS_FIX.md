# CSP Headers Fix - Import/Export Buttons & Bootstrap CDN

## Problem
After adding Content Security Policy headers, several issues occurred:
1. Import and export buttons stopped working
2. Bootstrap JavaScript from CDN was blocked
3. Media elements (data URLs) were blocked
4. Visual Studio Browser Link was blocked

## Root Cause
The CSP policy was **missing key sources**:
```csharp
// OLD (broken):
"script-src 'self' 'unsafe-inline' 'unsafe-eval';"           // ? Missing cdn.jsdelivr.net
"connect-src 'self' ws: wss: https:;"                         // ? Missing http: for Browser Link
// media-src not defined                                      // ? Falls back to restrictive default-src
```

This blocked:
- ? Bootstrap CDN JavaScript (`cdn.jsdelivr.net`)
- ? Browser Link development connection (`localhost:51047`)
- ? Media data URLs (`data:` for video/audio)
- ? Bootstrap dropdown functionality
- ? Blazor SignalR in some configurations

## Fix Applied

### Updated CSP Headers in `Program.cs`
```csharp
// NEW (working):
"script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net;"
"media-src 'self' data: blob:;"
"connect-src 'self' ws: wss: http: https:;"
```

**Full updated policy:**
- ? Allows Bootstrap JavaScript from CDN
- ? Allows Blazor Server SignalR connection
- ? Allows inline event handlers and eval() (required for Blazor)
- ? Allows media data URLs (video/audio)
- ? Allows Browser Link for development
- ? Still blocks external scripts not from allowed domains

## What Changed

| Directive | Old | New | Why |
|-----------|-----|-----|-----|
| `script-src` | `'self' 'unsafe-inline' 'unsafe-eval'` | Added `https://cdn.jsdelivr.net` | Bootstrap CDN scripts |
| `media-src` | Not defined | `'self' data: blob:` | Media elements with data URLs |
| `connect-src` | `'self' ws: wss: https:` | Added `http:` | Browser Link development connection |

## Browser Console Errors Fixed

**Before fix:**
```
? Content-Security-Policy: blocked script at https://cdn.jsdelivr.net/.../bootstrap.bundle.min.js
? Content-Security-Policy: blocked resource (media-src) at data:
? Content-Security-Policy: blocked resource (connect-src) at http://localhost:51047/...
```

**After fix:**
```
? No CSP errors
? Bootstrap loads from CDN
? Media elements work with data URLs
? Browser Link connects for development
```

## Testing After Fix

? **Import Buttons**: Dropdown opens, import dialog works  
? **Export Buttons**: Files download successfully
? **Bootstrap Modals**: Open/close properly
? **Blazor Components**: Update reactively
? **Bootstrap CDN**: JavaScript loads successfully
? **Media Elements**: Data URLs work
? **Browser Link**: Visual Studio development features work

## Note on Security

The updated CSP is necessary for:
1. **Blazor Server** - Requires eval() for hot reload and component updates
2. **Bootstrap 5** - Uses inline event handlers on data attributes
3. **SignalR** - Requires eval() for connection negotiation
4. **Bootstrap CDN** - Many projects load Bootstrap from jsdelivr.net
5. **Browser Link** - Visual Studio development connection

For production, consider:
- Using hash-based CSP for inline scripts (more complex)
- Hosting Bootstrap locally instead of using CDN
- Tightening `connect-src` to exclude `http:` if not using Browser Link
- Or using Blazor WebAssembly (doesn't need eval)

## Files Changed
- `OneManVan.Web/Program.cs` - Updated CSP headers for full compatibility

---

**Status**: ? Fixed - All import/export buttons and Bootstrap functionality now works correctly
