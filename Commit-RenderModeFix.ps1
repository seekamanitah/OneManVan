# Final Commit Script - Conditional Render Mode Fix
# This fixes both Login AND Buttons working together

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Committing Conditional Render Mode Fix" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Show current status
Write-Host "?? Current changes:" -ForegroundColor Yellow
git status --short

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Key Changes:" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. App.razor - Conditional Render Mode" -ForegroundColor Green
Write-Host "   - Account pages (/Account/*) use Static SSR for login"
Write-Host "   - All other pages use Interactive Server for buttons"
Write-Host ""
Write-Host "2. Program.cs - SignalR Cookie Configuration" -ForegroundColor Green
Write-Host "   - SameSite=Lax for WebSocket compatibility"
Write-Host "   - Prevent auth redirects for /_blazor path"
Write-Host ""
Write-Host "3. Routes.razor - Better Auth Handling" -ForegroundColor Green
Write-Host ""
Write-Host "4. Dashboard.razor - Fixed Service Agreements Route" -ForegroundColor Green
Write-Host "   - Changed /serviceagreements to /agreements"
Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan

# Add all changes
Write-Host ""
Write-Host "Adding all changes..." -ForegroundColor Yellow
git add -A

# Commit
$commitMsg = @"
Fix: Conditional render mode - Login AND Buttons both work

CRITICAL FIX: Conditional render mode based on URL

The issue was that:
- Account pages MUST use Static SSR (for HttpContext/login)
- Regular pages NEED Interactive Server (for @onclick buttons)

Solution: App.razor now conditionally applies render mode:
- /Account/* pages ? Static SSR (null render mode)
- All other pages ? Interactive Server

Changes:
1. App.razor
   - Added PageRenderMode property that checks URL
   - Account pages get null (Static SSR)
   - Other pages get InteractiveServer

2. Program.cs
   - Cookie configuration for SignalR
   - SameSite=Lax for WebSocket compatibility
   - Prevent auth redirects for /_blazor

3. Routes.razor
   - Better NotAuthorized handling

4. Dashboard.razor
   - Fixed Service Agreements route (/agreements)

5. Documentation
   - FIX_RENDER_MODE_LOGIN_AND_BUTTONS.md

Result:
? Login page works (Static SSR for HttpContext)
? All buttons work (Interactive Server for @onclick)
? Dashboard links work
? SignalR circuit establishes on regular pages
"@

Write-Host "Committing..." -ForegroundColor Yellow
git commit -m $commitMsg

if ($LASTEXITCODE -ne 0) {
    Write-Host "? Commit failed or nothing to commit" -ForegroundColor Red
} else {
    Write-Host "? Commit successful!" -ForegroundColor Green
}

# Push
Write-Host ""
Write-Host "Pushing to GitHub..." -ForegroundColor Yellow
git push origin master

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "==========================================" -ForegroundColor Cyan
    Write-Host "? Successfully pushed to GitHub!" -ForegroundColor Green
    Write-Host "==========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Repository: https://github.com/seekamanitah/OneManVan" -ForegroundColor White
    Write-Host ""
    Write-Host "==========================================" -ForegroundColor Yellow
    Write-Host "SERVER DEPLOYMENT INSTRUCTIONS:" -ForegroundColor Yellow
    Write-Host "==========================================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "1. SSH to your server:" -ForegroundColor White
    Write-Host "   ssh user@your-server-ip" -ForegroundColor Gray
    Write-Host ""
    Write-Host "2. Navigate to application:" -ForegroundColor White
    Write-Host "   cd /path/to/OneManVan" -ForegroundColor Gray
    Write-Host ""
    Write-Host "3. Deploy (copy these 4 commands):" -ForegroundColor White
    Write-Host "   docker-compose down" -ForegroundColor Gray
    Write-Host "   git pull origin master" -ForegroundColor Gray
    Write-Host "   docker-compose build --no-cache onemanvan-web" -ForegroundColor Gray
    Write-Host "   docker-compose up -d" -ForegroundColor Gray
    Write-Host ""
    Write-Host "4. Verify:" -ForegroundColor White
    Write-Host "   docker-compose logs -f onemanvan-web" -ForegroundColor Gray
    Write-Host ""
    Write-Host "==========================================" -ForegroundColor Cyan
} else {
    Write-Host "? Push failed!" -ForegroundColor Red
    Write-Host "To retry: git push origin master" -ForegroundColor Yellow
}
