# ?? DATABASE SCHEMA FIX - QUICK SOLUTION

**Issue:** Database columns don't match Shared models after consolidation  
**Solution:** Drop old database and recreate with correct schema  

---

## ? **QUICK FIX STEPS**

### **Step 1: Find Your Database File**

The database is typically located at:
```
C:\Users\YourUsername\AppData\Local\OneManVan\OneManVan.db
```

Or check your `appsettings.json` for the path.

### **Step 2: Close the Application**

Make sure OneManVan.exe is **completely closed** (check Task Manager).

### **Step 3: Delete Database Files**

Delete these files:
```
OneManVan.db
OneManVan.db-shm
OneManVan.db-wal
```

### **Step 4: Restart Application**

Run the application. Entity Framework will **automatically create** a new database with the correct schema from `OneManVan.Shared.Models`.

---

## ?? **Alternative: Manual Migration (Advanced)**

If you want to **preserve existing data**, I can help you create a migration script.

### **Missing/Changed Columns:**

**In Asset model:**
- ? `AccessInstructions` - Removed from Shared
- ? `CompressorWarrantyYears` - Should exist as `CompressorWarrantyYears`

Let me check what other columns might be mismatched...

---

## ?? **Recommended Action**

**For Development/Testing:**
? **Delete database** - Fastest, clean schema

**For Production:**
?? **Backup first** then create migration script

Which would you prefer?
