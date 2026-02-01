# Product SKU Fix - COMPLETE ?

## Issue
The import service was trying to use `Product.SKU` but the Product model actually uses `ProductNumber`.

## Fix Applied

### 1. Updated `EntityIdPrefixes.cs`
Changed comments to reflect correct field names:
- Product uses `ProductNumber` (not SKU)
- Inventory uses `Sku` (SKU is for inventory items)

### 2. Updated `CsvImportService.cs`
Replaced all references:
- `p.SKU` ? `p.ProductNumber`
- `nextSkuNumber` ? `nextProductNumber`
- `existingSkus` ? `existingProductNumbers`

### Changes Made
```csharp
// OLD (incorrect):
var existingSkus = await db.Products
    .Where(p => p.SKU != null && p.SKU.StartsWith(EntityIdPrefixes.Product))
    .Select(p => p.SKU)
    .ToListAsync();

// NEW (correct):
var existingProductNumbers = await db.Products
    .Where(p => p.ProductNumber != null && p.ProductNumber.StartsWith(EntityIdPrefixes.Product))
    .Select(p => p.ProductNumber)
    .ToListAsync();
```

## Verification
? `OneManVan.Web/Services/Import/CsvImportService.cs` - No errors  
? `OneManVan.Shared/Constants/EntityIdPrefixes.cs` - No errors  
?? `OneManVan.MauiBlazor` project - Pre-existing XAML issues (unrelated)

## Field Reference
| Entity | Field Name | Format | Location |
|--------|-----------|--------|----------|
| Customer | CustomerNumber | C-0001 | Customer.cs |
| Product | ProductNumber | PROD-0001 | Product.cs |
| Asset | AssetNumber | AST-0001 | Asset.cs |
| Inventory | Sku | INV-0001 | InventoryItem.cs |
| Inventory | InventoryNumber | INV-0001 | InventoryItem.cs |

**Note:** InventoryItem has both `Sku` and `InventoryNumber` - the auto-generation uses `InventoryNumber`.

## Next Steps
- ? Product import will now auto-generate ProductNumber correctly
- ?? Ready to commit and deploy
