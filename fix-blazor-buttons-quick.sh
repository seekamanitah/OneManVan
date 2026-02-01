#!/bin/bash
# Quick Blazor Button Fix for Local Development
# Run this then test buttons

echo "=========================================="
echo "Blazor Button Fix - Quick Start"
echo "=========================================="
echo ""

# Check we're in the right directory
if [ ! -f "OneManVan.Web/Components/App.razor" ]; then
    echo "? Error: Not in project root directory"
    echo "   Run: cd /path/to/TradeFlow"
    exit 1
fi

# Step 1: Verify fix
echo "Step 1: Verifying code fix..."
if grep -q '<div id="blazor-error-ui">' OneManVan.Web/Components/App.razor; then
    echo "? Fix is present"
else
    echo "? Fix NOT found!"
    echo "   Run the Windows PowerShell script first"
    exit 1
fi

# Step 2: Clean and rebuild
echo ""
echo "Step 2: Cleaning and rebuilding..."
dotnet clean > /dev/null 2>&1
dotnet build --no-incremental

if [ $? -eq 0 ]; then
    echo "? Build successful"
else
    echo "? Build failed!"
    exit 1
fi

# Step 3: Instructions
echo ""
echo "=========================================="
echo "? Ready to Test!"
echo "=========================================="
echo ""
echo "Next steps:"
echo "  1. Open Visual Studio"
echo "  2. Press F5 to start debugging"
echo "  3. CLOSE the browser that opens"
echo "  4. Open InPrivate/Incognito (Ctrl+Shift+N)"
echo "  5. Navigate to: https://localhost:7159/test-blazor-button"
echo ""
echo "In browser DevTools (F12):"
echo "  Console ? Type: Blazor"
echo "  Should return: Object (not undefined)"
echo ""
echo "  Network ? Filter: WS"
echo "  Should see: _blazor?id=..."
echo ""
echo "Test buttons:"
echo "  - Increment ? Count should increase"
echo "  - Navigate ? Should change page"
echo ""
echo "If buttons still don't work:"
echo "  See: BLAZOR_ONCLICK_DIAGNOSIS_COMPLETE.md"
echo "=========================================="
echo ""
