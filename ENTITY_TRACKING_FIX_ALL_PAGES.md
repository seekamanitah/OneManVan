# Entity Tracking Issues Across All Edit Pages - COMPREHENSIVE FIX

## Problem Summary
Multiple edit pages have the same entity tracking issue where they directly assign tracked entities to the model, causing empty forms after the DbContext is disposed.

## Root Cause
```csharp
// WRONG PATTERN (found in multiple pages):
await using var db = await DbFactory.CreateDbContextAsync();
var entity = await db.Entities.FindAsync(Id);
model = entity;  // ? Assigns tracked entity reference
// DbContext disposes here ? entity detached ? form empty
```

## Affected Pages Found

### ? FIXED
| Page | File | Status |
|------|------|--------|
| CustomerEdit | `OneManVan.Web/Components/Pages/Customers/CustomerEdit.razor` | ? **FIXED** |

### ? NEEDS FIXING
| Page | File | Line | Issue |
|------|------|------|-------|
| **ProductEdit** | `OneManVan.Web/Components/Pages/Products/ProductEdit.razor` | 284 | `model = product;` |
| **AssetEdit** | `OneManVan.Web/Components/Pages/Assets/AssetEdit.razor` | 260 | `model = asset;` |
| **InventoryEdit** | `OneManVan.Web/Components/Pages/Inventory/InventoryEdit.razor` | TBD | Needs checking |
| **JobEdit** | `OneManVan.Web/Components/Pages/Jobs/JobEdit.razor` | TBD | Needs checking |
| **InvoiceEdit** | `OneManVan.Web/Components/Pages/Invoices/InvoiceEdit.razor` | TBD | Needs checking |
| **EstimateEdit** | `OneManVan.Web/Components/Pages/Estimates/EstimateEdit.razor` | TBD | Needs checking |
| **SiteEdit** | `OneManVan.Web/Components/Pages/Sites/SiteEdit.razor` | TBD | Needs checking |
| **AgreementEdit** | `OneManVan.Web/Components/Pages/ServiceAgreements/AgreementEdit.razor` | TBD | Needs checking |
| **CompanyEdit** | `OneManVan.Web/Components/Pages/Companies/CompanyEdit.razor` | TBD | Needs checking |
| **EmployeeEdit** | `OneManVan.Web/Components/Pages/Employees/EmployeeEdit.razor` | TBD | Needs checking |

## The Fix Pattern

### Before (Broken):
```csharp
private async Task LoadEntityAsync()
{
    try
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        var entity = await db.Entities.FindAsync(Id!.Value);
        if (entity != null)
        {
            model = entity;  // ? WRONG
        }
    }
    finally
    {
        isLoading = false;
    }
}
```

### After (Fixed):
```csharp
private async Task LoadEntityAsync()
{
    try
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        var entity = await db.Entities.FindAsync(Id!.Value);
        if (entity != null)
        {
            // ? Create new instance with all properties copied
            model = new Entity
            {
                Id = entity.Id,
                Property1 = entity.Property1,
                Property2 = entity.Property2,
                // ... copy ALL properties
            };
        }
    }
    finally
    {
        isLoading = false;
    }
}
```

## Priority Fix Order

### HIGH PRIORITY (User-Facing Forms)
1. ? **CustomerEdit** - FIXED
2. **ProductEdit** - Critical (import feature)
3. **AssetEdit** - Critical (core feature)
4. **InvoiceEdit** - Critical (billing)
5. **JobEdit** - Critical (scheduling)

### MEDIUM PRIORITY
6. **EstimateEdit** - Important
7. **InventoryEdit** - Important
8. **SiteEdit** - Important

### LOW PRIORITY
9. **CompanyEdit** - Less frequently used
10. **AgreementEdit** - Less frequently used
11. **EmployeeEdit** - Admin feature

## Testing Checklist

After fixing each page, test:
- [ ] Navigate to list page
- [ ] Click "Edit" on existing record
- [ ] **Verify form is populated with data**
- [ ] Make a change and save
- [ ] Verify data persists correctly

## Why This Happens

### Blazor Server + EF Core Lifecycle Issue
```
1. Component renders
2. OnInitializedAsync() called
3. LoadEntityAsync() starts
   ??> Create DbContext
   ??> Load entity (now tracked by EF)
   ??> model = entity (reference assignment)
   ??> await using ends ? DbContext DISPOSES
4. Entity becomes DETACHED
5. Form tries to bind
6. Detached entity = EMPTY FORM ?
```

### Why Copying Works
```
1. Component renders
2. OnInitializedAsync() called
3. LoadEntityAsync() starts
   ??> Create DbContext
   ??> Load entity (tracked by EF)
   ??> model = new Entity { ... } (NEW instance)
   ??> await using ends ? DbContext disposes
4. model is INDEPENDENT POCO
5. Form binds successfully
6. Data displays correctly ?
```

## Alternative Solutions (Not Recommended)

### Option 1: AsNoTracking
```csharp
var entity = await db.Entities
    .AsNoTracking()
    .FirstOrDefaultAsync(e => e.Id == Id);
model = entity;  // Still risky, less explicit
```
**Why not:** Less clear, still assigns reference.

### Option 2: Keep DbContext Alive
```csharp
private OneManVanDbContext? _db;  // DON'T DO THIS
```
**Why not:** DbContext not thread-safe, memory leaks, disposal issues.

### Option 3: AutoMapper
```csharp
model = _mapper.Map<Entity>(entity);
```
**Why not:** Adds dependency, overkill for simple forms.

## Our Solution is Best Because:
- ? Explicit and clear
- ? No hidden behavior
- ? Type-safe
- ? Easy to maintain
- ? No extra dependencies
- ? Works with Blazor's component lifecycle

## Files to Commit After All Fixes
```
OneManVan.Web/Components/Pages/Customers/CustomerEdit.razor
OneManVan.Web/Components/Pages/Products/ProductEdit.razor
OneManVan.Web/Components/Pages/Assets/AssetEdit.razor
OneManVan.Web/Components/Pages/Inventory/InventoryEdit.razor
OneManVan.Web/Components/Pages/Jobs/JobEdit.razor
OneManVan.Web/Components/Pages/Invoices/InvoiceEdit.razor
OneManVan.Web/Components/Pages/Estimates/EstimateEdit.razor
OneManVan.Web/Components/Pages/Sites/SiteEdit.razor
OneManVan.Web/Components/Pages/ServiceAgreements/AgreementEdit.razor
OneManVan.Web/Components/Pages/Companies/CompanyEdit.razor
OneManVan.Web/Components/Pages/Employees/EmployeeEdit.razor
ENTITY_TRACKING_FIX_ALL_PAGES.md
```

## Commit Message
```
Fix: Entity tracking issues across all edit pages

## Problem
Edit forms showed empty data when editing existing records due to
entity tracking issues with Blazor Server + EF Core lifecycle.

## Root Cause
Pages were assigning tracked entity references directly to model.
When DbContext disposed, entities became detached, causing empty forms.

## Solution
Create new instances with copied properties instead of assigning
tracked entity references. This makes models independent POCOs
that survive DbContext disposal.

## Pages Fixed
- CustomerEdit ?
- ProductEdit ?
- AssetEdit ?
- InventoryEdit ?
- JobEdit ?
- InvoiceEdit ?
- EstimateEdit ?
- SiteEdit ?
- AgreementEdit ?
- CompanyEdit ?
- EmployeeEdit ?

## Testing
All edit forms now properly populate with existing data.
```

---

**Status**: CustomerEdit fixed, 10 more pages need fixing
**Priority**: Fix ProductEdit and AssetEdit next (high user impact)
