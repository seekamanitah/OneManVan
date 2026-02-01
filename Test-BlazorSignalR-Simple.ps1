# Complete Blazor SignalR Diagnostic - Run in Browser Console

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Blazor SignalR Connection Test" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "?? Steps:" -ForegroundColor Yellow
Write-Host "1. Rebuild: dotnet clean && dotnet build"
Write-Host "2. Start app (F5)"
Write-Host "3. Log in"
Write-Host "4. Open DevTools (F12) ? Console tab"
Write-Host "5. Paste the JavaScript below"
Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "JavaScript Diagnostic (copy to console):" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

$diagnostic = @'
console.log("=== BLAZOR DIAGNOSTIC ===");
console.log("Blazor loaded:", typeof Blazor !== 'undefined');
console.log("Blazor.platform:", Blazor?.platform || "UNDEFINED");
console.log("Blazor.navigateTo:", typeof Blazor?.navigateTo);

setTimeout(() => {
    const blazorConn = performance.getEntriesByType('resource')
        .filter(e => e.name.includes('_blazor'));
    console.log("");
    console.log("SignalR connections:", blazorConn.length);
    if (blazorConn.length > 0) {
        blazorConn.forEach(c => {
            console.log("  URL:", c.name);
            console.log("  Status:", c.responseStatus);
        });
    }
    console.log("");
    console.log("RESULT:", blazorConn.length > 0 && blazorConn[0].responseStatus === 101 ? "? WORKING" : "? FAILED");
}, 3000);
'@

Write-Host $diagnostic -ForegroundColor White
Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "What to look for:" -ForegroundColor Yellow
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "? WORKING if you see:" -ForegroundColor Green
Write-Host "   - Blazor loaded: true"
Write-Host "   - Blazor.platform: server" 
Write-Host "   - SignalR connections: 1"
Write-Host "   - Status: 101"
Write-Host "   - RESULT: ? WORKING"
Write-Host ""
Write-Host "? FAILED if you see:" -ForegroundColor Red
Write-Host "   - Blazor.platform: UNDEFINED"
Write-Host "   - SignalR connections: 0"
Write-Host "   - RESULT: ? FAILED"
Write-Host ""
Write-Host "Report back with the console output!" -ForegroundColor Yellow
Write-Host "=========================================" -ForegroundColor Cyan
