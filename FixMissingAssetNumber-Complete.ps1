# Fix Missing AssetNumber Column - Complete
# This script adds the missing column to your SQLite database

$ErrorActionPreference = "Stop"

Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Red
Write-Host "?          CRITICAL FIX: Missing AssetNumber Column             ?" -ForegroundColor Red
Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Red

Write-Host "`n??  ISSUE: SQLite Error - 'no such column: a.AssetNumber'" -ForegroundColor Yellow
Write-Host "Database schema is out of sync with Asset model.`n" -ForegroundColor White

# Stop the application first
Write-Host "?? Step 1: Stop the application if running..." -ForegroundColor Cyan
Write-Host "Press Shift+F5 in Visual Studio to stop debugging" -ForegroundColor Yellow
$continue = Read-Host "`nPress Enter when app is stopped"

# Find database file
Write-Host "`n?? Step 2: Locating database file..." -ForegroundColor Cyan
$dbPath = ".\OneManVan.Web\AppData\OneManVan.db"

if (-not (Test-Path $dbPath)) {
    Write-Host "? Database not found at: $dbPath" -ForegroundColor Red
    Write-Host "Checking alternate locations..." -ForegroundColor Yellow
    
    # Check alternate names
    $alternates = @(
        ".\OneManVan.Web\AppData\onemanvan.db",
        ".\OneManVan.Web\Data\app.db",
        ".\OneManVan.Web\Data\business.db"
    )
    
    foreach ($alt in $alternates) {
        if (Test-Path $alt) {
            $dbPath = $alt
            Write-Host "? Found database at: $dbPath" -ForegroundColor Green
            break
        }
    }
    
    if (-not (Test-Path $dbPath)) {
        Write-Host "`n? Could not locate database file!" -ForegroundColor Red
        Write-Host "Expected location: OneManVan.Web\AppData\OneManVan.db" -ForegroundColor Yellow
        Write-Host "`nManually specify path or press Ctrl+C to cancel" -ForegroundColor Yellow
        $dbPath = Read-Host "Enter database path"
    }
}

Write-Host "? Database: $dbPath" -ForegroundColor Green

# Option 1: Use sqlite3 if available
Write-Host "`n?? Step 3: Applying migration..." -ForegroundColor Cyan
Write-Host "Checking for sqlite3..." -ForegroundColor Gray

try {
    $null = & sqlite3 --version 2>&1
    $hasSqlite = $true
} catch {
    $hasSqlite = $false
}

if ($hasSqlite) {
    Write-Host "? sqlite3 found - using direct SQL" -ForegroundColor Green
    
    $sql = @"
-- Add AssetNumber column if missing
ALTER TABLE Assets ADD COLUMN AssetNumber TEXT;

-- Generate AssetNumbers for existing records
UPDATE Assets 
SET AssetNumber = 'AST-' || printf('%04d', Id)
WHERE AssetNumber IS NULL;
"@
    
    try {
        $sql | & sqlite3 $dbPath 2>&1
        Write-Host "? AssetNumber column added successfully!" -ForegroundColor Green
    } catch {
        if ($_ -like "*duplicate column*") {
            Write-Host "? Column already exists!" -ForegroundColor Green
        } else {
            Write-Host "??  Error: $_" -ForegroundColor Yellow
        }
    }
} else {
    Write-Host "??  sqlite3 not found - using .NET solution" -ForegroundColor Yellow
    
    # Option 2: Use EF Core raw SQL
    Write-Host "`nCreating C# migration script..." -ForegroundColor Cyan
    
    $migrationScript = @"
using Microsoft.EntityFrameworkCore;
using OneManVan.Shared.Data;

var factory = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<OneManVanDbContext>()
    .UseSqlite(`"Data Source=$dbPath`")
    .Options;

using var db = new OneManVanDbContext(factory);

try {
    // Add column
    db.Database.ExecuteSqlRaw(@`"ALTER TABLE Assets ADD COLUMN AssetNumber TEXT;`");
    
    // Generate AssetNumbers for existing records
    db.Database.ExecuteSqlRaw(@`"
        UPDATE Assets 
        SET AssetNumber = 'AST-' || printf('%04d', Id)
        WHERE AssetNumber IS NULL;
    `");
    
    Console.WriteLine(`"? Migration successful!`");
} catch (Exception ex) {
    if (ex.Message.Contains(`"duplicate column`")) {
        Console.WriteLine(`"? Column already exists!`");
    } else {
        Console.WriteLine(`$`"? Error: {ex.Message}`");
    }
}
"@
    
    $scriptPath = ".\Temp_FixAssetNumber.csx"
    $migrationScript | Out-File -FilePath $scriptPath -Encoding UTF8
    
    Write-Host "Running migration with dotnet-script..." -ForegroundColor Yellow
    & dotnet script $scriptPath
    
    Remove-Item $scriptPath -ErrorAction SilentlyContinue
}

Write-Host "`n? Step 3 Complete: AssetNumber column added" -ForegroundColor Green

# Verify
Write-Host "`n?? Step 4: Verifying migration..." -ForegroundColor Cyan
if ($hasSqlite) {
    $columns = & sqlite3 $dbPath "PRAGMA table_info(Assets);" | Select-String "AssetNumber"
    if ($columns) {
        Write-Host "? Verified: AssetNumber column exists" -ForegroundColor Green
    } else {
        Write-Host "??  Warning: Could not verify column" -ForegroundColor Yellow
    }
}

Write-Host "`n??????????????????????????????????????????????????????????????????" -ForegroundColor Green
Write-Host "?                  ? MIGRATION COMPLETE                         ?" -ForegroundColor Green
Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Green

Write-Host "`n?? Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Start your application (F5)" -ForegroundColor White
Write-Host "  2. Login with: admin@onemanvan.com / Admin@123456!" -ForegroundColor White
Write-Host "  3. Test customer edit forms" -ForegroundColor White
Write-Host "  4. The AssetNumber error should be gone ?`n" -ForegroundColor White

Write-Host "Press any key to close..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
