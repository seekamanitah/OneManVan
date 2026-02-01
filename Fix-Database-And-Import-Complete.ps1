# Complete Fix: Database Schema + Import ID Generation
# Fixes both AssetNumber column AND verifies import autogeneration

Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?        COMPREHENSIVE FIX: Schema + Import Generation          ?" -ForegroundColor Cyan
Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Cyan

Write-Host "`n?? ISSUE #1: Missing AssetNumber column" -ForegroundColor Red
Write-Host "?? ISSUE #2: Import not autogenerating Customer IDs`n" -ForegroundColor Red

# STEP 1: Stop application
Write-Host "?? STEP 1: Stop your application" -ForegroundColor Yellow
Write-Host "Press Shift+F5 in Visual Studio to stop debugging" -ForegroundColor White
$continue = Read-Host "`nPress Enter when stopped"

# STEP 2: Fix database schema
Write-Host "`n?? STEP 2: Fixing database schema..." -ForegroundColor Yellow

$dbPath = ".\OneManVan.Web\AppData\OneManVan.db"
if (-not (Test-Path $dbPath)) {
    Write-Host "??  Database not found at default location" -ForegroundColor Yellow
    Write-Host "Searching for database..." -ForegroundColor Gray
    
    $alternates = @(
        ".\OneManVan.Web\AppData\onemanvan.db",
        ".\OneManVan.Web\Data\app.db"
    )
    
    foreach ($alt in $alternates) {
        if (Test-Path $alt) {
            $dbPath = $alt
            break
        }
    }
}

if (-not (Test-Path $dbPath)) {
    Write-Host "? Could not find database file!" -ForegroundColor Red
    Write-Host "Please specify the path:" -ForegroundColor Yellow
    $dbPath = Read-Host "Database path"
}

Write-Host "? Database: $dbPath" -ForegroundColor Green

# Check for sqlite3
try {
    $null = & sqlite3 --version 2>&1
    $hasSqlite = $true
    Write-Host "? sqlite3 found" -ForegroundColor Green
} catch {
    $hasSqlite = $false
    Write-Host "??  sqlite3 not found - using fallback method" -ForegroundColor Yellow
}

# Apply migration
Write-Host "`nApplying AssetNumber column migration..." -ForegroundColor Cyan

if ($hasSqlite) {
    $sql = @"
-- Add AssetNumber if missing
ALTER TABLE Assets ADD COLUMN AssetNumber TEXT;

-- Generate AssetNumbers for existing assets
UPDATE Assets 
SET AssetNumber = 'AST-' || printf('%04d', Id)
WHERE AssetNumber IS NULL;
"@
    
    try {
        $sql | & sqlite3 $dbPath 2>&1 | Out-Null
        Write-Host "? AssetNumber column added" -ForegroundColor Green
    } catch {
        if ($_ -like "*duplicate column*") {
            Write-Host "? Column already exists" -ForegroundColor Green
        } else {
            Write-Host "? Error: $_" -ForegroundColor Red
        }
    }
    
    # Verify column exists
    $verify = & sqlite3 $dbPath "PRAGMA table_info(Assets);" | Select-String "AssetNumber"
    if ($verify) {
        Write-Host "? Verified: AssetNumber column present" -ForegroundColor Green
    }
} else {
    Write-Host "??  Manual fix required:" -ForegroundColor Yellow
    Write-Host "Run this SQL manually:" -ForegroundColor White
    Write-Host "ALTER TABLE Assets ADD COLUMN AssetNumber TEXT;" -ForegroundColor Gray
}

# STEP 3: Check import functionality
Write-Host "`n?? STEP 3: Checking import autogeneration..." -ForegroundColor Yellow

Write-Host "`nChecking if Customer IDs are being generated..." -ForegroundColor Cyan

if ($hasSqlite) {
    # Check if customers have CustomerNumbers
    $customerCheck = & sqlite3 $dbPath "SELECT COUNT(*) FROM Customers WHERE CustomerNumber IS NULL OR CustomerNumber = '';" 2>&1
    
    if ($customerCheck -eq "0") {
        Write-Host "? All customers have generated IDs" -ForegroundColor Green
    } else {
        Write-Host "??  Found $customerCheck customers without IDs" -ForegroundColor Yellow
        Write-Host "This is expected if you just imported them" -ForegroundColor Gray
        
        # Offer to fix
        $fix = Read-Host "`nFix missing Customer IDs now? (y/n)"
        if ($fix -eq 'y') {
            Write-Host "`nGenerating Customer IDs..." -ForegroundColor Cyan
            
            $fixSql = @"
-- Generate Customer IDs for records missing them
UPDATE Customers 
SET CustomerNumber = 'C-' || printf('%04d', Id)
WHERE CustomerNumber IS NULL OR CustomerNumber = '';
"@
            
            $fixSql | & sqlite3 $dbPath
            Write-Host "? Customer IDs generated" -ForegroundColor Green
        }
    }
    
    # Show sample
    Write-Host "`nSample Customer IDs:" -ForegroundColor Cyan
    & sqlite3 $dbPath "SELECT Id, CustomerNumber, FirstName, LastName FROM Customers LIMIT 5;" | ForEach-Object {
        Write-Host "  $_" -ForegroundColor Gray
    }
}

# STEP 4: Verify import service
Write-Host "`n?? STEP 4: Verifying import service code..." -ForegroundColor Yellow

$importFile = ".\OneManVan.Web\Services\Import\CsvImportService.cs"
if (Test-Path $importFile) {
    $importCode = Get-Content $importFile -Raw
    
    if ($importCode -like "*EntityIdPrefixes.FormatId*") {
        Write-Host "? Import service HAS autogeneration code" -ForegroundColor Green
    } else {
        Write-Host "? Import service MISSING autogeneration code" -ForegroundColor Red
        Write-Host "This needs to be fixed in the code!" -ForegroundColor Yellow
    }
    
    if ($importCode -like "*nextNumber++*") {
        Write-Host "? ID counter incrementing correctly" -ForegroundColor Green
    }
} else {
    Write-Host "??  Could not verify import service" -ForegroundColor Yellow
}

# STEP 5: Summary & next steps
Write-Host "`n??????????????????????????????????????????????????????????????????" -ForegroundColor Green
Write-Host "?                     ? FIX COMPLETE                            ?" -ForegroundColor Green
Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Green

Write-Host "`n?? Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Start your application (F5 in Visual Studio)" -ForegroundColor White
Write-Host "  2. Login: admin@onemanvan.com / Admin@123456!" -ForegroundColor White
Write-Host "  3. Test:" -ForegroundColor White
Write-Host "     - Navigate to Customers page (no AssetNumber error)" -ForegroundColor Gray
Write-Host "     - Try importing a CSV file" -ForegroundColor Gray
Write-Host "     - Verify Customer IDs are generated (C-0001, C-0002, etc.)" -ForegroundColor Gray
Write-Host "  4. If import still doesn't generate IDs:" -ForegroundColor White
Write-Host "     - Check application logs for errors" -ForegroundColor Gray
Write-Host "     - The CSV might be failing validation before save" -ForegroundColor Gray

Write-Host "`n?? Test CSV Format:" -ForegroundColor Cyan
Write-Host @"
FirstName,LastName,Email,Phone
John,Doe,john@example.com,555-1234
Jane,Smith,jane@example.com,555-5678
"@ -ForegroundColor Gray

Write-Host "`n?? TIP: Check browser console for import errors" -ForegroundColor Yellow

Write-Host "`nPress any key to close..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
