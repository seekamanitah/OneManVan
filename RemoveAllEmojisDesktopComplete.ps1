# =====================================
# Complete Desktop Emoji Removal Script
# Removes all emojis and ?? placeholders
# =====================================

$projectRoot = "C:\Users\tech\source\repos\TradeFlow"
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "Desktop Emoji & Placeholder Removal Tool" -ForegroundColor Cyan
Write-Host "===============================================`n" -ForegroundColor Cyan

# Comprehensive emoji pattern (Unicode ranges for common emojis)
$emojiPatterns = @(
    # Placeholder patterns (? and ??)
    '\?\?+',
    # Common emoji Unicode ranges
    '[\u{1F300}-\u{1F9FF}]',  # Misc Symbols and Pictographs + Supplemental
    '[\u{2600}-\u{27BF}]',    # Misc Symbols
    '[\u{1F600}-\u{1F64F}]',  # Emoticons
    '[\u{1F680}-\u{1F6FF}]',  # Transport and Map
    '[\u{2700}-\u{27BF}]',    # Dingbats
    '[\u{1F1E0}-\u{1F1FF}]',  # Flags
    '[\u{1F900}-\u{1F9FF}]',  # Supplemental Symbols
    '[\u{2B50}\u{2B55}]',     # Star symbols
    '[\u{231A}\u{231B}]',     # Watch/Clock
    '[\u{23E9}-\u{23FF}]',    # Media symbols
    '[\u{25AA}-\u{25FF}]',    # Geometric shapes
    '[\u{2934}\u{2935}]',     # Arrows
    '[\u{3030}\u{303D}]',     # Wave/Part alternation
    '[\u{3297}\u{3299}]',     # Circled ideographs
    # Specific common ones that might not be caught
    '[???????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????]'
)

$fileTypes = @("*.xaml", "*.cs", "*.md")
$totalFilesProcessed = 0
$totalFilesChanged = 0
$excludePaths = @('obj', 'bin', 'packages', 'node_modules', '.vs', '.git')

Write-Host "Scanning files..." -ForegroundColor Yellow

foreach ($ext in $fileTypes)
{
    Write-Host "`nProcessing $ext files..." -ForegroundColor Cyan
    
    $files = Get-ChildItem -Path $projectRoot -Filter $ext -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object { 
            $file = $_
            $exclude = $false
            foreach ($excludePath in $excludePaths) {
                if ($file.FullName -match [regex]::Escape("\$excludePath\")) {
                    $exclude = $true
                    break
                }
            }
            -not $exclude
        }
    
    foreach ($file in $files)
    {
        $totalFilesProcessed++
        
        try {
            $content = Get-Content $file.FullName -Raw -Encoding UTF8 -ErrorAction Stop
            if ($null -eq $content) { continue }
            
            $original = $content
            $changed = $false
            
            # Check for each emoji pattern
            foreach ($pattern in $emojiPatterns)
            {
                if ($content -match $pattern)
                {
                    # For ?? placeholders, just remove
                    if ($pattern -match '^\\\?')
                    {
                        $content = $content -replace $pattern, ''
                        $changed = $true
                    }
                    # For actual emojis, replace with space to avoid joining words
                    elseif ($content -match $pattern)
                    {
                        $content = $content -replace $pattern, ' '
                        $changed = $true
                    }
                }
            }
            
            # Clean up extra whitespace created by removals
            $content = $content -replace '\s{3,}', '  '  # Max 2 spaces
            $content = $content -replace '(?<=\n)\s+(?=\n)', ''  # Remove whitespace-only lines
            $content = $content -replace ' +\n', "`n"  # Remove trailing spaces
            
            if ($changed -and $content -ne $original)
            {
                # Backup original file
                $backupPath = $file.FullName + ".bak"
                Copy-Item $file.FullName $backupPath -Force
                
                # Write cleaned content
                Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -NoNewline
                
                Write-Host "  Fixed: $($file.Name)" -ForegroundColor Green
                $totalFilesChanged++
                
                # Remove backup if successful
                Remove-Item $backupPath -Force
            }
        }
        catch {
            Write-Host "  Error processing $($file.Name): $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

Write-Host "`n===============================================" -ForegroundColor Cyan
Write-Host "Emoji Removal Complete!" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "Files Processed: $totalFilesProcessed" -ForegroundColor White
Write-Host "Files Modified:  $totalFilesChanged" -ForegroundColor Green
Write-Host "`nAll emojis and ?? placeholders removed!" -ForegroundColor Green
