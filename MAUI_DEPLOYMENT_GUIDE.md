# MAUI Blazor Hybrid App - Deployment Guide

## Prerequisites

### 1. Install MAUI Workload
```bash
# Install MAUI workload (required for building)
dotnet workload install maui

# Verify installation
dotnet workload list
```

### 2. Android SDK Requirements
- Android SDK Platform 33 or higher
- Android SDK Build-Tools
- Android SDK Platform-Tools

### 3. Visual Studio Requirements (Windows)
- Visual Studio 2022 17.8+
- .NET MAUI workload installed via VS Installer
- Android SDK configuration

---

## Building the MAUI App

### Debug Build (for testing)
```bash
# Navigate to MAUI project
cd OneManVan.MauiBlazor

# Build for Android
dotnet build -f net10.0-android

# Or use Visual Studio:
# 1. Set OneManVan.MauiBlazor as startup project
# 2. Select Android Emulator or Device
# 3. Press F5
```

### Release Build (for deployment)
```bash
# Build Release APK
dotnet publish -f net10.0-android -c Release

# Build Release AAB (for Google Play)
dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormat=aab
```

### Signing the APK/AAB
```bash
# Generate a keystore (one-time)
keytool -genkey -v -keystore onemanvan.keystore -alias onemanvan -keyalg RSA -keysize 2048 -validity 10000

# Build signed release
dotnet publish -f net10.0-android -c Release \
  -p:AndroidKeyStore=true \
  -p:AndroidSigningKeyStore=onemanvan.keystore \
  -p:AndroidSigningKeyAlias=onemanvan \
  -p:AndroidSigningKeyPass=YOUR_KEY_PASSWORD \
  -p:AndroidSigningStorePass=YOUR_STORE_PASSWORD
```

---

## Testing

### Android Emulator
1. Open Android Device Manager (Visual Studio > Tools > Android > Android Device Manager)
2. Create emulator with API 33+
3. Start emulator
4. Run app from Visual Studio

### Physical Device
1. Enable Developer Options on device
2. Enable USB Debugging
3. Connect via USB
4. Run from Visual Studio

---

## Google Play Deployment

### 1. Prepare for Release
- Update version in `.csproj`:
  ```xml
  <ApplicationDisplayVersion>1.0.0</ApplicationDisplayVersion>
  <ApplicationVersion>1</ApplicationVersion>
  ```
- Test thoroughly on multiple devices
- Create screenshots (phone & tablet)

### 2. Create Google Play Console Account
- Go to: https://play.google.com/console
- Pay one-time $25 registration fee

### 3. Create App Listing
- App name: OneManVan
- Category: Business
- Content rating questionnaire
- Privacy policy URL

### 4. Upload AAB
- Build signed AAB
- Upload to Production/Internal Testing track
- Roll out percentage

### 5. Release Tracks
1. **Internal Testing** - Up to 100 testers (email list)
2. **Closed Testing** - Limited beta users
3. **Open Testing** - Public beta
4. **Production** - Full release

---

## Troubleshooting

### MC3074 XAML Namespace Errors
```
Error: The tag 'Application' does not exist in XML namespace
```
**Solution:** MAUI workload not installed
```bash
dotnet workload install maui
dotnet workload repair
```

### Build Errors after .NET Update
```bash
dotnet workload update
dotnet clean
dotnet restore
dotnet build
```

### APK Too Large
- Enable AOT compilation: `-p:RunAOTCompilation=true`
- Enable linker: `-p:PublishTrimmed=true`
- Remove unused assemblies

### App Crashes on Startup
1. Check Android logcat for errors
2. Verify permissions in AndroidManifest.xml
3. Check database initialization

---

## Configuration Files

### AndroidManifest.xml
Located at: `Platforms/Android/AndroidManifest.xml`
```xml
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
```

### App Icons
- Located at: `Resources/AppIcon/`
- SVG format recommended
- Auto-generates all sizes

### Splash Screen
- Located at: `Resources/Splash/`
- SVG format recommended

---

## Sync Feature Notes

The `SyncService` is prepared but requires server-side API endpoints:

### Required API Endpoints
```
GET  /api/sync/pull?since={timestamp}  - Get changes since timestamp
POST /api/sync/push                     - Push local changes
POST /api/sync/conflicts                - Report conflicts
```

### Entity Tracking Fields (Future)
Add to entities for sync support:
```csharp
public DateTime LastModified { get; set; }
public SyncStatus SyncStatus { get; set; }
public string? ETag { get; set; }
```

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2025-02 | Initial MAUI Blazor Hybrid release |

---

## Support

For issues with the MAUI app:
1. Check this guide first
2. Review MAUI_AUDIT_GAP_ANALYSIS.md for known gaps
3. File issues at: https://github.com/seekamanitah/OneManVan
