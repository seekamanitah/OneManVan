# Switch Database Mode
# Quick script to switch between SQL Server (Docker) and SQLite (local) modes

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('SQLite', 'SQLServer')]
    [string]$Mode
)

$appsettingsPath = "OneManVan.Web\appsettings.json"

if (!(Test-Path $appsettingsPath)) {
    Write-Error "appsettings.json not found at $appsettingsPath"
    exit 1
}

$appsettings = Get-Content $appsettingsPath -Raw | ConvertFrom-Json

if ($Mode -eq 'SQLite') {
    Write-Host "Switching to SQLite (Local Development) mode..." -ForegroundColor Cyan
    
    # Update connection strings to SQLite
    $appsettings.ConnectionStrings.DefaultConnection = "Data Source=AppData/OneManVan.db"
    $appsettings.ConnectionStrings.IdentityConnection = "Data Source=AppData/Identity.db"
    
    Write-Host "  - DefaultConnection: SQLite (AppData/OneManVan.db)" -ForegroundColor Green
    Write-Host "  - IdentityConnection: SQLite (AppData/Identity.db)" -ForegroundColor Green
}
else {
    Write-Host "Switching to SQL Server (Docker) mode..." -ForegroundColor Cyan
    
    # Update connection strings to SQL Server
    $appsettings.ConnectionStrings.DefaultConnection = "Server=sqlserver;Database=TradeFlowFSM;User Id=sa;Password=`${SA_PASSWORD};TrustServerCertificate=True;"
    $appsettings.ConnectionStrings.IdentityConnection = "Server=sqlserver;Database=TradeFlowIdentity;User Id=sa;Password=`${SA_PASSWORD};TrustServerCertificate=True;"
    
    Write-Host "  - DefaultConnection: SQL Server (requires Docker)" -ForegroundColor Yellow
    Write-Host "  - IdentityConnection: SQL Server (requires Docker)" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  Remember to set SA_PASSWORD environment variable or start with Docker!" -ForegroundColor Yellow
}

# Save back to file
$appsettings | ConvertTo-Json -Depth 10 | Set-Content $appsettingsPath

Write-Host ""
Write-Host "Database mode switched to: $Mode" -ForegroundColor Green
Write-Host ""

if ($Mode -eq 'SQLServer') {
    Write-Host "To start with Docker:" -ForegroundColor Cyan
    Write-Host "  docker-compose up -d" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Or set environment variable:" -ForegroundColor Cyan
    Write-Host "  `$env:SA_PASSWORD='YourStrongPassword!'" -ForegroundColor Gray
}
else {
    Write-Host "SQLite databases will be created in AppData/ folder automatically" -ForegroundColor Cyan
}
