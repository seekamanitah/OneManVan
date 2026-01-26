# Comprehensive Desktop Emoji Removal Script
Write-Host "=== Removing All Desktop Emojis ===" -ForegroundColor Cyan

$filesChanged = 0
$totalReplacements = 0

# Get all C# and XAML files in Desktop project (excluding bin/obj)
$desktopFiles = Get-ChildItem -Path ".\Pages",".\Dialogs",".\Services" -Include "*.cs","*.xaml" -Recurse -ErrorAction SilentlyContinue | 
    Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\' }

foreach ($file in $desktopFiles) {
    try {
        $content = Get-Content -Path $file.FullName -Raw -Encoding UTF8 -ErrorAction Stop
        $originalContent = $content
        
        # Remove emojis by replacing quoted strings containing "??"
        # This pattern matches strings like "??" or "???" etc.
        $content = $content -replace '"\?\?+"', '""'
        
        if ($content -ne $originalContent) {
            Set-Content -Path $file.FullName -Value $content -NoNewline -Encoding UTF8
            $filesChanged++
            Write-Host "  Cleaned: $($file.Name)" -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "  Error processing $($file.Name): $_" -ForegroundColor Red
    }
}

Write-Host "`n=== Summary ===" -ForegroundColor Cyan
Write-Host "Files cleaned: $filesChanged" -ForegroundColor Green
Write-Host "`nDesktop emoji removal complete!" -ForegroundColor Green
