# Apply QuickNotes Migration
# Adds QuickNotes table to the database

Write-Host "Applying QuickNotes Migration..." -ForegroundColor Cyan
Write-Host ""

# Check if Web app is running
$webProcess = Get-Process -Name "OneManVan.Web" -ErrorAction SilentlyContinue
if ($webProcess) {
    Write-Host "WARNING: OneManVan.Web is currently running." -ForegroundColor Yellow
    Write-Host "The migration will be applied when the app restarts." -ForegroundColor Yellow
    Write-Host ""
}

# Read the migration script
$migrationScript = Get-Content "Migrations\AddQuickNotes.sql" -Raw

# For SQLite (local development)
Write-Host "For SQLite (local):" -ForegroundColor Yellow
Write-Host "  The QuickNotes table will be created automatically by EF Core" -ForegroundColor Gray
Write-Host "  when the app starts (EnsureCreated)." -ForegroundColor Gray
Write-Host ""

# For SQL Server (remote)
Write-Host "For SQL Server (remote Docker):" -ForegroundColor Yellow
Write-Host "  Run this command on your server:" -ForegroundColor Gray
Write-Host ""
Write-Host "  docker exec -i tradeflow-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'TradeFlow2025!' -d TradeFlowFSM -Q """ + $migrationScript.Replace('"', '\"') + """" -ForegroundColor Green
Write-Host ""

# Alternative: Restart the Web app to trigger EnsureCreated
Write-Host "OR simply restart the Web app:" -ForegroundColor Cyan
Write-Host "  cd OneManVan.Web" -ForegroundColor Gray
Write-Host "  dotnet run" -ForegroundColor Gray
Write-Host ""
Write-Host "The QuickNotes table will be created automatically on startup." -ForegroundColor Green
