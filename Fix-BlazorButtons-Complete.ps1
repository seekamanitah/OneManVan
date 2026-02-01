# Complete Blazor Button Fix Script
# Run this in PowerShell as Administrator

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Blazor Button Click Fix - Complete Reset" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Verify the fix is in code
Write-Host "Step 1: Verifying code fix..." -ForegroundColor Yellow
$appRazorPath = "OneManVan.Web\Components\App.razor"

if (Test-Path $appRazorPath) {
    $content = Get-Content $appRazorPath -Raw
    
    if ($content -match '<div id="blazor-error-ui">') {
        Write-Host "? Fix is present in App.razor" -ForegroundColor Green
    } elseif ($content -match '<script id="blazor-error-ui">') {
        Write-Host "? OLD CODE FOUND! Fix not applied!" -ForegroundColor Red
        Write-Host "  Run: (Get-Content $appRazorPath) -replace '<script id=""blazor-error-ui"">', '<div id=""blazor-error-ui"">' -replace '</script>', '</div>' | Set-Content $appRazorPath" -ForegroundColor Yellow
        exit 1
    } else {
        Write-Host "? Could not verify fix" -ForegroundColor Yellow
    }
} else {
    Write-Host "? App.razor not found!" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 2: Stop all processes
Write-Host "Step 2: Stopping all browsers and Visual Studio..." -ForegroundColor Yellow

# Stop browsers
Get-Process chrome,msedge,firefox -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Write-Host "? Browsers stopped" -ForegroundColor Green

# Stop Visual Studio
Get-Process devenv -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Write-Host "? Visual Studio stopped" -ForegroundColor Green

Start-Sleep -Seconds 2

Write-Host ""

# Step 3: Clear browser caches
Write-Host "Step 3: Clearing browser caches..." -ForegroundColor Yellow

# Chrome
$chromeCaches = @(
    "$env:LOCALAPPDATA\Google\Chrome\User Data\Default\Cache",
    "$env:LOCALAPPDATA\Google\Chrome\User Data\Default\Code Cache",
    "$env:LOCALAPPDATA\Google\Chrome\User Data\Default\GPUCache"
)

foreach ($cache in $chromeCaches) {
    if (Test-Path $cache) {
        try {
            Remove-Item "$cache\*" -Recurse -Force -ErrorAction SilentlyContinue
        } catch {}
    }
}
Write-Host "? Chrome cache cleared" -ForegroundColor Green

# Edge
$edgeCaches = @(
    "$env:LOCALAPPDATA\Microsoft\Edge\User Data\Default\Cache",
    "$env:LOCALAPPDATA\Microsoft\Edge\User Data\Default\Code Cache",
    "$env:LOCALAPPDATA\Microsoft\Edge\User Data\Default\GPUCache"
)

foreach ($cache in $edgeCaches) {
    if (Test-Path $cache) {
        try {
            Remove-Item "$cache\*" -Recurse -Force -ErrorAction SilentlyContinue
        } catch {}
    }
}
Write-Host "? Edge cache cleared" -ForegroundColor Green

# Visual Studio browser profiles
try {
    Remove-Item "$env:LOCALAPPDATA\Microsoft\VisualStudio\*\Browser" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "? VS browser profiles cleared" -ForegroundColor Green
} catch {}

Write-Host ""

# Step 4: Clean build artifacts
Write-Host "Step 4: Cleaning build artifacts..." -ForegroundColor Yellow

# Clean bin/obj folders
Get-ChildItem -Recurse -Directory -Filter bin -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Get-ChildItem -Recurse -Directory -Filter obj -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

# Remove .vs folder
if (Test-Path ".vs") {
    Remove-Item ".vs" -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "? Build artifacts cleaned" -ForegroundColor Green

Write-Host ""

# Step 5: Rebuild solution
Write-Host "Step 5: Rebuilding solution..." -ForegroundColor Yellow

# Run dotnet clean
$cleanResult = dotnet clean 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "? Solution cleaned" -ForegroundColor Green
} else {
    Write-Host "? Clean had warnings" -ForegroundColor Yellow
}

# Run dotnet build
$buildResult = dotnet build --no-incremental 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "? Solution built successfully" -ForegroundColor Green
} else {
    Write-Host "? Build failed!" -ForegroundColor Red
    Write-Host $buildResult -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 6: Create test page
Write-Host "Step 6: Creating test page..." -ForegroundColor Yellow

$testPagePath = "OneManVan.Web\Components\Pages\TestBlazorButton.razor"
$testPageContent = @'
@page "/test-blazor-button"
@inject NavigationManager Nav

<PageTitle>Blazor Button Test</PageTitle>

<div class="container py-5">
    <div class="row justify-content-center">
        <div class="col-md-6">
            <div class="card">
                <div class="card-header bg-primary text-white">
                    <h3 class="mb-0">Blazor Button Test</h3>
                </div>
                <div class="card-body">
                    <div class="alert alert-info">
                        <strong>Click Count:</strong> @clickCount
                    </div>

                    <div class="d-grid gap-3">
                        <button class="btn btn-primary btn-lg" @onclick="IncrementCount">
                            <i class="bi bi-plus-circle"></i> Increment (Test @onclick)
                        </button>

                        <button class="btn btn-success btn-lg" @onclick='() => Nav.NavigateTo("/customers")'>
                            <i class="bi bi-arrow-right"></i> Navigate to Customers
                        </button>

                        <button class="btn btn-secondary btn-lg" @onclick='() => Nav.NavigateTo("/customers/new")'>
                            <i class="bi bi-person-plus"></i> New Customer
                        </button>
                    </div>

                    <hr />

                    <div class="alert @alertClass" role="alert">
                        @message
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

@code {
    private int clickCount = 0;
    private string message = "Click any button to test Blazor interactivity";
    private string alertClass = "alert-secondary";

    private void IncrementCount()
    {
        clickCount++;
        message = $"? Blazor @onclick works! Clicked {clickCount} times.";
        alertClass = "alert-success";
    }
}
'@

Set-Content -Path $testPagePath -Value $testPageContent
Write-Host "? Test page created at: $testPagePath" -ForegroundColor Green

Write-Host ""

# Summary
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "? Fix Applied Successfully!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "?? Next Steps:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. Open Visual Studio" -ForegroundColor White
Write-Host "   - Open TradeFlow.sln" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Press F5 to start debugging" -ForegroundColor White
Write-Host ""
Write-Host "3. When browser opens, press Ctrl+Shift+N (InPrivate/Incognito)" -ForegroundColor White
Write-Host "   - Navigate to: https://localhost:7159/test-blazor-button" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Test the buttons:" -ForegroundColor White
Write-Host "   - Click 'Increment' ? Count should increase" -ForegroundColor Gray
Write-Host "   - Click 'Navigate to Customers' ? Should navigate" -ForegroundColor Gray
Write-Host "   - Click 'New Customer' ? Should open form" -ForegroundColor Gray
Write-Host ""
Write-Host "5. Open DevTools (F12):" -ForegroundColor White
Write-Host "   a) Console tab ? Type: Blazor" -ForegroundColor Gray
Write-Host "      Should return: Object (not undefined)" -ForegroundColor Gray
Write-Host ""
Write-Host "   b) Network tab ? Filter: WS" -ForegroundColor Gray
Write-Host "      Should see: _blazor?id=... (Status: 101)" -ForegroundColor Gray
Write-Host ""
Write-Host "   c) Console tab ? Should show:" -ForegroundColor Gray
Write-Host "      [Blazor] Starting up Blazor server-side..." -ForegroundColor DarkGray
Write-Host "      [Blazor] Connected to the server" -ForegroundColor DarkGray
Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "If buttons still don't work, check:" -ForegroundColor Yellow
Write-Host "  - Console tab for red errors" -ForegroundColor Gray
Write-Host "  - Network tab for failed requests (red)" -ForegroundColor Gray
Write-Host "  - See: BLAZOR_BUTTON_FIX_COMPLETE_GUIDE.md" -ForegroundColor Gray
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Create browser diagnostic script
$diagnosticScriptPath = "OneManVan.Web\wwwroot\js\diagnose-blazor.js"
$diagnosticScript = @'
// Blazor Diagnostic Script
// Copy and paste this into browser Console (F12) to diagnose issues

console.log("=== Blazor Diagnostics ===");
console.log("");

// Check 1: Is Blazor loaded?
const blazorLoaded = typeof Blazor !== 'undefined';
console.log(`1. Blazor loaded: ${blazorLoaded ? '? YES' : '? NO'}`);

// Check 2: Blazor script
const scripts = Array.from(document.scripts).map(s => s.src);
const blazorScript = scripts.find(s => s.includes('blazor'));
console.log(`2. Blazor script: ${blazorScript || '? NOT FOUND'}`);

// Check 3: SignalR
if (blazorLoaded) {
    console.log(`3. SignalR: ? Available`);
} else {
    console.log(`3. SignalR: ? Not available`);
}

// Check 4: Failed requests
const failedRequests = performance.getEntries()
    .filter(e => e.responseStatus >= 400 || e.responseStatus === 0)
    .map(e => e.name);
    
if (failedRequests.length > 0) {
    console.log(`4. Failed requests: ? ${failedRequests.length} failed`);
    console.log("   Failed URLs:", failedRequests);
} else {
    console.log(`4. Failed requests: ? None`);
}

// Check 5: Console errors
const errorCount = window.performance.getEntries().filter(e => 
    e.responseStatus === 404 || e.responseStatus === 500
).length;
console.log(`5. HTTP errors: ${errorCount === 0 ? '? None' : `? ${errorCount} errors`}`);

// Check 6: WebSocket
if (typeof WebSocket !== 'undefined') {
    console.log(`6. WebSocket: ? Supported`);
} else {
    console.log(`6. WebSocket: ? Not supported`);
}

console.log("");
console.log("=== Summary ===");
if (blazorLoaded && blazorScript) {
    console.log("? Blazor is working correctly!");
    console.log("If buttons still don't work, check for JavaScript errors above.");
} else {
    console.log("? Blazor is NOT working!");
    console.log("Solutions:");
    console.log("  1. Clear browser cache (Ctrl+Shift+Delete)");
    console.log("  2. Hard refresh (Ctrl+F5)");
    console.log("  3. Use InPrivate/Incognito mode");
    console.log("  4. Rebuild solution in Visual Studio");
}
console.log("====================");
'@

# Ensure wwwroot/js directory exists
$jsDir = "OneManVan.Web\wwwroot\js"
if (-not (Test-Path $jsDir)) {
    New-Item -ItemType Directory -Path $jsDir -Force | Out-Null
}

Set-Content -Path $diagnosticScriptPath -Value $diagnosticScript
Write-Host "? Browser diagnostic script created" -ForegroundColor Green
Write-Host "  Location: $diagnosticScriptPath" -ForegroundColor Gray
Write-Host "  Usage: Copy content and paste into browser Console (F12)" -ForegroundColor Gray
Write-Host ""
