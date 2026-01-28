# Quick fix for Mobile SettingsPage CSV methods
$file = "OneManVan.Mobile\Pages\SettingsPage.xaml.cs"

$content = Get-Content $file -Raw

# Comment out ShareCsvAsync calls
$content = $content -replace '(\s+)(await _csvService\.ShareCsvAsync)', '$1// TODO: Re-implement file sharing$1// $2'

# Comment out ImportAllDataAsync
$content = $content -replace '(\s+)(var result = await _csvService\.ImportAllDataAsync)', '$1// TODO: Re-implement ImportAllDataAsync$1// $2'

# Comment out ExportJobsAsync  
$content = $content -replace '(\s+)(var result = await _csvService\.ExportJobsAsync)', '$1// TODO: Re-implement ExportJobsAsync$1// $2'

# Comment out GetExportedFilesAsync
$content = $content -replace '(\s+)(var files = await _csvService\.GetExportedFilesAsync)', '$1// TODO: Re-implement GetExportedFilesAsync$1var files = new List<string>(); // $2'

Set-Content $file $content -NoNewline

Write-Host "Mobile SettingsPage CSV methods commented out" -ForegroundColor Green
