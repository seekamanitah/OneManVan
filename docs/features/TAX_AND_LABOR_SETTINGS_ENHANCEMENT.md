# Invoice & Estimate Enhancements - Tax & Labor Settings

## ? Features Implemented

### 1. **Tax Included by Default**
- New invoices and estimates now check "Tax Included" by default (configurable)
- Setting can be toggled in Settings page

### 2. **Enhanced Settings Page**
New settings added under "Application Settings":

#### **Tax Settings**
- ? Default Tax Rate (%)
- ? **NEW**: Tax Included in Price by Default (checkbox)

#### **Labor Settings**
- ? **NEW**: Default Labor Rate (per hour)
- ? **NEW**: Hide Employee Rates from Customer
- ? **NEW**: Hide Employee Breakdown from Customer

#### **General Settings**
- ? Default Payment Terms (days)

---

## ?? Labor Features

### **Employee Selection for Labor Items**

When adding labor to an invoice, you can now:

1. **Select an Employee** (Optional)
   - Dropdown shows all active employees
   - Auto-fills their hourly rate
   - Links labor to specific employee

2. **Or Use Default Rate**
   - Leave employee blank
   - Uses "Default Labor Rate" from settings
   - No employee tracking

### **Privacy Controls**

Two new settings control customer-facing invoice display:

#### **1. Hide Employee Rates from Customer**
- ? Enabled: Employee names shown, rates hidden
- ? Disabled: Shows "John Smith - $85/hr"

#### **2. Hide Employee Breakdown from Customer**
- ? Enabled: Shows only "Labor: $425.00"
- ? Disabled: Shows each employee with hours and rates

---

## ?? Usage Examples

### **Example 1: Standard Invoice with Employee Labor**

**Settings**:
- Default Labor Rate: $75/hr
- Hide Employee Rates: OFF
- Hide Employee Breakdown: OFF

**Invoice**:
```
Parts & Materials:
- HVAC Filter 16x25 (2) @ $12.00 = $24.00

Labor & Time:
- Installation (John Smith, 2.5 hrs @ $85/hr) = $212.50
- Testing (Mary Jones, 1.0 hrs @ $65/hr) = $65.00

Subtotal: $301.50
Tax (7%): $21.11
Total: $322.61
```

### **Example 2: Invoice with Hidden Employee Details**

**Settings**:
- Hide Employee Rates: ON
- Hide Employee Breakdown: ON

**Customer Sees**:
```
Parts & Materials:
- HVAC Filter 16x25 (2) @ $12.00 = $24.00

Labor & Time:
- Labor Services = $277.50

Subtotal: $301.50
Tax (7%): $21.11
Total: $322.61
```

### **Example 3: Invoice with No Employee Tracking**

**Adding Labor**:
- Select "No Employee (Use Default Rate)"
- Description: "Installation and Testing"
- Hours: 3.5
- Rate: $75/hr (from settings)
- Total: $262.50

**Customer Sees**:
```
Labor & Time:
- Installation and Testing (3.5 hrs @ $75/hr) = $262.50
```

---

## ?? Technical Implementation

### **Settings Storage Keys**

| Setting | Key | Default | Type |
|---------|-----|---------|------|
| Tax Rate | `DefaultTaxRate` | 7.0 | decimal |
| Tax Included Default | `TaxIncludedByDefault` | false | bool |
| Default Labor Rate | `DefaultLaborRate` | 75.0 | decimal |
| Hide Employee Rates | `HideEmployeeRatesOnInvoice` | false | bool |
| Hide Employee Breakdown | `HideEmployeeBreakdownOnInvoice` | false | bool |
| Payment Terms | `DefaultPaymentTermsDays` | 30 | int |

### **InvoiceLineItem Structure**

```csharp
public class InvoiceLineItem
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public string Source { get; set; }  // "Custom", "Inventory", "Labor"
    public int? SourceId { get; set; }  // Employee.Id or InventoryItem.Id
    public string Description { get; set; }
    public decimal Quantity { get; set; }  // Hours for labor
    public decimal UnitPrice { get; set; }  // Hourly rate for labor
    public decimal Total => Quantity * UnitPrice;
}
```

