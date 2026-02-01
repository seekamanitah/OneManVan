#!/bin/bash
# Test Authentication Fix for Blazor Buttons

echo "========================================"
echo "Blazor Authentication Fix - Test Script"
echo "========================================"
echo ""

echo "? Fixes Applied:"
echo "  1. Added .AddAuthentication().AddIdentityCookies()"
echo "  2. Added .ConfigureApplicationCookie() for SignalR"
echo "  3. Mapped Blazor Hub explicitly with larger buffer"
echo ""

echo "?? Testing Steps:"
echo ""
echo "1. Rebuild the application:"
echo "   dotnet clean"
echo "   dotnet build"
echo ""
echo "2. Start debugging (F5 in Visual Studio)"
echo ""
echo "3. Open browser console (F12)"
echo ""
echo "4. After login, run this diagnostic:"
echo ""
cat << 'EOF'
setTimeout(() => {
    console.log("=== SignalR Connection Check ===");
    const entries = performance.getEntriesByType('resource');
    const blazorConn = entries.filter(e => e.name.includes('_blazor'));
    
    if (blazorConn.length > 0) {
        console.log("? SignalR connection found!");
        blazorConn.forEach(b => {
            console.log("  URL:", b.name);
            console.log("  Status:", b.responseStatus);
            console.log("  Duration:", b.duration.toFixed(2) + "ms");
        });
        
        if (blazorConn[0].responseStatus === 101) {
            console.log("? WebSocket established (Status 101)");
        } else {
            console.log("? WebSocket failed (Status " + blazorConn[0].responseStatus + ")");
        }
    } else {
        console.log("? No SignalR connection found!");
    }
    
    console.log("=================================");
}, 3000);
EOF
echo ""

echo "5. Test button click:"
echo "   - Navigate to: http://localhost:5024/customers"
echo "   - Click: 'Add Customer'"
echo "   - Expected: Navigates to /customers/new immediately"
echo ""

echo "6. If still not working, check Network tab:"
echo "   - Filter: WS (WebSocket)"
echo "   - Look for: _blazor?id=..."
echo "   - Status should be: 101"
echo "   - If 401/403: Authentication still blocking"
echo ""

echo "========================================"
echo "Expected Results:"
echo "========================================"
echo "? Blazor loaded: YES"
echo "? SignalR connection: Status 101"
echo "? Button clicks: Navigate immediately"
echo "? Console: No red errors"
echo ""
echo "If still broken, see: FIX_AUTHENTICATION_BLOCKING_BLAZOR.md"
echo "========================================"
echo ""
