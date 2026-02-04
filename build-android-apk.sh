#!/bin/bash
# Build Android APK for OneManVan Mobile
# Run this from the solution root directory

set -e

echo "============================================"
echo "  Building OneManVan Mobile APK"
echo "============================================"
echo ""

# Configuration
PROJECT_PATH="OneManVan.Mobile/OneManVan.Mobile.csproj"
CONFIGURATION="Release"
FRAMEWORK="net10.0-android"

# Check if project exists
if [ ! -f "$PROJECT_PATH" ]; then
    echo "ERROR: Project file not found at $PROJECT_PATH"
    exit 1
fi

echo "Project: $PROJECT_PATH"
echo "Configuration: $CONFIGURATION"
echo "Framework: $FRAMEWORK"
echo ""

# Clean previous builds
echo "Cleaning previous builds..."
dotnet clean "$PROJECT_PATH" -c "$CONFIGURATION" -f "$FRAMEWORK"
echo ""

# Restore dependencies
echo "Restoring dependencies..."
dotnet restore "$PROJECT_PATH"
echo ""

# Build APK (unsigned for testing)
echo "Building APK..."
echo "This may take several minutes on first build..."
echo ""

dotnet publish "$PROJECT_PATH" \
    -c "$CONFIGURATION" \
    -f "$FRAMEWORK" \
    -p:AndroidPackageFormat=apk \
    --no-restore

if [ $? -ne 0 ]; then
    echo ""
    echo "============================================"
    echo "  Build Failed!"
    echo "============================================"
    echo ""
    echo "Common issues:"
    echo "1. Android SDK not installed or ANDROID_HOME not set"
    echo "2. Java JDK not installed or JAVA_HOME not set"
    echo "3. .NET MAUI workload not installed"
    echo ""
    echo "To install MAUI workload, run:"
    echo "  dotnet workload install maui"
    exit 1
fi

echo ""
echo "============================================"
echo "  Build Successful!"
echo "============================================"
echo ""

# Find the APK
APK_PATH=$(find OneManVan.Mobile/bin/$CONFIGURATION/$FRAMEWORK/publish -name "*.apk" -type f 2>/dev/null | head -n 1)

if [ -n "$APK_PATH" ]; then
    echo "APK Location:"
    echo "  $APK_PATH"
    echo ""
    APK_SIZE=$(du -h "$APK_PATH" | cut -f1)
    echo "APK Size: $APK_SIZE"
    echo ""
    echo "To install on your Android device:"
    echo "1. Connect device via USB with USB Debugging enabled"
    echo "2. Run: adb install -r \"$APK_PATH\""
    echo ""
    echo "Or copy the APK to your device and install manually"
else
    echo "Warning: Could not locate the APK file automatically"
    echo "Check: OneManVan.Mobile/bin/Release/net10.0-android/publish/"
fi

echo ""
