# Invoice Line Items - Inventory Integration

## ? Feature Added

### **New Capability**
Invoice line items can now pull from **Inventory** OR use **Custom Input** as needed!

---

## ?? How It Works

### **Adding a Line Item**

When you click "Add Item", you now see:

```
???????????????????????????????????????????????
?  Item Source                                ?
?  ???????????????????????????????????       ?
?  ? Custom Item  ? From Inventory   ?       ?
?  ???????????????????????????????????       ?
???????????????????????????????????????????????
```

### **Option 1: Custom Item** (Default)
- Enter description manually
- Set quantity and price
- Perfect for one-off items or services

### **Option 2: From Inventory**
1. Select from dropdown of in-stock inventory items
2. **Auto-fills**:
   - Description (from inventory name)
   - Unit Price (from inventory price)
3. **Shows**:
   - Available stock quantity
   - Current price
4. You can still:
   - Adjust quantity (with stock warning)
   - Edit price if needed (override)
   - Add notes

---

## ?? Example Workflow

### **Scenario: HVAC Service Call**

**Parts from Inventory:**
1. Click "Add Item"
2. Select "From Inventory"
3. Choose "HVAC Filter 16x25" from dropdown
   - Shows: "HVAC Filter 16x25 (15 ea available) - $12.00"
4. Auto-fills:
   - Description: "HVAC Filter 16x25"
   - Unit Price: $12.00
5. Set Quantity: 2
6. Total: $24.00

**Custom Labor:**
1. Click "Add Labor"
2. Enter: "Installation and Testing"
3. Hours: 2.5
4. Rate: $75.00
5. Total: $187.50

**Final Invoice:**
- Parts: $24.00
- Labor: $187.50
- Subtotal: $211.50
- Tax (7%): $14.81
- **Total: $226.31**

---

## ?? UI Features

### **Inventory Dropdown Shows:**
```
HVAC Filter 16x25 (15 ea available) - $12.00
Refrigerant R-410A (5 lb available) - $85.00
Thermostat Digital (8 ea available) - $45.00
```

### **Stock Indicators:**
- Shows available quantity
- Shows unit of measure (ea, lb, ft, etc.)
- Only displays **in-stock items**

### **Auto-Fill Benefits:**
- ? Consistent naming
- ? Accurate pricing
- ? Faster data entry
- ? Tracks inventory usage

---

## ?? Technical Details

### **Data Model**
```csharp
public class InvoiceLineItem
{
    public string Source { get; set; }  // "Custom", "Inventory", "Labor"
    public int? SourceId { get; set; }  // Links to InventoryItem.Id
    public string Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total => Quantity * UnitPrice;
}
```

### **Source Types**
| Source | Description | SourceId |
|--------|-------------|----------|
| `Custom` | Manual entry | null |
| `Inventory` | From inventory stock | InventoryItem.Id |
| `Labor` | Time/labor charges | null |
| `Product` | From products catalog | Product.Id (future) |

### **Inventory Filtering**
- Only shows items with `QuantityOnHand > 0`
- Sorted alphabetically by name
- Displays: Name (Stock) - Price

---

## ?? Benefits

### **For Business**
1. **Inventory Tracking**: Know what's being used
2. **Accurate Pricing**: Pull current prices automatically
3. **Stock Awareness**: See available quantities
4. **Consistency**: Standardized item names

### **For Users**
1. **Faster Entry**: Select instead of typing
2. **No Typos**: Consistent descriptions
3. **Price Updates**: Always current pricing
4. **Flexibility**: Can still override or use custom

---

## ?? Usage Scenarios

### **When to Use Inventory**
- ? Selling stocked parts/materials
- ? Common items you track
- ? Want accurate inventory levels
- ? Need consistent naming

### **When to Use Custom**
- ? One-off special items
- ? Services not in inventory
- ? Subcontractor charges
- ? Miscellaneous fees

### **Labor Items**
- Always use "Add Labor" button
- Automatically marked as labor
- Tracks hours and hourly rates

---

## ?? Inventory Stock Tracking

### **Current Behavior**
- Line items reference inventory
- Shows available stock when selecting
- **Note**: Inventory quantity is NOT automatically reduced when invoice is saved
- Future enhancement: Auto-deduct on invoice payment/completion

### **Stock Warnings**
When selecting inventory item:
```
Quantity: [2]
Available: 15 ea  ? Shows current stock
```

If you enter more than available:
- Still allows (for customer orders)
- Shows warning about stock levels

---

## ?? Tips

1. **Search the Dropdown**: Most browsers let you type to search
2. **Price Override**: Inventory price is editable after selection
3. **Stock Check**: Always shows current availability
4. **Mix and Match**: Use inventory + custom + labor on same invoice
5. **Notes Field**: Add serial numbers, warranty info, etc.

---

## ?? Future Enhancements

Planned improvements:
- [ ] Search/filter inventory dropdown
- [ ] Product catalog integration (similar to inventory)
- [ ] Auto-deduct inventory on invoice payment
- [ ] Stock reservation system
- [ ] Bulk item add
- [ ] Recent items quick-add
- [ ] Price history tracking

---

## ? Testing Checklist

- [x] Load inventory items (in-stock only)
- [x] Toggle between Custom and Inventory source
- [x] Select inventory item from dropdown
- [x] Auto-fill description and price
- [x] Show available stock quantity
- [x] Allow price override
- [x] Save with inventory reference (SourceId)
- [x] Mix inventory and custom items
- [x] Add labor items separately
- [x] Calculate totals correctly

---

**Status**: ? Complete and Ready to Use!

**Files Modified**:
- `OneManVan.Web/Components/Pages/Invoices/InvoiceEdit.razor`

**Models Used**:
- `InvoiceLineItem` (Source, SourceId fields)
- `InventoryItem` (for selection)
