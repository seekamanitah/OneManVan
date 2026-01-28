# Build APK Script for TradeFlow FSM Mobile App
# Usage: .\BuildAPK.ps1 [-Configuration Debug|Release] [-Install] [-Launch]

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$Install,
    [switch]$Launch
)

$ErrorActionPreference = "Stop"

# Configuration
$ProjectPath = "OneManVan.Mobile"
$ProjectFile = "$ProjectPath\OneManVan.Mobile.csproj"
$AppPackageId = "com.tradeflow.onemanvan"

# Colors
function Write-ColorOutput($ForegroundColor) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    if ($args) {
        Write-Output $args
    }
    $host.UI.RawUI.ForegroundColor = $fc
}

Write-Host ""
Write-ColorOutput Cyan "???????????????????????????????????????????????????????"
Write-ColorOutput Cyan "  TradeFlow FSM - Android APK Build Script"
Write-ColorOutput Cyan "???????????????????????????????????????????????????????"
Write-Host ""

# Check prerequisites
Write-Host "?? Checking prerequisites..." -ForegroundColor Yellow

# Check if project exists
if (-not (Test-Path $ProjectFile)) {
    Write-Host "? Project file not found: $ProjectFile" -ForegroundColor Red
    exit 1
}

# Check .NET SDK
try {
    $dotnetVersion = dotnet --version
    Write-Host "? .NET SDK: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "? .NET SDK not found. Please install .NET 10 SDK." -ForegroundColor Red
    exit 1
}

# Check Android workload
$workloads = dotnet workload list
if ($workloads -notmatch "maui-android") {
    Write-Host "??  Android workload not installed. Installing..." -ForegroundColor Yellow
    dotnet workload install maui-android
}

Write-Host ""
Write-Host "?? Building Configuration: $Configuration" -ForegroundColor Cyan
Write-Host ""

# Clean previous builds
Write-Host "?? Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean $ProjectFile -c $Configuration -v quiet

if ($LASTEXITCODE -ne 0) {
    Write-Host "??  Clean failed, continuing anyway..." -ForegroundColor Yellow
}

# Restore packages
Write-Host "?? Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore $ProjectFile

if ($LASTEXITCODE -ne 0) {
    Write-Host "? Package restore failed!" -ForegroundColor Red
    exit 1
}

# Build or Publish
$StartTime = Get-Date

if ($Configuration -eq "Release") {
    Write-Host "?? Publishing Release APK (this may take a few minutes)..." -ForegroundColor Green
    dotnet publish $ProjectFile -f net10.0-android -c Release /p:AndroidPackageFormat=apk
    $ApkPath = "$ProjectPath\bin\Release\net10.0-android\publish\$AppPackageId-Signed.apk"
} else {
    Write-Host "?? Building Debug APK (this may take a few minutes)..." -ForegroundColor Green
    dotnet build $ProjectFile -f net10.0-android -c Debug
    $ApkPath = "$ProjectPath\bin\Debug\net10.0-android\$AppPackageId-Signed.apk"
}

$BuildTime = (Get-Date) - $StartTime

# Check if build succeeded
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "? Build failed! Check the output above for errors." -ForegroundColor Red
    exit 1
}

# Check if APK was created
if (-not (Test-Path $ApkPath)) {
    # Try alternative locations
    $AlternativePaths = @(
        "$ProjectPath\bin\$Configuration\net10.0-android\$AppPackageId-Signed.apk",
        "$ProjectPath\bin\$Configuration\net10.0-android\publish\$AppPackageId-Signed.apk"
    )
    
    foreach ($AltPath in $AlternativePaths) {
        if (Test-Path $AltPath) {
            $ApkPath = $AltPath
            break
        }
    }
    
    if (-not (Test-Path $ApkPath)) {
        Write-Host ""
        Write-Host "? APK not found! Expected location:" -ForegroundColor Red
        Write-Host "   $ApkPath" -ForegroundColor Gray
        Write-Host ""
        Write-Host "Searched in:" -ForegroundColor Yellow
        foreach ($path in $AlternativePaths) {
            Write-Host "   $path" -ForegroundColor Gray
        }
        exit 1
    }
}

# Success! Display APK info
$ApkSize = (Get-Item $ApkPath).Length / 1MB
$ApkSizeFormatted = "{0:N2}" -f $ApkSize

