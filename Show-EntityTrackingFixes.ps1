# Quick Fix Script for High-Priority Edit Pages

Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Yellow
Write-Host "?    Fix Entity Tracking Issues - High Priority Pages Only     ?" -ForegroundColor Yellow
Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Yellow

Write-Host "`n?? Summary of Issue:" -ForegroundColor Cyan
Write-Host "  Edit forms show empty data when editing existing records" -ForegroundColor White
Write-Host "  Root cause: Direct assignment of tracked entities`n" -ForegroundColor White

Write-Host "?? Pages Already Fixed:" -ForegroundColor Green
Write-Host "  ? CustomerEdit.razor" -ForegroundColor White

Write-Host "`n??  Pages Still Broken (High Priority):" -ForegroundColor Red
Write-Host "  ? ProductEdit.razor (line 284: model = product;)" -ForegroundColor White
Write-Host "  ? AssetEdit.razor (line 260: model = asset;)" -ForegroundColor White
Write-Host "  ? JobEdit.razor (needs checking)" -ForegroundColor White
Write-Host "  ? InvoiceEdit.razor (needs checking)" -ForegroundColor White
Write-Host "  ? EstimateEdit.razor (needs checking)" -ForegroundColor White

Write-Host "`n?? Manual Fix Required:" -ForegroundColor Yellow
Write-Host "Due to model complexity, each page needs individual attention." -ForegroundColor White
Write-Host "The fix pattern is documented in ENTITY_TRACKING_FIX_ALL_PAGES.md`n" -ForegroundColor White

Write-Host "?? Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Read ENTITY_TRACKING_FIX_ALL_PAGES.md for the fix pattern" -ForegroundColor White
Write-Host "  2. Apply fixes to each page starting with ProductEdit" -ForegroundColor White
Write-Host "  3. Test each page after fixing" -ForegroundColor White
Write-Host "  4. Commit all fixes together`n" -ForegroundColor White

$response = Read-Host "Would you like to see the fix example for ProductEdit? (y/n)"

if ($response -eq 'y' -or $response -eq 'Y') {
    Write-Host "`n???? ProductEdit.razor Fix Example ????`n" -ForegroundColor Green
    
    Write-Host "BEFORE (line 273-295):" -ForegroundColor Red
    Write-Host @"
private async Task LoadProductAsync()
{
    try
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        
        var product = await db.Products
            .FirstOrDefaultAsync(p => p.Id == Id!.Value);

        if (product != null)
        {
            model = product;  // ? WRONG
        }
    }
    catch (Exception ex)
    {
        errorMessage = `$"Failed to load product: {ex.Message}";
    }
    finally
    {
        isLoading = false;
    }
}
"@ -ForegroundColor White

    Write-Host "`nAFTER (create new instance):" -ForegroundColor Green
    Write-Host @"
private async Task LoadProductAsync()
{
    try
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        
        var product = await db.Products
            .FirstOrDefaultAsync(p => p.Id == Id!.Value);

        if (product != null)
        {
            // ? Create new instance
            model = new Product
            {
                Id = product.Id,
                ProductNumber = product.ProductNumber,
                Manufacturer = product.Manufacturer,
                ModelNumber = product.ModelNumber,
                SerialNumber = product.SerialNumber,
                ProductName = product.ProductName,
                Description = product.Description,
                Features = product.Features,
                Category = product.Category,
                EquipmentType = product.EquipmentType,
                FuelType = product.FuelType,
                RefrigerantType = product.RefrigerantType,
                RefrigerantChargeOz = product.RefrigerantChargeOz,
                // ... copy ALL properties from the Product model
                IsActive = product.IsActive
            };
        }
    }
    catch (Exception ex)
    {
        errorMessage = `$"Failed to load product: {ex.Message}";
    }
    finally
    {
        isLoading = false;
    }
}
"@ -ForegroundColor White
}

Write-Host "`n?? Documentation Created:" -ForegroundColor Cyan
Write-Host "  - ENTITY_TRACKING_FIX_ALL_PAGES.md (comprehensive guide)" -ForegroundColor White
Write-Host "  - CUSTOMER_EDIT_EMPTY_FORM_FIX.md (CustomerEdit example)" -ForegroundColor White

Write-Host "`n? Ready to proceed with fixes!" -ForegroundColor Green
Write-Host "Press any key to continue..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
