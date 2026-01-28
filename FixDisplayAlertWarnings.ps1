# Fix all DisplayAlert and DisplayActionSheet obsolete warnings
# This script replaces obsolete MAUI dialog methods with their Async versions

$files = @(
    "OneManVan.Mobile\Extensions\PageExtensions.cs",
    "OneManVan.Mobile\Pages\AddCompanyPage.xaml.cs",
    "OneManVan.Mobile\Pages\AddAssetPage.xaml.cs",
    "OneManVan.Mobile\Pages\AddInvoicePage.xaml.cs",
    "OneManVan.Mobile\Services\LineItemDialogService.cs",
    "OneManVan.Mobile\Pages\CompanyDetailPage.xaml.cs",
    "OneManVan.Mobile\Pages\CompanyListPage.xaml.cs",
    "OneManVan.Mobile\Pages\EditCompanyPage.xaml.cs",
    "OneManVan.Mobile\Pages\SetupWizardPage.xaml.cs",
    "OneManVan.Mobile\Pages\EditCustomerPage.xaml.cs",
    "OneManVan.Mobile\Pages\EditAssetPage.xaml.cs",
    "OneManVan.Mobile\Pages\EnhancedSchemaEditorPage.xaml.cs",
    "OneManVan.Mobile\Pages\SettingsPage.xaml.cs"
)

$replacements = @{
    "\.DisplayAlert\(" = ".DisplayAlertAsync("
    "\.DisplayActionSheet\(" = ".DisplayActionSheetAsync("
}

$totalReplacements = 0

foreach ($file in $files) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        $originalContent = $content
        
        foreach ($pattern in $replacements.Keys) {
            $replacement = $replacements[$pattern]
            $matches = [regex]::Matches($content, $pattern)
            if ($matches.Count -gt 0) {
                Write-Host "$file : Found $($matches.Count) occurrences of $pattern"
                $content = $content -replace $pattern, $replacement
                $totalReplacements += $matches.Count
            }
        }
        
        if ($content -ne $originalContent) {
            Set-Content -Path $file -Value $content -NoNewline
            Write-Host "  Updated $file" -ForegroundColor Green
        }
    } else {
        Write-Host "File not found: $file" -ForegroundColor Yellow
    }
}

Write-Host "`nTotal replacements made: $totalReplacements" -ForegroundColor Cyan
Write-Host "All DisplayAlert ? DisplayAlertAsync" -ForegroundColor Cyan
Write-Host "All DisplayActionSheet ? DisplayActionSheetAsync" -ForegroundColor Cyan
