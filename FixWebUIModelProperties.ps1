# Fix Web UI Model Property Names
# Invoice and Estimate property mismatches

Write-Host "Fixing Invoice property names..." -ForegroundColor Cyan

$invoiceFiles = @(
    "OneManVan.Web\Components\Pages\Invoices\InvoiceList.razor",
    "OneManVan.Web\Components\Pages\Invoices\InvoiceDetail.razor",
    "OneManVan.Web\Components\Pages\Invoices\InvoiceEdit.razor"
)

foreach ($file in $invoiceFiles) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        
        # Replace Invoice property names
        $content = $content -replace 'invoice\.Subtotal', 'invoice.SubTotal'
        $content = $content -replace 'model\.Subtotal', 'model.SubTotal'
        $content = $content -replace 'invoice\.Tax([^A-Za-z])', 'invoice.TaxAmount$1'
        $content = $content -replace 'model\.Tax([^A-Za-z])', 'model.TaxAmount$1'
        $content = $content -replace '@model\.Tax', '@model.TaxAmount'
        $content = $content -replace 'invoice\.Balance', 'invoice.BalanceDue'
        $content = $content -replace 'model\.Balance', 'model.BalanceDue'
        $content = $content -replace 'PaymentTerms', 'Terms'
        $content = $content -replace 'model\.ModifiedAt =', '// model.ModifiedAt ='
        
        Set-Content $file $content -NoNewline
        Write-Host "  Fixed: $file" -ForegroundColor Green
    }
}

Write-Host "`nFixing Estimate property names..." -ForegroundColor Cyan

$estimateFiles = @(
    "OneManVan.Web\Components\Pages\Estimates\EstimateList.razor",
    "OneManVan.Web\Components\Pages\Estimates\EstimateDetail.razor",
    "OneManVan.Web\Components\Pages\Estimates\EstimateEdit.razor"
)

foreach ($file in $estimateFiles) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        
        # Replace Estimate property names
        $content = $content -replace 'estimate\.Subtotal', 'estimate.SubTotal'
        $content = $content -replace 'model\.Subtotal', 'model.SubTotal'
        $content = $content -replace 'estimate\.Tax([^A-Za-z])', 'estimate.TaxAmount$1'
        $content = $content -replace 'model\.Tax([^A-Za-z])', 'model.TaxAmount$1'
        $content = $content -replace '@model\.Tax', '@model.TaxAmount'
        $content = $content -replace '\.LineItems', '.Lines'
        $content = $content -replace 'model\.ModifiedAt =', '// model.ModifiedAt ='
        
        # Fix date properties
        $content = $content -replace 'EstimateDate', 'CreatedAt'
        $content = $content -replace 'ValidUntil', 'ExpiresAt'
        $content = $content -replace 'EstimateNumber', 'Title'
        
        Set-Content $file $content -NoNewline
        Write-Host "  Fixed: $file" -ForegroundColor Green
    }
}

Write-Host "`nAll property name fixes applied!" -ForegroundColor Green
Write-Host "Run 'dotnet build OneManVan.Web\OneManVan.Web.csproj' to verify" -ForegroundColor Yellow
