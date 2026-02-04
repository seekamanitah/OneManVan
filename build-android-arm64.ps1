# Build Android APK for ARM64 Physical Devices (Samsung Galaxy Fold, etc.)
# Run this from the solution root directory

$ErrorActionPreference = 'Stop'

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Building OneManVan Mobile APK (ARM64)" -ForegroundColor Cyan
Write-Host "  For Physical Android Devices" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$ProjectPath = "OneManVan.Mobile\OneManVan.Mobile.csproj"
$Configuration = "Release"
$Framework = "net10.0-android"
$RuntimeIdentifier = "android-arm64"

# Check if project exists
if (-not (Test-Path $ProjectPath)) {
    Write-Host "ERROR: Project file not found at $ProjectPath" -ForegroundColor Red
    exit 1
}

Write-Host "Project: $ProjectPath" -ForegroundColor Yellow
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Framework: $Framework" -ForegroundColor Yellow
Write-Host "Architecture: ARM64 (for physical devices)" -ForegroundColor Yellow
Write-Host ""

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean $ProjectPath -c $Configuration -f $Framework
if (Test-Path "OneManVan.Mobile\bin\$Configuration") {
    Remove-Item -Path "OneManVan.Mobile\bin\$Configuration" -Recurse -Force -ErrorAction SilentlyContinue
}
if (Test-Path "OneManVan.Mobile\obj\$Configuration") {
    Remove-Item -Path "OneManVan.Mobile\obj\$Configuration" -Recurse -Force -ErrorAction SilentlyContinue
}
Write-Host ""

# Restore dependencies
Write-Host "Restoring dependencies..." -ForegroundColor Yellow
dotnet restore $ProjectPath -r $RuntimeIdentifier
Write-Host ""

# Build APK for ARM64 devices
Write-Host "Building ARM64 Release APK..." -ForegroundColor Yellow
Write-Host "This may take several minutes..." -ForegroundColor Gray
Write-Host ""

dotnet publish $ProjectPath `
    -c $Configuration `
    -f $Framework `
    -r $RuntimeIdentifier `
    -p:AndroidPackageFormat=apk `
    -p:RuntimeIdentifiers=android-arm64 `
    -p:EmbedAssembliesIntoApk=true `
    --self-contained `
    /p:AndroidSupportedAbis=arm64-v8a

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
$ApkPath = Get-ChildItem -Path "OneManVan.Mobile\bin\$Configuration\$Framework\$RuntimeIdentifier" -Filter "*-Signed.apk" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1

if (-not $ApkPath) {
    # Try without publish folder
    $ApkPath = Get-ChildItem -Path "OneManVan.Mobile\bin\$Configuration\$Framework" -Filter "*.apk" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
}

if ($ApkPath) {
    Write-Host "APK Location:" -ForegroundColor Cyan
    Write-Host "  $($ApkPath.FullName)" -ForegroundColor White
    Write-Host ""
    $SizeMB = [math]::Round($ApkPath.Length / 1MB, 2)
    Write-Host "APK Size: $SizeMB MB" -ForegroundColor Yellow
    Write-Host "Architecture: ARM64 (arm64-v8a)" -ForegroundColor Yellow
    Write-Host "Compatible with: Samsung Galaxy Fold, S-series, and most modern Android phones" -ForegroundColor Gray
    Write-Host ""
    
    # Copy to Desktop for easy access
    $DesktopApk = "$env:USERPROFILE\Desktop\OneManVan-ARM64.apk"
    Copy-Item $ApkPath.FullName $DesktopApk -Force
    Write-Host "Copied to Desktop: $DesktopApk" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "To install on your Samsung Galaxy Fold 7:" -ForegroundColor Cyan
    Write-Host "Method 1: USB (Recommended)" -ForegroundColor White
    Write-Host "  1. Enable USB Debugging on your phone" -ForegroundColor Gray
    Write-Host "     Settings -> Developer options -> USB debugging" -ForegroundColor Gray
    Write-Host "  2. Connect via USB" -ForegroundColor Gray
    Write-Host "  3. Run: adb install -r `"$($ApkPath.FullName)`"" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Method 2: File Transfer" -ForegroundColor White
    Write-Host "  1. Copy from Desktop: $DesktopApk" -ForegroundColor Gray
    Write-Host "  2. Transfer to your Fold 7 (USB, Google Drive, etc.)" -ForegroundColor Gray
    Write-Host "  3. Open file on phone and tap 'Install'" -ForegroundColor Gray
    Write-Host ""
} else {
    Write-Host "Warning: Could not locate the APK file automatically" -ForegroundColor Yellow
    Write-Host "Check: OneManVan.Mobile\bin\$Configuration\$Framework\$RuntimeIdentifier\" -ForegroundColor Gray
}

Write-Host ""
