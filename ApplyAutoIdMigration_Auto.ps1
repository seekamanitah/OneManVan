# Auto-detect and Apply Database Migration
# Automatically detects SQLite or SQL Server and applies the appropriate migration

param(
    [switch]$Force,
    [switch]$Backfill
)

Write-Host "=== Auto-Detect Database Migration ===" -ForegroundColor Cyan
Write-Host ""

# Check for SQL Server (Docker)
Write-Host "Checking for SQL Server (Docker)..." -ForegroundColor Gray

$envFile = Join-Path $PSScriptRoot ".env"
$useSqlServer = $false

if (Test-Path $envFile) {
    $saPassword = (Get-Content $envFile | Select-String "SA_PASSWORD=").ToString().Split('=')[1]
    if ($saPassword) {
        $dockerRunning = docker ps --format "{{.Names}}" 2>&1 | Select-String "sql"
        if ($dockerRunning) {
            Write-Host "  ? SQL Server container found" -ForegroundColor Green
            $useSqlServer = $true
        }
    }
}

if (-not $useSqlServer) {
    Write-Host "  ? SQL Server not detected" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Checking for SQLite..." -ForegroundColor Gray
    
    $sqliteDb = Join-Path $PSScriptRoot "OneManVan.Web\AppData\OneManVan.db"
    
    if (Test-Path $sqliteDb) {
        Write-Host "  ? SQLite database found at: $sqliteDb" -ForegroundColor Green
        Write-Host ""
        Write-Host "Applying SQLite migration..." -ForegroundColor Cyan
        
        & "$PSScriptRoot\ApplyAutoIdMigration.ps1"
        exit $LASTEXITCODE
    } else {
        Write-Host "  ? SQLite database not found" -ForegroundColor Red
        Write-Host ""
        Write-Host "No database detected!" -ForegroundColor Red
        Write-Host "Please ensure your database is initialized." -ForegroundColor Yellow
        exit 1
    }
} else {
    Write-Host ""
    Write-Host "Applying SQL Server migration..." -ForegroundColor Cyan
    
    & "$PSScriptRoot\ApplyAutoIdMigration_SqlServer.ps1"
    exit $LASTEXITCODE
}
