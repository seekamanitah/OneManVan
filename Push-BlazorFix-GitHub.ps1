# Complete Blazor SignalR Fix - GitHub Push Script
# Run this in PowerShell

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Blazor Interactive Server Fix" -ForegroundColor Cyan
Write-Host "Pushing to GitHub" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Check if we're in a git repository
if (-not (Test-Path ".git")) {
    Write-Host "? Error: Not in a git repository" -ForegroundColor Red
    exit 1
}

# Show what changed
Write-Host "?? Files changed:" -ForegroundColor Yellow
git status --short

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Changes to commit:" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "1. App.razor - Added @rendermode='InteractiveServer' (CRITICAL)"
Write-Host "2. Program.cs - Cookie configuration for SignalR authentication"
Write-Host "3. Program.cs - Fixed middleware order"
Write-Host "4. Routes.razor - Better authentication handling"
Write-Host "5. Removed .AddServerSideBlazor() conflict"
Write-Host ""

# Add all changes
Write-Host "Adding changes to git..." -ForegroundColor Yellow
git add OneManVan.Web/Components/App.razor
git add OneManVan.Web/Program.cs
git add OneManVan.Web/Components/Routes.razor

# Show what will be committed
Write-Host ""
Write-Host "?? Git diff --staged:" -ForegroundColor Yellow
git diff --staged --stat

Write-Host ""
$proceed = Read-Host "Proceed with commit? (y/n)"
if ($proceed -ne 'y' -and $proceed -ne 'Y') {
    Write-Host "? Commit cancelled" -ForegroundColor Red
    exit 1
}

# Commit
$commitMsg = @"
Fix: Enable Blazor Interactive Server for @onclick events

Critical fixes to enable SignalR circuit and button interactivity:

1. App.razor
   - Added @rendermode='InteractiveServer' to <Routes> component
   - Added @using Microsoft.AspNetCore.Components.Web
   - This enables SignalR WebSocket connection

2. Program.cs
   - Added .ConfigureApplicationCookie() with SignalR settings
   - Set SameSite=Lax for SignalR compatibility
   - Prevent auth redirects for /_blazor path
   - Fixed middleware order: Auth ? Authorization ? RateLimiter

3. Routes.razor
   - Better <NotAuthorized> handling
   - Distinguish authenticated vs unauthenticated users

Issue: Buttons with @onclick were non-responsive after adding authentication
Root Cause: Pages rendering as Static SSR without SignalR circuit
Solution: @rendermode='InteractiveServer' enables interactive features

Tested: ? SignalR WebSocket connects (Status 101)
Tested: ? Blazor.platform returns 'server'
Tested: ? All @onclick events work

Closes: Authentication blocking Blazor interactivity
"@

Write-Host "Committing..." -ForegroundColor Yellow
git commit -m $commitMsg

if ($LASTEXITCODE -ne 0) {
    Write-Host "? Commit failed!" -ForegroundColor Red
    exit 1
}

Write-Host "? Commit successful!" -ForegroundColor Green
Write-Host ""

# Push to GitHub
Write-Host "Pushing to GitHub..." -ForegroundColor Yellow
git push origin master

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "==========================================" -ForegroundColor Cyan
    Write-Host "? Successfully pushed to GitHub!" -ForegroundColor Green
    Write-Host "==========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Repository: https://github.com/seekamanitah/OneManVan" -ForegroundColor White
    Write-Host "Branch: master" -ForegroundColor White
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Review commit on GitHub"
    Write-Host "2. Deploy to server (see SERVER_DEPLOYMENT_GUIDE.md)"
    Write-Host "==========================================" -ForegroundColor Cyan
} else {
    Write-Host ""
    Write-Host "? Push failed!" -ForegroundColor Red
    Write-Host "Common issues:" -ForegroundColor Yellow
    Write-Host "  - Check internet connection"
    Write-Host "  - Verify GitHub credentials"
    Write-Host "  - Check branch permissions"
    Write-Host ""
    Write-Host "To retry: git push origin master" -ForegroundColor Yellow
}
