# Fix Dialog UI Scaling Issues
# This script updates dialogs to handle UI scaling properly

Write-Host "=== Dialog UI Scaling Fix ===" -ForegroundColor Cyan
Write-Host ""

# Dialogs that need fixing (ResizeMode=NoResize and no ScrollViewer)
$dialogsToFix = @(
    "Dialogs\SerialNumberDuplicateDialog.xaml",
    "Dialogs\MarkAsRegisteredDialog.xaml",
    "Dialogs\QuickAssetDetailsDialog.xaml",
    "Dialogs\EditManufacturerDialog.xaml"
)

$fixedCount = 0

foreach ($dialogPath in $dialogsToFix) {
    if (Test-Path $dialogPath) {
        Write-Host "Checking: $dialogPath" -ForegroundColor Yellow
        
        $content = Get-Content $dialogPath -Raw
        
        # Check if it already has ResizeMode="CanResize"
        if ($content -match 'ResizeMode="CanResize"') {
            Write-Host "  Already fixed (CanResize)" -ForegroundColor Green
            continue
        }
        
        # Check if it has ResizeMode="NoResize"
        if ($content -match 'ResizeMode="NoResize"') {
            # Change to CanResize and add MinHeight
            $content = $content -replace 'ResizeMode="NoResize"', 'ResizeMode="CanResize"'
            
            # If it has Height but no MinHeight, add MinHeight
            if ($content -match 'Height="(\d+)"' -and $content -notmatch 'MinHeight=') {
                $height = $matches[1]
                $minHeight = [int]($height * 0.8)
                $maxHeight = [int]($height * 1.5)
                $content = $content -replace 'Height="(\d+)"', "Height=`"$height`"`r`n        MinHeight=`"$minHeight`"`r`n        MaxHeight=`"$maxHeight`""
            }
            
            Set-Content -Path $dialogPath -Value $content -NoNewline
            Write-Host "  Fixed: Made resizable with min/max constraints" -ForegroundColor Green
            $fixedCount++
        }
    }
    else {
        Write-Host "  Not found: $dialogPath" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
Write-Host "Fixed $fixedCount dialog(s)" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Build the solution to verify changes"
Write-Host "2. Test dialogs at different UI scale levels (100%, 125%, 150%)"
Write-Host "3. Ensure all content is accessible with scrolling"
