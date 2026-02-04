# Cleanup Orphaned Projects Script
# Run this from the solution root directory

$ErrorActionPreference = 'Stop'

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Cleanup Orphaned Projects" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# List of orphaned project folders to remove
$orphanedFolders = @(
    "OneManVan.MauiBlazor",
    "Tools\CheckTables"
)

foreach ($folder in $orphanedFolders) {
    if (Test-Path $folder) {
        Write-Host "Removing orphaned project: $folder" -ForegroundColor Yellow
        Remove-Item -Path $folder -Recurse -Force
        Write-Host "  Removed: $folder" -ForegroundColor Green
    } else {
        Write-Host "  Not found (already removed): $folder" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "  Cleanup Complete!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Now run: dotnet build" -ForegroundColor Cyan
