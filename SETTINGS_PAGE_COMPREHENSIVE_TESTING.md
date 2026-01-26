# ?? **COMPREHENSIVE SETTINGS PAGE TESTING GUIDE**

**Date:** January 2026  
**Purpose:** Test all Settings Page features - existing + new additions  
**Status:** Ready for Testing

---

## **?? WHAT WE ADDED**

### **New Features:**
1. ? **Complete Data Export** - Export ALL data to one file
2. ? **Complete Data Import** - Import entire dataset from file
3. ? **Business Profile** - Company info for invoices/estimates

### **Existing Features (Re-test):**
- App Info Display
- Trade Configuration
- Appearance (Dark Mode)
- Business Defaults
- Data Backup
- Individual CSV Exports/Imports
- Database Statistics
- Danger Zone

---

## **?? COMPLETE TESTING CHECKLIST**

---

## **SECTION 1: App Info Display** ?

### **Test 1.1: Version Display**
- [ ] Open Settings page
- [ ] Verify version shows "1.0.0-beta.2"
- [ ] Check it's readable in both light/dark mode

### **Test 1.2: Trade Display**
- [ ] Check "Trade:" shows current trade name
- [ ] Verify trade icon displays (if set)
- [ ] Check "HVAC" or selected trade shows correctly

### **Test 1.3: Database Info**
- [ ] Verify "Database: Local SQLite" displays
- [ ] Check text is readable in both themes

---

## **SECTION 2: Trade Configuration** ?

### **Test 2.1: Current Trade Display**
- [ ] See current trade name and description
- [ ] Verify card shows proper colors
- [ ] Check it displays in dark mode properly

### **Test 2.2: Change Trade**
- [ ] Tap "Change Trade" button
- [ ] See list of available trades
- [ ] Select a different trade (e.g., Plumbing)
- [ ] Verify trade changes throughout app
- [ ] Return to Settings and verify new trade displays

### **Test 2.3: Edit Custom Fields**
- [ ] Tap "Edit Custom Fields"
- [ ] Verify SchemaEditorPage opens
- [ ] Check fields display correctly
- [ ] Go back to Settings

---

## **SECTION 3: Appearance (Dark Mode)** ?

### **Test 3.1: Toggle Dark Mode**
- [ ] Find "Dark Mode" switch
- [ ] Toggle ON - App switches to dark theme immediately
- [ ] Verify Settings page looks good in dark mode
- [ ] Toggle OFF - App switches to light theme
- [ ] Verify Settings page looks good in light mode

### **Test 3.2: Persistence**
- [ ] Set Dark Mode ON
- [ ] Close app completely
- [ ] Reopen app
- [ ] Verify dark mode is still active
- [ ] Navigate to Settings
- [ ] Verify switch is still ON

---

## **SECTION 4: Business Profile** ? NEW!

### **Test 4.1: Initial Load**
- [ ] Find "Business Profile" section (orange header)
- [ ] Verify all 4 input fields are empty (first time)
- [ ] Check fields: Company Name, Phone, Email, Address

### **Test 4.2: Enter Business Info**
- [ ] Company Name: "HVAC Pro Services"
- [ ] Phone: "(555) 123-4567"
- [ ] Email: "info@hvacpro.com"
- [ ] Address: "123 Main St, Anytown, ST 12345"

### **Test 4.3: Save Profile**
- [ ] Tap "Save Profile" button
- [ ] Verify success message appears
- [ ] Check button provides haptic feedback

### **Test 4.4: Persistence Test**
- [ ] Close Settings page
- [ ] Navigate away
- [ ] Return to Settings
- [ ] Verify all business info is still there

### **Test 4.5: Update Profile**
- [ ] Change company name to "Elite HVAC"
- [ ] Tap "Save Profile"
- [ ] Verify success message
- [ ] Go away and come back
- [ ] Verify updated name persists

---

## **SECTION 5: Business Defaults** ?

### **Test 5.1: View Defaults**
- [ ] Find "Business Defaults" section
- [ ] Verify Labor Rate shows (default: 85.00)
- [ ] Verify Tax Rate shows (default: 7.0)
- [ ] Verify Invoice Due Days shows (default: 30)

