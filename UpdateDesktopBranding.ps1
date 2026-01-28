# Replace TradeFlow with OneManVan in Desktop project
Write-Host "Replacing TradeFlow with OneManVan in Desktop project..." -ForegroundColor Yellow

$desktopFiles = Get-ChildItem -Path "." -Recurse -Include *.xaml,*.cs -Exclude *OneManVan.Web*,*OneManVan.Mobile*,*obj*,*bin*

$count = 0
foreach ($file in $desktopFiles) {
    $content = Get-Content $file.FullName -Raw
    if ($content -match "TradeFlow") {
        $newContent = $content -replace "TradeFlow", "OneManVan"
        Set-Content -Path $file.FullName -Value $newContent -NoNewline
        Write-Host "? Updated: $($file.Name)" -ForegroundColor Green
        $count++
    }
}

Write-Host "`n? Replaced TradeFlow in $count Desktop files" -ForegroundColor Green
