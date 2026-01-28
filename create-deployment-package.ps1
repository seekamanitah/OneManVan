# OneManVan Web - Create Docker Deployment Package
# Run this from the solution root directory

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "OneManVan Docker Deployment Package Creator" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Stop"

# Configuration
$outputZip = "deployment.zip"
$tempDir = "deployment-temp"

# Clean up previous deployment
if (Test-Path $outputZip) {
    Write-Host "Removing old deployment package..." -ForegroundColor Yellow
    Remove-Item $outputZip -Force
}

if (Test-Path $tempDir) {
    Write-Host "Cleaning up temp directory..." -ForegroundColor Yellow
    Remove-Item $tempDir -Recurse -Force
}

# Create temp directory
Write-Host "Creating deployment package..." -ForegroundColor Green
New-Item -ItemType Directory -Path $tempDir | Out-Null

# Copy necessary files
Write-Host "Copying Web project files..." -ForegroundColor Green
Copy-Item -Path "OneManVan.Web" -Destination "$tempDir\" -Recurse -Exclude @(
    "bin", "obj", "*.user", "AppData", ".vs", "Properties"
)

Write-Host "Copying Shared project files..." -ForegroundColor Green
Copy-Item -Path "OneManVan.Shared" -Destination "$tempDir\" -Recurse -Exclude @(
    "bin", "obj", "*.user"
)

Write-Host "Copying Docker configuration files..." -ForegroundColor Green
Copy-Item -Path "docker-compose-full.yml" -Destination "$tempDir\"
Copy-Item -Path ".dockerignore" -Destination "$tempDir\"
Copy-Item -Path "OneManVan.Web\Dockerfile" -Destination "$tempDir\OneManVan.Web\" -Force

Write-Host "Copying SQL initialization scripts..." -ForegroundColor Green
Copy-Item -Path "docker" -Destination "$tempDir\" -Recurse

Write-Host "Copying deployment scripts..." -ForegroundColor Green
Copy-Item -Path "deploy-to-docker.sh" -Destination "$tempDir\"
Copy-Item -Path "deploy-production.sh" -Destination "$tempDir\"
Copy-Item -Path "DEPLOYMENT_INSTRUCTIONS.md" -Destination "$tempDir\README.md"
Copy-Item -Path "PRODUCTION_DEPLOYMENT_CHECKLIST.md" -Destination "$tempDir\" -ErrorAction SilentlyContinue

Write-Host "Copying .env file..." -ForegroundColor Green
Copy-Item -Path ".env" -Destination "$tempDir\"

# Create README in package
$readmeContent = @"
# OneManVan Web Docker Deployment Package

## Quick Start

1. Upload this entire package to your Linux server at 192.168.100.107
2. Extract: unzip deployment.zip -d /opt/onemanvan
3. Deploy: cd /opt/onemanvan && chmod +x deploy-to-docker.sh && ./deploy-to-docker.sh
4. Access: http://192.168.100.107:5000

## Full Instructions

See README.md (DEPLOYMENT_INSTRUCTIONS.md) in this package for complete details.

## Contents

- OneManVan.Web/ - Web application source
- OneManVan.Shared/ - Shared library source
- docker-compose-full.yml - Docker Compose configuration
- deploy-to-docker.sh - Automated deployment script
- docker/init/ - SQL Server initialization scripts

## Requirements

- Linux server with Docker
- Minimum 2GB RAM, 10GB disk space
- Open ports: 5000 (Web UI), 1433 (SQL Server)
"@

Set-Content -Path "$tempDir\QUICKSTART.txt" -Value $readmeContent

# Create .dockerignore if it doesn't exist in temp
if (-not (Test-Path "$tempDir\.dockerignore")) {
    $dockerignoreContent = @"
**/.classpath
**/.dockerignore
**/.env
**/.git
**/.gitignore
**/.project
**/.settings
**/.toolstarget
**/.vs
**/.vscode
**/*.*proj.user
**/*.dbmdl
**/*.jfm
**/azds.yaml
**/bin
**/charts
**/docker-compose*
**/Dockerfile*
**/node_modules
**/npm-debug.log
**/obj
**/secrets.dev.yaml
**/values.dev.yaml
LICENSE
README.md
**/AppData
**/Data
**/.idea
**/TestResults
**/*.DotSettings.user
"@
    Set-Content -Path "$tempDir\.dockerignore" -Value $dockerignoreContent
}

# Create the zip file
Write-Host ""
Write-Host "Creating deployment.zip..." -ForegroundColor Green
Compress-Archive -Path "$tempDir\*" -DestinationPath $outputZip -CompressionLevel Optimal

# Clean up temp directory
Write-Host "Cleaning up..." -ForegroundColor Green
Remove-Item $tempDir -Recurse -Force

# Get file size
$fileSize = [Math]::Round((Get-Item $outputZip).Length / 1MB, 2)

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Deployment Package Created Successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Package: $outputZip ($fileSize MB)" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Transfer deployment.zip to your Linux server:" -ForegroundColor White
Write-Host "   scp deployment.zip root@192.168.100.107:/root/" -ForegroundColor Gray
Write-Host ""
Write-Host "2. SSH into the server:" -ForegroundColor White
Write-Host "   ssh root@192.168.100.107" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Extract and deploy:" -ForegroundColor White
Write-Host "   unzip deployment.zip -d /opt/onemanvan" -ForegroundColor Gray
Write-Host "   cd /opt/onemanvan" -ForegroundColor Gray
Write-Host "   chmod +x deploy-to-docker.sh" -ForegroundColor Gray
Write-Host "   ./deploy-to-docker.sh" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Access Web UI:" -ForegroundColor White
Write-Host "   http://192.168.100.107:5000" -ForegroundColor Gray
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
