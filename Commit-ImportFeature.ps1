# Commit Import Functionality Changes
# Run this script to commit all import-related changes

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Committing Import Functionality Changes" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Show current status
Write-Host "Current changes:" -ForegroundColor Yellow
git status --short

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Key Changes:" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Import Services" -ForegroundColor Green
Write-Host "   - IImportService.cs - Interface definitions"
Write-Host "   - CsvImportService.cs - CSV import implementation"
Write-Host "   - ImportController.cs - API endpoints"
Write-Host ""
Write-Host "2. ImportButton Component" -ForegroundColor Green
Write-Host "   - Supports CSV and Excel (.xlsx, .xls) files"
Write-Host "   - Fixed HttpClient to use correct base URL for remote access"
Write-Host ""
Write-Host "3. Import Buttons Added to All List Pages" -ForegroundColor Green
Write-Host "   - Customers, Products, Assets, Inventory"
Write-Host "   - Jobs, Invoices, Estimates"
Write-Host "   - Companies, Sites, Agreements"
Write-Host "   - Employees, Warranty Claims, Material Lists"
Write-Host ""
Write-Host "4. Settings Page Export/Import Section" -ForegroundColor Green
Write-Host "   - Bulk export with checkboxes"
Write-Host "   - Import for all entity types"
Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan

# Add all changes
Write-Host ""
Write-Host "Adding all changes..." -ForegroundColor Yellow
git add -A

# Commit
$commitMsg = @"
Feature: Add import functionality with remote access fix

NEW FILES:
- Services/Import/IImportService.cs - Import interfaces and DTOs
- Services/Import/CsvImportService.cs - CSV import implementation
- Controllers/ImportController.cs - Import API endpoints
- Components/Shared/ImportButton.razor - Reusable import button

IMPORT FEATURES:
- CSV and Excel (.xlsx, .xls) file support
- Preview before import
- Duplicate detection with update option
- Validation with error reporting
- Template download for each entity type

FIX: HttpClient now uses NavigationManager.BaseUri
- Resolves 'Connection refused localhost:5024' error
- Import now works from remote computers

PAGES UPDATED (Import buttons added):
- CustomerList, ProductList, AssetList, InventoryList
- JobList, InvoiceList, EstimateList
- CompanyList, SiteList, AgreementList
- EmployeeList, ClaimsList, MaterialListIndex

SETTINGS PAGE:
- Added Export/Import section
- Checkbox selection for bulk export
- Import dropdown for all 13 entity types

API ENDPOINTS:
- POST /api/import/{entityType} - Import data
- POST /api/import/{entityType}/preview - Preview import
- GET /api/import/template/{entityType} - Download template
"@

Write-Host "Committing..." -ForegroundColor Yellow
git commit -m $commitMsg

if ($LASTEXITCODE -ne 0) {
    Write-Host "Commit failed or nothing to commit" -ForegroundColor Red
} else {
    Write-Host "Commit successful!" -ForegroundColor Green
}

# Push
Write-Host ""
Write-Host "Pushing to GitHub..." -ForegroundColor Yellow
git push origin master

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "==========================================" -ForegroundColor Cyan
    Write-Host "Successfully pushed to GitHub!" -ForegroundColor Green
    Write-Host "==========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Repository: https://github.com/seekamanitah/OneManVan" -ForegroundColor White
    Write-Host ""
    Write-Host "==========================================" -ForegroundColor Yellow
    Write-Host "SERVER DEPLOYMENT INSTRUCTIONS:" -ForegroundColor Yellow
    Write-Host "==========================================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "SSH to your server and run:" -ForegroundColor White
    Write-Host ""
    Write-Host "cd /path/to/OneManVan" -ForegroundColor Gray
    Write-Host "docker-compose down" -ForegroundColor Gray
    Write-Host "git pull origin master" -ForegroundColor Gray
    Write-Host "docker-compose build --no-cache onemanvan-web" -ForegroundColor Gray
    Write-Host "docker-compose up -d" -ForegroundColor Gray
    Write-Host ""
    Write-Host "==========================================" -ForegroundColor Cyan
} else {
    Write-Host "Push failed!" -ForegroundColor Red
    Write-Host "To retry: git push origin master" -ForegroundColor Yellow
}
