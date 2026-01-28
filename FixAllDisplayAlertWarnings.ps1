# Fix remaining DisplayAlert and DisplayActionSheet warnings  
# This handles cases where the method is called directly without a variable

$files = Get-ChildItem -Path "OneManVan.Mobile" -Filter "*.cs" -Recurse | Where-Object { 
    $_.FullName -notlike "*\obj\*" -and 
    $_.FullName -notlike "*\bin\*" 
}

$replacements = @{
    'DisplayAlert\(' = 'DisplayAlertAsync('
    'DisplayActionSheet\(' = 'DisplayActionSheetAsync('
}

$totalReplacements = 0
$filesModified = 0

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content
    $fileReplacements = 0
    
    foreach ($pattern in $replacements.Keys) {
        $replacement = $replacements[$pattern]
        $matches = [regex]::Matches($content, $pattern)
        if ($matches.Count -gt 0) {
            $content = $content -replace $pattern, $replacement
            $fileReplacements += $matches.Count
        }
    }
    
    if ($fileReplacements -gt 0) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        Write-Host "$($file.Name): $fileReplacements replacements" -ForegroundColor Green
        $totalReplacements += $fileReplacements
        $filesModified++
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Files modified: $filesModified" -ForegroundColor Cyan
Write-Host "Total replacements: $totalReplacements" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
