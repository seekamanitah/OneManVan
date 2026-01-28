# Replace TradeFlow with OneManVan in Web project
Write-Host "Replacing TradeFlow with OneManVan in Web project..." -ForegroundColor Yellow

$webPath = "OneManVan.Web"
$files = Get-ChildItem -Path $webPath -Recurse -Include *.razor,*.cs,*.html,*.json -Exclude *obj*,*bin*

$count = 0
foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    if ($content -match "TradeFlow") {
        $newContent = $content -replace "TradeFlow", "OneManVan"
        Set-Content -Path $file.FullName -Value $newContent -NoNewline
        Write-Host "? Updated: $($file.Name)" -ForegroundColor Green
        $count++
    }
}

Write-Host "`n? Replaced TradeFlow in $count files" -ForegroundColor Green
