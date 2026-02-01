# Commit All Entity Tracking Fixes - Complete
# ALL 11 Edit Pages Fixed

Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Green
Write-Host "?    Committing Entity Tracking Fixes - ALL 11 PAGES COMPLETE   ?" -ForegroundColor Green
Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Green

Write-Host "`n? ALL Pages Fixed:" -ForegroundColor Cyan
$pages = @(
    "CustomerEdit (13 properties)",
    "ProductEdit (61 properties)",
    "AssetEdit (81 properties)",
    "JobEdit (75 properties)",
    "InvoiceEdit (23 properties)",
    "EstimateEdit (17 properties)",
    "InventoryEdit (24 properties)",
    "SiteEdit (56 properties)",
    "AgreementEdit (57 properties)",
    "CompanyEdit (21 properties)",
    "EmployeeEdit (58 properties)"
)

for ($i = 0; $i -lt $pages.Count; $i++) {
    Write-Host "  $($i+1). $($pages[$i])" -ForegroundColor White
}

Write-Host "`n?? Staging changes..." -ForegroundColor Yellow
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

# Also add documentation
git add CUSTOMER_EDIT_EMPTY_FORM_FIX.md 2>$null
git add ENTITY_TRACKING_FIX_ALL_PAGES.md 2>$null
git add ENTITY_TRACKING_FIX_PROGRESS.md 2>$null
git add CSP_HEADERS_FIX.md 2>$null

$commitMessage = @"
Fix: Entity tracking issues in ALL 11 edit pages

## Problem
Edit forms showed empty data when editing existing records due to entity
tracking issues with Blazor Server + EF Core `await using` lifecycle.

When DbContext disposes after loading, tracked entities become detached,
causing empty forms despite data being loaded from database.

## Root Cause
Pages were assigning tracked entity references directly to model:
  model = entity;  // Entity becomes detached when DbContext disposes

## Solution
Create new instances with all properties copied:
  model = new Entity {
      Id = entity.Id,
      Prop1 = entity.Prop1,
      // ... copy ALL properties
  };

This makes models independent POCOs that survive DbContext disposal.

## Pages Fixed (11/11 - 100% Complete)

HIGH PRIORITY (User-Facing):
- CustomerEdit (13 properties)
- ProductEdit (61 properties)
- AssetEdit (81 properties)
- JobEdit (75 properties)
- InvoiceEdit (23 properties)
- EstimateEdit (17 properties)

MEDIUM PRIORITY:
- InventoryEdit (24 properties)
- SiteEdit (56 properties)

LOW PRIORITY (Admin):
- AgreementEdit (57 properties)
- CompanyEdit (21 properties)
- EmployeeEdit (58 properties)

## Testing
All edit forms now:
- Properly populate with existing data on load
- Save changes correctly
- Reflect changes on list pages
- Work with CSP headers enabled

## Total Properties Copied
486 properties across 11 pages

Co-authored-by: GitHub Copilot <noreply@github.com>
"@

Write-Host "`n? Committing..." -ForegroundColor Green
git commit -m $commitMessage

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Commit successful!" -ForegroundColor Green
    
    $push = Read-Host "`nPush to GitHub? (y/n)"
    if ($push -eq 'y' -or $push -eq 'Y') {
        git push origin master
        if ($LASTEXITCODE -eq 0) {
            Write-Host "? Pushed to GitHub!" -ForegroundColor Green
            Write-Host "`n?? Ready to deploy to server!" -ForegroundColor Cyan
        }
    }
} else {
    Write-Host "? Commit failed!" -ForegroundColor Red
}

Write-Host "`nPress any key to continue..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
