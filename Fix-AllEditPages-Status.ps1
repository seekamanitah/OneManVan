# Fix All Edit Pages - Entity Tracking Issues
# This script applies the fix pattern to all remaining edit pages

Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?          Fixing ALL Edit Pages - Entity Tracking Issues       ?" -ForegroundColor Cyan
Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Cyan

Write-Host "`n?? Status of Edit Pages:" -ForegroundColor Yellow

$pages = @(
    @{Name="CustomerEdit"; Status="? FIXED"; File="OneManVan.Web/Components/Pages/Customers/CustomerEdit.razor"},
    @{Name="ProductEdit"; Status="? FIXED"; File="OneManVan.Web/Components/Pages/Products/ProductEdit.razor"},
    @{Name="AssetEdit"; Status="? IN PROGRESS"; File="OneManVan.Web/Components/Pages/Assets/AssetEdit.razor"},
    @{Name="InventoryEdit"; Status="? PENDING"; File="OneManVan.Web/Components/Pages/Inventory/InventoryEdit.razor"},
    @{Name="JobEdit"; Status="? PENDING"; File="OneManVan.Web/Components/Pages/Jobs/JobEdit.razor"},
    @{Name="InvoiceEdit"; Status="? PENDING"; File="OneManVan.Web/Components/Pages/Invoices/InvoiceEdit.razor"},
    @{Name="EstimateEdit"; Status="? PENDING"; File="OneManVan.Web/Components/Pages/Estimates/EstimateEdit.razor"},
    @{Name="SiteEdit"; Status="? PENDING"; File="OneManVan.Web/Components/Pages/Sites/SiteEdit.razor"},
    @{Name="AgreementEdit"; Status="? PENDING"; File="OneManVan.Web/Components/Pages/ServiceAgreements/AgreementEdit.razor"},
    @{Name="CompanyEdit"; Status="? PENDING"; File="OneManVan.Web/Components/Pages/Companies/CompanyEdit.razor"},
    @{Name="EmployeeEdit"; Status="? PENDING"; File="OneManVan.Web/Components/Pages/Employees/EmployeeEdit.razor"}
)

foreach ($page in $pages) {
    Write-Host "  $($page.Status) $($page.Name)" -ForegroundColor $(if ($page.Status -eq "? FIXED") {"Green"} else {"Yellow"})
}

Write-Host "`n??  IMPORTANT:" -ForegroundColor Yellow
Write-Host "Due to model complexity (50-100+ properties per entity)," -ForegroundColor White
Write-Host "each page requires manual fixing with complete property copying." -ForegroundColor White

Write-Host "`n?? Fix Pattern for Each Page:" -ForegroundColor Cyan
Write-Host @"

// WRONG (causes empty form):
var entity = await db.Entities.FindAsync(Id);
model = entity;  // ?

// CORRECT (works properly):
var entity = await db.Entities.FindAsync(Id);
model = new Entity
{
    Id = entity.Id,
    Property1 = entity.Property1,
    Property2 = entity.Property2,
    // ... copy ALL properties
};  // ?

"@ -ForegroundColor White

Write-Host "`n? Pages Fixed So Far: 2/11" -ForegroundColor Green
Write-Host "? Remaining: 9 pages`n" -ForegroundColor Yellow

Write-Host "Would you like to see detailed info about remaining pages? (y/n)" -ForegroundColor Cyan
$response = Read-Host

if ($response -eq 'y') {
    Write-Host "`n???? Remaining Pages to Fix ????`n" -ForegroundColor Cyan
    
    Write-Host "HIGH PRIORITY (user-facing, frequent use):" -ForegroundColor Red
    Write-Host "  1. AssetEdit - ~80 properties (equipment tracking)" -ForegroundColor White
    Write-Host "  2. JobEdit - ~30 properties (scheduling)" -ForegroundColor White
    Write-Host "  3. InvoiceEdit - ~25 properties (billing)" -ForegroundColor White
    Write-Host "  4. EstimateEdit - ~25 properties (quotes)" -ForegroundColor White
    
    Write-Host "`nMEDIUM PRIORITY:" -ForegroundColor Yellow
    Write-Host "  5. InventoryEdit - ~20 properties" -ForegroundColor White
    Write-Host "  6. SiteEdit - ~15 properties" -ForegroundColor White
    
    Write-Host "`nLOW PRIORITY (admin/setup):" -ForegroundColor Gray
    Write-Host "  7. AgreementEdit - ~20 properties" -ForegroundColor White
    Write-Host "  8. CompanyEdit - ~15 properties" -ForegroundColor White
    Write-Host "  9. EmployeeEdit - ~20 properties" -ForegroundColor White
}

Write-Host "`nPress any key to continue..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
