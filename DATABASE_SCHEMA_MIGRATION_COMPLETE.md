# ?? DATABASE SCHEMA MIGRATION - COMPLETE FIX

**Date:** 2025-01-23  
**Issue:** Database schema doesn't match OneManVan.Shared models  
**Status:** ?? **ACTION REQUIRED**  

---

## ?? **The Problem**

After consolidating to `OneManVan.Shared.Models`, your **old database** has columns that don't match the new schema:

### **Errors:**
```
SQLiteException: no such column: AccessInstructions
SQLiteException: no such column: CompressorWarrantyYears
```

### **Root Cause:**
- Old Desktop database was created with old model definitions
- Shared models have different properties
- Database needs to be recreated or migrated

---

## ? **SOLUTION 1: Fresh Start** (Recommended for Development)

### **Step-by-Step:**

1. **Close Application**
   - Exit OneManVan.exe completely
   - Check Task Manager to ensure no process running

2. **Find Database Location**
   ```
   Default: C:\Users\YourUsername\AppData\Local\OneManVan\OneManVan.db
   ```
   
   Or check in `appsettings.json`:
   ```json
   {
     "Database": {
       "LocalPath": "OneManVan.db"
     }
   }
   ```

3. **Backup Current Database** (Optional - if you have important data)
   ```powershell
   Copy-Item "C:\Users\YourUsername\AppData\Local\OneManVan\OneManVan.db" `
             "C:\Users\YourUsername\AppData\Local\OneManVan\OneManVan_backup.db"
   ```

4. **Delete Database Files**
   ```powershell
   Remove-Item "C:\Users\YourUsername\AppData\Local\OneManVan\OneManVan.db*"
   ```
   
   This deletes:
   - `OneManVan.db` (main database)
   - `OneManVan.db-shm` (shared memory)
   - `OneManVan.db-wal` (write-ahead log)

5. **Restart Application**
   - Run OneManVan.exe
   - Entity Framework will **automatically create** new database with correct schema
   - Database will be empty but with correct structure

6. **Seed Test Data** (Optional)
   - Go to Settings ? Test Runner
   - Click "Seed Test Data"
   - This will populate sample customers, assets, etc.

---

## ?? **SOLUTION 2: Add Missing Columns** (Preserve Data)

If you have **important existing data** and want to preserve it:

### **Migration Script:**

Run this PowerShell script to add missing columns:

```powershell
# Install SQLite command-line tool if needed
# choco install sqlite (requires Chocolatey)

# Or download from: https://www.sqlite.org/download.html

# Navigate to database folder
cd "$env:LOCALAPPDATA\OneManVan"

# Backup first!
Copy-Item OneManVan.db OneManVan_backup_$(Get-Date -Format 'yyyyMMdd_HHmmss').db

# Open SQLite
sqlite3 OneManVan.db
```

Then run these SQL commands inside SQLite:

```sql
-- Add missing columns to Assets table
ALTER TABLE Assets ADD COLUMN CompressorWarrantyYears INTEGER NOT NULL DEFAULT 10;

-- Remove AccessInstructions if it exists (not in Shared model)
-- Note: SQLite doesn't support DROP COLUMN, so this is okay to skip

-- Verify schema
.schema Assets

-- Exit
.quit
```

### **Alternative: Use C# Migration Code**

Add this to your `App.xaml.cs` OnStartup method **before** `DbContext.Database.EnsureCreated()`:

```csharp
// Add missing columns via raw SQL
try
{
    DbContext.Database.ExecuteSqlRaw(
        "ALTER TABLE Assets ADD COLUMN CompressorWarrantyYears INTEGER NOT NULL DEFAULT 10");
}
catch (SqliteException ex) when (ex.Message.Contains("duplicate column"))
{
    // Column already exists, ignore
}
```

---

## ?? **RECOMMENDED APPROACH**

### **For Your Situation:**

Since this is a **development/testing** environment and you recently did major model consolidation:

? **USE SOLUTION 1** (Fresh Start)

**Why?**
- Ensures 100% schema correctness
- No risk of migration errors
- Clean slate with Shared models
- Can re-seed test data easily

### **When to Use Solution 2:**

Only if you have **critical production data** that can't be recreated.

---

## ?? **Verify After Fix**

After recreating database, verify it works:

1. **Run Application**
   - Should start without SQLite errors
   - No "no such column" errors

2. **Test Key Pages**
   - Assets page loads
   - Customers page loads
   - Settings page loads
   - DataGrids display correctly

3. **Check Database Schema**
   ```powershell
   # Install SQLite browser
   # Download: https://sqlitebrowser.org/
   
   # Or use command line
   cd "$env:LOCALAPPDATA\OneManVan"
   sqlite3 OneManVan.db ".schema Assets"
   ```

   Should show all columns from Shared.Models.Asset

---

## ?? **Quick Command Reference**

### **Find Database:**
```powershell
Get-ChildItem -Path "$env:LOCALAPPDATA\OneManVan" -Filter "*.db"
```

### **Delete Database:**
```powershell
Remove-Item "$env:LOCALAPPDATA\OneManVan\OneManVan.db*" -Force
```

### **Backup Database:**
```powershell
$timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
Copy-Item "$env:LOCALAPPDATA\OneManVan\OneManVan.db" `
          "$env:LOCALAPPDATA\OneManVan\OneManVan_backup_$timestamp.db"
```

### **View Database Schema:**
```powershell
cd "$env:LOCALAPPDATA\OneManVan"
sqlite3 OneManVan.db ".schema"
```

---

## ?? **IMPORTANT NOTES**

### **Why This Happened:**
1. Desktop had its own `Models/Asset.cs`
2. We deleted it and switched to `Shared/Models/Asset.cs`
3. Shared model has different properties
4. Old database still has old schema
5. **Database wasn't migrated** during consolidation

### **Prevention for Future:**
When changing models significantly:
1. ? Use Entity Framework Migrations
2. ? Add migration: `Add-Migration UpdateAssetSchema`
3. ? Apply migration: `Update-Database`

But for now, **fresh start is easiest!**

---

## ?? **STEP-BY-STEP GUIDE**

### **DO THIS NOW:**

1. **Close OneManVan.exe**

2. **Open PowerShell as Administrator**

3. **Run these commands:**
   ```powershell
   # Backup current database
   $dbPath = "$env:LOCALAPPDATA\OneManVan\OneManVan.db"
   if (Test-Path $dbPath) {
       Copy-Item $dbPath "$env:LOCALAPPDATA\OneManVan\OneManVan_OLD.db"
       Write-Host "? Backup created"
   }
   
   # Delete database files
   Remove-Item "$env:LOCALAPPDATA\OneManVan\OneManVan.db*" -Force
   Write-Host "? Database deleted"
   ```

4. **Start OneManVan.exe**
   - New database will be created automatically
   - Open Settings ? Test Runner ? "Seed Test Data"

5. **Verify**
   - Open Assets page
   - Open Customers page
   - Check for any errors

---

## ? **Expected Result**

After following these steps:

```
? No more "no such column" errors
? All pages load correctly
? DataGrids display without errors
? CSV Import/Export works
? Fresh database with correct schema
```

---

## ?? **Need Help?**

If you get stuck or have production data to preserve, let me know and I'll create a custom migration script for you!

---

**Generated:** 2025-01-23  
**Recommended:** ? **Solution 1 - Fresh Start**  
**Time Required:** 2 minutes  
**Risk:** None (backup created first)  

?? **DO THIS NOW** ? Follow Step-by-Step Guide above!