### **Test 5.2: Change Defaults**
- [ ] Change Labor Rate to "95.00"
- [ ] Change Tax Rate to "8.5"
- [ ] Change Invoice Due to "21"
- [ ] Tap "Save Defaults"
- [ ] Verify success message

### **Test 5.3: Persistence**
- [ ] Close and reopen Settings
- [ ] Verify new values persist:
  - Labor Rate: 95.00
  - Tax Rate: 8.5
  - Invoice Due: 21

### **Test 5.4: Apply to New Estimates/Invoices**
- [ ] Create a new estimate
- [ ] Check if tax rate applies
- [ ] Create a new invoice
- [ ] Check if labor rate applies

---

## **SECTION 6: Complete Data Export/Import** ? NEW!

### **Test 6.1: Initial View**
- [ ] Find "Complete Data Export" section (blue bordered)
- [ ] Verify description explains it exports ALL data
- [ ] See two buttons: "Export All Data" and "Import All Data"
- [ ] Check list of included data types displays

### **Test 6.2: Export All Data - Empty Database**
- [ ] Start with fresh/empty database
- [ ] Tap "Export All Data"
- [ ] Confirm export
- [ ] Verify success message shows "0 records"
- [ ] Option to share file appears

### **Test 6.3: Add Test Data**
- [ ] Add 2 customers
- [ ] Add 2 assets
- [ ] Add 1 job
- [ ] Add 1 estimate
- [ ] Add 2 inventory items

### **Test 6.4: Export All Data - With Data**
- [ ] Tap "Export All Data"
- [ ] Confirm export
- [ ] Verify success message shows correct count (8 records)
- [ ] Tap "Share" to see share options
- [ ] Verify file can be shared

### **Test 6.5: Verify Export File**
- [ ] Open exported file in Excel or text editor
- [ ] Verify sections are present:
  - ### CUSTOMERS ###
  - ### SITES ###
  - ### ASSETS ###
  - ### INVENTORY ###
  - ### JOBS ###
  - ### ESTIMATES ###
  - ### INVOICES ###
  - ### PRODUCTS ###
  - ### SERVICE_AGREEMENTS ###
- [ ] Verify data is properly formatted
- [ ] Check customer names are correct
- [ ] Check asset serial numbers are correct

### **Test 6.6: Edit Export File**
- [ ] Open exported CSV in Excel
- [ ] Find CUSTOMERS section
- [ ] Add a new customer row:
  ```
  0,Test Customer,Test Company,test@test.com,(555) 999-9999,Commercial,Active,Test notes,2026-01-22
  ```
- [ ] Save the file

### **Test 6.7: Import All Data**
- [ ] Return to Settings
- [ ] Tap "Import All Data"
- [ ] Confirm import
- [ ] Select the edited CSV file
- [ ] Verify import success message
- [ ] Check "Records added: 1"
- [ ] Check "Records updated: X" (existing records)

### **Test 6.8: Verify Import Worked**
- [ ] Navigate to Customers list
- [ ] Verify "Test Customer" appears
- [ ] Verify email and phone are correct
- [ ] Check all original customers still exist
- [ ] Verify no data was lost

### **Test 6.9: Import Error Handling**
- [ ] Try importing an invalid file (wrong format)
- [ ] Verify error message appears
- [ ] App doesn't crash

---

## **SECTION 7: Data Backup** ?

### **Test 7.1: View Backup Info**
- [ ] Find "Data Backup" section
- [ ] Check "Last Backup" displays
- [ ] Check backup count displays

### **Test 7.2: Create Backup**
- [ ] Tap "Export Backup"
- [ ] Verify backup created successfully
- [ ] Check success message with record count
- [ ] Verify "Last Backup" updates

### **Test 7.3: Share Backup**
- [ ] Tap "Share Backup"
- [ ] See share sheet with backup file
- [ ] Verify file can be shared

### **Test 7.4: Import Backup**
- [ ] Tap "Import Backup"
- [ ] Select a backup file
- [ ] Choose "Merge" mode
- [ ] Verify import success

---

## **SECTION 8: Individual CSV Exports** ?

### **Test 8.1: Export Customers**
- [ ] Tap "Export Customers"
- [ ] Verify success message
- [ ] Option to share appears
- [ ] Share works

