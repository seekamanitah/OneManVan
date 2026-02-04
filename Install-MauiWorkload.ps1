# Install-MauiWorkload.ps1
# Script to install .NET MAUI workload for building the OneManVan.MauiBlazor project

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  .NET MAUI Workload Installation Script" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator (recommended for workload installation)
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "[WARNING] Not running as Administrator. Some installations may require elevated permissions." -ForegroundColor Yellow
    Write-Host "Consider running PowerShell as Administrator if installation fails." -ForegroundColor Yellow
    Write-Host ""
}

# Check current .NET SDK version
Write-Host "Checking .NET SDK version..." -ForegroundColor Gray
try {
    $dotnetVersion = dotnet --version
    Write-Host "  .NET SDK Version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] .NET SDK not found. Please install .NET SDK first." -ForegroundColor Red
    exit 1
}

# List currently installed workloads
Write-Host ""
Write-Host "Currently installed workloads:" -ForegroundColor Gray
dotnet workload list

# Check if MAUI is already installed
Write-Host ""
Write-Host "Checking if MAUI workload is installed..." -ForegroundColor Gray
$workloadList = dotnet workload list 2>&1
if ($workloadList -match "maui") {
    Write-Host "[OK] MAUI workload is already installed!" -ForegroundColor Green
    Write-Host ""
    Write-Host "To update the workload, run:" -ForegroundColor Cyan
    Write-Host "  dotnet workload update" -ForegroundColor White
} else {
    Write-Host "[INFO] MAUI workload not found. Installing..." -ForegroundColor Yellow
    Write-Host ""
    
    # Install MAUI workload
    Write-Host "Running: dotnet workload install maui" -ForegroundColor Cyan
    Write-Host "This may take several minutes..." -ForegroundColor Gray
    Write-Host ""
    
    dotnet workload install maui
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "[SUCCESS] MAUI workload installed successfully!" -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "[ERROR] MAUI workload installation failed." -ForegroundColor Red
        Write-Host "Try running as Administrator or check your internet connection." -ForegroundColor Yellow
        exit 1
    }
}

# Verify installation
Write-Host ""
Write-Host "Verifying installation..." -ForegroundColor Gray
dotnet workload list

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  Installation Complete" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "You can now build the solution:" -ForegroundColor White
Write-Host "  dotnet build OneManVan.sln" -ForegroundColor Cyan
Write-Host ""
Write-Host "Or open Visual Studio and rebuild the solution." -ForegroundColor White
