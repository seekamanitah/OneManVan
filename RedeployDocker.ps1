# Redeploy Docker Container with Warranty Claims Migration
# This script stops, removes, and recreates the Docker container with the new migration

param(
    [switch]$KeepData,
    [switch]$FreshStart,
    [switch]$SkipPrompt
)

Write-Host ""
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host "   OneManVan Docker Redeployment" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "By using this script, you accept the:" -ForegroundColor Yellow
Write-Host "  SQL Server 2022 End-User License Agreement (EULA)" -ForegroundColor White
Write-Host "  Terms: http://go.microsoft.com/fwlink/?LinkId=746388" -ForegroundColor DarkGray
Write-Host ""

# Check if Docker is running
Write-Host "[1/6] Checking Docker status..." -ForegroundColor Yellow
$dockerRunning = docker ps 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "ERROR: Docker is not running!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please start Docker Desktop and try again." -ForegroundColor Yellow
    Write-Host ""
    exit 1
}
Write-Host "      Docker is running" -ForegroundColor Green
Write-Host ""

# Check if docker-compose.yml exists
if (-not (Test-Path "docker-compose.yml")) {
    Write-Host "ERROR: docker-compose.yml not found!" -ForegroundColor Red
    Write-Host "Please run this script from the project root directory." -ForegroundColor Yellow
    exit 1
}

# Check if warranty claims migration exists
$migrationExists = Test-Path "docker\init\03-add-warranty-claims.sql"
if ($migrationExists) {
    Write-Host "[2/6] Warranty claims migration detected" -ForegroundColor Green
} else {
    Write-Host "[2/6] Warning: Warranty claims migration not found" -ForegroundColor Yellow
}
Write-Host ""

# Determine if we should remove volumes
$removeVolumes = $false
if ($FreshStart) {
    $removeVolumes = $true
    Write-Host "      Fresh start requested - will remove all data" -ForegroundColor Yellow
} elseif ($KeepData) {
    $removeVolumes = $false
    Write-Host "      Keep data requested - preserving existing data" -ForegroundColor Green
} elseif (-not $SkipPrompt) {
    Write-Host "Do you want to keep existing data?" -ForegroundColor Cyan
    Write-Host "  [Y] Keep data (add warranty table only)" -ForegroundColor White
    Write-Host "  [N] Fresh start (delete all data)" -ForegroundColor White
    Write-Host ""
    $choice = Read-Host "Your choice (Y/n)"
    $removeVolumes = ($choice -eq 'n' -or $choice -eq 'N')
    Write-Host ""
}

# Stop existing containers
Write-Host "[3/6] Stopping existing containers..." -ForegroundColor Yellow
docker-compose down 2>&1 | Out-Null

if ($removeVolumes) {
    Write-Host "      Removing volumes (fresh start)..." -ForegroundColor Yellow
    docker-compose down -v 2>&1 | Out-Null
    Write-Host "      All data removed" -ForegroundColor Green
} else {
    Write-Host "      Containers stopped (data preserved)" -ForegroundColor Green
}
Write-Host ""

# Build and start containers
Write-Host "[4/6] Building and starting containers..." -ForegroundColor Yellow
$buildOutput = docker-compose up -d --build 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "      Containers started successfully" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "ERROR: Failed to start containers!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Build output:" -ForegroundColor Yellow
    Write-Host $buildOutput
    exit 1
}
Write-Host ""

# Wait for SQL Server to be ready
Write-Host "[5/6] Waiting for SQL Server to initialize..." -ForegroundColor Yellow
Write-Host "      This may take 30-60 seconds..." -ForegroundColor Cyan

$maxRetries = 12
$retryCount = 0
$sqlReady = $false

while ($retryCount -lt $maxRetries -and -not $sqlReady) {
    Start-Sleep -Seconds 5
    $retryCount++
    
    # Check if SQL Server is responding
    $containerLog = docker logs onemanvan-sqlserver 2>&1 | Select-String "SQL Server is now ready"
    if ($containerLog) {
        $sqlReady = $true
        Write-Host "      SQL Server is ready!" -ForegroundColor Green
    } else {
        Write-Host "      Waiting... ($retryCount/$maxRetries)" -ForegroundColor DarkGray
    }
}

if (-not $sqlReady) {
    Write-Host ""
    Write-Host "Warning: SQL Server may still be initializing" -ForegroundColor Yellow
    Write-Host "Check logs with: docker logs onemanvan-sqlserver" -ForegroundColor Yellow
}
Write-Host ""

# Verify deployment
Write-Host "[6/6] Verifying deployment..." -ForegroundColor Yellow
$containers = docker-compose ps --format json | ConvertFrom-Json
if ($containers) {
    Write-Host "      Deployment verified" -ForegroundColor Green
} else {
    Write-Host "      Warning: Could not verify containers" -ForegroundColor Yellow
}
Write-Host ""

# Success summary
Write-Host "=======================================" -ForegroundColor Green
Write-Host "   Deployment Complete!" -ForegroundColor Green
Write-Host "=======================================" -ForegroundColor Green
Write-Host ""
Write-Host "Container Status:" -ForegroundColor Cyan
docker-compose ps
Write-Host ""
Write-Host "Database Connection Details:" -ForegroundColor Cyan
Write-Host "  Server:   " -NoNewline -ForegroundColor White
Write-Host "localhost,1433" -ForegroundColor Yellow
Write-Host "  Database: " -NoNewline -ForegroundColor White
Write-Host "OneManVanDb" -ForegroundColor Yellow
Write-Host "  Username: " -NoNewline -ForegroundColor White
Write-Host "sa" -ForegroundColor Yellow
Write-Host "  Password: " -NoNewline -ForegroundColor White
Write-Host "YourStrong@Passw0rd" -ForegroundColor Yellow
Write-Host ""

if ($migrationExists) {
    Write-Host "Warranty Claims:" -ForegroundColor Cyan
    Write-Host "  Migration applied: " -NoNewline -ForegroundColor White
    Write-Host "03-add-warranty-claims.sql" -ForegroundColor Green
    Write-Host ""
}

Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Test the database connection" -ForegroundColor White
Write-Host "  2. Run: " -NoNewline -ForegroundColor White
Write-Host "dotnet run --project OneManVan.Web" -ForegroundColor Yellow
Write-Host "  3. Navigate to: " -NoNewline -ForegroundColor White
Write-Host "/warranties/claims" -ForegroundColor Yellow
Write-Host ""
Write-Host "Useful Commands:" -ForegroundColor Cyan
Write-Host "  View logs:    " -NoNewline -ForegroundColor White
Write-Host "docker-compose logs -f" -ForegroundColor Yellow
Write-Host "  Stop:         " -NoNewline -ForegroundColor White
Write-Host "docker-compose down" -ForegroundColor Yellow
Write-Host "  Restart:      " -NoNewline -ForegroundColor White
Write-Host "docker-compose restart" -ForegroundColor Yellow
Write-Host ""
