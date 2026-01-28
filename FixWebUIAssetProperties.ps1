# Fix Web UI Asset Property Names
# Asset model property mismatches

Write-Host "Fixing Asset property names..." -ForegroundColor Cyan

$assetFiles = @(
    "OneManVan.Web\Components\Pages\Assets\AssetList.razor",
    "OneManVan.Web\Components\Pages\Assets\AssetDetail.razor",
    "OneManVan.Web\Components\Pages\Assets\AssetEdit.razor"
)

foreach ($file in $assetFiles) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        
        # Replace Asset property names
        $content = $content -replace '\.SerialNumber', '.Serial'
        $content = $content -replace 'asset\.Product', 'asset.Brand + " " + asset.Model'
        $content = $content -replace '@asset\.Product', '@(asset.Brand + " " + asset.Model)'
        $content = $content -replace 'asset\.Product\.Name', 'asset.Brand + " " + asset.Model'
        $content = $content -replace 'asset\.Product\.Manufacturer', 'asset.Brand'
        $content = $content -replace 'asset\.Product\.Model', 'asset.Model'
        $content = $content -replace '\.WarrantyRegistrationNumber', '.RegistrationConfirmation'
        $content = $content -replace '\.WarrantyRegistrationDate', '.RegistrationDate'
        
        # Remove Include Product navigation
        $content = $content -replace '\.Include\(a => a\.Product\)', ''
        $content = $content -replace '\.ThenInclude\(a => a\.Product\)', ''
        
        Set-Content $file $content -NoNewline
        Write-Host "  Fixed: $file" -ForegroundColor Green
    }
}

Write-Host "`nAsset property fixes applied!" -ForegroundColor Green
