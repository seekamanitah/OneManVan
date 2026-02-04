# Build Android APK for OneManVan Mobile
# Run this from the solution root directory

$ErrorActionPreference = 'Stop'

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Building OneManVan Mobile APK" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$ProjectPath = "OneManVan.Mobile\OneManVan.Mobile.csproj"
$Configuration = "Release"
$Framework = "net10.0-android"

# Check if project exists
if (-not (Test-Path $ProjectPath)) {
    Write-Host "ERROR: Project file not found at $ProjectPath" -ForegroundColor Red
    exit 1
}

Write-Host "Project: $ProjectPath" -ForegroundColor Yellow
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Framework: $Framework" -ForegroundColor Yellow
Write-Host ""

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean $ProjectPath -c $Configuration -f $Framework
Write-Host ""

# Restore dependencies
Write-Host "Restoring dependencies..." -ForegroundColor Yellow
dotnet restore $ProjectPath
Write-Host ""

# Build APK (unsigned for testing)
Write-Host "Building APK..." -ForegroundColor Yellow
Write-Host "This may take several minutes on first build..." -ForegroundColor Gray
Write-Host ""

dotnet publish $ProjectPath `
    -c $Configuration `
    -f $Framework `
    -p:AndroidPackageFormat=apk `
    -p:AndroidSdkDirectory="$env:ANDROID_HOME" `
    --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "============================================" -ForegroundColor Red
    Write-Host "  Build Failed!" -ForegroundColor Red
    Write-Host "============================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Common issues:" -ForegroundColor Yellow
    Write-Host "1. Android SDK not installed or ANDROID_HOME not set" -ForegroundColor Gray
    Write-Host "2. Java JDK not installed or JAVA_HOME not set" -ForegroundColor Gray
    Write-Host "3. .NET MAUI workload not installed" -ForegroundColor Gray
    Write-Host ""
    Write-Host "To install MAUI workload, run:" -ForegroundColor Cyan
    Write-Host "  dotnet workload install maui" -ForegroundColor White
    exit 1
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "  Build Successful!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""

# Find the APK
$ApkPath = Get-ChildItem -Path "OneManVan.Mobile\bin\$Configuration\$Framework\publish" -Filter "*.apk" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1

if ($ApkPath) {
    Write-Host "APK Location:" -ForegroundColor Cyan
    Write-Host "  $($ApkPath.FullName)" -ForegroundColor White
    Write-Host ""
    Write-Host "APK Size: $([math]::Round($ApkPath.Length / 1MB, 2)) MB" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "To install on your Android device:" -ForegroundColor Cyan
    Write-Host "1. Connect device via USB with USB Debugging enabled" -ForegroundColor Gray
    Write-Host "2. Run: adb install -r `"$($ApkPath.FullName)`"" -ForegroundColor White
    Write-Host ""
    Write-Host "Or copy the APK to your device and install manually" -ForegroundColor Gray
} else {
    Write-Host "Warning: Could not locate the APK file automatically" -ForegroundColor Yellow
    Write-Host "Check: OneManVan.Mobile\bin\Release\net10.0-android\publish\" -ForegroundColor Gray
}

Write-Host ""
