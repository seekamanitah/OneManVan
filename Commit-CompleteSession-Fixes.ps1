# Commit All Entity Tracking Fixes + Database Fixes + Admin Config
# This commits everything we've done in this session

Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Green
Write-Host "?         Committing ALL Fixes from This Session                ?" -ForegroundColor Green
Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Green

Write-Host "`n? Changes to commit:" -ForegroundColor Cyan
Write-Host "  1. Fixed CustomerEdit - Added missing properties (CompanyName, SecondaryEmail, CreatedAt)" -ForegroundColor White
Write-Host "  2. Fixed ProductEdit - All 61 properties" -ForegroundColor White  
Write-Host "  3. Fixed AssetEdit - All 81 properties" -ForegroundColor White
Write-Host "  4. Fixed JobEdit - All 75 properties" -ForegroundColor White
Write-Host "  5. Fixed InvoiceEdit - All 23 properties" -ForegroundColor White
Write-Host "  6. Fixed EstimateEdit - All 17 properties" -ForegroundColor White
Write-Host "  7. Fixed InventoryEdit - All 24 properties" -ForegroundColor White
Write-Host "  8. Fixed SiteEdit - All 56 properties" -ForegroundColor White
Write-Host "  9. Fixed AgreementEdit - All 57 properties" -ForegroundColor White
Write-Host "  10. Fixed CompanyEdit - All 21 properties" -ForegroundColor White
Write-Host "  11. Fixed EmployeeEdit - All 58 properties" -ForegroundColor White
Write-Host "  12. Added AdminUser config to appsettings.json" -ForegroundColor White
Write-Host "  13. Added AdminUser config to appsettings.Development.json" -ForegroundColor White
Write-Host "  14. Created database migration scripts" -ForegroundColor White
Write-Host "  15. Created audit and fix scripts" -ForegroundColor White

Write-Host "`n?? Staging files..." -ForegroundColor Yellow

# Stage all edit page fixes
git add OneManVan.Web/Components/Pages/Customers/CustomerEdit.razor
git add OneManVan.Web/Components/Pages/Products/ProductEdit.razor
git add OneManVan.Web/Components/Pages/Assets/AssetEdit.razor
git add OneManVan.Web/Components/Pages/Jobs/JobEdit.razor
git add OneManVan.Web/Components/Pages/Invoices/InvoiceEdit.razor
git add OneManVan.Web/Components/Pages/Estimates/EstimateEdit.razor
git add OneManVan.Web/Components/Pages/Inventory/InventoryEdit.razor
git add OneManVan.Web/Components/Pages/Sites/SiteEdit.razor
git add OneManVan.Web/Components/Pages/ServiceAgreements/AgreementEdit.razor
git add OneManVan.Web/Components/Pages/Companies/CompanyEdit.razor
git add OneManVan.Web/Components/Pages/Employees/EmployeeEdit.razor

# Stage config files
git add OneManVan.Web/appsettings.json
git add OneManVan.Web/appsettings.Development.json

# Stage documentation
git add CUSTOMER_EDIT_EMPTY_FORM_FIX.md
git add ENTITY_TRACKING_FIX_ALL_PAGES.md
git add ENTITY_TRACKING_FIX_PROGRESS.md
git add CRITICAL_FIXES_SUMMARY.md

# Stage migration scripts
git add Migrations/FixMissingAssetNumber_SQLite.sql
git add FixMissingAssetNumber.ps1
git add FixMissingAssetNumber-Complete.ps1
git add Fix-Database-And-Import-Complete.ps1

# Stage audit scripts
git add Audit-EditPages-Properties.ps1
git add Commit-EntityTracking-AllPages.ps1

$commitMessage = @"
Fix: Complete entity tracking fixes + admin config + database schema

## Issues Fixed

### 1. Entity Tracking in Edit Forms (ALL 11 PAGES)
**Problem:** Edit forms showed empty data when editing existing records.
**Cause:** Blazor Server + EF Core `await using` lifecycle caused entities
to become detached when DbContext disposed.
**Solution:** Create new POCO instances with all properties copied.

