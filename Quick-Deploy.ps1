# Quick Deploy Package Creator
# Simple wrapper with common options

Write-Host "??????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?  OneManVan - Quick Package Creator   ?" -ForegroundColor Cyan
Write-Host "??????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

Write-Host "Choose an option:" -ForegroundColor Yellow
Write-Host ""
Write-Host "  1. Create deployment folder only (fastest)" -ForegroundColor White
Write-Host "  2. Create deployment folder + ZIP file" -ForegroundColor White
Write-Host "  3. Create deployment folder + ZIP + Git info" -ForegroundColor White
Write-Host "  4. Exit" -ForegroundColor White
Write-Host ""

$choice = Read-Host "Enter choice (1-4)"

switch ($choice) {
    "1" {
        Write-Host ""
        Write-Host "Creating deployment folder..." -ForegroundColor Green
        .\Create-DeploymentPackage.ps1
    }
    "2" {
        Write-Host ""
        Write-Host "Creating deployment folder and ZIP..." -ForegroundColor Green
        .\Create-DeploymentPackage.ps1 -ZipPackage
    }
    "3" {
        Write-Host ""
        Write-Host "Creating deployment folder + ZIP + Git info..." -ForegroundColor Green
        .\Create-DeploymentPackage.ps1 -ZipPackage -IncludeGit
    }
    "4" {
        Write-Host "Exiting..." -ForegroundColor Gray
        exit
    }
    default {
        Write-Host "Invalid choice. Exiting..." -ForegroundColor Red
        exit
    }
}
