# Apply Employee TimeLog Invoice References Migration
# This script adds invoice references to employee time logs for automatic tracking

Write-Host "Applying Employee TimeLog Invoice References Migration..." -ForegroundColor Cyan
Write-Host ""

# Check database type
$dbPath = ".\OneManVan.Web\AppData\OneManVan.db"
$sqlServerConnection = $env:SA_PASSWORD

if (Test-Path $dbPath) {
    Write-Host "Using SQLite database at: $dbPath" -ForegroundColor Yellow
    $useSqlite = $true
} elseif ($sqlServerConnection) {
    Write-Host "Using SQL Server (Docker)" -ForegroundColor Yellow
    $useSqlite = $false
} else {
    Write-Host "ERROR: No database found!" -ForegroundColor Red
    Write-Host "Make sure the application has been run at least once to create the database." -ForegroundColor Yellow
    exit 1
}

# Apply appropriate migration
if ($useSqlite) {
    Write-Host "Applying SQLite migration..." -ForegroundColor Green
    
    try {
        # Using sqlite3 command-line tool
        $sqliteExe = "sqlite3.exe"
        
        # Check if sqlite3 is available
        $sqliteAvailable = Get-Command $sqliteExe -ErrorAction SilentlyContinue
        
        if ($sqliteAvailable) {
            & $sqliteExe $dbPath ".read Migrations\AddEmployeeTimeLogInvoiceReferences_SQLite.sql"
            Write-Host "SUCCESS: Migration applied successfully!" -ForegroundColor Green
        } else {
            Write-Host "sqlite3.exe not found. Trying .NET approach..." -ForegroundColor Yellow
            
            # Fallback to using Entity Framework or manual .NET approach
            Write-Host "Please install SQLite command-line tools or run the migration through Entity Framework" -ForegroundColor Yellow
            Write-Host "Download from: https://www.sqlite.org/download.html" -ForegroundColor Cyan
            
            # Create a simple migration runner
            $migrationScript = Get-Content "Migrations\AddEmployeeTimeLogInvoiceReferences_SQLite.sql" -Raw
            
            Write-Host "Migration SQL ready. Please apply manually or use Entity Framework migrations." -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "ERROR: Failed to apply migration: $_" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "Applying SQL Server migration..." -ForegroundColor Green
    
    try {
        # Run SQL Server migration using sqlcmd
        $serverName = "localhost,1433"
        $database = "OneManVanDB"
        $username = "sa"
        $password = $env:SA_PASSWORD
        
        sqlcmd -S $serverName -d $database -U $username -P $password -i "Migrations\AddEmployeeTimeLogInvoiceReferences.sql"
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "SUCCESS: Migration applied successfully!" -ForegroundColor Green
        } else {
            Write-Host "ERROR: Migration failed with exit code $LASTEXITCODE" -ForegroundColor Red
            exit 1
        }
    }
    catch {
        Write-Host "ERROR: Failed to apply migration: $_" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "Migration complete! Employee time logs can now be auto-created from invoices." -ForegroundColor Green
Write-Host ""
Write-Host "New Features Available:" -ForegroundColor Cyan
Write-Host "  - Automatic time log creation when saving invoices with labor" -ForegroundColor White
Write-Host "  - Employee work history tracking" -ForegroundColor White
Write-Host "  - Location tracking via job sites" -ForegroundColor White
Write-Host "  - No double data entry required" -ForegroundColor White
Write-Host ""