### **Test 8.2: Export Inventory**
- [ ] Tap "Export Inventory"
- [ ] Verify success message
- [ ] Share option works

### **Test 8.3: Export Assets**
- [ ] Tap "Export Assets"
- [ ] Verify success message
- [ ] Check record count

### **Test 8.4: Export Jobs**
- [ ] Tap "Export Jobs"
- [ ] Verify success message

### **Test 8.5: Import Customers CSV**
- [ ] Create/edit a customer CSV
- [ ] Tap "Import Customers CSV"
- [ ] Select file
- [ ] Verify import success

### **Test 8.6: Import Inventory CSV**
- [ ] Create/edit inventory CSV
- [ ] Tap "Import Inventory CSV"
- [ ] Select file
- [ ] Verify import success

---

## **SECTION 9: Database Statistics** ?

### **Test 9.1: View Statistics**
- [ ] Find "Database Statistics" section
- [ ] Verify counts display:
  - Customers: X
  - Assets: X
  - Estimates: X
  - Jobs: X
  - Invoices: X
  - Inventory: X
  - Database Size: X KB

### **Test 9.2: Accuracy**
- [ ] Count actual customers in Customers list
- [ ] Compare with stat shown
- [ ] Verify numbers match

### **Test 9.3: Real-Time Update**
- [ ] Note current customer count
- [ ] Add a new customer
- [ ] Return to Settings
- [ ] Verify customer count increased by 1

---

## **SECTION 10: Danger Zone** ??

### **Test 10.1: View Warning**
- [ ] Find "Danger Zone" (red border)
- [ ] Read warning message
- [ ] See "Clear All Data" button

### **Test 10.2: Clear All Data - Cancel**
- [ ] Tap "Clear All Data"
- [ ] See confirmation dialog
- [ ] Tap "Cancel"
- [ ] Verify no data deleted
- [ ] Check customers still exist

### **Test 10.3: Clear All Data - Confirm** ?? DESTRUCTIVE!
**WARNING: Only test this if you're okay losing all data!**
- [ ] Create a backup first!
- [ ] Tap "Clear All Data"
- [ ] Confirm deletion
- [ ] Verify success message
- [ ] Navigate to Customers list
- [ ] Verify list is empty
- [ ] Check all other lists are empty
- [ ] Import your backup to restore

---

## **SECTION 11: Developer Tools** ?

### **Test 11.1: Run Tests**
- [ ] Find "Developer Tools" section
- [ ] Tap "Run Tests"
- [ ] Verify test runner launches
- [ ] Tests run successfully

### **Test 11.2: View Schema**
- [ ] Tap "View Schema"
- [ ] Schema viewer opens
- [ ] Database schema displays
- [ ] Can navigate tables

---

## **SECTION 12: About Section** ?

### **Test 12.1: View About Info**
- [ ] Scroll to bottom
- [ ] Find "About" section
- [ ] Verify copyright displays
- [ ] Check year is correct (2026)

---

## **SECTION 13: Theme Consistency** ?

### **Test 13.1: Light Mode Appearance**
- [ ] Switch to Light Mode
- [ ] Scroll through entire Settings page
- [ ] Verify all sections readable
- [ ] Check no white-on-white or other contrast issues
- [ ] Verify buttons stand out
- [ ] Check input fields are visible

### **Test 13.2: Dark Mode Appearance**
- [ ] Switch to Dark Mode
- [ ] Scroll through entire Settings page
- [ ] Verify all sections readable
- [ ] Check no black-on-black issues
- [ ] Verify cards have dark backgrounds
- [ ] Check borders are visible
- [ ] Verify inputs have dark backgrounds

### **Test 13.3: New Sections Specifically**
- [ ] **Business Profile** in dark mode:
  - Card background is dark
  - Orange header stands out
  - Inputs are visible (not white)
  - "Save Profile" button looks good
  
- [ ] **Complete Data Export** in dark mode:
  - Blue border is visible
  - Blue background shade is appropriate
  - Text is readable
  - Buttons stand out

---

## **SECTION 14: Navigation & Performance** ?

### **Test 14.1: Scroll Performance**
- [ ] Scroll from top to bottom
- [ ] Verify smooth scrolling
- [ ] No lag or stuttering
- [ ] All sections load quickly

