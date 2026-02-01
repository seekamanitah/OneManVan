# Import File Type Detection Fix - Complete

## Problem Fixed
Error: `'<' is an invalid start of a value. Path: $ | LineNumber: 0 | BytePositionInLine: 0.`

**Root Cause:** Excel files were being sent with `content-type: text/csv`, causing the API to reject them and return an HTML error page instead of JSON.

## Solution Applied
Added file type detection to set the correct `Content-Type` header based on file extension:

### Content Types
- `.xlsx` ? `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- `.xls` ? `application/vnd.ms-excel`
- `.csv`, `.txt` ? `text/csv`
- Other ? `text/plain`

## Files Updated

### 1. `OneManVan.Web/Components/Shared/ImportButton.razor`
**Location:** `StartImport()` method (lines 224-260)
- Added file extension detection
- Set correct Content-Type header based on extension
- Applied to all list page imports

### 2. `OneManVan.Web/Components/Pages/Settings/Settings.razor`
**Location:** `ImportSelectedData()` method (lines 1313-1360)
- Added file extension detection
- Set correct Content-Type header based on extension
- Applied to Settings page bulk import

## Coverage
All import functionality now supports:
? Import Button component (used on all list pages)
- CustomerList, ProductList, AssetList, InventoryList
- JobList, InvoiceList, EstimateList
- CompanyList, SiteList, AgreementList
- EmployeeList, ClaimsList, MaterialListIndex

? Settings page bulk import

## Testing
Test with:
1. CSV file ? Should import successfully
2. Excel file (.xlsx) ? Should now import successfully
3. Excel file (.xls) ? Should now import successfully

## Deploy
```powershell
cd C:\Users\tech\source\repos\TradeFlow
git add -A
git commit -m "Fix: Correct file type detection for Excel and CSV imports"
git push origin master
```

Server deployment:
```sh
cd /var/www/onemanvan
docker-compose down
git pull origin master
docker-compose build --no-cache onemanvan-web
docker-compose up -d
```
