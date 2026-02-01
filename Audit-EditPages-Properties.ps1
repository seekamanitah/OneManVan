# Audit All Edit Pages for Missing Properties
# Checks each LoadAsync method against its model to find missing properties

Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?        Entity Tracking Fix - Property Completeness Audit      ?" -ForegroundColor Cyan
Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Cyan

$issues = @()

# Helper function to check if a property is in the LoadAsync method
function Test-PropertyCopied {
    param($editFile, $propertyName)
    
    $content = Get-Content $editFile -Raw
    return $content -match "$propertyName\s*=\s*\w+\.$propertyName"
}

# Helper function to get all public properties from a model
function Get-ModelProperties {
    param($modelFile)
    
    $content = Get-Content $modelFile -Raw
    $properties = [regex]::Matches($content, 'public\s+(\w+\??)\s+(\w+)\s*{\s*get;')
    
    $props = @()
    foreach ($match in $properties) {
        $props += $match.Groups[2].Value
    }
    return $props
}

Write-Host "`n?? Auditing all 11 edit pages...`n" -ForegroundColor Yellow

# 1. CustomerEdit
Write-Host "1. CustomerEdit" -ForegroundColor Cyan
$customerProps = Get-ModelProperties "OneManVan.Shared\Models\Customer.cs"
$missing = @()
foreach ($prop in $customerProps) {
    if ($prop -notin @("Customer", "Assets", "Sites", "Jobs", "Estimates", "Invoices", "ServiceAgreements", "CustomFields", "Companies", "CustomerRoles")) {
        if (!(Test-PropertyCopied "OneManVan.Web\Components\Pages\Customers\CustomerEdit.razor" $prop)) {
            $missing += $prop
        }
    }
}
if ($missing) {
    Write-Host "   ??  Missing: $($missing -join ', ')" -ForegroundColor Yellow
    $issues += @{Page="CustomerEdit"; Missing=$missing}
} else {
    Write-Host "   ? Complete" -ForegroundColor Green
}

# 2. ProductEdit
Write-Host "2. ProductEdit" -ForegroundColor Cyan
$productProps = Get-ModelProperties "OneManVan.Shared\Models\Product.cs"
$missing = @()
foreach ($prop in $productProps) {
    if ($prop -notin @("Assets", "EstimateLines", "Tonnage", "CreatedAt")) {
        if (!(Test-PropertyCopied "OneManVan.Web\Components\Pages\Products\ProductEdit.razor" $prop)) {
            $missing += $prop
        }
    }
}
if ($missing) {
    Write-Host "   ??  Missing: $($missing -join ', ')" -ForegroundColor Yellow
    $issues += @{Page="ProductEdit"; Missing=$missing}
} else {
    Write-Host "   ? Complete" -ForegroundColor Green
}

# 3. AssetEdit
Write-Host "3. AssetEdit" -ForegroundColor Cyan
$assetProps = Get-ModelProperties "OneManVan.Shared\Models\Asset.cs"
$missing = @()
foreach ($prop in $assetProps) {
    if ($prop -notin @("Customer", "Site", "ReplacedByAsset", "CustomFields", "WarrantyEndDate", "Company")) {
        if (!(Test-PropertyCopied "OneManVan.Web\Components\Pages\Assets\AssetEdit.razor" $prop)) {
            $missing += $prop
        }
    }
}
if ($missing) {
    Write-Host "   ??  Missing: $($missing -join ', ')" -ForegroundColor Yellow
    $issues += @{Page="AssetEdit"; Missing=$missing}
} else {
    Write-Host "   ? Complete" -ForegroundColor Green
}

# 4. JobEdit
Write-Host "4. JobEdit" -ForegroundColor Cyan
$jobProps = Get-ModelProperties "OneManVan.Shared\Models\Job.cs"
$missing = @()
foreach ($prop in $jobProps) {
    if ($prop -notin @("Estimate", "Customer", "Site", "Asset", "FollowUpJob", "FollowUpFromJob", "TimeEntries", "Invoices", "ActualHours", "StatusDisplay")) {
        if (!(Test-PropertyCopied "OneManVan.Web\Components\Pages\Jobs\JobEdit.razor" $prop)) {
            $missing += $prop
        }
    }
}
if ($missing) {
    Write-Host "   ??  Missing: $($missing -join ', ')" -ForegroundColor Yellow
    $issues += @{Page="JobEdit"; Missing=$missing}
} else {
    Write-Host "   ? Complete" -ForegroundColor Green
}

# 5. InvoiceEdit
Write-Host "5. InvoiceEdit" -ForegroundColor Cyan
$invoiceProps = Get-ModelProperties "OneManVan.Shared\Models\Invoice.cs"
$missing = @()
foreach ($prop in $invoiceProps) {
    if ($prop -notin @("Job", "Estimate", "Customer", "Payments", "LineItems", "BalanceDue", "IsPaid", "IsOverdue", "StatusDisplay")) {
        if (!(Test-PropertyCopied "OneManVan.Web\Components\Pages\Invoices\InvoiceEdit.razor" $prop)) {
            $missing += $prop
        }
    }
}
if ($missing) {
    Write-Host "   ??  Missing: $($missing -join ', ')" -ForegroundColor Yellow
    $issues += @{Page="InvoiceEdit"; Missing=$missing}
} else {
    Write-Host "   ? Complete" -ForegroundColor Green
}

