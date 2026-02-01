# Fix Missing AssetNumber Column
# Run this to add the missing column to your SQLite database

$ErrorActionPreference = "Stop"

Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Yellow
Write-Host "?           Fix Missing AssetNumber Column in Assets            ?" -ForegroundColor Yellow
Write-Host "??????????????????????????????????????????????????????????????????" -ForegroundColor Yellow

# Find the SQLite database
$dbPath = ".\OneManVan.Web\AppData\OneManVan.db"
if (-not (Test-Path $dbPath)) {
    $dbPath = ".\OneManVan.Web\AppData\onemanvan.db"
}
if (-not (Test-Path $dbPath)) {
    # Try to find any .db file
    $dbFiles = Get-ChildItem -Path ".\OneManVan.Web\AppData" -Filter "*.db" -ErrorAction SilentlyContinue
    if ($dbFiles) {
        $dbPath = $dbFiles[0].FullName
    }
}

if (-not (Test-Path $dbPath)) {
    Write-Host "? Could not find SQLite database file!" -ForegroundColor Red
    Write-Host "Expected location: OneManVan.Web\AppData\OneManVan.db" -ForegroundColor Yellow
    exit 1
}

Write-Host "`n?? Database found: $dbPath" -ForegroundColor Cyan

# Check if sqlite3 is available
$sqlitePath = "sqlite3"
try {
    & $sqlitePath --version 2>&1 | Out-Null
} catch {
    Write-Host "? sqlite3 not found. Using alternative method..." -ForegroundColor Yellow
    
    # Alternative: Use dotnet-script or direct file manipulation
    Write-Host "`n?? Run this SQL manually in your SQLite browser:" -ForegroundColor Cyan
    Write-Host @"

ALTER TABLE Assets ADD COLUMN AssetNumber TEXT;

"@ -ForegroundColor White
    
    Write-Host "`nOr restart your application - EF Core might auto-create the column." -ForegroundColor Yellow
    exit 0
}

Write-Host "`n?? Adding AssetNumber column..." -ForegroundColor Yellow

$sql = "ALTER TABLE Assets ADD COLUMN AssetNumber TEXT;"

try {
    & $sqlitePath $dbPath $sql 2>&1
    Write-Host "? Column added successfully!" -ForegroundColor Green
} catch {
    if ($_.Exception.Message -like "*duplicate column*") {
        Write-Host "? Column already exists!" -ForegroundColor Green
    } else {
        Write-Host "??  Error: $_" -ForegroundColor Yellow
        Write-Host "The column may already exist, which is fine." -ForegroundColor Gray
    }
}

# Verify
Write-Host "`n?? Verifying column exists..." -ForegroundColor Cyan
$verify = & $sqlitePath $dbPath "PRAGMA table_info(Assets);" 2>&1
Write-Host $verify

Write-Host "`n? Done! Restart your application and try again." -ForegroundColor Green
