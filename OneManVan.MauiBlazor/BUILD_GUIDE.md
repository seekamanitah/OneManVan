# ?? Building OneManVan MAUI Blazor Android App

## ? Prerequisites

1. **Visual Studio 2022 (17.13+)** with workloads:
   - .NET MAUI development
   - Mobile development with .NET

2. **Android SDK** (installed automatically with VS):
   - Android SDK 21+ (minimum)
   - Android SDK 34 (target)

3. **Android Emulator** or Physical Device:
   - Recommended: Pixel 5 API 34 (Android 14)

---

## ?? **Quick Start**

### **Step 1: Build the Solution**

```powershell
# Restore packages
dotnet restore

# Build shared libraries first
dotnet build OneManVan.Shared
dotnet build OneManVan.BlazorShared

# Build MAUI app
dotnet build OneManVan.MauiBlazor -f net10.0-android
```

### **Step 2: Run on Android Emulator**

**In Visual Studio:**
1. Open solution in Visual Studio 2022
2. Set `OneManVan.MauiBlazor` as startup project
3. Select `net10.0-android` from target framework dropdown
4. Select Android emulator from device dropdown
5. Press **F5** to run

**Command Line:**
```powershell
# List available devices
dotnet build OneManVan.MauiBlazor -t:Run -f net10.0-android

# Run on specific emulator
dotnet build OneManVan.MauiBlazor -t:Run -f net10.0-android -p:AndroidEmulator="pixel_5_-_api_34"
```

### **Step 3: Run on Physical Android Device**

1. **Enable Developer Options** on your Android device:
   - Go to Settings ? About Phone
   - Tap "Build Number" 7 times
   - Go back to Settings ? Developer Options
   - Enable "USB Debugging"

2. **Connect device via USB**

3. **Trust the computer** (popup on device)

4. **Deploy:**
```powershell
# Build and deploy to connected device
dotnet build OneManVan.MauiBlazor -t:Run -f net10.0-android
```

---

## ?? **Common Issues & Fixes**

### **Issue: Build Fails with "Android SDK not found"**

```powershell
# Set Android SDK path
$env:ANDROID_HOME = "C:\Program Files (x86)\Android\android-sdk"
$env:ANDROID_SDK_ROOT = $env:ANDROID_HOME
```

### **Issue: "Java SDK not found"**

```powershell
# Download and install JDK 17
# https://www.oracle.com/java/technologies/javase/jdk17-archive-downloads.html

# Set JAVA_HOME
$env:JAVA_HOME = "C:\Program Files\Java\jdk-17"
```

### **Issue: Emulator won't start**

```powershell
# List available emulators
emulator -list-avds

# Start emulator manually
emulator -avd pixel_5_-_api_34
```

### **Issue: "Unable to open database file"**

This is expected on first run. The app will create the database automatically.

**Fix:**
```csharp
// Ensure directory exists in MauiProgram.cs
var dbPath = Path.Combine(FileSystem.AppDataDirectory, "onemanvan.db");
var directory = Path.GetDirectoryName(dbPath);
if (!Directory.Exists(directory))
{
    Directory.CreateDirectory(directory!);
}
```

---

## ?? **Project Structure**

```
OneManVan.MauiBlazor/
??? Platforms/
?   ??? Android/
?   ?   ??? MainActivity.cs
?   ?   ??? AndroidManifest.xml
?   ?   ??? Resources/
?   ??? iOS/ (for future)
?   ??? Windows/ (for future)
??? Components/
?   ??? Layout/
?   ?   ??? MainLayout.razor
?   ?   ??? NavMenu.razor
?   ??? Pages/
?   ?   ??? Index.razor
?   ?   ??? Customers.razor
?   ?   ??? ...
?   ??? Routes.razor
??? wwwroot/
?   ??? index.html
?   ??? css/
??? MauiProgram.cs
??? App.xaml
??? MainPage.xaml (BlazorWebView)
```

---

## ?? **Next Steps**

### **1. Move Existing Blazor Components to Shared Library**

```powershell
# Copy components from OneManVan.Web to OneManVan.BlazorShared
Copy-Item OneManVan.Web\Components\Pages\Customers\*.razor OneManVan.BlazorShared\Components\Pages\Customers\
Copy-Item OneManVan.Web\Components\Pages\Jobs\*.razor OneManVan.BlazorShared\Components\Pages\Jobs\
# ... repeat for other pages
```

### **2. Update Web Project to Reference Shared Library**

Edit `OneManVan.Web.csproj`:
```xml
<ItemGroup>
  <ProjectReference Include="..\OneManVan.BlazorShared\OneManVan.BlazorShared.csproj" />
</ItemGroup>
```

### **3. Handle Platform-Specific Features**

```csharp
// Example: Camera access
public async Task<FileResult?> TakePictureAsync()
{
    if (MediaPicker.Default.IsCaptureSupported)
    {
        var photo = await MediaPicker.Default.CapturePhotoAsync();
        return photo;
    }
    return null;
}

// Example: Get device storage path
var documentsPath = FileSystem.AppDataDirectory;
```

### **4. Add Offline Support**

```csharp
// Check network connectivity
if (Connectivity.NetworkAccess == NetworkAccess.Internet)
{
    // Sync with server
}
else
{
    // Work offline with local SQLite
}
```

---

## ?? **Build APK for Distribution**

```powershell
# Build Release APK
dotnet publish OneManVan.MauiBlazor `
    -f net10.0-android `
    -c Release `
    -p:AndroidKeyStore=true `
    -p:AndroidSigningKeyStore=myapp.keystore `
    -p:AndroidSigningKeyAlias=myapp `
    -p:AndroidSigningKeyPass=yourpassword `
    -p:AndroidSigningStorePass=yourpassword

# APK location:
# bin\Release\net10.0-android\publish\com.onemanvan.tradeflow-Signed.apk
```

---

## ?? **Target Other Platforms**

**iOS:**
```powershell
dotnet build -f net10.0-ios
```

**Windows:**
```powershell
dotnet build -f net10.0-windows10.0.19041.0
```

**macOS:**
```powershell
dotnet build -f net10.0-maccatalyst
```

---

## ? **Success Checklist**

- [ ] Solution builds without errors
- [ ] App runs on Android emulator
- [ ] App runs on physical Android device
- [ ] Database creates successfully
- [ ] Can navigate between pages
- [ ] Can create/edit/delete data
- [ ] Offline functionality works
- [ ] Camera/file picker works (if implemented)

---

**?? You now have a MAUI Blazor Hybrid app!**

This app:
- ? Shares code with your web app
- ? Runs natively on Android
- ? Can target iOS, Windows, macOS later
- ? Uses local SQLite database
- ? Works offline
- ? Accesses device hardware (camera, GPS, etc.)
