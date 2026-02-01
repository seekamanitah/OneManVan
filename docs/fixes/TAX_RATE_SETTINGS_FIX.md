# Tax Rate Settings - Fixed & Enhanced

## ? Issue Fixed

### **Problem**
When changing the global tax rate in Settings, existing invoices/estimates didn't reflect the change.

### **Root Cause**
This is **actually correct behavior** for existing records:
- **New invoices/estimates** ? Use global default tax rate
- **Existing invoices/estimates** ? Keep their saved tax rate (preserves historical data)

However, there was **no easy way** to apply the global default to an existing invoice/estimate.

---

## ?? Solution Implemented

### **1. Invoice Edit Page**
? **Added "Reset" Button** next to Tax Rate field
- Shows current global default: "Current default: 7.0%"
- Click "Reset" button to apply global default to this invoice
- Only visible when tax is not included in price

**Location**: Invoice Totals section ? Tax Rate (%)

### **2. Estimate Edit Page**
? **Changed from Tax Amount to Tax Rate**
- Previously: Manual entry of tax amount ($)
- Now: Tax rate (%) that auto-calculates tax amount
- **Added "Reset" Button** to apply global default
- Consistent with Invoice edit behavior

? **Auto-calculation of Tax Amount**
- Tax Amount = Subtotal × (Tax Rate / 100)
- Displays calculated amount in the total row

---

## ?? How It Works Now

### **Creating New Invoice/Estimate**
1. Opens with **global default tax rate** from Settings
2. Example: If Settings = 7%, new invoice starts at 7%

### **Editing Existing Invoice/Estimate**
1. Shows the **saved tax rate** from the record
2. See current global default displayed: "Current default: X%"
3. Click **"Reset" button** to apply global default
4. Or manually adjust tax rate as needed

### **Changing Global Default**
1. Go to **Settings** ? Application Settings
2. Change "Default Tax Rate (%)"
3. Click "Save Settings"
4. **Effect**:
   - All **new** invoices/estimates use new rate
   - **Existing** records keep their rate (unless manually reset)

---

## ?? Technical Changes

### **InvoiceEdit.razor**
```razor
<label class="form-label d-flex justify-content-between align-items-center">
    <span>Tax Rate (%)</span>
    <button type="button" @onclick="ResetToGlobalTaxRate">
        <i class="bi bi-arrow-counterclockwise"></i> Reset
    </button>
</label>
<InputNumber @bind-Value="model.TaxRate" ... />
<small class="text-muted">Current default: @defaultTaxRate%</small>
```

**Code**:
```csharp
private decimal defaultTaxRate = 7.0m;

protected override async Task OnInitializedAsync()
{
    defaultTaxRate = SettingsStorage.GetDecimal("DefaultTaxRate", 7.0m);
    // ...
}

private void ResetToGlobalTaxRate()
{
    model.TaxRate = defaultTaxRate;
    StateHasChanged();
}
```

### **EstimateEdit.razor**
**Before**: Used `TaxAmount` (manual dollar entry)
```razor
<input @bind="model.TaxAmount" />  <!-- OLD -->
```

**After**: Uses `TaxRate` (percentage, auto-calculates)
```razor
<input @bind="model.TaxRate" placeholder="Tax %" />  <!-- NEW -->
<!-- Auto-calculated display -->
<td>@((model.SubTotal * (model.TaxRate / 100)).ToString("C"))</td>
```

**Calculation Logic**:
```csharp
private void CalculateTotals()
{
    model.SubTotal = lineItems.Sum(item => item.Quantity * item.UnitPrice);
    if (model.TaxIncluded)
    {
        model.TaxAmount = 0;
        model.Total = model.SubTotal;
    }
    else
    {
        model.TaxAmount = model.SubTotal * (model.TaxRate / 100);  // NEW
        model.Total = model.SubTotal + model.TaxAmount;
    }
}
```

---

## ? Pages Checked & Updated

| Page | Tax Input | Uses Global Default (New) | Reset Button | Status |
|------|-----------|---------------------------|--------------|--------|
| **InvoiceEdit** | Tax Rate (%) | ? Yes | ? Added | Fixed |
| **EstimateEdit** | Tax Rate (%) | ? Yes | ? Added | Fixed |
| **Settings** | Default Tax Rate | N/A | N/A | Working |

### **Other Pages with Tax**
No other pages have direct tax input - they display calculated values only:
- InvoiceDetail (display only)
- EstimateDetail (display only)
- InvoiceList (display only)
- EstimateList (display only)

---

## ?? Usage Examples

### **Example 1: Update Global Default**
1. Settings ? Default Tax Rate: Change 7% to 8%
2. Save Settings
3. **Result**:
   - New invoices: Start at 8% ?
   - Existing invoices: Still at 7% (historical accuracy)
   - Can click "Reset" on existing to change to 8%

### **Example 2: Special Tax Rate for One Invoice**
1. Create new invoice (starts at 8%)
2. Customer in different state needs 6%
3. Manually change to 6%
4. Save
5. **Result**: Invoice saved at 6%, doesn't affect other invoices

### **Example 3: Bulk Update After Tax Law Change**
1. Tax law changes from 7% to 7.5%
2. Update Settings to 7.5%
3. For pending/draft invoices:
   - Open each one
   - Click "Reset" button
   - Save
4. **Result**: Pending invoices updated to new rate

---

## ?? Benefits

1. **Preserves Historical Data**: Existing records keep their tax rate
2. **Easy Updates**: One-click reset to global default
3. **Visibility**: Always see current global default
4. **Consistency**: Invoice and Estimate work the same way
5. **Flexibility**: Can override for special cases

---

**Status**: ? Complete and Ready to Use!

**Files Modified**:
- `OneManVan.Web/Components/Pages/Invoices/InvoiceEdit.razor`
- `OneManVan.Web/Components/Pages/Estimates/EstimateEdit.razor`
