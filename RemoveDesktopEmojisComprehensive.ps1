# Remove all emoji HTML entities from Desktop WPF XAML files
# Replaces emoji codes with descriptive text or removes them entirely

$files = Get-ChildItem -Path "Pages","Dialogs" -Include *.xaml -Recurse -ErrorAction SilentlyContinue

$emojiMap = @{
    '&#x1F4D6;' = '' # Book emoji
    '&#x1F4B3;' = '' # Credit card
    '&#x1F4A1;' = '' # Light bulb
    '&#x1F4C5;' = '' # Calendar
    '&#x1F527;' = '' # Wrench/tools
    '&#x1F464;' = '' # Person silhouette
    '&#x1F465;' = '' # People
    '&#x1F4CD;' = '' # Pin/location
    '&#x1F50D;' = '' # Magnifying glass
    '&#x1F4DE;' = 'Call' # Phone
    '&#x1F4E7;' = 'Email' # Email
    '&#x2022;' = '•' # Bullet point (keep this one)
}

$totalReplaced = 0

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content
    
    foreach ($emoji in $emojiMap.Keys) {
        $replacement = $emojiMap[$emoji]
        
        if ($emoji -eq '&#x2022;') {
            # Keep bullet points as-is
            continue
        }
        
        # Count occurrences
        $matches = [regex]::Matches($content, [regex]::Escape($emoji))
        if ($matches.Count -gt 0) {
            Write-Host "$($file.Name): Found $($matches.Count) occurrences of $emoji"
            
            # Special handling for specific patterns
            if ($replacement -eq 'Call' -or $replacement -eq 'Email') {
                # Replace button content emojis with text
                $content = $content -replace "Content=`"$([regex]::Escape($emoji))`"", "Content=`"$replacement`""
            } elseif ($emoji -eq '&#x1F50D;') {
                # Search icon - remove it and extra space
                $content = $content -replace "$([regex]::Escape($emoji))\s*", ""
            } else {
                # For decorative emojis in TextBlocks, remove the entire TextBlock if it's standalone
                # Or just remove the emoji if it's inline
                $content = $content -replace "<TextBlock\s+Text=`"$([regex]::Escape($emoji))`"[^>]*/>", ""
                $content = $content -replace "$([regex]::Escape($emoji))", $replacement
            }
            
            $totalReplaced += $matches.Count
        }
    }
    
    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        Write-Host "  ? Updated $($file.Name)" -ForegroundColor Green
    }
}

Write-Host "`n=== Summary ===" -ForegroundColor Cyan
Write-Host "Total emoji occurrences replaced: $totalReplaced" -ForegroundColor Green
Write-Host "Desktop app is now emoji-free!" -ForegroundColor Green