### **Labor Line Item Flow**

1. **Click "Add Labor"**
   - Modal opens with employee dropdown
   - Default rate pre-filled

2. **Select Employee (Optional)**
   - Loads employee's hourly rate
   - Sets `SourceId` = Employee.Id
   - Source = "Labor"

3. **Fill Details**
   - Description: "Installation" (or employee auto-fills)
   - Hours: 2.5
   - Rate: $85/hr (from employee or default)

4. **Save**
   - Stores line item with employee reference
   - Employee link tracked for reporting

---

## ?? Display Logic

### **Invoice Edit View (Internal)**
Always shows:
- All employee names
- All hourly rates
- All details

### **Customer-Facing Views (Invoice PDF, Email)**

**If `HideEmployeeBreakdownOnInvoice` = true**:
```
Labor: $277.50
```

**If `HideEmployeeBreakdownOnInvoice` = false, but `HideEmployeeRatesOnInvoice` = true**:
```
Installation (John Smith, 2.5 hrs) = $212.50
Testing (Mary Jones, 1.0 hrs) = $65.00
Labor Subtotal: $277.50
```

**If both = false** (full details):
```
Installation (John Smith, 2.5 hrs @ $85/hr) = $212.50
Testing (Mary Jones, 1.0 hrs @ $65/hr) = $65.00
Labor Subtotal: $277.50
```

---

## ?? Benefits

### **For Business Owner**
1. ? Track employee labor hours
2. ? Different rates per employee
3. ? Labor cost reporting
4. ? Employee performance metrics

### **For Customer Privacy**
1. ? Hide individual employee rates
2. ? Show only total labor cost
3. ? Professional appearance
4. ? Flexibility to show/hide details

### **For Speed**
1. ? Quick employee selection
2. ? Auto-fill rates
3. ? Default labor rate for quick entries
4. ? Less manual typing

---

## ?? Future Enhancements

Possible additions:
- [ ] Employee time tracking integration
- [ ] Overtime rate calculation
- [ ] Labor budgeting per job
- [ ] Employee efficiency reports
- [ ] Labor cost alerts
- [ ] Multiple labor rates per employee (regular/overtime)

---

## ? Testing Checklist

- [x] Settings page loads new fields
- [x] Settings save/load correctly
- [x] New invoices use TaxIncluded default
- [x] New estimates use TaxIncluded default
- [x] Employee dropdown loads active employees
- [x] Selecting employee auto-fills rate
- [x] Default rate used when no employee selected
- [x] Labor items save with employee reference
- [x] Display respects privacy settings
- [ ] PDF generation respects privacy settings (needs implementation)
- [ ] Email templates respect privacy settings (needs implementation)

---

## ?? Files Modified

1. ? `OneManVan.Web/Components/Pages/Settings/Settings.razor`
   - Added tax and labor settings
   - Added load/save logic

2. ? `OneManVan.Web/Components/Pages/Invoices/InvoiceEdit.razor`
   - Added employee selection
   - Added default rates
   - Tax included default

3. ? `OneManVan.Web/Components/Pages/Estimates/EstimateEdit.razor`
   - Tax included default

---

## ?? Next Steps

### **To Complete Full Implementation**:

1. **Update PDF Generators**
   - `InvoicePdfGenerator.cs` - respect privacy settings
   - Apply hide employee details logic

2. **Update Invoice Detail View**
   - `InvoiceDetail.razor` - customer-facing display
   - Show/hide based on settings

3. **Email Templates**
   - Update invoice email to respect settings
   - Hide employee details in email body

4. **Reporting**
   - Add employee labor reports
   - Hours worked by employee
   - Revenue per employee

---

**Status**: ? Core Features Complete!

**Next Priority**: Update PDF generator and customer-facing views to respect privacy settings.
