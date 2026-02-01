# Customer Edit Form Empty Data Fix

## Problem
After importing customers via CSV, the customers appear correctly in the list. However, when trying to edit an existing customer, the edit form shows up completely empty - no data is populated.

## Root Cause
**Entity Tracking Issue with Blazor Server and `await using` DbContext**

In `CustomerEdit.razor`, line 264:
```csharp
var customer = await db.Customers.FindAsync(Id!.Value);
if (customer != null)
{
    model = customer;  // ? WRONG: Assigns tracked entity reference
}
```

### Why This Failed:
1. `LoadCustomerAsync()` uses `await using var db = ...`
2. The DbContext is **disposed immediately** after the method completes
3. `model = customer` assigns the **tracked entity reference**
4. When the DbContext disposes, the entity becomes **detached**
5. Blazor tries to bind to a detached entity ? **empty form**

### The Lifecycle Issue:
```
1. LoadCustomerAsync() starts
2. Create DbContext
3. Load customer entity (tracked by EF)
4. model = customer (reference to tracked entity)
5. Method ends ? DbContext disposes
6. Entity becomes detached
7. Form tries to bind ? No data!
```

## Fix Applied

### ? Create a New Instance
Instead of assigning the tracked entity, create a **new Customer instance** and copy all values:

```csharp
var customer = await db.Customers.FindAsync(Id!.Value);
if (customer != null)
{
    // ? Create a new instance to avoid tracking issues
    model = new Customer
    {
        Id = customer.Id,
        CustomerNumber = customer.CustomerNumber,
        FirstName = customer.FirstName,
        LastName = customer.LastName,
        Name = customer.Name,
        Email = customer.Email,
        Phone = customer.Phone,
        Mobile = customer.Mobile,
        HomeAddress = customer.HomeAddress,
        CustomerType = customer.CustomerType,
        Status = customer.Status,
        PaymentTerms = customer.PaymentTerms,
        PreferredContact = customer.PreferredContact,
        Notes = customer.Notes
    };
}
```

### Why This Works:
1. `model` is now a **detached POCO** (Plain Old CLR Object)
2. Not tracked by any DbContext
3. Lives independently in Blazor component memory
4. Survives after DbContext disposal
5. Form bindings work correctly ?

## Files Changed
- `OneManVan.Web/Components/Pages/Customers/CustomerEdit.razor` - Fixed LoadCustomerAsync method

## Testing
1. Import customers via CSV
2. Navigate to customer list
3. Click "Edit" on any customer
4. **Expected Result:** Form should be populated with all customer data
5. **Previously:** Form was empty

## Related Patterns

This same pattern should be checked in other edit pages:
- `ProductEdit.razor`
- `AssetEdit.razor`
- `InventoryEdit.razor`
- `JobEdit.razor`
- `InvoiceEdit.razor`
- `EstimateEdit.razor`

If they use `model = entity` with `await using` DbContext, they may have the same issue.

## Alternative Solutions (Not Recommended)

### Option 1: Use AsNoTracking (Worse)
```csharp
var customer = await db.Customers
    .AsNoTracking()
    .FirstOrDefaultAsync(c => c.Id == Id!.Value);
model = customer;
```
**Issue:** Still assigns reference, but now entity is untracked. Safer, but less explicit.

### Option 2: Keep DbContext Alive (Bad)
```csharp
// Store DbContext as field - DO NOT DO THIS
private OneManVanDbContext? _db;
```
**Issue:** DbContext not thread-safe, memory leaks, disposal issues.

### Option 3: Use a DTO (Best for Complex Scenarios)
```csharp
public class CustomerEditDto { /* properties */ }
```
**Issue:** Overkill for simple edit forms, but good for complex validations.

## Why Our Fix is Best
- ? Simple and explicit
- ? No magic or hidden behavior
- ? Easy to understand and maintain
- ? Works with Blazor's component lifecycle
- ? No tracking overhead
- ? Clear separation between DB entities and UI models

---

**Status**: ? Fixed - Customer edit form now populates correctly
