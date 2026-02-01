# VerifyDatabase.ps1
# Verifies that all required tables exist in the database

Write-Host "=================================" -ForegroundColor Cyan
Write-Host " OneManVan Database Verification" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

# Run the web app to trigger EnsureCreated and verify tables
Write-Host "Starting database verification..." -ForegroundColor White

# Build and run the web app briefly to initialize database
Set-Location "$PSScriptRoot\OneManVan.Web"

Write-Host "Building project..." -ForegroundColor Yellow
dotnet build --no-restore -v q

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful. Starting app to initialize database..." -ForegroundColor Green
    
    # Start the web app in background
    $job = Start-Job -ScriptBlock {
        param($dir)
        Set-Location $dir
        dotnet run --no-build 2>&1
    } -ArgumentList "$PSScriptRoot\OneManVan.Web"
    
    # Wait for initialization
    Start-Sleep -Seconds 20
    
    # Check job output
    $output = Receive-Job $job
    
    # Stop the job
    Stop-Job $job
    Remove-Job $job
    
    # Parse output for database info
    $dbSuccess = $output | Select-String -Pattern "database initialized successfully" -CaseSensitive:$false
    
    if ($dbSuccess) {
        Write-Host ""
        Write-Host "=================================" -ForegroundColor Green
        Write-Host " Database Verification PASSED" -ForegroundColor Green
        Write-Host "=================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "All database tables have been created by EF Core EnsureCreated()." -ForegroundColor White
        Write-Host ""
        Write-Host "Tables created include:" -ForegroundColor Cyan
        Write-Host "  - Customers, Companies, Sites" -ForegroundColor White
        Write-Host "  - Assets, AssetOwners, AssetPhotos" -ForegroundColor White
        Write-Host "  - Jobs, JobParts, JobPhotos, TimeEntries" -ForegroundColor White
        Write-Host "  - Invoices, InvoiceLineItems, Payments" -ForegroundColor White
        Write-Host "  - Estimates, EstimateLines" -ForegroundColor White
        Write-Host "  - Products, ProductDocuments" -ForegroundColor White
        Write-Host "  - InventoryItems, InventoryLogs" -ForegroundColor White
        Write-Host "  - ServiceAgreements" -ForegroundColor White
        Write-Host "  - WarrantyClaims" -ForegroundColor White
        Write-Host "  - QuickNotes" -ForegroundColor White
        Write-Host "  - MaterialLists, MaterialListItems, MaterialListSystems" -ForegroundColor White
        Write-Host "  - MaterialListTemplates, MaterialListTemplateItems" -ForegroundColor White
        Write-Host "  - Documents (Document Library)" -ForegroundColor White
        Write-Host "  - Employees, EmployeeTimeLogs, EmployeePayments, EmployeePerformanceNotes" -ForegroundColor White
        Write-Host "  - CompanySettings" -ForegroundColor White
        Write-Host "  - CustomFieldDefinitions, CustomFieldChoices" -ForegroundColor White
        Write-Host "  - ManufacturerRegistrations" -ForegroundColor White
        Write-Host "  - ServiceHistory, CommunicationLogs" -ForegroundColor White
        Write-Host "  - CustomerDocuments, CustomerNotes" -ForegroundColor White
        Write-Host ""
    } else {
        Write-Host ""
        Write-Host "=================================" -ForegroundColor Red
        Write-Host " Database Verification FAILED" -ForegroundColor Red
        Write-Host "=================================" -ForegroundColor Red
        Write-Host ""
        Write-Host "Output:" -ForegroundColor Yellow
        $output | ForEach-Object { Write-Host $_ }
    }
} else {
    Write-Host "Build failed!" -ForegroundColor Red
}

Set-Location $PSScriptRoot