# 6. EstimateEdit
Write-Host "6. EstimateEdit" -ForegroundColor Cyan
$estimateProps = Get-ModelProperties "OneManVan.Shared\Models\Estimate.cs"
$missing = @()
foreach ($prop in $estimateProps) {
    if ($prop -notin @("Customer", "Site", "Asset", "Lines", "Jobs")) {
        if (!(Test-PropertyCopied "OneManVan.Web\Components\Pages\Estimates\EstimateEdit.razor" $prop)) {
            $missing += $prop
        }
    }
}
if ($missing) {
    Write-Host "   ??  Missing: $($missing -join ', ')" -ForegroundColor Yellow
    $issues += @{Page="EstimateEdit"; Missing=$missing}
} else {
    Write-Host "   ? Complete" -ForegroundColor Green
}

# 7. InventoryEdit
Write-Host "7. InventoryEdit" -ForegroundColor Cyan
$inventoryProps = Get-ModelProperties "OneManVan.Shared\Models\InventoryItem.cs"
$missing = @()
foreach ($prop in $inventoryProps) {
    if ($prop -notin @("Logs", "EstimateLines", "IsLowStock", "IsOutOfStock", "ProfitMargin")) {
        if (!(Test-PropertyCopied "OneManVan.Web\Components\Pages\Inventory\InventoryEdit.razor" $prop)) {
            $missing += $prop
        }
    }
}
if ($missing) {
    Write-Host "   ??  Missing: $($missing -join ', ')" -ForegroundColor Yellow
    $issues += @{Page="InventoryEdit"; Missing=$missing}
} else {
    Write-Host "   ? Complete" -ForegroundColor Green
}

# 8. SiteEdit
Write-Host "8. SiteEdit" -ForegroundColor Cyan
$siteProps = Get-ModelProperties "OneManVan.Shared\Models\Site.cs"
$missing = @()
foreach ($prop in $siteProps) {
    if ($prop -notin @("Customer", "Company", "Assets", "Jobs", "Estimates", "ServiceAgreements")) {
        if (!(Test-PropertyCopied "OneManVan.Web\Components\Pages\Sites\SiteEdit.razor" $prop)) {
            $missing += $prop
        }
    }
}
if ($missing) {
    Write-Host "   ??  Missing: $($missing -join ', ')" -ForegroundColor Yellow
    $issues += @{Page="SiteEdit"; Missing=$missing}
} else {
    Write-Host "   ? Complete" -ForegroundColor Green
}

# 9. AgreementEdit
Write-Host "9. AgreementEdit" -ForegroundColor Cyan
$agreementProps = Get-ModelProperties "OneManVan.Shared\Models\ServiceAgreement.cs"
$missing = @()
foreach ($prop in $agreementProps) {
    if ($prop -notin @("Customer", "Site", "Jobs")) {
        if (!(Test-PropertyCopied "OneManVan.Web\Components\Pages\ServiceAgreements\AgreementEdit.razor" $prop)) {
            $missing += $prop
        }
    }
}
if ($missing) {
    Write-Host "   ??  Missing: $($missing -join ', ')" -ForegroundColor Yellow
    $issues += @{Page="AgreementEdit"; Missing=$missing}
} else {
    Write-Host "   ? Complete" -ForegroundColor Green
}

# 10. CompanyEdit
Write-Host "10. CompanyEdit" -ForegroundColor Cyan
$companyProps = Get-ModelProperties "OneManVan.Shared\Models\Company.cs"
$missing = @()
foreach ($prop in $companyProps) {
    if ($prop -notin @("ContactCustomer", "Customers", "Assets", "Sites", "CustomerRoles", "OwnedAssets")) {
        if (!(Test-PropertyCopied "OneManVan.Web\Components\Pages\Companies\CompanyEdit.razor" $prop)) {
            $missing += $prop
        }
    }
}
if ($missing) {
    Write-Host "   ??  Missing: $($missing -join ', ')" -ForegroundColor Yellow
    $issues += @{Page="CompanyEdit"; Missing=$missing}
} else {
    Write-Host "   ? Complete" -ForegroundColor Green
}

# 11. EmployeeEdit
Write-Host "11. EmployeeEdit" -ForegroundColor Cyan
$employeeProps = Get-ModelProperties "OneManVan.Shared\Models\Employee.cs"
$missing = @()
foreach ($prop in $employeeProps) {
    if ($prop -notin @("TimeLogs", "PerformanceNotes", "Payments")) {
        if (!(Test-PropertyCopied "OneManVan.Web\Components\Pages\Employees\EmployeeEdit.razor" $prop)) {
            $missing += $prop
        }
    }
}
if ($missing) {
    Write-Host "   ??  Missing: $($missing -join ', ')" -ForegroundColor Yellow
    $issues += @{Page="EmployeeEdit"; Missing=$missing}
} else {
    Write-Host "   ? Complete" -ForegroundColor Green
}

# Summary
Write-Host "`n??????????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?                        AUDIT SUMMARY                           ?" -ForegroundColor Cyan
Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Cyan

if ($issues.Count -eq 0) {
    Write-Host "`n? ALL PAGES COMPLETE - No missing properties!" -ForegroundColor Green
} else {
    Write-Host "`n??  Found $($issues.Count) page(s) with missing properties:`n" -ForegroundColor Yellow
    
    foreach ($issue in $issues) {
        Write-Host "?? $($issue.Page):" -ForegroundColor Yellow
        Write-Host "   Missing: $($issue.Missing -join ', ')" -ForegroundColor White
    }
    
    Write-Host "`n?? Recommendation:" -ForegroundColor Cyan
    Write-Host "Add the missing properties to each page's LoadAsync method" -ForegroundColor White
    Write-Host "Follow the same pattern as CustomerEdit" -ForegroundColor Gray
}

Write-Host "`nPress any key to close..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
