# Remove all emoji Unicode characters from the project
# This script finds and removes emoji characters (U+1F000 to U+1FFFF range and other common emoji ranges)

Write-Host "=== Emoji Removal Script ===" -ForegroundColor Cyan
Write-Host "Scanning for emoji characters in C# and XAML files..." -ForegroundColor Yellow

# Define emoji Unicode ranges (regex pattern)
# This matches most emojis including:
# - Emoticons (U+1F600-U+1F64F)
# - Miscellaneous Symbols (U+2600-U+26FF)
# - Dingbats (U+2700-U+27BF)
# - Transport and Map Symbols (U+1F680-U+1F6FF)
# - And many more
$emojiPattern = '[\u{1F300}-\u{1F9FF}]|[\u{2600}-\u{26FF}]|[\u{2700}-\u{27BF}]|[\u{1F600}-\u{1F64F}]|[\u{1F680}-\u{1F6FF}]|[\u{1F900}-\u{1F9FF}]'

$totalFiles = 0
$totalReplacements = 0
$changedFiles = @()

# Process Mobile project
Write-Host "`nProcessing OneManVan.Mobile project..." -ForegroundColor Green
$mobileFiles = Get-ChildItem -Path ".\OneManVan.Mobile" -Include "*.cs","*.xaml" -Recurse -ErrorAction SilentlyContinue

foreach ($file in $mobileFiles) {
    $content = Get-Content -Path $file.FullName -Raw -ErrorAction SilentlyContinue
    if ($content -match $emojiPattern) {
        $newContent = $content -replace $emojiPattern, ''
        Set-Content -Path $file.FullName -Value $newContent -NoNewline
        $totalFiles++
        $changedFiles += $file.FullName
        Write-Host "  Cleaned: $($file.Name)" -ForegroundColor Yellow
    }
}

# Process Desktop project
Write-Host "`nProcessing Desktop WPF project..." -ForegroundColor Green
$desktopDirs = @(".\Pages", ".\Dialogs", ".\Services", ".\Themes", ".\Controls")
foreach ($dir in $desktopDirs) {
    if (Test-Path $dir) {
        $desktopFiles = Get-ChildItem -Path $dir -Include "*.cs","*.xaml" -Recurse -ErrorAction SilentlyContinue
        foreach ($file in $desktopFiles) {
            $content = Get-Content -Path $file.FullName -Raw -ErrorAction SilentlyContinue
            if ($content -match $emojiPattern) {
                $newContent = $content -replace $emojiPattern, ''
                Set-Content -Path $file.FullName -Value $newContent -NoNewline
                $totalFiles++
                $changedFiles += $file.FullName
                Write-Host "  Cleaned: $($file.Name)" -ForegroundColor Yellow
            }
        }
    }
}

Write-Host "`n=== Summary ===" -ForegroundColor Cyan
Write-Host "Total files cleaned: $totalFiles" -ForegroundColor Green
Write-Host "`nEmoji removal complete!" -ForegroundColor Green
