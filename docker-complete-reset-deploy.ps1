# Complete Docker Cleanup and Fresh Deployment for OneManVan
# Windows PowerShell Version

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "OneManVan - Complete Docker Reset & Redeploy" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$PROJECT_NAME = "onemanvan"
$COMPOSE_FILE = "docker-compose.yml"
$ENV_FILE = ".env"

Write-Host "WARNING: This will COMPLETELY DELETE all OneManVan Docker resources!" -ForegroundColor Yellow
Write-Host "This includes:" -ForegroundColor Yellow
Write-Host "  - All containers (running and stopped)" -ForegroundColor Yellow
Write-Host "  - All images" -ForegroundColor Yellow
Write-Host "  - All volumes (DATABASE WILL BE DELETED!)" -ForegroundColor Yellow
Write-Host "  - All networks" -ForegroundColor Yellow
Write-Host ""

$confirm = Read-Host "Are you sure you want to continue? (type 'yes' to confirm)"

if ($confirm -ne "yes") {
    Write-Host "Cancelled by user" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 1: Stopping all OneManVan containers..." -ForegroundColor Blue
# Stop all containers with onemanvan or tradeflow in the name
docker ps -a --filter "name=onemanvan" --format "{{.Names}}" | ForEach-Object { docker stop $_ }
docker ps -a --filter "name=tradeflow" --format "{{.Names}}" | ForEach-Object { docker stop $_ }
Write-Host "Containers stopped" -ForegroundColor Green

Write-Host ""
Write-Host "Step 2: Removing all OneManVan containers..." -ForegroundColor Blue
docker ps -a --filter "name=onemanvan" --format "{{.Names}}" | ForEach-Object { docker rm -f $_ }
docker ps -a --filter "name=tradeflow" --format "{{.Names}}" | ForEach-Object { docker rm -f $_ }
Write-Host "Containers removed" -ForegroundColor Green

Write-Host ""
Write-Host "Step 3: Removing all OneManVan volumes (DATABASE DELETION)..." -ForegroundColor Blue
docker volume ls --filter "name=onemanvan" --format "{{.Name}}" | ForEach-Object { docker volume rm $_ -ErrorAction SilentlyContinue }
docker volume ls --filter "name=tradeflow" --format "{{.Name}}" | ForEach-Object { docker volume rm $_ -ErrorAction SilentlyContinue }
Write-Host "Volumes removed" -ForegroundColor Green

Write-Host ""
Write-Host "Step 4: Removing all OneManVan images..." -ForegroundColor Blue
docker images --filter "reference=onemanvan*" --format "{{.Repository}}:{{.Tag}}" | ForEach-Object { docker rmi -f $_ -ErrorAction SilentlyContinue }
docker images --filter "reference=tradeflow*" --format "{{.Repository}}:{{.Tag}}" | ForEach-Object { docker rmi -f $_ -ErrorAction SilentlyContinue }
Write-Host "Images removed" -ForegroundColor Green

Write-Host ""
Write-Host "Step 5: Removing OneManVan networks..." -ForegroundColor Blue
docker network ls --filter "name=onemanvan" --format "{{.Name}}" | ForEach-Object { docker network rm $_ -ErrorAction SilentlyContinue }
docker network ls --filter "name=tradeflow" --format "{{.Name}}" | ForEach-Object { docker network rm $_ -ErrorAction SilentlyContinue }
Write-Host "Networks removed" -ForegroundColor Green

Write-Host ""
Write-Host "Step 6: Pruning Docker system..." -ForegroundColor Blue
docker system prune -f
Write-Host "System pruned" -ForegroundColor Green

Write-Host ""
Write-Host "==================================================" -ForegroundColor Green
Write-Host "Complete cleanup finished!" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Starting fresh deployment..." -ForegroundColor Yellow
Write-Host ""

# Check if .env exists
if (!(Test-Path $ENV_FILE)) {
    Write-Host ".env file not found. Creating from example..." -ForegroundColor Yellow
    if (Test-Path ".env.example") {
        Copy-Item ".env.example" ".env"
        Write-Host "Created .env from .env.example" -ForegroundColor Green
        Write-Host "Please edit .env with your configuration before continuing" -ForegroundColor Yellow
        Read-Host "Press Enter after editing .env to continue"
    } else {
        Write-Host ".env.example not found. Please create .env manually" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "Step 7: Building fresh Docker images..." -ForegroundColor Blue
docker compose -f $COMPOSE_FILE build --no-cache
Write-Host "Images built" -ForegroundColor Green

Write-Host ""
Write-Host "Step 8: Starting containers..." -ForegroundColor Blue
docker compose -f $COMPOSE_FILE up -d
Write-Host "Containers started" -ForegroundColor Green

Write-Host ""
Write-Host "Step 9: Waiting for services to be ready..." -ForegroundColor Blue
Start-Sleep -Seconds 10

# Wait for SQL Server
Write-Host "Waiting for SQL Server..." -ForegroundColor Yellow
$sqlReady = $false
$attempts = 0
while (!$sqlReady -and $attempts -lt 30) {
    try {
        docker exec onemanvan-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'OneManVan2025!' -Q "SELECT 1" 2>&1 | Out-Null
        $sqlReady = $true
    } catch {
        Write-Host "." -NoNewline
        Start-Sleep -Seconds 2
        $attempts++
    }
}
Write-Host ""
if ($sqlReady) {
    Write-Host "SQL Server is ready" -ForegroundColor Green
} else {
    Write-Host "SQL Server did not start in time" -ForegroundColor Red
}

# Wait for Web UI
Write-Host "Waiting for Web UI..." -ForegroundColor Yellow
Start-Sleep -Seconds 5
$webReady = $false
$attempts = 0
while (!$webReady -and $attempts -lt 30) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:7159/health" -UseBasicParsing -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200) {
            $webReady = $true
        }
    } catch {
        Write-Host "." -NoNewline
        Start-Sleep -Seconds 2
        $attempts++
    }
}
Write-Host ""
if ($webReady) {
    Write-Host "Web UI is ready" -ForegroundColor Green
} else {
    Write-Host "Web UI did not start in time" -ForegroundColor Red
}

Write-Host ""
Write-Host "==================================================" -ForegroundColor Green
Write-Host "DEPLOYMENT COMPLETE!" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Access your application:" -ForegroundColor Cyan
Write-Host "  Web UI:      http://localhost:7159" -ForegroundColor White
Write-Host "  SQL Server:  localhost:1433" -ForegroundColor White
Write-Host "  Username:    sa" -ForegroundColor White
Write-Host "  Password:    OneManVan2025!" -ForegroundColor White
Write-Host ""
Write-Host "View logs:" -ForegroundColor Cyan
Write-Host "  docker compose logs -f" -ForegroundColor White
Write-Host ""
Write-Host "Stop services:" -ForegroundColor Cyan
Write-Host "  docker compose down" -ForegroundColor White
Write-Host ""
Write-Host "Happy testing!" -ForegroundColor Green
