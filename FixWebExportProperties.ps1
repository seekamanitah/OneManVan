# Fix Web Export Property Mismatches

Write-Host "=== Fixing Web Export Service Property Mismatches ===" -ForegroundColor Cyan

# Fix Customer properties (Home not Address)
$files = @(
    "OneManVan.Web\Services\Export\CsvExportService.cs",
    "OneManVan.Web\Services\Export\ExcelExportService.cs",
    "OneManVan.Web\Services\Pdf\InvoicePdfGenerator.cs"
)

foreach ($file in $files) {
    Write-Host "Fixing: $file" -ForegroundColor Yellow
    $content = Get-Content $file -Raw
    
    # Customer address fields
    $content = $content -replace 'c\.Address', 'c.HomeAddress'
    $content = $content -replace 'c\.City', 'c.HomeCity'
    $content = $content -replace 'c\.State', 'c.HomeState'
    $content = $content -replace 'c\.ZipCode', 'c.HomeZipCode'
    $content = $content -replace 'customers\[i\]\.Address', 'customers[i].HomeAddress'
    $content = $content -replace 'customers\[i\]\.City', 'customers[i].HomeCity'
    $content = $content -replace 'customers\[i\]\.State', 'customers[i].HomeState'
    $content = $content -replace 'customers\[i\]\.ZipCode', 'customers[i].HomeZipCode'
    $content = $content -replace 'invoice\.Customer\.Address', 'invoice.Customer.HomeAddress'
    
    # Company address fields  
    $content = $content -replace 'companies\[i\]\.Type', 'companies[i].CompanyType'
    $content = $content -replace 'c\.Type\.ToString\(\)', 'c.CompanyType.ToString()'
    $content = $content -replace 'companies\[i\]\.Address', 'companies[i].BillingAddress'
    $content = $content -replace 'c\.Address,', 'c.BillingAddress,'
    $content = $content -replace 'c\.City,', 'c.BillingCity,'
    $content = $content -replace 'c\.State,', 'c.BillingState,'
    $content = $content -replace 'c\.ZipCode,', 'c.BillingZipCode,'
    
    # Job properties
    $content = $content -replace 'j\.CompletedDate', 'j.CompletionDate'
    $content = $content -replace 'jobs\[i\]\.CompletedDate', 'jobs[i].CompletionDate'
    
    # Asset properties
    $content = $content -replace 'a\.Manufacturer', 'a.Product?.Manufacturer'
    $content = $content -replace 'a\.ModelNumber', 'a.Product?.ModelNumber'
    $content = $content -replace 'a\.SerialNumber', 'a.SerialNumber'
    $content = $content -replace 'assets\[i\]\.Manufacturer', 'assets[i].Product?.Manufacturer'
    $content = $content -replace 'assets\[i\]\.ModelNumber', 'assets[i].Product?.ModelNumber'
    
    # Product properties
    $content = $content -replace 'products\[i\]\.Cost', 'products[i].Cost'
    $content = $content -replace 'products\[i\]\.RetailPrice', 'products[i].Price'
    $content = $content -replace 'p\.Cost,', 'p.Cost,'
    $content = $content -replace 'p\.RetailPrice,', 'p.Price,'
    
    # Estimate properties
    $content = $content -replace 'e\.EstimateDate', 'e.DateCreated'
    $content = $content -replace 'estimates\[i\]\.EstimateDate', 'estimates[i].DateCreated'
    
    # Site properties (Site.Name ? Site.SiteName)
    $content = $content -replace 'sites\[i\]\.Name', 'sites[i].SiteName'
    $content = $content -replace 's\.Name\)', 's.SiteName)'
    $content = $content -replace 's\.ContactName', 's.PrimaryContactName'
    $content = $content -replace 'sites\[i\]\.ContactName', 'sites[i].PrimaryContactName'
    $content = $content -replace 'sites\[i\]\.ContactPhone', 'sites[i].PrimaryContactPhone'
    
    # ServiceAgreement properties
    $content = $content -replace 'sa\.ServiceType', 'sa.AgreementType'
    $content = $content -replace 'agreements\[i\]\.MonthlyFee', 'agreements[i].RecurringAmount'
    $content = $content -replace 'sa\.MonthlyFee,', 'sa.RecurringAmount,'
    
    Set-Content -Path $file -Value $content -NoNewline
    Write-Host "  Fixed!" -ForegroundColor Green
}

Write-Host ""
Write-Host "=== All Property Mismatches Fixed ===" -ForegroundColor Green
Write-Host "Run 'dotnet build' to verify" -ForegroundColor Yellow
