# Final Cleanup: Remove Non-Existent Navigation Properties
# Customer and Product don't have all the navigation properties we assumed

Write-Host "===== FINAL CLEANUP: NAVIGATION PROPERTIES =====" -ForegroundColor Cyan
Write-Host ""

# ===== CUSTOMER FIXES =====
Write-Host "Fixing Customer navigation properties..." -ForegroundColor Yellow

$customerFiles = @(
    "OneManVan.Web\Components\Pages\Customers\CustomerList.razor",
    "OneManVan.Web\Components\Pages\Customers\CustomerDetail.razor"
)

foreach ($file in $customerFiles) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        
        # Customer doesn't have Jobs or Invoices navigation
        # Remove or comment out those sections
        $content = $content -replace '\.Include\(c => c\.Jobs\)', ''
        $content = $content -replace '\.Include\(c => c\.Invoices\)', ''
        $content = $content -replace '@if \(customer\.Jobs.*?@\}', '<!-- Jobs section removed - use query instead -->'
        $content = $content -replace '@if \(customer\.Invoices.*?@\}', '<!-- Invoices section removed - use query instead -->'
        $content = $content -replace 'customer\.Jobs', 'new List<Job>()'
        $content = $content -replace 'customer\.Invoices', 'new List<Invoice>()'
        
        Set-Content $file $content -NoNewline
        Write-Host "  Fixed: $file" -ForegroundColor Green
    }
}

# ===== PRODUCT FIXES =====
Write-Host ""
Write-Host "Fixing Product properties and navigation..." -ForegroundColor Yellow

$productFiles = @(
    "OneManVan.Web\Components\Pages\Products\ProductList.razor",
    "OneManVan.Web\Components\Pages\Products\ProductDetail.razor",
    "OneManVan.Web\Components\Pages\Products\ProductEdit.razor"
)

foreach ($file in $productFiles) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        
        # Fix double replacement issue (ModelNumberNumber -> ModelNumber)
        $content = $content -replace 'ModelNumberNumber', 'ModelNumber'
        
        # Product doesn't have SKU - use ProductNumber
        $content = $content -replace 'product\.SKU', 'product.ProductNumber'
        
        # Product doesn't have Price - use Cost or wholesale
        $content = $content -replace 'product\.Price([^a-zA-Z])', 'product.RetailPrice$1'
        $content = $content -replace '@product\.Price', '@product.RetailPrice'
        
        # Product doesn't have Assets navigation
        $content = $content -replace '\.Include\(p => p\.Assets\)', ''
        $content = $content -replace 'product\.Assets', 'new List<Asset>()'
        $content = $content -replace '@if \(product\.Assets.*?@\}', '<!-- Assets section removed -->'
        
        # RequiresSerialNumber - Product uses TracksInventory
        $content = $content -replace 'RequiresSerialNumber', 'RequiresSerial'
        
        # Category enum to string conversion
        $content = $content -replace 'GetCategoryDisplay\(product\.Category\)', 'product.Category.ToString()'
        
        Set-Content $file $content -NoNewline
        Write-Host "  Fixed: $file" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "===== CLEANUP COMPLETE =====" -ForegroundColor Green
Write-Host ""
Write-Host "Run: dotnet build OneManVan.Web\OneManVan.Web.csproj" -ForegroundColor Cyan
