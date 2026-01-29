# OneManVan Project Cleanup Script
# Removes unused scripts and empty files identified in the code audit

Write-Host "OneManVan Project Cleanup" -ForegroundColor Cyan
Write-Host "=========================" -ForegroundColor Cyan
Write-Host ""

# Files to delete (one-time fix scripts that are no longer needed)
$filesToDelete = @(
    # Fix scripts (already applied)
    "FixAllDisplayAlertWarnings.ps1",
    "FixDisplayAlertWarnings.ps1",
    "FixFinal19Errors.ps1",
    "FixMobileCSVMethods.ps1",
    "FixRemainingWarnings.ps1",
    "FixWebUIAssetFinalCleanup.ps1",
    "FixWebUIAssetProperties.ps1",
    "FixWebUIEnumsAndValues.ps1",
    "FixWebUIEstimatePropertiesRound2.ps1",
    "FixWebUIFinalComprehensive.ps1",
    "FixWebUIInventoryReferences.ps1",
    "FixWebUIModelProperties.ps1",
    "FixWebUINavigationCleanup.ps1",
    "FixWebExportProperties.ps1",
    "FixDialogUIScaling.ps1",
    "FixMobilePerformance.ps1",
    
    # Emoji removal scripts (already applied)
    "RemoveAllEmojis.ps1",
    "RemoveAllEmojisDesktopComplete.ps1",
    "RemoveDesktopEmojis.ps1",
    "RemoveDesktopEmojisComprehensive.ps1",
    
    # Branding update scripts (already applied)
    "UpdateDesktopBranding.ps1",
    "UpdateMobileBranding.ps1",
    "UpdateWebBranding.ps1",
    
    # Empty Razor file
    "OneManVan.Web\Components\Pages\Customers\CustomerDetail_CLEAN.razor"
)

$deletedCount = 0
$skippedCount = 0

foreach ($file in $filesToDelete) {
    if (Test-Path $file) {
        Remove-Item $file -Force
        Write-Host "  [DELETED] $file" -ForegroundColor Green
        $deletedCount++
    } else {
        Write-Host "  [SKIPPED] $file (not found)" -ForegroundColor Gray
        $skippedCount++
    }
}

Write-Host ""
Write-Host "Summary:" -ForegroundColor Yellow
Write-Host "  Deleted: $deletedCount files" -ForegroundColor Green
Write-Host "  Skipped: $skippedCount files" -ForegroundColor Gray
Write-Host ""

# List remaining scripts for reference
Write-Host "Remaining scripts (KEEP these):" -ForegroundColor Cyan
$remainingScripts = Get-ChildItem -Path "." -Filter "*.ps1" -Depth 1 | Select-Object -ExpandProperty Name
foreach ($script in $remainingScripts) {
    Write-Host "  - $script" -ForegroundColor White
}

Write-Host ""
Write-Host "Cleanup complete!" -ForegroundColor Green