Write-Host ""
Write-ColorOutput Green "???????????????????????????????????????????????????????"
Write-ColorOutput Green "  ? BUILD SUCCESSFUL!"
Write-ColorOutput Green "???????????????????????????????????????????????????????"
Write-Host ""
Write-Host "?? APK Location:" -ForegroundColor Cyan
Write-Host "   $ApkPath" -ForegroundColor White
Write-Host ""
Write-Host "?? APK Info:" -ForegroundColor Cyan
Write-Host "   Configuration: $Configuration" -ForegroundColor White
Write-Host "   Size: $ApkSizeFormatted MB" -ForegroundColor White
Write-Host "   Build Time: $($BuildTime.ToString('mm\:ss'))" -ForegroundColor White
Write-Host ""

# Install on device if requested
if ($Install) {
    Write-ColorOutput Yellow "???????????????????????????????????????????????????????"
    Write-ColorOutput Yellow "  ?? Installing on Device..."
    Write-ColorOutput Yellow "???????????????????????????????????????????????????????"
    Write-Host ""
    
    # Check if adb is available
    try {
        $adbVersion = adb version 2>&1 | Select-Object -First 1
        Write-Host "? ADB: $adbVersion" -ForegroundColor Green
    } catch {
        Write-Host "? ADB not found! Please install Android SDK Platform Tools." -ForegroundColor Red
        Write-Host "   Download: https://developer.android.com/studio/releases/platform-tools" -ForegroundColor Yellow
        exit 1
    }
    
    # Check if device is connected
    $devices = adb devices | Select-Object -Skip 1 | Where-Object { $_ -match "\t" }
    
    if (-not $devices) {
        Write-Host "? No Android devices connected!" -ForegroundColor Red
        Write-Host ""
        Write-Host "Please:" -ForegroundColor Yellow
        Write-Host "  1. Enable USB Debugging on your device" -ForegroundColor Gray
        Write-Host "  2. Connect device via USB" -ForegroundColor Gray
        Write-Host "  3. Accept USB debugging prompt on device" -ForegroundColor Gray
        Write-Host "  4. Run 'adb devices' to verify" -ForegroundColor Gray
        exit 1
    }
    
    Write-Host "?? Connected Devices:" -ForegroundColor Cyan
    foreach ($device in $devices) {
        Write-Host "   $device" -ForegroundColor White
    }
    Write-Host ""
    
    # Uninstall old version first (ignore errors if not installed)
    Write-Host "???  Uninstalling old version (if exists)..." -ForegroundColor Yellow
    adb uninstall $AppPackageId 2>&1 | Out-Null
    
    # Install APK
    Write-Host "?? Installing APK..." -ForegroundColor Yellow
    adb install -r $ApkPath
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-ColorOutput Green "? Installation successful!"
        Write-Host ""
        
        # Launch app if requested
        if ($Launch) {
            Write-Host "?? Launching app..." -ForegroundColor Yellow
            Start-Sleep -Seconds 1
            adb shell am start -n "$AppPackageId/crc64.MainActivity"
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "? App launched!" -ForegroundColor Green
            } else {
                Write-Host "??  Could not launch app automatically. Please launch manually." -ForegroundColor Yellow
            }
        } else {
            Write-Host "?? To launch app, run:" -ForegroundColor Cyan
            Write-Host "   adb shell am start -n $AppPackageId/crc64.MainActivity" -ForegroundColor Gray
        }
    } else {
        Write-Host ""
        Write-Host "? Installation failed!" -ForegroundColor Red
        Write-Host ""
        Write-Host "Common issues:" -ForegroundColor Yellow
        Write-Host "  • Device storage full" -ForegroundColor Gray
        Write-Host "  • Conflicting signature (uninstall old version first)" -ForegroundColor Gray
        Write-Host "  • USB debugging not authorized" -ForegroundColor Gray
        exit 1
    }
}

Write-Host ""
Write-ColorOutput Green "???????????????????????????????????????????????????????"
Write-ColorOutput Green "  ? All Done!"
Write-ColorOutput Green "???????????????????????????????????????????????????????"
Write-Host ""

# Show next steps
if (-not $Install) {
    Write-Host "?? Next Steps:" -ForegroundColor Cyan
    Write-Host "   1. Copy APK to your device" -ForegroundColor White
    Write-Host "   2. Enable 'Install from Unknown Sources'" -ForegroundColor White
    Write-Host "   3. Tap APK to install" -ForegroundColor White
    Write-Host ""
    Write-Host "   Or install via ADB:" -ForegroundColor Cyan
    Write-Host "   .\BuildAPK.ps1 -Configuration $Configuration -Install" -ForegroundColor Gray
    Write-Host ""
}

Write-Host "?? Documentation: BUILD_ANDROID_APK_GUIDE.md" -ForegroundColor Cyan
Write-Host ""
