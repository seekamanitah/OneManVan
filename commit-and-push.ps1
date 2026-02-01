# Commit and Push Script
# Commits all staged changes and pushes to GitHub

param(
    [string]$Message = "Fix Blazor buttons issue and add new features"
)

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Git Commit and Push" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Check if there are changes
$status = git status --porcelain
if ([string]::IsNullOrWhiteSpace($status)) {
    Write-Host "No changes to commit!" -ForegroundColor Yellow
    exit 0
}

# Show what will be committed
Write-Host "Changes to be committed:" -ForegroundColor Yellow
git status --short
Write-Host ""

# Confirm
$confirm = Read-Host "Commit with message: '$Message'? (Y/N)"
if ($confirm -ne 'Y' -and $confirm -ne 'y') {
    Write-Host "Aborted." -ForegroundColor Red
    exit 1
}

# Commit
Write-Host ""
Write-Host "Committing changes..." -ForegroundColor Yellow
git commit -m $Message

if ($LASTEXITCODE -ne 0) {
    Write-Host "Commit failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Committed successfully!" -ForegroundColor Green

# Push
Write-Host ""
Write-Host "Pushing to GitHub..." -ForegroundColor Yellow
git push origin master

if ($LASTEXITCODE -ne 0) {
    Write-Host "Push failed!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Common fixes:" -ForegroundColor Yellow
    Write-Host "  1. Check your internet connection" -ForegroundColor White
    Write-Host "  2. Verify GitHub credentials" -ForegroundColor White
    Write-Host "  3. Pull latest changes first: git pull origin master" -ForegroundColor White
    exit 1
}

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Success!" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Your changes have been pushed to GitHub!" -ForegroundColor Green
Write-Host "View at: https://github.com/seekamanitah/OneManVan" -ForegroundColor Cyan
Write-Host ""
