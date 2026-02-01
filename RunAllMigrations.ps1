# RunAllMigrations.ps1
# Consolidated migration runner for OneManVan SQLite database
# Runs all pending migrations in the correct order

param(
    [string]$DatabasePath = ".\OneManVan.Web\Data\onemanvan.db"
)

$ErrorActionPreference = "Stop"

Write-Host "=================================" -ForegroundColor Cyan
Write-Host " OneManVan Migration Runner" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

# Find SQLite executable or use dotnet-ef
$sqlitePath = $null
if (Get-Command "sqlite3" -ErrorAction SilentlyContinue) {
    $sqlitePath = "sqlite3"
} elseif (Test-Path "C:\sqlite\sqlite3.exe") {
    $sqlitePath = "C:\sqlite\sqlite3.exe"
}

if (-not $sqlitePath) {
    Write-Host "SQLite3 not found. Will use EF Core migrations instead." -ForegroundColor Yellow
    Write-Host ""
}

# List of migrations in order
$migrations = @(
    "Migrations\AddCustomerHomeAddress.sql",
    "Migrations\AddCompanyAndEnhancedFields_Manual.sql",
    "Migrations\AddManufacturerRegistrations.sql",
    "Migrations\AddInvoiceLineItems.sql",
    "Migrations\AddProductSerialAndRegistrationTracking.sql",
    "Migrations\AddTaxIncludedAndEnhancements.sql",
    "Migrations\AddInvoiceSoftDelete.sql",
    "Migrations\AddWarrantyClaims.sql",
    "Migrations\AddQuickNotes.sql",
    "Migrations\AddCompanySettings.sql",
    "Migrations\AddMaterialLists.sql",
    "Migrations\AddServiceAgreementTiers.sql",
    "Migrations\AddDocumentLibrary.sql",
    "Migrations\AddEmployees.sql"
)

Write-Host "Checking for migration files..." -ForegroundColor White
Write-Host ""

$pendingMigrations = @()
$completedCount = 0

foreach ($migration in $migrations) {
    if (Test-Path $migration) {
        $pendingMigrations += $migration
        Write-Host "[FOUND] $migration" -ForegroundColor Green
    } else {
        Write-Host "[SKIP]  $migration (not found)" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "Found $($pendingMigrations.Count) migration files" -ForegroundColor Cyan
Write-Host ""

if ($pendingMigrations.Count -eq 0) {
    Write-Host "No migrations to run." -ForegroundColor Yellow
    exit 0
}

# Check if database exists
if (-not (Test-Path $DatabasePath)) {
    Write-Host "Database not found at: $DatabasePath" -ForegroundColor Yellow
    Write-Host "Creating new database..." -ForegroundColor White
    New-Item -ItemType Directory -Force -Path (Split-Path $DatabasePath) | Out-Null
}

# Run migrations
Write-Host "Running migrations..." -ForegroundColor Cyan
Write-Host ""

foreach ($migration in $pendingMigrations) {
    $migrationName = Split-Path $migration -Leaf
    Write-Host "Applying: $migrationName" -ForegroundColor White
    
    try {
        if ($sqlitePath) {
            # Run with SQLite CLI
            $content = Get-Content $migration -Raw
            $result = & $sqlitePath $DatabasePath $content 2>&1
            
            if ($LASTEXITCODE -ne 0) {
                Write-Host "  Warning: $result" -ForegroundColor Yellow
            } else {
                Write-Host "  Applied successfully" -ForegroundColor Green
                $completedCount++
            }
        } else {
            Write-Host "  SQLite CLI not available - migration skipped" -ForegroundColor Yellow
            Write-Host "  Please run manually: sqlite3 $DatabasePath < $migration" -ForegroundColor Gray
        }
    } catch {
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=================================" -ForegroundColor Cyan
Write-Host " Migration Summary" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host "  Applied: $completedCount / $($pendingMigrations.Count)" -ForegroundColor White
Write-Host ""

# Alternative: Use EF Core to ensure model sync
Write-Host "Ensuring EF Core model is synchronized..." -ForegroundColor White
try {
    Push-Location "OneManVan.Web"
    $result = dotnet ef database update 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "EF Core database updated successfully" -ForegroundColor Green
    } else {
        Write-Host "EF Core update: $result" -ForegroundColor Yellow
    }
    Pop-Location
} catch {
    Write-Host "EF Core update skipped: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Migration process complete!" -ForegroundColor Green
Write-Host ""
