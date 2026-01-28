# Final Web UI Enum and Property Value Fixes

Write-Host "===== FINAL ENUM & PROPERTY VALUE FIXES =====" -ForegroundColor Cyan
Write-Host ""

# ===== CUSTOMER ENUM FIXES =====
Write-Host "Fixing Customer enum values..." -ForegroundColor Yellow

$customerFiles = @(
    "OneManVan.Web\Components\Pages\Customers\CustomerList.razor",
    "OneManVan.Web\Components\Pages\Customers\CustomerEdit.razor"
)

foreach ($file in $customerFiles) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        
        # CustomerStatus.Prospect doesn't exist - use Lead
        $content = $content -replace 'CustomerStatus\.Prospect', 'CustomerStatus.Lead'
        
        # Remove ModifiedAt (doesn't exist in Customer model)
        $content = $content -replace 'model\.ModifiedAt = DateTime\.UtcNow;', '// ModifiedAt not in model'
        
        Set-Content $file $content -NoNewline
        Write-Host "  Fixed: $file" -ForegroundColor Green
    }
}

# ===== ESTIMATE ENUM FIXES =====
Write-Host ""
Write-Host "Fixing Estimate enum values..." -ForegroundColor Yellow

$estimateFiles = @(
    "OneManVan.Web\Components\Pages\Estimates\EstimateDetail.razor",
    "OneManVan.Web\Components\Pages\Estimates\EstimateList.razor"
)

foreach ($file in $estimateFiles) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        
        # EstimateStatus.Rejected doesn't exist - use Declined
        $content = $content -replace 'EstimateStatus\.Rejected', 'EstimateStatus.Declined'
        $content = $content -replace '"Rejected"', '"Declined"'
        
        Set-Content $file $content -NoNewline
        Write-Host "  Fixed: $file" -ForegroundColor Green
    }
}

# ===== PRODUCT PROPERTY FIXES =====
Write-Host ""
Write-Host "Fixing Product property values..." -ForegroundColor Yellow

$productFiles = @(
    "OneManVan.Web\Components\Pages\Products\ProductList.razor",
    "OneManVan.Web\Components\Pages\Products\ProductDetail.razor"
)

foreach ($file in $productFiles) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        
        # Product doesn't have RetailPrice - use SuggestedSellPrice
        $content = $content -replace 'product\.RetailPrice', 'product.SuggestedSellPrice'
        
        # Product doesn't have RequiresSerial - use RegistrationRequired
        $content = $content -replace 'RequiresSerial', 'RegistrationRequired'
        
        # ProductCategory enum to string - need .ToString()
        $content = $content -replace 'GetCategoryDisplay\(product\.Category\)', 'product.Category.ToString()'
        
        # If still using GetCategoryDisplay, convert enum properly
        $content = $content -replace 'string\? categoryFilter', 'ProductCategory? categoryFilter'
        
        Set-Content $file $content -NoNewline
        Write-Host "  Fixed: $file" -ForegroundColor Green
    }
}

# ===== ASSET PROPERTY FIXES (Final cleanup) =====
Write-Host ""
Write-Host "Final Asset cleanup..." -ForegroundColor Yellow

$assetFiles = @(
    "OneManVan.Web\Components\Pages\Assets\AssetDetail.razor",
    "OneManVan.Web\Components\Pages\Assets\AssetList.razor"
)

foreach ($file in $assetFiles) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        
        # Remove any remaining Product references
        $content = $content -replace 'asset\.ProductId', '0'
        $content = $content -replace '@asset\.Product', '@(asset.Brand + " " + asset.Model)'
        $content = $content -replace 'asset\.Product\.', 'asset.'
        
        Set-Content $file $content -NoNewline
        Write-Host "  Fixed: $file" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "===== ALL ENUM & PROPERTY VALUE FIXES COMPLETE =====" -ForegroundColor Green
Write-Host ""
Write-Host "Run: dotnet build OneManVan.Web\OneManVan.Web.csproj" -ForegroundColor Cyan
