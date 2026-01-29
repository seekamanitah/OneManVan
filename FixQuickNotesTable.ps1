# Add QuickNotes Table to Existing Database
# For SQLite (local development)

Write-Host "Adding QuickNotes table to database..." -ForegroundColor Cyan
Write-Host ""

# Stop the Web app if running
$webProcess = Get-Process -Name "OneManVan.Web" -ErrorAction SilentlyContinue
if ($webProcess) {
    Write-Host "Stopping OneManVan.Web..." -ForegroundColor Yellow
    Stop-Process -Name "OneManVan.Web" -Force
    Start-Sleep -Seconds 2
}

# Path to SQLite database
$dbPath = "OneManVan.Web\Data\business.db"

if (Test-Path $dbPath) {
    # Backup existing database
    $backupPath = "OneManVan.Web\Data\business.db.backup_before_quicknotes"
    Write-Host "Creating backup: $backupPath" -ForegroundColor Yellow
    Copy-Item $dbPath $backupPath -Force
    
    # Delete the database to trigger recreation
    Write-Host "Deleting existing database..." -ForegroundColor Yellow
    Remove-Item $dbPath -Force
    Remove-Item "$dbPath-shm" -Force -ErrorAction SilentlyContinue
    Remove-Item "$dbPath-wal" -Force -ErrorAction SilentlyContinue
    
    Write-Host "[OK] Database deleted" -ForegroundColor Green
    Write-Host ""
    Write-Host "The database will be recreated with QuickNotes table when you start the app." -ForegroundColor Cyan
} else {
    Write-Host "Database not found at: $dbPath" -ForegroundColor Red
}

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Start the Web app: cd OneManVan.Web && dotnet run" -ForegroundColor Gray
Write-Host "  2. The database will be recreated automatically with all tables" -ForegroundColor Gray
Write-Host "  3. QuickNotes will work!" -ForegroundColor Green
Write-Host ""
Write-Host "NOTE: Your old data is backed up at:" -ForegroundColor Cyan
Write-Host "  $backupPath" -ForegroundColor Gray
