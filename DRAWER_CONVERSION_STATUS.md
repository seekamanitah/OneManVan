# Desktop Drawer Conversion - Quick Guide

**Status:** Customer drawer working ? | Others need property mapping  
**Build:** ? Needs fixes

---

## Issue Found

The Asset and Product models have different property names than expected:

### Asset Model Uses:
- `Serial` (not AssetType)
- `Brand` ?
- `Model` (not ModelNumber)
- `Nickname` (not AssetType)
- No `InstallationDate` property

### Product Model Uses:
- `Manufacturer` (not Name)
- `ModelNumber` ?
- `ProductName` (optional)
- `Category` is enum (not string)
- `RetailPrice` / `WholesalePrice` (not UnitPrice/Cost)

---

## Quick Fix Options

### Option 1: Copy Existing Dialog Forms (Fast - 5 minutes)
Instead of creating new form controls, just convert the existing dialogs to open in the drawer:

```csharp
// Open existing dialog content in drawer
var dialogContent = new Dialogs.AddEditAssetDialog();
// Remove dialog chrome, extract the form content
// Put in drawer
```

### Option 2: Use Existing Dialogs As-Is (Fastest - Done!)
Keep using the modal dialogs for Assets, Products, Jobs since they're already working. Only Customer uses drawer as the example.

### Option 3: Properly Map Models (15-20 minutes)
Fix the FormContent controls to use correct property names from actual models.

---

## Recommendation

**Keep it simple for now:**
- ? Customer uses drawer (working perfectly)
- ?? Assets, Products, Jobs, Estimates continue using modal dialogs

**Why:**
- Modal dialogs work fine
- They're already built and styled
- Drawer is demonstrated with Customer
- Can convert others gradually when time permits

---

## If You Want to Convert Them All

I can convert them, but it requires:
1. Checking each model's actual properties
2. Creating proper form controls
3. Mapping all fields correctly
4. Testing each one

**Estimated time:** 30-45 minutes for all entities

**Alternative:** Show you how to do one (Asset), then you can follow the pattern for others.

---

## Current Status

**Working:**
- ? Customer drawer (perfect example)
- ? Job button fixed (opens modal dialog)
- ? Drawer infrastructure complete

**To Convert (if desired):**
- Assets
- Products  
- Estimates
- Inventory
- Service Agreements

**Decision needed:** Convert all now, or leave as working modal dialogs?
