# OneManVan Deployment Package Creator
# Run this from your project root directory: C:\Users\tech\source\repos\TradeFlow

param(
    [switch]$ZipPackage = $false,
    [switch]$IncludeGit = $false
)

$ErrorActionPreference = "Stop"

# Colors
function Write-Success { param($Message) Write-Host $Message -ForegroundColor Green }
function Write-Info { param($Message) Write-Host $Message -ForegroundColor Cyan }
function Write-Warning { param($Message) Write-Host $Message -ForegroundColor Yellow }
function Write-Error { param($Message) Write-Host $Message -ForegroundColor Red }

Write-Info "???????????????????????????????????????????????????????????"
Write-Info "  OneManVan - Deployment Package Creator"
Write-Info "???????????????????????????????????????????????????????????"
Write-Host ""

# Configuration
$ProjectRoot = Get-Location
$DeployFolderName = "OneManVan-Deployment-$(Get-Date -Format 'yyyy-MM-dd-HHmm')"
$DestinationPath = Join-Path $env:USERPROFILE "Desktop\$DeployFolderName"

Write-Info "?? Project Root: $ProjectRoot"
Write-Info "?? Destination: $DestinationPath"
Write-Host ""

# Step 1: Create deployment folder
Write-Info "Step 1: Creating deployment folder..."
if (Test-Path $DestinationPath) {
    Write-Warning "Deployment folder already exists. Removing..."
    Remove-Item $DestinationPath -Recurse -Force
}
New-Item -Path $DestinationPath -ItemType Directory -Force | Out-Null
Write-Success "? Created: $DestinationPath"
Write-Host ""

# Step 2: Copy Docker configuration files
Write-Info "Step 2: Copying Docker configuration files..."
$dockerFiles = @(
    "docker-compose.yml",
    "docker-compose-production.yml",
    ".env",
    ".env.example",
    ".env.production.example",
    ".dockerignore"
)

foreach ($file in $dockerFiles) {
    $sourcePath = Join-Path $ProjectRoot $file
    if (Test-Path $sourcePath) {
        Copy-Item $sourcePath -Destination $DestinationPath -Force
        Write-Success "  ? Copied: $file"
    } else {
        Write-Warning "  ? Not found: $file"
    }
}
Write-Host ""

# Step 3: Copy deployment scripts
Write-Info "Step 3: Copying deployment scripts..."
$deploymentScripts = @(
    "docker-complete-reset-deploy.sh",
    "docker-complete-reset-deploy.ps1",
    "DOCKER_COMPLETE_RESET_GUIDE.md",
    "DEPLOYMENT_QUICK_START.md"
)

foreach ($script in $deploymentScripts) {
    $sourcePath = Join-Path $ProjectRoot $script
    if (Test-Path $sourcePath) {
        Copy-Item $sourcePath -Destination $DestinationPath -Force
        Write-Success "  ? Copied: $script"
    } else {
        Write-Warning "  ? Not found: $script"
    }
}
Write-Host ""

# Step 4: Copy OneManVan.Web folder (excluding build artifacts)
Write-Info "Step 4: Copying OneManVan.Web folder..."
$webSource = Join-Path $ProjectRoot "OneManVan.Web"
$webDest = Join-Path $DestinationPath "OneManVan.Web"

if (Test-Path $webSource) {
    $excludeDirs = @("bin", "obj", "node_modules", ".vs", "TestResults", "publish")
    $excludeFiles = @("*.user", "*.suo", "*.cache")
    
    # Create the destination folder
    New-Item -Path $webDest -ItemType Directory -Force | Out-Null
    
    # Copy files
    Get-ChildItem -Path $webSource -Recurse | Where-Object {
        $item = $_
        $relativePath = $item.FullName.Substring($webSource.Length)
        
        # Exclude directories
        $excludeDir = $false
        foreach ($dir in $excludeDirs) {
            if ($relativePath -like "*\$dir\*" -or $relativePath -like "*\$dir") {
                $excludeDir = $true
                break
            }
        }
        
        # Exclude files
        $excludeFile = $false
        foreach ($pattern in $excludeFiles) {
            if ($item.Name -like $pattern) {
                $excludeFile = $true
                break
            }
        }
        
        -not $excludeDir -and -not $excludeFile
    } | ForEach-Object {
        $targetPath = Join-Path $webDest $_.FullName.Substring($webSource.Length)
        $targetDir = Split-Path $targetPath -Parent
        
        if (-not (Test-Path $targetDir)) {
            New-Item -Path $targetDir -ItemType Directory -Force | Out-Null
        }
        
        if (-not $_.PSIsContainer) {
            Copy-Item $_.FullName -Destination $targetPath -Force
        }
    }
    
    Write-Success "  ? Copied: OneManVan.Web folder (excluding build artifacts)"
} else {
    Write-Error "  ? OneManVan.Web folder not found!"
    exit 1
}
Write-Host ""

