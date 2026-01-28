# Final Fix Script for Remaining 19 Errors

Write-Host "===== FINAL 19 ERRORS FIX =====" -ForegroundColor Cyan

# Fix 1: Site.Name -> Site.Address (Site doesn't have Name property)
$jobEditFile = "OneManVan.Web\Components\Pages\Jobs\JobEdit.razor"
$jobDetailFile = "OneManVan.Web\Components\Pages\Jobs\JobDetail.razor"

foreach ($file in @($jobEditFile, $jobDetailFile)) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        $content = $content -replace 'site\.Name', 'site.Address'
        $content = $content -replace 'job\.Site\?\.Name', 'job.Site?.Address'
        Set-Content $file $content -NoNewline
        Write-Host "  Fixed Site.Name in: $file" -ForegroundColor Green
    }
}

# Fix 2: TaxAmountAmount -> TaxAmount (double replacement)
$estimateEditFile = "OneManVan.Web\Components\Pages\Estimates\EstimateEdit.razor"
$invoiceEditFile = "OneManVan.Web\Components\Pages\Invoices\InvoiceEdit.razor"

foreach ($file in @($estimateEditFile, $invoiceEditFile)) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        $content = $content -replace 'TaxAmountAmount', 'TaxAmount'
        Set-Content $file $content -NoNewline
        Write-Host "  Fixed TaxAmountAmount in: $file" -ForegroundColor Green
    }
}

# Fix 3: Invoice.Balance -> remove or change approach (BalanceDue is computed, read-only)
if (Test-Path $invoiceEditFile) {
    $content = Get-Content $invoiceEditFile -Raw
    # Remove any assignment to BalanceDue
    $content = $content -replace 'model\.BalanceDue = .*?;', '// BalanceDue is computed property'
    $content = $content -replace 'invoice\.Balance', 'invoice.BalanceDue'
    $content = $content -replace 'model\.Balance', 'model.BalanceDue'
    Set-Content $invoiceEditFile $content -NoNewline
    Write-Host "  Fixed Invoice.Balance in: $invoiceEditFile" -ForegroundColor Green
}

# Fix 4: EstimateStatus.Rejected -> EstimateStatus.Declined
$estimateFiles = @(
    "OneManVan.Web\Components\Pages\Estimates\EstimateList.razor",
    "OneManVan.Web\Components\Pages\Estimates\EstimateDetail.razor",
    "OneManVan.Web\Components\Pages\Estimates\EstimateEdit.razor"
)

foreach ($file in $estimateFiles) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        $content = $content -replace 'EstimateStatus\.Rejected', 'EstimateStatus.Declined'
        $content = $content -replace '"Rejected"', '"Declined"'
        Set-Content $file $content -NoNewline
        Write-Host "  Fixed EstimateStatus.Rejected in: $file" -ForegroundColor Green
    }
}

# Fix 5: CustomerStatus.Prospect -> CustomerStatus.Lead
$customerFiles = @(
    "OneManVan.Web\Components\Pages\Customers\CustomerList.razor",
    "OneManVan.Web\Components\Pages\Customers\CustomerDetail.razor",
    "OneManVan.Web\Components\Pages\Customers\CustomerEdit.razor"
)

foreach ($file in $customerFiles) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        $content = $content -replace 'CustomerStatus\.Prospect', 'CustomerStatus.Lead'
        Set-Content $file $content -NoNewline
        Write-Host "  Fixed CustomerStatus.Prospect in: $file" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "===== ALL FIXES APPLIED =====" -ForegroundColor Green
Write-Host "Run: dotnet build OneManVan.Web\OneManVan.Web.csproj" -ForegroundColor Cyan
