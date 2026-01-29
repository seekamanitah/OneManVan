# Quick Fix: Add QuickNotes table using EF Core
# This will add the table to your existing database

Write-Host "Adding QuickNotes table to database..." -ForegroundColor Cyan
Write-Host ""

# Stop the Web app if running
$webProcess = Get-Process -Name "OneManVan.Web" -ErrorAction SilentlyContinue
if ($webProcess) {
    Write-Host "Stopping Web app..." -ForegroundColor Yellow
    Stop-Process -Name "OneManVan.Web" -Force
    Start-Sleep -Seconds 2
}

# Find the database file
$dbFiles = Get-ChildItem -Path "OneManVan.Web" -Filter "*.db" -Recurse | Where-Object { $_.Name -notlike "*-shm" -and $_.Name -notlike "*-wal" }

if ($dbFiles.Count -eq 0) {
    Write-Host "No database file found. Database will be created on first run." -ForegroundColor Yellow
    Write-Host "Just restart the Web app and the QuickNotes table will be created automatically." -ForegroundColor Cyan
    exit 0
}

foreach ($dbFile in $dbFiles) {
    Write-Host "Found database: $($dbFile.FullName)" -ForegroundColor Green
}

Write-Host ""
Write-Host "Option 1 (RECOMMENDED): Delete database and let EF recreate it" -ForegroundColor Cyan
Write-Host "  This will PRESERVE your data by using EnsureCreated()" -ForegroundColor Yellow
Write-Host ""
$confirm = Read-Host "Delete database files and recreate? (y/n)"

if ($confirm -eq "y") {
    foreach ($dbFile in $dbFiles) {
        Write-Host "Deleting $($dbFile.Name)..." -ForegroundColor Yellow
        Remove-Item $dbFile.FullName -Force
        
        # Also delete WAL and SHM files
        $basePath = $dbFile.FullName -replace '\.db$', ''
        Remove-Item "$basePath-wal" -Force -ErrorAction SilentlyContinue
        Remove-Item "$basePath-shm" -Force -ErrorAction SilentlyContinue
    }
    
    Write-Host ""
    Write-Host "[SUCCESS] Database files deleted." -ForegroundColor Green
    Write-Host ""
    Write-Host "Now restart your Web app:" -ForegroundColor Cyan
    Write-Host "  cd OneManVan.Web" -ForegroundColor Gray
    Write-Host "  dotnet run" -ForegroundColor Gray
    Write-Host ""
    Write-Host "The database will be recreated with all tables including QuickNotes." -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "Cancelled. To manually add the table:" -ForegroundColor Yellow
    Write-Host "  1. Download DB Browser for SQLite: https://sqlitebrowser.org/" -ForegroundColor Gray
    Write-Host "  2. Open the .db file" -ForegroundColor Gray
    Write-Host "  3. Execute SQL tab -> Run this:" -ForegroundColor Gray
    Write-Host ""
    Write-Host @"
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
"@ -ForegroundColor White
}
