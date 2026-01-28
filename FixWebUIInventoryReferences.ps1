# Fix Web UI Inventory References
# Replace "Inventory" with "InventoryItem" in inventory pages

$files = @(
    "OneManVan.Web\Components\Pages\Inventory\InventoryList.razor",
    "OneManVan.Web\Components\Pages\Inventory\InventoryDetail.razor",
    "OneManVan.Web\Components\Pages\Inventory\InventoryEdit.razor"
)

foreach ($file in $files) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        
        # Replace OneManVan.Shared.Models.Inventory with InventoryItem
        $content = $content -replace 'OneManVan\.Shared\.Models\.Inventory', 'InventoryItem'
        
        Set-Content $file $content -NoNewline
        Write-Host "Fixed: $file" -ForegroundColor Green
    } else {
        Write-Host "Not found: $file" -ForegroundColor Yellow
    }
}

Write-Host "`nDone! All Inventory references updated to InventoryItem" -ForegroundColor Cyan
