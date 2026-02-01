# Hotfix: CSP Headers Breaking Import/Export Buttons

Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Red
Write-Host "?              HOTFIX: Import/Export Buttons Fix                ?" -ForegroundColor Red
Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Red

Write-Host "`nIssue: CSP headers were too strict, blocking Bootstrap and Blazor" -ForegroundColor Yellow
Write-Host "Fix: Relaxed CSP policy to allow Blazor Server and Bootstrap to work`n" -ForegroundColor Yellow

# Stage changes
Write-Host "Staging changes..." -ForegroundColor Cyan
git add OneManVan.Web/Program.cs CSP_HEADERS_FIX.md

# Show what changed
Write-Host "`nChanges to commit:" -ForegroundColor Cyan
git diff --staged --stat

Write-Host "`n"
$confirm = Read-Host "Commit this hotfix? (y/n)"

if ($confirm -eq 'y' -or $confirm -eq 'Y') {
    $commitMessage = @"
Hotfix: Relax CSP headers for Blazor Server compatibility

## Issue
Import/Export buttons stopped working after CSP headers were added.
Bootstrap dropdowns and Blazor SignalR connection were blocked.

## Fix
- Updated default-src to allow 'unsafe-inline' and 'unsafe-eval'
- Added https: to connect-src for SignalR
- Added Google Fonts to style-src and font-src

## Why This Is Needed
Blazor Server requires:
- 'unsafe-eval' for hot reload and component updates
- 'unsafe-inline' for Bootstrap data attributes
- Extended connect-src for SignalR negotiation

## Tested
? Import buttons work (dropdowns open)
? Export buttons work (files download)
? Bootstrap modals work
? Blazor components update reactively

Co-authored-by: GitHub Copilot <noreply@github.com>
"@
    
    Write-Host "`nCommitting hotfix..." -ForegroundColor Gray
    git commit -m $commitMessage
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? Hotfix committed!" -ForegroundColor Green
        
        Write-Host "`n"
        $push = Read-Host "Push to GitHub? (y/n)"
        
        if ($push -eq 'y' -or $push -eq 'Y') {
            Write-Host "`nPushing..." -ForegroundColor Gray
            git push origin master
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "? Pushed to GitHub!" -ForegroundColor Green
                Write-Host "`nYou should now redeploy to the server." -ForegroundColor Yellow
            }
        }
    }
} else {
    Write-Host "Hotfix cancelled." -ForegroundColor Yellow
    git reset HEAD
}

Write-Host "`nPress any key to continue..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
