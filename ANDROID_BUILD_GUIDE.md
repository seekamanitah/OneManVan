# Android APK Build Guide

## Quick Start

### Windows (PowerShell)
```powershell
.\build-android-apk.ps1
```

### Linux/macOS (Bash)
```bash
chmod +x build-android-apk.sh
./build-android-apk.sh
```

---

## Prerequisites

### 1. Install .NET MAUI Workload

```powershell
# Check if already installed
dotnet workload list

# Install MAUI workload
dotnet workload install maui

# Or use the included script
.\Install-MauiWorkload.ps1
```

### 2. Install Android SDK

**Option A: Via Visual Studio Installer**
- Open Visual Studio Installer
- Modify your VS installation
- Select ".NET Multi-platform App UI development"
- Ensure "Android SDK setup" is checked

**Option B: Via Android Studio**
- Download and install [Android Studio](https://developer.android.com/studio)
- Open Android Studio ? SDK Manager
- Install:
  - Android SDK Platform 34 (or latest)
  - Android SDK Build-Tools 34.0.0
  - Android SDK Command-line Tools

### 3. Set Environment Variables

**Windows:**
```powershell
# Set ANDROID_HOME
[System.Environment]::SetEnvironmentVariable('ANDROID_HOME', 'C:\Program Files (x86)\Android\android-sdk', 'Machine')

# Or typical Visual Studio path:
[System.Environment]::SetEnvironmentVariable('ANDROID_HOME', 'C:\Program Files\Microsoft\AndroidSDK\25', 'Machine')

# Add to PATH
$path = [System.Environment]::GetEnvironmentVariable('Path', 'Machine')
$newPath = "$path;$env:ANDROID_HOME\platform-tools;$env:ANDROID_HOME\tools"
[System.Environment]::SetEnvironmentVariable('Path', $newPath, 'Machine')

# Restart your terminal after setting
```

**Linux/macOS:**
```bash
# Add to ~/.bashrc or ~/.zshrc
export ANDROID_HOME=$HOME/Android/Sdk
export PATH=$PATH:$ANDROID_HOME/platform-tools:$ANDROID_HOME/tools
```

### 4. Verify Setup

```powershell
# Check .NET version (should be 10.0+)
dotnet --version

# Check MAUI workload
dotnet workload list

# Check Android tools
adb version
```

---

## Building the APK

### Method 1: Using Build Script (Recommended)

**Windows:**
```powershell
.\build-android-apk.ps1
```

**Linux/macOS:**
```bash
chmod +x build-android-apk.sh
./build-android-apk.sh
```

### Method 2: Manual Command

```powershell
# From solution root
dotnet publish OneManVan.Mobile/OneManVan.Mobile.csproj `
    -c Release `
    -f net10.0-android `
    -p:AndroidPackageFormat=apk
```

### Method 3: Visual Studio

1. Set **OneManVan.Mobile** as startup project
2. Select **Android ? Release** configuration
3. Right-click project ? **Publish** ? **Ad Hoc**
4. Choose signing or skip for testing

---

## Build Output

**APK Location:**
```
OneManVan.Mobile/bin/Release/net10.0-android/publish/com.onemanvan.fsm-Signed.apk
```

**Version Info:**
- App ID: `com.onemanvan.fsm`
- Display Version: `1.0.2`
- Build Version: `3`

---

## Installing the APK

### Method 1: ADB (Recommended for Testing)

```powershell
# Enable USB Debugging on your Android device:
# Settings ? About Phone ? tap Build Number 7 times
# Settings ? Developer Options ? USB Debugging ? Enable

# Connect device via USB
adb devices

# Install APK
adb install -r "OneManVan.Mobile\bin\Release\net10.0-android\publish\com.onemanvan.fsm-Signed.apk"

# View logs while testing
adb logcat | findstr "OneManVan"
```

### Method 2: File Transfer

1. Copy APK to your Android device
2. Open file manager on device
3. Tap the APK file
4. Allow installation from unknown sources if prompted
5. Tap "Install"

### Method 3: Email/Cloud

1. Email the APK to yourself
2. Open email on Android device
3. Download and install the APK

---

## Troubleshooting

### Build Errors

**Error: Android SDK not found**
```powershell
# Set ANDROID_HOME
$env:ANDROID_HOME = "C:\Program Files\Microsoft\AndroidSDK\25"
```

**Error: Java SDK not found**
```powershell
# Install Java JDK 17 or later
# Download from: https://learn.microsoft.com/en-us/java/openjdk/download
```

**Error: MAUI workload not installed**
```powershell
dotnet workload install maui
```

**Error: AndroidPackageFormat not recognized**
```powershell
# Update to latest .NET SDK
dotnet --version  # Should be 10.0 or higher
```

### Runtime Errors

**App crashes on startup**
- Check logcat: `adb logcat | findstr "OneManVan"`
- Ensure Android version is 5.0+ (API 21+)

**Database connection fails**
- Mobile app uses SQLite by default (works offline)
- To connect to remote SQL Server:
  - Open Settings in app
  - Switch to "Remote (SQL Server)"
  - Enter server details

**Permissions denied**
- Check AndroidManifest.xml has required permissions
- Grant permissions in device Settings ? Apps ? OneManVan

---

## Signing APK for Distribution (Optional)

For production or side-loading outside development:

### 1. Create Keystore

```powershell
keytool -genkey -v -keystore onemanvan.keystore -alias onemanvan -keyalg RSA -keysize 2048 -validity 10000
```

### 2. Update Project File

Add to `OneManVan.Mobile.csproj`:

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <AndroidKeyStore>true</AndroidKeyStore>
  <AndroidSigningKeyStore>onemanvan.keystore</AndroidSigningKeyStore>
  <AndroidSigningKeyAlias>onemanvan</AndroidSigningKeyAlias>
  <AndroidSigningKeyPass>YOUR_PASSWORD</AndroidSigningKeyPass>
  <AndroidSigningStorePass>YOUR_PASSWORD</AndroidSigningStorePass>
</PropertyGroup>
```

### 3. Build Signed APK

```powershell
dotnet publish OneManVan.Mobile/OneManVan.Mobile.csproj `
    -c Release `
    -f net10.0-android `
    -p:AndroidPackageFormat=apk
```

---

## Testing Checklist

After installing the APK:

- [ ] App launches successfully
- [ ] Can create/edit customers, jobs, estimates
- [ ] QuickNotes page works (mobile-only feature)
- [ ] QuickTimeClock page works (mobile-only feature)
- [ ] SQLite database works (default, offline mode)
- [ ] Can switch to SQL Server in Settings
- [ ] Can connect to remote server (seekmedia.duckdns.org:1433)
- [ ] Barcode scanner works (if testing equipment tracking)

---

## Build Variants

### Debug APK (faster build, larger size)
```powershell
dotnet publish -c Debug -f net10.0-android -p:AndroidPackageFormat=apk
```

### Release APK (optimized, smaller size)
```powershell
dotnet publish -c Release -f net10.0-android -p:AndroidPackageFormat=apk
```

### AAB for Google Play (if distributing via Play Store)
```powershell
dotnet publish -c Release -f net10.0-android -p:AndroidPackageFormat=aab
```

---

## Version Management

To increment version for new build:

Edit `OneManVan.Mobile/OneManVan.Mobile.csproj`:

```xml
<ApplicationDisplayVersion>1.0.3</ApplicationDisplayVersion>  <!-- User-visible version -->
<ApplicationVersion>4</ApplicationVersion>  <!-- Build number, must increment -->
```

Then rebuild:
```powershell
.\build-android-apk.ps1
```

---

## Support

- **Android Requirements**: Android 5.0+ (API 21+)
- **Recommended**: Android 8.0+ (API 26+)
- **Architecture**: arm64-v8a, armeabi-v7a, x86_64

For issues, check:
- Visual Studio Output window
- Android Device Log: `adb logcat`
- Project issues: https://github.com/seekamanitah/OneManVan/issues
