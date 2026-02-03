# OneManVan Beta Tester Feedback - Development Plan

## Executive Summary
**Total Issues:** 35+  
**Estimated Development Time:** 6-8 weeks  
**Priority Categories:** Critical (P1), High (P2), Medium (P3), Low (P4)

---

## Phase 1: Critical Fixes (P1) - Week 1-2

### 1.1 Entity Tracking Bug Fix (BLOCKING)
**Issue:** Invoice save fails with tracking error after creating from material list
**File:** `InvoiceEdit.razor`
**Status:** Similar to CustomerEdit fix already applied
**Priority:** P1 - Blocking user workflow

### 1.2 Currency Symbol Fix ($)
**Issue:** Currency shows "¤" instead of "$" throughout application
**Files:** All pages displaying currency
**Fix:** Update culture/formatting configuration
**Priority:** P1 - Affects all financial displays

### 1.3 Cancel Button on All Forms
**Issue:** No way to cancel entry on add/edit pages
**Scope:** All Add/Edit pages (Material List, Jobs, Invoices, etc.)
**Priority:** P1 - Basic UX requirement

### 1.4 Actions Dropdown Visibility
**Issue:** Material list actions dropdown gets cut off by table container
**File:** `MaterialListIndex.razor`
**Fix:** CSS z-index and overflow handling
**Priority:** P1 - Blocking functionality

### 1.5 Duplicate Invoice Prevention
**Issue:** Can create duplicate invoices from material list
**File:** Material List -> Invoice creation logic
**Fix:** Check existing linked invoices before creating
**Priority:** P1 - Data integrity issue

---

## Phase 2: Navigation & Workflow (P2) - Week 2-3

### 2.1 Post-Save Navigation Guidance
**Issue:** After saving, no clear path to next step
**Scope:** All save operations
**Solution:** Add "What's Next?" panel with contextual options
**Pages:** MaterialListEdit, JobEdit, InvoiceEdit, EstimateEdit

### 2.2 Quick Job Status Controls
**Issue:** Need quick Start/Pause/Complete buttons on job list
**File:** `JobList.razor`
**Add:** Inline status buttons between Status and Actions columns

### 2.3 Job Summary Quick Actions
**Issue:** Need quick Start/Cancel/Pause/Create Invoice on job detail
**File:** `JobDetail.razor`
**Add:** Action button bar at top of page

### 2.4 Print Button in Actions Dropdown
**Issue:** Print button not working in material list edit view
**File:** `MaterialListDetail.razor`
**Fix:** Implement print functionality

### 2.5 Default Date/Time Values
**Issue:** Date fields should default to today, arrival time to 9 AM
**Scope:** All date pickers and time fields
**Files:** JobEdit, EstimateEdit, InvoiceEdit, etc.

---

## Phase 3: Data Linking & Automation (P2) - Week 3-4

### 3.1 Material List -> Job/Site Auto-Creation
**Issue:** Want to auto-create job and site from material list
**Scope:** MaterialListDetail.razor
**Solution:** Add "Create Job & Site" button that populates from material list data

### 3.2 Link Jobs to Material Lists
**Issue:** Need to link existing jobs to material lists
**File:** JobEdit.razor
**Add:** Material List dropdown selector

### 3.3 Product -> Asset Data Population
**Issue:** Assets should pull data from products or inventory marked as asset, create ?isAsset=true links to inventory form
**File:** AssetEdit.razor
**Add:** Product picker that populates brand/model/type/warranty

### 3.4 Material List -> Asset Creation
**Issue:** Equipment in material list should optionally create assets
**File:** MaterialListDetail.razor
**Add:** "Create Assets" button for equipment items

### 3.5 Asset -> Job Creation Preserve Data
**Issue:** Creating job from asset loses data
**File:** AssetDetail.razor -> JobEdit.razor
**Fix:** Pass query parameters or use temp storage

### 3.6 Inventory Search in Material List
**Issue:** Material list items should search existing inventory
**File:** MaterialListEdit.razor (item input)
**Add:** Autocomplete from inventory, "Save & Add to Inventory" button

