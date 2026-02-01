# CRITICAL FIXES SUMMARY

## ?? Issues Found

### Issue #1: Missing Database Column
```
SQLite Error 1: 'no such column: a.AssetNumber'
```
**Cause:** Database schema out of sync with Asset model  
**Impact:** Customer detail page crashes when loading assets

### Issue #2: Import Not Autogenerating IDs
```
Imported customers have no CustomerNumber
```
**Cause:** Needs investigation (code looks correct)  
**Impact:** Imported customers don't get C-0001, C-0002, etc.

---

## ? Solutions

### Fix #1: Add AssetNumber Column
**Run this script:**
```powershell
.\Fix-Database-And-Import-Complete.ps1
```

**Or manually:**
```sql
ALTER TABLE Assets ADD COLUMN AssetNumber TEXT;

UPDATE Assets 
SET AssetNumber = 'AST-' || printf('%04d', Id)
WHERE AssetNumber IS NULL;
```

### Fix #2: Verify Import Generation
The code **looks correct** (lines 171-175 in CsvImportService.cs):
```csharp
if (string.IsNullOrEmpty(customer.CustomerNumber))
{
    customer.CustomerNumber = EntityIdPrefixes.FormatId(EntityIdPrefixes.Customer, nextNumber);
    nextNumber++;
}
```

**Possible causes why IDs aren't generating:**
1. **CSV validation failing** before save
2. **Import stopping on error** before reaching generation code
3. **Transaction rollback** due to other errors

---

## ?? Quick Fix Instructions

### Stop & Fix
1. **Stop the application** (Shift+F5)
2. **Run:** `.\Fix-Database-And-Import-Complete.ps1`
3. **Start application** (F5)
4. **Login:** `admin@onemanvan.com` / `Admin@123456!`

### Test Import
1. Go to **Customers** page
2. Click **Import** button
3. Upload a CSV file:
```csv
FirstName,LastName,Email,Phone
John,Doe,john@example.com,555-1234
Jane,Smith,jane@example.com,555-5678
```
4. **Verify** customers get IDs: `C-0001`, `C-0002`, etc.

---

## ?? If Import Still Fails

### Check Browser Console
Look for errors like:
- `Validation failed`
- `Duplicate email`
- `Required field missing`

### Check Application Logs
The import service logs errors:
```csharp
_logger.LogError(ex, "Error importing customers from CSV");
```

### Check Database Directly
```powershell
sqlite3 .\OneManVan.Web\AppData\OneManVan.db
```
```sql
SELECT Id, CustomerNumber, FirstName, LastName FROM Customers LIMIT 10;
```

---

## ?? Files Created

- `Fix-Database-And-Import-Complete.ps1` - Comprehensive fix script
- `FixMissingAssetNumber-Complete.ps1` - Asset column fix only
- `Migrations/FixMissingAssetNumber_SQLite.sql` - SQL migration
- `CRITICAL_FIXES_SUMMARY.md` (this file)

---

## ? Success Criteria

After running fixes:
- [ ] Application starts without errors
- [ ] Can login successfully
- [ ] Customer detail page loads (no AssetNumber error)
- [ ] CSV import generates Customer IDs automatically
- [ ] Imported customers show up with C-0001, C-0002, etc.

---

## ?? Notes

**Entity ID Prefixes:**
- Customers: `C-0001`
- Products: `PROD-0001`
- Assets: `AST-0001`
- Inventory: `INV-0001`
- Jobs: `JOB-0001`
- Invoices: `INV-0001`
- Estimates: `EST-0001`
- Sites: `SITE-0001`
- Service Agreements: `SA-0001`

**The import service SHOULD autogenerate these for all entities!**
