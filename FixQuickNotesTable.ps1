# Fix QuickNotes Table - Add to existing SQLite database
# Run this script to add the QuickNotes table without losing existing data

Write-Host "Adding QuickNotes table to SQLite database..." -ForegroundColor Cyan
Write-Host ""

# Find the SQLite database file
$dbPath = "OneManVan.Web\Data\business.db"

if (-not (Test-Path $dbPath)) {
    $dbPath = "OneManVan.Web\AppData\business.db"
}

if (-not (Test-Path $dbPath)) {
    Write-Host "ERROR: Could not find business.db file" -ForegroundColor Red
    Write-Host "Looking in:" -ForegroundColor Yellow
    Write-Host "  - OneManVan.Web\Data\business.db" -ForegroundColor Gray
    Write-Host "  - OneManVan.Web\AppData\business.db" -ForegroundColor Gray
    exit 1
}

Write-Host "Found database at: $dbPath" -ForegroundColor Green
Write-Host ""

# SQLite command to create QuickNotes table
$sqlCommand = @"
CREATE TABLE IF NOT EXISTS QuickNotes (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Title TEXT NULL,
    Content TEXT NOT NULL,
    Category TEXT NULL,
    IsPinned INTEGER NOT NULL DEFAULT 0,
    IsArchived INTEGER NOT NULL DEFAULT 0,
    Color TEXT NULL,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    UpdatedAt TEXT NOT NULL DEFAULT (datetime('now'))
);
"@

try {
    # Check if sqlite3 is available
    $sqlite3Path = $null
    
    # Try to find sqlite3.exe in common locations
    $possiblePaths = @(
        "C:\Program Files\SQLite\sqlite3.exe",
        "C:\sqlite\sqlite3.exe",
        "sqlite3.exe"
    )
    
    foreach ($path in $possiblePaths) {
        if (Test-Path $path -ErrorAction SilentlyContinue) {
            $sqlite3Path = $path
            break
        }
    }
    
    if ($null -eq $sqlite3Path) {
        # Try to use sqlite3 from PATH
        $result = Get-Command sqlite3 -ErrorAction SilentlyContinue
        if ($result) {
            $sqlite3Path = "sqlite3"
        }
    }
    
    if ($null -ne $sqlite3Path) {
        Write-Host "Using SQLite at: $sqlite3Path" -ForegroundColor Green
        
        # Execute SQL command
        $sqlCommand | & $sqlite3Path $dbPath
        
        Write-Host ""
        Write-Host "[SUCCESS] QuickNotes table created!" -ForegroundColor Green
        Write-Host ""
        Write-Host "You can now restart the Web app and Quick Notes will work." -ForegroundColor Cyan
    } else {
        Write-Host "sqlite3.exe not found. Using alternative method..." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Option 1: Install SQLite Tools" -ForegroundColor Cyan
        Write-Host "  Download from: https://www.sqlite.org/download.html" -ForegroundColor Gray
        Write-Host "  Extract sqlite3.exe to C:\sqlite\" -ForegroundColor Gray
        Write-Host ""
        Write-Host "Option 2: Recreate database (DELETES ALL DATA)" -ForegroundColor Red
        Write-Host "  Remove-Item '$dbPath' -Force" -ForegroundColor Gray
        Write-Host "  Then restart the Web app to recreate with QuickNotes table" -ForegroundColor Gray
        Write-Host ""
        Write-Host "Option 3: Use DB Browser for SQLite" -ForegroundColor Cyan
        Write-Host "  1. Download from: https://sqlitebrowser.org/" -ForegroundColor Gray
        Write-Host "  2. Open $dbPath" -ForegroundColor Gray
        Write-Host "  3. Execute SQL tab -> Paste the SQL below -> Execute" -ForegroundColor Gray
        Write-Host ""
        Write-Host "SQL Command:" -ForegroundColor Yellow
        Write-Host $sqlCommand -ForegroundColor White
    }
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Manual fix required. Use DB Browser for SQLite:" -ForegroundColor Yellow
    Write-Host "  1. Download from: https://sqlitebrowser.org/" -ForegroundColor Gray
    Write-Host "  2. Open $dbPath" -ForegroundColor Gray
    Write-Host "  3. Execute SQL tab -> Paste the SQL below -> Execute" -ForegroundColor Gray
    Write-Host ""
    Write-Host $sqlCommand -ForegroundColor White
}

Write-Host ""
Write-Host "After the table is created, restart your Web app." -ForegroundColor Cyan