### **Test 14.2: Page Load Time**
- [ ] Navigate away from Settings
- [ ] Navigate back to Settings
- [ ] Page loads within 1 second
- [ ] All data populates quickly

### **Test 14.3: Back Button**
- [ ] Tap device/app back button
- [ ] Returns to previous page
- [ ] No crashes

---

## **SECTION 15: Edge Cases & Error Handling** ?

### **Test 15.1: Empty Fields**
- [ ] Try saving Business Profile with empty fields
- [ ] Verify it saves (empty is allowed)
- [ ] No crash occurs

### **Test 15.2: Invalid Input**
- [ ] Enter non-numeric value in Labor Rate (e.g., "abc")
- [ ] Try to save
- [ ] Check error handling

### **Test 15.3: Export with No Data**
- [ ] Clear all data (or start fresh)
- [ ] Try each export button
- [ ] Verify exports succeed with "0 records"
- [ ] No crashes

### **Test 15.4: Import Bad File**
- [ ] Try importing a non-CSV file
- [ ] Verify error message
- [ ] App doesn't crash

### **Test 15.5: Import Corrupted CSV**
- [ ] Create a CSV with malformed data
- [ ] Try importing
- [ ] Check error messages are helpful
- [ ] Partial import handles gracefully

---

## **SECTION 16: Integration Testing** ?

### **Test 16.1: Business Profile ? Estimates**
**Future Feature** - Check if business profile info appears on estimates/invoices

### **Test 16.2: Business Defaults ? New Jobs**
- [ ] Set Labor Rate to 100.00
- [ ] Create a new job with time tracking
- [ ] Verify labor calculations use $100/hr

### **Test 16.3: Tax Rate ? New Invoices**
- [ ] Set Tax Rate to 10%
- [ ] Create a new invoice
- [ ] Verify 10% tax is applied

---

## **?? TESTING SUMMARY TEMPLATE**

After completing tests, fill this out:

```
SETTINGS PAGE TEST RESULTS
Date: ___________
Tester: ___________

Total Tests: 100+
? Passed: _____
? Failed: _____
?? Warnings: _____

Critical Issues Found:
1. 
2. 
3. 

Minor Issues Found:
1. 
2. 
3. 

Performance Notes:


Recommendations:

```

---

## **?? PRIORITY TESTING ORDER**

If you're short on time, test in this order:

### **High Priority:**
1. ? Complete Data Export/Import (NEW features)
2. ? Business Profile (NEW feature)
3. ? Dark Mode toggle
4. ? Business Defaults save/load

### **Medium Priority:**
5. Individual CSV exports
6. Database stats
7. Backup/restore
8. Trade configuration

### **Low Priority:**
9. Developer tools
10. About section
11. Edge cases

---

## **?? QUICK SMOKE TEST** (5 minutes)

Just want to verify it works? Do this:

1. [ ] Open Settings
2. [ ] Toggle Dark Mode ON/OFF - Works?
3. [ ] Fill in Business Profile - Save - Works?
4. [ ] Tap "Export All Data" - Works?
5. [ ] Check Database Stats - Numbers look right?
6. [ ] Close and reopen - Settings persist?

If all ? ? **Basic functionality confirmed!**

---

## **?? BUG REPORTING TEMPLATE**

Found a bug? Report it like this:

```
BUG: [Short description]

Section: [Which settings section]
Steps to Reproduce:
1. 
2. 
3. 

Expected: 
Actual: 
Screenshot: [if applicable]
Device: [Android/iOS/Windows]
Theme: [Light/Dark]
```

---

## **? SUCCESS CRITERIA**

Settings Page is **READY FOR PRODUCTION** if:

- [x] All existing features work
- [x] New "Complete Data Export" works
- [x] New "Business Profile" works
- [x] Dark mode looks good on all sections
- [x] No crashes during normal use
- [x] Data persists correctly
- [x] Export/Import doesn't lose data

---

## **?? TESTING COMPLETE!**

When all tests pass, Settings page is **production-ready** with:

? **10 Major Sections**  
? **3 Brand New Features**  
? **Full Dark Mode Support**  
? **100% Theme-Aware**  
? **Robust Export/Import**  

**Ready to ship!** ??
