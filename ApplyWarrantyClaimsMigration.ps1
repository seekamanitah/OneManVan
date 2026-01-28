# Apply Warranty Claims Migration
# Run this from PowerShell in the project root directory

Write-Host "Applying WarrantyClaims Migration..." -ForegroundColor Cyan

# Check if Docker is running
$dockerRunning = docker ps 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Docker is not running!" -ForegroundColor Red
    Write-Host "Please start Docker Desktop first." -ForegroundColor Yellow
    exit 1
}

# Check if SQL Server container exists
$containerName = "onemanvan-sqlserver"
$containerExists = docker ps -a --filter "name=$containerName" --format "{{.Names}}"

if (-not $containerExists) {
    Write-Host "ERROR: SQL Server container '$containerName' not found!" -ForegroundColor Red
    Write-Host "Run 'docker-compose up -d' first to start the database." -ForegroundColor Yellow
    exit 1
}

# Check if container is running
$containerRunning = docker ps --filter "name=$containerName" --format "{{.Names}}"
if (-not $containerRunning) {
    Write-Host "Starting SQL Server container..." -ForegroundColor Yellow
    docker start $containerName
    Start-Sleep -Seconds 10
}

Write-Host "`nApplying migration to Docker SQL Server..." -ForegroundColor Green

# Apply migration using docker exec
docker exec -i $containerName /opt/mssql-tools/bin/sqlcmd `
    -S localhost -U sa -P "YourStrong@Passw0rd" `
    -d OneManVanDb `
    -i /docker-entrypoint-initdb.d/AddWarrantyClaims.sql

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n? Migration applied successfully!" -ForegroundColor Green
    
    # Verify table was created
    Write-Host "`nVerifying WarrantyClaims table..." -ForegroundColor Cyan
    $verifyQuery = "SELECT COUNT(*) as TableCount FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'WarrantyClaims'"
    
    docker exec -i $containerName /opt/mssql-tools/bin/sqlcmd `
        -S localhost -U sa -P "YourStrong@Passw0rd" `
        -d OneManVanDb `
        -Q $verifyQuery
    
    Write-Host "`n? Migration complete! You can now test the warranty claims system." -ForegroundColor Green
} else {
    Write-Host "`n? Migration failed!" -ForegroundColor Red
    Write-Host "Check the error messages above for details." -ForegroundColor Yellow
}
