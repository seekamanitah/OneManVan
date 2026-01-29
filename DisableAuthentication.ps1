# Remove all [Authorize] attributes from Web UI pages
# Temporarily disable authentication for development

Write-Host "Removing [Authorize] attributes from all Web UI pages..." -ForegroundColor Cyan

$files = Get-ChildItem -Path "OneManVan.Web\Components\Pages" -Filter "*.razor" -Recurse

$count = 0
foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    
    if ($content -match '@attribute \[Authorize\]') {
        # Replace with commented version
        $newContent = $content -replace '@attribute \[Authorize\]', '@* [Authorize] temporarily disabled for development *@'
        
        Set-Content -Path $file.FullName -Value $newContent -NoNewline
        Write-Host "  [OK] $($file.Name)" -ForegroundColor Green
        $count++
    }
}

Write-Host ""
Write-Host "Removed [Authorize] from $count files" -ForegroundColor Green
Write-Host ""
Write-Host "Authentication is now DISABLED for development." -ForegroundColor Yellow
Write-Host "All pages are publicly accessible." -ForegroundColor Yellow
Write-Host ""
Write-Host "To re-enable authentication later:" -ForegroundColor Cyan
Write-Host "  1. Uncomment middleware in Program.cs" -ForegroundColor Gray
Write-Host "  2. Uncomment [Authorize] attributes in pages" -ForegroundColor Gray
Write-Host "  3. Uncomment MapAdditionalIdentityEndpoints()" -ForegroundColor Gray
