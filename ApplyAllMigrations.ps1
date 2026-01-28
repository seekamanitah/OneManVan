# ApplyAllMigrations.ps1
# Applies all pending database migrations to fix missing column errors

param(
    [string]$DatabasePath = ""
)

Write-Host "================================" -ForegroundColor Cyan
Write-Host "  Apply All Migrations Script  " -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Function to find database
function Find-Database {
    Write-Host "?? Locating database..." -ForegroundColor Yellow
    
    # Check common locations
    $locations = @(
        "$env:APPDATA\OneManVan\OneManVan.db",
        ".\OneManVan.db",
        ".\onemanvan.db",
        "..\OneManVan.db"
    )
    
    foreach ($loc in $locations) {
        if (Test-Path $loc) {
            Write-Host "? Found database at: $loc" -ForegroundColor Green
            return $loc
        }
    }
    
    # Search recursively
    Write-Host "Searching for database recursively..." -ForegroundColor Yellow
    $found = Get-ChildItem -Recurse -Filter "*onemanvan.db" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($found) {
        Write-Host "? Found database at: $($found.FullName)" -ForegroundColor Green
        return $found.FullName
    }
    
    return $null
}

# Find database
if ([string]::IsNullOrEmpty($DatabasePath)) {
    $DatabasePath = Find-Database
}

if ([string]::IsNullOrEmpty($DatabasePath) -or -not (Test-Path $DatabasePath)) {
    Write-Host "? ERROR: Database not found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please specify the database path:" -ForegroundColor Yellow
    Write-Host "  .\ApplyAllMigrations.ps1 -DatabasePath `"C:\path\to\OneManVan.db`"" -ForegroundColor Cyan
    exit 1
}

Write-Host ""
Write-Host "?? Database: $DatabasePath" -ForegroundColor Cyan
Write-Host ""

# Define migrations in order
$migrations = @(
    @{
        Name = "AddCustomerHomeAddress"
        File = "Migrations\AddCustomerHomeAddress.sql"
        Description = "Adds HomeAddress column to Customers table"
    },
    @{
        Name = "AddTaxIncludedAndEnhancements"
        File = "Migrations\AddTaxIncludedAndEnhancements.sql"
        Description = "Adds TaxIncluded to Estimates, Jobs, and Invoices"
    },
    @{
        Name = "AddInvoiceSoftDelete"
        File = "Migrations\AddInvoiceSoftDelete.sql"
        Description = "Adds soft delete columns to Invoices"
    }
)

# Function to execute SQL
function Execute-Migration {
    param(
        [string]$SqlFile,
        [string]$DbPath
    )
    
    if (-not (Test-Path $SqlFile)) {
        throw "Migration file not found: $SqlFile"
    }
    
    $sql = Get-Content $SqlFile -Raw
    
    # Try using sqlite3 command
    try {
        $sql | sqlite3 $DbPath 2>&1 | Out-Null
        return $true
    } catch {
        Write-Host "sqlite3 not found, trying alternative..." -ForegroundColor Yellow
    }
    
    # Alternative: Use .NET SQLite
    try {
        # Load SQLite assembly (might need Microsoft.Data.Sqlite NuGet)
        Add-Type -Path ".\bin\Debug\net10.0-windows\Microsoft.Data.Sqlite.dll" -ErrorAction Stop
        
        $connectionString = "Data Source=$DbPath"
        $connection = New-Object Microsoft.Data.Sqlite.SqliteConnection($connectionString)
        $connection.Open()
        
        $command = $connection.CreateCommand()
        $command.CommandText = $sql
        $command.ExecuteNonQuery() | Out-Null
        
        $connection.Close()
        return $true
    } catch {
        throw "Failed to execute migration: $_"
    }
}

# Apply each migration
$successCount = 0
$failCount = 0

foreach ($migration in $migrations) {
    Write-Host "????????????????????????????????????????" -ForegroundColor DarkGray
    Write-Host "?? Migration: $($migration.Name)" -ForegroundColor Cyan
    Write-Host "   $($migration.Description)" -ForegroundColor Gray
    Write-Host ""
    
    try {
        Execute-Migration -SqlFile $migration.File -DbPath $DatabasePath
        Write-Host "? SUCCESS" -ForegroundColor Green
        $successCount++
    } catch {
        Write-Host "? FAILED: $_" -ForegroundColor Red
        $failCount++
    }
    
    Write-Host ""
}

# Summary
Write-Host "????????????????????????????????????????" -ForegroundColor DarkGray
Write-Host ""
Write-Host "?? SUMMARY" -ForegroundColor Cyan
Write-Host "   ? Successful: $successCount" -ForegroundColor Green
Write-Host "   ? Failed: $failCount" -ForegroundColor $(if ($failCount -gt 0) { "Red" } else { "Gray" })
Write-Host ""

if ($failCount -eq 0) {
    Write-Host "?? All migrations applied successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "  1. Restart your Desktop application" -ForegroundColor White
    Write-Host "  2. Go to Settings ? Database Management" -ForegroundColor White
    Write-Host "  3. Click 'Seed Demo Data'" -ForegroundColor White
    Write-Host "  4. Verify no SQL errors occur" -ForegroundColor White
} else {
    Write-Host "??  Some migrations failed. Please check the errors above." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "  • Make sure no apps are using the database" -ForegroundColor White
    Write-Host "  • Check you have write permissions" -ForegroundColor White
    Write-Host "  • Verify migration files exist in Migrations folder" -ForegroundColor White
}

Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
