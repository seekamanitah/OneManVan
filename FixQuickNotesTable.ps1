# Fix QuickNotes Table - SQLite
# Creates the missing QuickNotes table in the SQLite database

Write-Host "Creating QuickNotes table in SQLite database..." -ForegroundColor Cyan
Write-Host ""

# Find the SQLite database file
$dbPath = "OneManVan.Web\AppData\onemanvan.db"

if (-not (Test-Path $dbPath)) {
    $dbPath = "OneManVan.Web\Data\business.db"
}

if (-not (Test-Path $dbPath)) {
    Write-Host "[ERROR] Could not find database file" -ForegroundColor Red
    Write-Host "Expected locations:" -ForegroundColor Yellow
    Write-Host "  - OneManVan.Web\AppData\onemanvan.db" -ForegroundColor Gray
    Write-Host "  - OneManVan.Web\Data\business.db" -ForegroundColor Gray
    exit 1
}

Write-Host "Found database: $dbPath" -ForegroundColor Green
Write-Host ""

# SQLite command to create the table
$createTableSql = @"
CREATE TABLE IF NOT EXISTS QuickNotes (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Title TEXT,
    Content TEXT NOT NULL,
    Category TEXT,
    IsPinned INTEGER NOT NULL DEFAULT 0,
    IsArchived INTEGER NOT NULL DEFAULT 0,
    Color TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);
"@

# Check if sqlite3 is available
$sqlite3Path = Get-Command sqlite3 -ErrorAction SilentlyContinue

if ($sqlite3Path) {
    Write-Host "Using sqlite3 command-line tool..." -ForegroundColor Yellow
    $createTableSql | sqlite3 $dbPath
    Write-Host "[SUCCESS] QuickNotes table created!" -ForegroundColor Green
} else {
    Write-Host "sqlite3 command not found." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "MANUAL STEPS:" -ForegroundColor Cyan
    Write-Host "1. Stop the Web app if running" -ForegroundColor White
    Write-Host "2. Delete the database file:" -ForegroundColor White
    Write-Host "   Remove-Item '$dbPath'" -ForegroundColor Gray
    Write-Host "3. Restart the Web app - it will recreate the database with QuickNotes" -ForegroundColor White
    Write-Host ""
    Write-Host "OR install SQLite command-line tools:" -ForegroundColor Cyan
    Write-Host "   winget install SQLite.SQLite" -ForegroundColor Gray
}

Write-Host ""
Write-Host "After fixing, restart the Web app:" -ForegroundColor Yellow
Write-Host "  cd OneManVan.Web" -ForegroundColor Gray
Write-Host "  dotnet run" -ForegroundColor Gray
