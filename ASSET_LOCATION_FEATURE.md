# ??? **Asset Location Flexibility - Feature Implementation Summary**

**Date:** January 2026  
**Feature:** Assets can now be linked to Sites OR Customers  
**Purpose:** Allow HVAC equipment to stay with properties when ownership changes

---

## **? FEATURE OVERVIEW**

### **Problem Solved:**
Previously, assets were **always** linked to customers. When a homeowner changed, the equipment history was tied to the old owner, making it difficult to track equipment lifecycle with the property.

### **Solution:**
Assets can now be associated with:
- **Site/Property** (Preferred) - Equipment stays with the property
- **Customer** - For portable equipment or when site is unknown

**At least one must be specified**, but both can be set if needed.

---

## **?? CHANGES MADE**

### **1. Asset Model Updated**
**File:** `OneManVan.Shared/Models/Asset.cs`

**Changes:**
- `CustomerId` changed from `int` to `int?` (nullable)
- `SiteId` remains `int?` (nullable)
- Added `HasValidLocation` computed property for validation
- Added `LocationDescription` computed property for display
- Updated documentation to reflect new behavior

**Before:**
```csharp
public int CustomerId { get; set; }  // Required
public int? SiteId { get; set; }      // Optional
```

**After:**
```csharp
public int? CustomerId { get; set; }  // Optional
public int? SiteId { get; set; }      // Optional (but one required)

[NotMapped]
public bool HasValidLocation => CustomerId.HasValue || SiteId.HasValue;

[NotMapped]
public string LocationDescription => SiteId.HasValue 
    ? (Site?.Address ?? "Site") 
    : (Customer?.Name ?? "Customer");
```

---

### **2. AddAssetPage Enhanced**
**Files:** 
- `OneManVan.Mobile/Pages/AddAssetPage.xaml`
- `OneManVan.Mobile/Pages/AddAssetPage.xaml.cs`

**New Features:**
1. **Location Type Selection**
   - User taps location selector
   - Chooses "Site/Property" or "Customer"
   - UI updates with appropriate icon (?? for site, ?? for customer)

2. **Site Selector**
   - Displays all sites with addresses
   - Shows current owner for each site
   - Updates asset to link to site (clears customer)

3. **Customer Selector**
   - Uses existing CustomerSelectionHelper
   - Updates asset to link to customer (clears site)

4. **Enhanced Validation**
   - Ensures at least one (Site OR Customer) is selected
   - Clear error messages

5. **Save Logic**
   - Saves CustomerId or SiteId (whichever is selected)
   - Null for the unselected option

**UI Changes:**
- Label changed from "Customer *" to "Asset Location (Site or Customer) *"
- Added LocationDetailLabel to show additional context (e.g., site owner)
- Icons added: ?? for sites, ?? for customers

---

### **3. EditAssetPage Updated**
**Files:**
- `OneManVan.Mobile/Pages/EditAssetPage.xaml.cs`

**Changes:**
- Added `.Include(a => a.Site)` to load query
- Updated PopulateForm() to display location correctly:
  - Shows site address with ?? icon if linked to site
  - Shows customer name with ?? icon if linked to customer
  - Shows "No location assigned" if neither

---

### **4. AssetDetailPage Fixed**
**File:** `OneManVan.Mobile/Pages/AssetDetailPage.xaml.cs`

**Changes:**
- Fixed null handling: `_customerId = asset.CustomerId ?? 0`

---

### **5. MobileBackupService Fixed**
**File:** `OneManVan.Mobile/Services/MobileBackupService.cs`

**Changes:**
- Added null check: `asset.CustomerId.HasValue && customerIdMap.TryGetValue(asset.CustomerId.Value, ...)`

---

## **?? USE CASES**

### **Use Case 1: Property-Based Tracking (Recommended)**
**Scenario:** Installing new HVAC system at 123 Main Street

**Steps:**
1. Navigate to Add Asset
2. Tap "Asset Location"
3. Select "Site/Property"
4. Choose "123 Main Street (John Smith)"
5. Fill in equipment details
6. Save

**Result:** 
- Asset linked to the property
- If John Smith sells the house, asset history stays with property
- New owner inherits equipment service history

---

### **Use Case 2: Customer-Based Tracking**
**Scenario:** Portable generator owned by customer