### 2. Missing Admin Account Configuration
**Problem:** No admin account created on startup.
**Cause:** Missing AdminUser section in appsettings.json.
**Solution:** Added AdminUser config with default credentials.

### 3. Missing Database Column
**Problem:** SQLite Error - 'no such column: a.AssetNumber'
**Cause:** Database schema out of sync with Asset model.
**Solution:** Created migration scripts to add AssetNumber column.

## Pages Fixed (11/11 - 100% Complete)

| Page | Properties Copied | Status |
|------|------------------|--------|
| CustomerEdit | 16 (added 3 missing) | ? Fixed |
| ProductEdit | 61 | ? Fixed |
| AssetEdit | 81 | ? Fixed |
| JobEdit | 75 | ? Fixed |
| InvoiceEdit | 23 | ? Fixed |
| EstimateEdit | 17 | ? Fixed |
| InventoryEdit | 24 | ? Fixed |
| SiteEdit | 56 | ? Fixed |
| AgreementEdit | 57 | ? Fixed |
| CompanyEdit | 21 | ? Fixed |
| EmployeeEdit | 58 | ? Fixed |

**Total: 486+ properties across 11 pages**

## Configuration Changes

### appsettings.json
- Added `AdminUser` section with default credentials
- Email: admin@onemanvan.com
- Password: Admin@123456!

### appsettings.Development.json  
- Added `AdminUser` section for development environment

## Database Migrations

### Created Migration Scripts
- `Migrations/FixMissingAssetNumber_SQLite.sql`
- `FixMissingAssetNumber-Complete.ps1`
- `Fix-Database-And-Import-Complete.ps1`

### What They Do
- Add `AssetNumber` column to Assets table
- Generate AssetNumbers for existing assets (AST-0001, AST-0002, etc.)
- Fix customers missing CustomerNumbers
- Verify import autogeneration code

## Tools Created

- `Audit-EditPages-Properties.ps1` - Check all pages for missing properties
- `Commit-EntityTracking-AllPages.ps1` - Commit script
- `CRITICAL_FIXES_SUMMARY.md` - Complete documentation

## Testing Checklist

After deployment:
- [ ] All 11 edit forms populate with data
- [ ] Changes save correctly
- [ ] Admin login works (admin@onemanvan.com)
- [ ] Customer detail page loads (no AssetNumber error)
- [ ] CSV import generates IDs automatically

## Breaking Changes
None - All changes are backward compatible.

## Migration Required
Yes - Run database migration scripts:
```powershell
.\Fix-Database-And-Import-Complete.ps1
```

Co-authored-by: GitHub Copilot <noreply@github.com>
"@

Write-Host "`n? Committing..." -ForegroundColor Green
git commit -m $commitMessage

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n? Commit successful!" -ForegroundColor Green
    Write-Host "`n?? Commit Summary:" -ForegroundColor Cyan
    Write-Host "  - 11 edit pages fixed (486+ properties)" -ForegroundColor White
    Write-Host "  - Admin configuration added" -ForegroundColor White
    Write-Host "  - Database migration scripts created" -ForegroundColor White
    Write-Host "  - Audit and fix tools created" -ForegroundColor White
    
    $push = Read-Host "`nPush to GitHub? (y/n)"
    if ($push -eq 'y' -or $push -eq 'Y') {
        Write-Host "`nPushing to origin/master..." -ForegroundColor Yellow
        git push origin master
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "? Pushed to GitHub!" -ForegroundColor Green
            Write-Host "`n?? Ready to deploy!" -ForegroundColor Cyan
        } else {
            Write-Host "? Push failed! Check network connection." -ForegroundColor Red
        }
    }
} else {
    Write-Host "`n? Commit failed!" -ForegroundColor Red
    Write-Host "Check git status for issues" -ForegroundColor Yellow
}

Write-Host "`nPress any key to close..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
