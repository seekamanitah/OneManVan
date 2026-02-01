#!/bin/bash
# Complete Blazor SignalR Diagnostic Script

echo "=========================================="
echo "Blazor SignalR Connection Diagnostic"
echo "=========================================="
echo ""
echo "?? Instructions:"
echo "1. Stop debugging (Shift + F5)"
echo "2. Rebuild: dotnet clean && dotnet build"
echo "3. Start debugging (F5)"
echo "4. Log in to the application"
echo "5. Open Browser DevTools (F12)"
echo "6. Copy and paste the script below into Console"
echo ""
echo "=========================================="
echo "Copy this entire script into browser console:"
echo "=========================================="
echo ""

cat << 'EOF'
console.log("=== COMPLETE BLAZOR SIGNALR DIAGNOSTIC ===");
console.log("");

// Test 1: Blazor Object
console.log("1?? Blazor Object Check:");
if (typeof Blazor !== 'undefined') {
    console.log("   ? Blazor is loaded");
    console.log("   Platform:", Blazor.platform || "? UNDEFINED");
    console.log("   NavigateTo:", typeof Blazor.navigateTo);
} else {
    console.log("   ? Blazor is NOT loaded!");
}

// Test 2: Blazor Scripts
console.log("");
console.log("2?? Script Loading:");
const blazorScripts = Array.from(document.scripts)
    .filter(s => s.src.includes('blazor'));
if (blazorScripts.length > 0) {
    blazorScripts.forEach(s => {
        console.log("   ? Found:", s.src);
    });
} else {
    console.log("   ? No Blazor scripts found!");
}

// Test 3: Check after delay
console.log("");
console.log("3?? Checking SignalR connection (wait 3 seconds)...");
setTimeout(() => {
    console.log("");
    console.log("=== SIGNALR STATUS ===");
    
    // Check performance entries
    const perfEntries = performance.getEntriesByType('resource');
    const blazorConnections = perfEntries.filter(e => 
        e.name.includes('_blazor') || e.name.includes('negotiate')
    );
    
    if (blazorConnections.length > 0) {
        console.log("? SignalR connection attempts found:");
        blazorConnections.forEach(conn => {
            console.log(`   URL: ${conn.name}`);
            console.log(`   Status: ${conn.responseStatus || 'N/A'}`);
            console.log(`   Duration: ${conn.duration.toFixed(2)}ms`);
            console.log("");
        });
    } else {
        console.log("? NO SignalR connection attempts!");
        console.log("   This means SignalR is not even trying to connect.");
        console.log("   Possible causes:");
        console.log("   - Blazor Server mode not configured");
        console.log("   - AddInteractiveServerComponents() missing");
        console.log("   - App.razor script reference wrong");
    }
    
    // Check WebSocket status
    console.log("");
    console.log("=== WEBSOCKET CHECK ===");
    if (typeof WebSocket !== 'undefined') {
        console.log("? WebSocket API available");
    } else {
        console.log("? WebSocket API not available");
    }
    
    // Check authentication
    console.log("");
    console.log("=== AUTHENTICATION ===");
    const authCookie = document.cookie.split(';')
        .find(c => c.trim().startsWith('.AspNetCore.Identity'));
    if (authCookie) {
        console.log("? Auth cookie present");
    } else {
        console.log("?? Auth cookie not found");
        console.log("   User might not be logged in");
    }
    
    // Final diagnosis
    console.log("");
    console.log("=== DIAGNOSIS ===");
    
    if (typeof Blazor === 'undefined') {
        console.log("? CRITICAL: Blazor JavaScript not loaded");
        console.log("   Fix: Check App.razor has <script src=\"_framework/blazor.web.js\">");
    } else if (!Blazor.platform) {
        console.log("? CRITICAL: Blazor loaded but platform undefined");
        console.log("   This means SignalR circuit did not establish");
        
        if (blazorConnections.length === 0) {
            console.log("");
            console.log("   ?? No connection attempts - Configuration issue");
            console.log("   Check Program.cs:");
            console.log("   1. AddInteractiveServerComponents() is called");
            console.log("   2. AddServerSideBlazor() is configured");
            console.log("   3. MapRazorComponents<App>().AddInteractiveServerRenderMode()");
        } else {
            console.log("");
            console.log("   ?? Connection attempted but failed");
            console.log("   Check:");
            console.log("   1. Authentication middleware order");
            console.log("   2. Cookie configuration (SameSite=Lax)");
            console.log("   3. SignalR not blocked by auth redirects");
        }
    } else {
        console.log("? SUCCESS: Blazor Server circuit established!");
        console.log("   Platform:", Blazor.platform);
        console.log("");
        console.log("   If buttons still don't work:");
        console.log("   - Check browser console for other errors");
        console.log("   - Verify @onclick handlers are properly bound");
        console.log("   - Check NavigateTo is working");
    }
    
    console.log("");
    console.log("=== END DIAGNOSTIC ===");
}, 3000);

// Test manual navigation
console.log("");
console.log("4?? Testing Manual Navigation:");
if (typeof Blazor !== 'undefined' && typeof Blazor.navigateTo === 'function') {
    console.log("   ?? Attempting manual navigation to /customers");
    console.log("   Watch for page change...");
    try {
        Blazor.navigateTo('/customers', false, false);
        console.log("   ? NavigateTo executed (check if page changed)");
    } catch (e) {
        console.log("   ? NavigateTo failed:", e.message);
    }
} else {
    console.log("   ? Cannot test - Blazor.navigateTo not available");
}
EOF

echo ""
echo "=========================================="
echo "After running the diagnostic:"
echo "=========================================="
echo "1. Wait for all checks to complete (3 seconds)"
echo "2. Read the DIAGNOSIS section"
echo "3. Copy the ENTIRE console output"
echo "4. Report back with:"
echo "   - What DIAGNOSIS says"
echo "   - Did manual navigation work?"
echo "   - Full console output"
echo ""
echo "=========================================="