**Steps:**
1. Navigate to Add Asset
2. Tap "Asset Location"
3. Select "Customer"
4. Choose customer from list
5. Fill in equipment details
6. Save

**Result:**
- Asset follows the customer
- Useful for portable/rental equipment

---

### **Use Case 3: Converting Existing Assets**
**Scenario:** Existing asset linked to customer needs to be property-based

**Steps:**
1. Navigate to Asset Details
2. Tap Edit
3. Update location if needed (future enhancement)
4. Save

**Current Limitation:** Edit page shows location but doesn't yet allow changing it. This can be added in a future update if needed.

---

## **?? TESTING CHECKLIST**

### **Manual Testing:**
- ? Create asset with Site selection
- ? Create asset with Customer selection
- ? Validate "at least one" error shows correctly
- ? Verify asset saves correctly to database
- ? Load existing asset in Edit mode
- ? Verify AssetDetailPage shows correct location
- ? Test with site that has customer vs no customer

### **Database Validation:**
```sql
-- Check assets by location type
SELECT 
    Id, 
    Serial, 
    Brand, 
    CustomerId, 
    SiteId,
    CASE 
        WHEN SiteId IS NOT NULL THEN 'Site-based'
        WHEN CustomerId IS NOT NULL THEN 'Customer-based'
        ELSE 'No location'
    END AS LocationType
FROM Assets;
```

---

## **?? MIGRATION NOTES**

### **Existing Data:**
- All existing assets have CustomerId set (not null)
- No migration script needed
- Existing assets will continue to work normally
- New assets can choose Site or Customer

### **Database Schema:**
```sql
-- CustomerId is now nullable
ALTER TABLE Assets ALTER COLUMN CustomerId INT NULL;

-- SiteId remains nullable (already was)
-- No change needed for SiteId
```

**Note:** Entity Framework migrations will handle this automatically on next update.

---

## **? BENEFITS**

1. **Property History Tracking**
   - Equipment maintenance history stays with the property
   - Useful for warranty tracking
   - Better for property management companies

2. **Flexibility**
   - Still supports customer-based tracking when needed
   - Works for both scenarios

3. **Better Data Organization**
   - Clear separation between property equipment and customer equipment
   - More accurate for HVAC business model

4. **Future-Proof**
   - Supports rental properties
   - Supports property management scenarios
   - Supports commercial properties with multiple tenants

---

## **?? FUTURE ENHANCEMENTS (Optional)**

### **Priority 1: Edit Location**
Allow changing asset location in EditAssetPage:
- Add location selector like AddAssetPage
- Support switching from Customer to Site or vice versa
- Validation to ensure at least one is always set

### **Priority 2: Bulk Operations**
- Bulk transfer assets from customer to site
- Useful when creating site records for existing customers
- Example: "Move all John Smith's assets to 123 Main Street"

### **Priority 3: Smart Suggestions**
- When adding asset, suggest linking to site if customer has only one site
- Auto-populate site from recent jobs
- Show warning if customer has multiple sites

### **Priority 4: Reports**
- Asset report by property
- Equipment inventory by location
- Service history by site

---

## **?? DEVELOPER NOTES**

### **Key Files Changed:**
1. `OneManVan.Shared/Models/Asset.cs` - Model changes
2. `OneManVan.Mobile/Pages/AddAssetPage.xaml.cs` - Site selection logic
3. `OneManVan.Mobile/Pages/AddAssetPage.xaml` - UI updates
4. `OneManVan.Mobile/Pages/EditAssetPage.xaml.cs` - Load + display
5. `OneManVan.Mobile/Pages/AssetDetailPage.xaml.cs` - Null handling
6. `OneManVan.Mobile/Services/MobileBackupService.cs` - Backup logic

### **Testing:**
- All existing tests should still pass
- No new unit tests added (manual testing sufficient)
- Consider adding integration tests for site-based assets

### **Performance:**
- Added `.Include(a => a.Site)` to queries - minimal impact
- No additional database queries needed

---

## **? COMPLETION STATUS**

**Implementation:** ? COMPLETE  
**Testing:** ?? Manual testing needed  
**Documentation:** ? COMPLETE  
**Migration:** ? Handled automatically by EF

---

**Feature Status:** ? **READY FOR TESTING**

Test the new site selection feature and verify assets can be created with both sites and customers!

---

**Document Version:** 1.0  
**Last Updated:** January 2026  
**Author:** AI Assistant
