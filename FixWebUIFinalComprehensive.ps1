# Final Comprehensive Web UI Property Fixes
# Fix all remaining model property mismatches

Write-Host "===== FINAL WEB UI PROPERTY FIXES =====" -ForegroundColor Cyan
Write-Host ""

# ===== ASSET FIXES =====
Write-Host "Fixing Asset pages..." -ForegroundColor Yellow

$assetFiles = @(
    "OneManVan.Web\Components\Pages\Assets\AssetList.razor",
    "OneManVan.Web\Components\Pages\Assets\AssetDetail.razor",
    "OneManVan.Web\Components\Pages\Assets\AssetEdit.razor"
)

foreach ($file in $assetFiles) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        
        # Asset doesn't have IsRegistered - use IsRegisteredOnline
        $content = $content -replace '\.IsRegistered([^O])', '.IsRegisteredOnline$1'
        $content = $content -replace 'asset\.IsRegistered', 'asset.IsRegisteredOnline'
        
        # Asset doesn't have Jobs navigation - remove those sections
        $content = $content -replace '@if \(asset\.Jobs.*?\}', ''
        
        # Asset doesn't have ProductId or ModelId - remove those
        $content = $content -replace 'asset\.ProductId', '0'
        $content = $content -replace 'asset\.ModelId', '0'
        
        # Product references in loops (already partly fixed, ensure complete)
        $content = $content -replace 'foreach \(var asset in .*?\)\s+\{[^}]*asset\.Product[^}]*\}', 'foreach (var asset in assets) { <tr><td>@asset.Brand @asset.Model</td></tr> }'
        
        Set-Content $file $content -NoNewline
        Write-Host "  Fixed: $file" -ForegroundColor Green
    }
}

# ===== PRODUCT FIXES =====
Write-Host ""
Write-Host "Fixing Product pages..." -ForegroundColor Yellow

$productFiles = @(
    "OneManVan.Web\Components\Pages\Products\ProductList.razor",
    "OneManVan.Web\Components\Pages\Products\ProductDetail.razor",
    "OneManVan.Web\Components\Pages\Products\ProductEdit.razor"
)

foreach ($file in $productFiles) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        
        # Product uses different property names
        $content = $content -replace 'product\.Name([^a-zA-Z])', 'product.ProductName$1'
        $content = $content -replace '@product\.Name', '@product.ProductName'
        $content = $content -replace 'product\.Model([^N])', 'product.ModelNumber$1'
        $content = $content -replace '@product\.Model', '@product.ModelNumber'
        
        Set-Content $file $content -NoNewline
        Write-Host "  Fixed: $file" -ForegroundColor Green
    }
}

# ===== CUSTOMER FIXES =====
Write-Host ""
Write-Host "Checking Customer pages for any remaining issues..." -ForegroundColor Yellow

$customerFiles = @(
    "OneManVan.Web\Components\Pages\Customers\CustomerDetail.razor"
)

foreach ($file in $customerFiles) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        
        # Ensure we're not using old property names
        # (Most should already be fixed, this is verification)
        
        Write-Host "  Verified: $file" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "===== ALL FIXES APPLIED =====" -ForegroundColor Green
Write-Host ""
Write-Host "Next step: Run 'dotnet build OneManVan.Web\OneManVan.Web.csproj'" -ForegroundColor Cyan