---

## Phase 4: PDF/Print System (P3) - Week 4-5

### 4.1 Universal Print/PDF System
**Issue:** All pages need print capability
**Solution:** Create reusable PDF generation service
**Scope:** Jobs, Invoices, Estimates, Material Lists, Products, Warranty Claims

### 4.2 Customizable PDF Content
**Issue:** Users want to choose what's included in PDFs
**Solution:** Add PDF options modal before generation
- Include/exclude prices
- Show/hide labor breakdown
- Flat rate vs itemized
- Hide internal notes

### 4.3 Material List PDF for Supply House
**Issue:** Need minimal PDF (item, qty, notes only)
**File:** New MaterialListSupplierPdf generator
**Fields:** Item name, quantity, notes (no prices)

### 4.4 Invoice PDF Options
**Issue:** Need flat rate option, combined materials/labor
**File:** InvoicePdfGenerator.cs
**Options:**
- Itemized with prices
- Itemized no prices
- Labor hourly vs flat rate
- Total flat rate only

### 4.5 Warranty/Repair PDF Export
**Issue:** Warranty summary needs PDF capability
**File:** ClaimsList.razor
**Add:** Export button and PDF generator

---

## Phase 5: Form Enhancements (P3) - Week 5-6

### 5.1 Price Override Field
**Issue:** Want to override Total Bid without showing calculation
**File:** MaterialListEdit.razor
**Add:** "Price Override" field (hidden from customer PDFs)

### 5.2 Internal Notes for Supply House
**Issue:** Separate internal notes section on material list
**File:** MaterialListEdit.razor
**Add:** "Supply House Notes" field separate from customer notes

### 5.3 Phone Number Formatting in Settings
**Issue:** Company phone not formatted properly
**File:** Settings.razor
**Fix:** Apply PhoneNumberInput component

### 5.4 Tonnage Direct Input
**Issue:** Don't want x10 tonnage, want 2, 2.5, 4 tons
**File:** ProductEdit.razor
**Change:** Dropdown with 0.5-12 ton options

### 5.5 Add Electric to Fuel Types
**Issue:** Missing "Electric" fuel type
**File:** FuelType.cs enum
**Add:** Electric option

### 5.6 Dynamic Inventory Form Fields
**Issue:** Category-specific fields (ductwork: size, style)
**Solution:** Conditional form sections based on category
**File:** InventoryEdit.razor

---

## Phase 6: Searchable Dropdowns (P3) - Week 6-7

### 6.1 Manufacturer Dropdown (Products)
**Scope:** ProductEdit.razor
**Solution:** Searchable dropdown from existing manufacturers

### 6.2 Model Number by Manufacturer
**Scope:** ProductEdit.razor
**Solution:** Filtered model numbers based on selected manufacturer

### 6.3 Warranty Presets
**Scope:** ProductEdit.razor
**Add:** Dropdown with 0,1,2,3,5,7,10,12 year options

### 6.4 Refrigerant Type Dropdown
**Scope:** ProductEdit.razor
**Add:** Searchable dropdown (R-22, R-410A, R-32, etc.)

### 6.5 Supplier from Companies
**Scope:** InventoryEdit.razor
**Link:** Searchable dropdown from Companies table

### 6.6 Filed By from Employees
**Scope:** Warranty claim form
**Link:** Searchable dropdown from Employees table

---

## Phase 7: Configurable Settings (P4) - Week 7-8

### 7.1 Dropdown Preset Editor
**Issue:** All preset dropdowns should be editable in settings
**Solution:** New Settings section for managing dropdown options
**Categories:**
- Manufacturers
- Model Numbers
- Equipment Types
- Document Categories
- Warranty Lengths
- Refrigerant Types
- Fuel Types

### 7.2 Repair History vs Warranty Claims
**Issue:** Track repairs even when not in warranty
**Solution:** Rename "Warranty Claims" to "Repair History"
**Add:** "Is Warranty Claim" checkbox
**Keep:** All warranty-specific fields

---

## Technical Implementation Details

