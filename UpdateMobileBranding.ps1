# Replace TradeFlow with OneManVan in Mobile project
Write-Host "Replacing TradeFlow with OneManVan in Mobile project..." -ForegroundColor Yellow

$mobilePath = "OneManVan.Mobile"
$mobileFiles = Get-ChildItem -Path $mobilePath -Recurse -Include *.xaml,*.cs -Exclude *obj*,*bin*

$count = 0
foreach ($file in $mobileFiles) {
    try {
        $content = Get-Content $file.FullName -Raw -ErrorAction Stop
        if ($content -match "TradeFlow") {
            $newContent = $content -replace "TradeFlow", "OneManVan"
            Set-Content -Path $file.FullName -Value $newContent -NoNewline
            Write-Host "? Updated: $($file.Name)" -ForegroundColor Green
            $count++
        }
    }
    catch {
        # Skip files we can't read
    }
}

Write-Host "`n? Replaced TradeFlow in $count Mobile files" -ForegroundColor Green
