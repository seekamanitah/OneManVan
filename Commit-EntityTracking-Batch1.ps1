# Commit Entity Tracking Fixes - Batch 1
# CustomerEdit, ProductEdit, AssetEdit

Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Green
Write-Host "?     Committing Entity Tracking Fixes - High Priority Pages    ?" -ForegroundColor Green
Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Green

Write-Host "`n? Pages Fixed in This Batch:" -ForegroundColor Cyan
Write-Host "  1. CustomerEdit (13 properties)" -ForegroundColor White
Write-Host "  2. ProductEdit (61 properties)" -ForegroundColor White
Write-Host "  3. AssetEdit (81 properties)" -ForegroundColor White

Write-Host "`n?? Staging changes..." -ForegroundColor Yellow
git add OneManVan.Web/Components/Pages/Customers/CustomerEdit.razor
git add OneManVan.Web/Components/Pages/Products/ProductEdit.razor
git add OneManVan.Web/Components/Pages/Assets/AssetEdit.razor
git add CUSTOMER_EDIT_EMPTY_FORM_FIX.md
git add ENTITY_TRACKING_FIX_ALL_PAGES.md
git add ENTITY_TRACKING_FIX_PROGRESS.md

$commitMessage = @"
Fix: Entity tracking issues in CustomerEdit, ProductEdit, AssetEdit

## Problem
Edit forms showed empty data when editing existing records due to entity
tracking issues with Blazor Server + EF Core `await using` lifecycle.

## Root Cause
Pages were assigning tracked entity references directly to model. When
DbContext disposed, entities became detached, causing empty forms.

## Solution  
Create new instances with copied properties instead of assigning tracked
entity references. This makes models independent POCOs that survive
DbContext disposal.

## Pages Fixed (3/11)
- ? CustomerEdit (13 properties)
- ? ProductEdit (61 properties)  
- ? AssetEdit (81 properties)

## Testing
All three edit forms now properly populate with existing data and save correctly.

## Remaining
- JobEdit, InvoiceEdit, EstimateEdit (high priority)
- InventoryEdit, SiteEdit (medium priority)
- AgreementEdit, CompanyEdit, EmployeeEdit (low priority)

Co-authored-by: GitHub Copilot <noreply@github.com>
"@

Write-Host "`n? Committing..." -ForegroundColor Green
git commit -m $commitMessage

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Commit successful!" -ForegroundColor Green
    
    $push = Read-Host "`nPush to GitHub? (y/n)"
    if ($push -eq 'y') {
        git push origin master
        if ($LASTEXITCODE -eq 0) {
            Write-Host "? Pushed to GitHub!" -ForegroundColor Green
        }
    }
} else {
    Write-Host "? Commit failed!" -ForegroundColor Red
}

Write-Host "`nPress any key to continue..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