### Entity Tracking Pattern (Already Applied)
```csharp
// DO THIS - Create new instance
model = new Entity
{
    Id = loaded.Id,
    Property1 = loaded.Property1,
    // ... copy ALL properties
};

// NOT THIS - Direct assignment
model = loaded;  // Causes tracking issues
```

### Currency Fix
```csharp
// In Program.cs
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("en-US");
    options.SupportedCultures = new[] { new CultureInfo("en-US") };
});
```

### Searchable Dropdown Component
```razor
<!-- New reusable component -->
<SearchableDropdown 
    TItem="Company"
    Items="@companies"
    @bind-SelectedId="model.SupplierId"
    DisplayProperty="Name"
    Placeholder="Search suppliers..." />
```

---

## File Changes Summary

### New Files to Create
1. `SearchableDropdown.razor` - Reusable component
2. `PdfOptionsModal.razor` - Print customization
3. `MaterialListSupplierPdf.cs` - Supply house PDF
4. `DropdownPresetEditor.razor` - Settings page
5. `RepairHistory.razor` - Renamed warranty claims

### Files to Modify
1. All 11 Edit pages - Entity tracking (DONE)
2. `MaterialListEdit.razor` - Inventory search, notes, override
3. `MaterialListIndex.razor` - Dropdown z-index
4. `MaterialListDetail.razor` - Job/Site creation, PDF
5. `JobList.razor` - Quick status buttons
6. `JobEdit.razor` - Material list link, defaults
7. `JobDetail.razor` - Quick actions
8. `InvoiceEdit.razor` - Entity tracking fix
9. `InvoicePdfGenerator.cs` - Flat rate options
10. `ProductEdit.razor` - Dropdowns, tonnage, fuel
11. `AssetEdit.razor` - Product data population
12. `InventoryEdit.razor` - Dynamic fields, supplier
13. `Settings.razor` - Phone format, preset editor
14. `ClaimsList.razor` - PDF export
15. `FuelType.cs` - Add Electric

---

## Priority Execution Order

### Week 1 (Critical)
- [ ] Currency symbol fix ($)
- [ ] Invoice entity tracking fix
- [ ] Cancel buttons on all forms
- [ ] Actions dropdown visibility fix
- [ ] Duplicate invoice prevention

### Week 2 (High - Workflow)
- [ ] Post-save navigation guidance
- [ ] Default date/time values
- [ ] Quick job status controls
- [ ] Print button fixes

### Week 3 (High - Linking)
- [ ] Material list -> Job auto-creation
- [ ] Job -> Material list linking
- [ ] Product -> Asset data population

### Week 4 (Medium - PDF)
- [ ] Universal print system
- [ ] PDF customization options
- [ ] Material list supplier PDF

### Week 5 (Medium - Forms)
- [ ] Price override field
- [ ] Tonnage dropdown
- [ ] Electric fuel type
- [ ] Dynamic inventory fields

### Week 6 (Medium - Dropdowns)
- [ ] Searchable dropdown component
- [ ] Apply to all relevant fields

### Week 7-8 (Low - Settings)
- [ ] Dropdown preset editor
- [ ] Repair history refactor

---

## Testing Checklist

After each phase:
- [ ] All forms can be cancelled
- [ ] All forms save without errors
- [ ] Currency displays as $ USD
- [ ] PDFs generate correctly
- [ ] Data links work bidirectionally
- [ ] Dropdowns are searchable
- [ ] Presets are configurable
- [ ] No duplicate records created
- [ ] Navigation is clear and intuitive

---

## Notes

1. **Entity Tracking:** Already fixed 11 pages. Need to apply same pattern to any remaining pages.

2. **PDF System:** Consider using QuestPDF or similar for better control over layouts.

3. **Searchable Dropdowns:** May want to use a library like Blazorise or create custom component.

4. **Database Changes:** Some features may require new columns (PriceOverride, SupplyHouseNotes).

5. **Settings Storage:** Dropdown presets should be stored in database, not hardcoded.

---

**Created:** February 2, 2026  
**Version:** 1.0  
**Status:** Ready for Implementation
