# FINAL FINAL Asset Page Cleanup

Write-Host "===== FINAL ASSET PAGE CLEANUP =====" -ForegroundColor Cyan

$assetFiles = @(
    "OneManVan.Web\Components\Pages\Assets\AssetList.razor",
    "OneManVan.Web\Components\Pages\Assets\AssetDetail.razor"
)

foreach ($file in $assetFiles) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        
        # Fix double replacement issue
        $content = $content -replace 'IsRegisteredOnlineOnline', 'IsRegisteredOnline'
        
        # Asset doesn't have Jobs navigation - remove entirely
        $content = $content -replace '@if \(asset\.Jobs.*?@\}', '<!-- Jobs removed - not in model -->'
        $content = $content -replace 'asset\.Jobs\.Any\(\)', 'false'
        $content = $content -replace 'foreach \(var job in asset\.Jobs\).*?\}', ''
        
        # Fix manufacturer filter - Brand is not an object
        $content = $content -replace '\.Manufacturer', ''
        
        Set-Content $file $content -NoNewline
        Write-Host "  Fixed: $file" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "===== CLEANUP COMPLETE =====" -ForegroundColor Green
Write-Host "Build now!" -ForegroundColor Cyan