# Step 5: Copy OneManVan.Shared folder
Write-Info "Step 5: Copying OneManVan.Shared folder..."
$sharedSource = Join-Path $ProjectRoot "OneManVan.Shared"
$sharedDest = Join-Path $DestinationPath "OneManVan.Shared"

if (Test-Path $sharedSource) {
    $excludeDirs = @("bin", "obj", ".vs")
    $excludeFiles = @("*.user", "*.suo", "*.cache")
    
    New-Item -Path $sharedDest -ItemType Directory -Force | Out-Null
    
    Get-ChildItem -Path $sharedSource -Recurse | Where-Object {
        $item = $_
        $relativePath = $item.FullName.Substring($sharedSource.Length)
        
        $excludeDir = $false
        foreach ($dir in $excludeDirs) {
            if ($relativePath -like "*\$dir\*" -or $relativePath -like "*\$dir") {
                $excludeDir = $true
                break
            }
        }
        
        $excludeFile = $false
        foreach ($pattern in $excludeFiles) {
            if ($item.Name -like $pattern) {
                $excludeFile = $true
                break
            }
        }
        
        -not $excludeDir -and -not $excludeFile
    } | ForEach-Object {
        $targetPath = Join-Path $sharedDest $_.FullName.Substring($sharedSource.Length)
        $targetDir = Split-Path $targetPath -Parent
        
        if (-not (Test-Path $targetDir)) {
            New-Item -Path $targetDir -ItemType Directory -Force | Out-Null
        }
        
        if (-not $_.PSIsContainer) {
            Copy-Item $_.FullName -Destination $targetPath -Force
        }
    }
    
    Write-Success "  ? Copied: OneManVan.Shared folder (excluding build artifacts)"
} else {
    Write-Error "  ? OneManVan.Shared folder not found!"
    exit 1
}
Write-Host ""

# Step 6: Create deployment manifest
Write-Info "Step 6: Creating deployment manifest..."
$manifestPath = Join-Path $DestinationPath "DEPLOYMENT_MANIFEST.txt"
$manifest = @"
OneManVan Deployment Package
=============================

Created: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Source: $ProjectRoot
Package: $DeployFolderName

Contents:
---------
- Docker configuration files (docker-compose.yml, .env, etc.)
- Deployment scripts (docker-complete-reset-deploy.*)
- OneManVan.Web folder (application code)
- OneManVan.Shared folder (shared libraries)
- Documentation (guides and instructions)

Excluded:
---------
- bin/ and obj/ folders
- node_modules/
- .vs/ and TestResults/
- .user and .suo files
- Build artifacts

Next Steps:
-----------
1. Copy this entire folder to your Docker server
2. Follow instructions in DOCKER_COMPLETE_RESET_GUIDE.md
3. Or run docker-complete-reset-deploy.sh (Linux/Mac)
4. Or run docker-complete-reset-deploy.ps1 (Windows)

Important:
----------
- Review .env file and update passwords before deployment
- Ensure Docker is installed on target server
- Backup any existing data if needed

"@

Set-Content -Path $manifestPath -Value $manifest
Write-Success "  ? Created: DEPLOYMENT_MANIFEST.txt"
Write-Host ""

# Step 7: Copy Git info (optional)
if ($IncludeGit) {
    Write-Info "Step 7: Including Git information..."
    $gitInfo = @"
Git Commit Information
======================

Commit: $(git rev-parse HEAD 2>$null)
Branch: $(git rev-parse --abbrev-ref HEAD 2>$null)
Author: $(git log -1 --pretty=format:'%an <%ae>' 2>$null)
Date: $(git log -1 --pretty=format:'%ad' 2>$null)
Message: $(git log -1 --pretty=format:'%s' 2>$null)

"@
    Set-Content -Path (Join-Path $DestinationPath "GIT_INFO.txt") -Value $gitInfo
    Write-Success "  ? Created: GIT_INFO.txt"
    Write-Host ""
}

# Step 8: Create ZIP package (optional)
if ($ZipPackage) {
    Write-Info "Step 8: Creating ZIP package..."
    $zipPath = "$DestinationPath.zip"
    
    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }
    
    Compress-Archive -Path $DestinationPath -DestinationPath $zipPath -CompressionLevel Optimal
    Write-Success "  ? Created: $zipPath"
    Write-Host ""
}

# Step 9: Calculate package size
Write-Info "Step 9: Calculating package size..."
$totalSize = (Get-ChildItem -Path $DestinationPath -Recurse | Measure-Object -Property Length -Sum).Sum
$sizeInMB = [math]::Round($totalSize / 1MB, 2)
Write-Success "  ? Package size: $sizeInMB MB"
Write-Host ""

# Summary
Write-Success "???????????????????????????????????????????????????????????"
Write-Success "  ? Deployment Package Created Successfully!"
Write-Success "???????????????????????????????????????????????????????????"
Write-Host ""

Write-Info "?? Package Location:"
Write-Host "   $DestinationPath" -ForegroundColor White
Write-Host ""

if ($ZipPackage) {
    Write-Info "?? ZIP Package:"
    Write-Host "   $DestinationPath.zip" -ForegroundColor White
    Write-Host ""
}

Write-Info "?? Package Size: $sizeInMB MB"
Write-Host ""

Write-Info "?? Package Contents:"
Write-Host "   • Docker configuration files" -ForegroundColor White
Write-Host "   • Deployment scripts (Linux + Windows)" -ForegroundColor White
Write-Host "   • OneManVan.Web (application code)" -ForegroundColor White
Write-Host "   • OneManVan.Shared (shared libraries)" -ForegroundColor White
Write-Host "   • Documentation and guides" -ForegroundColor White
Write-Host ""

Write-Info "?? Next Steps:"
Write-Host ""
Write-Host "   1. Transfer to Server:" -ForegroundColor Cyan
Write-Host "      • Use WinSCP, FileZilla, or SCP command" -ForegroundColor Gray
Write-Host "      • Upload entire folder to: ~/OneManVan-Deployment" -ForegroundColor Gray
Write-Host ""
Write-Host "   2. Connect to Server:" -ForegroundColor Cyan
Write-Host "      ssh user@server-ip" -ForegroundColor Gray
Write-Host ""
Write-Host "   3. Navigate to Folder:" -ForegroundColor Cyan
Write-Host "      cd ~/OneManVan-Deployment" -ForegroundColor Gray
Write-Host ""
Write-Host "   4. Run Deployment:" -ForegroundColor Cyan
Write-Host "      chmod +x docker-complete-reset-deploy.sh" -ForegroundColor Gray
Write-Host "      ./docker-complete-reset-deploy.sh" -ForegroundColor Gray
Write-Host ""
Write-Host "   OR follow step-by-step guide in:" -ForegroundColor Cyan
Write-Host "      DOCKER_COMPLETE_RESET_GUIDE.md" -ForegroundColor Gray
Write-Host ""

Write-Warning "??  Remember:"
Write-Host "   • Review and update .env file before deployment" -ForegroundColor Yellow
Write-Host "   • This deployment will DELETE all existing data" -ForegroundColor Yellow
Write-Host "   • Backup any important data first" -ForegroundColor Yellow
Write-Host ""

Write-Success "? Ready to deploy!"
Write-Host ""

# Open Windows Explorer to show the package
Write-Info "Opening folder in Windows Explorer..."
Start-Process "explorer.exe" -ArgumentList "/select,`"$DestinationPath`""
