# Fix remaining build warnings manually

# 1. Fix AssetListPage - add 'new' keyword
$file = "OneManVan.Mobile\Pages\AssetListPage.xaml.cs"
$content = Get-Content $file -Raw
$content = $content -replace 'public int Count =>', 'public new int Count =>'
Set-Content -Path $file -Value $content -NoNewline
Write-Host "Fixed AssetListPage - added 'new' keyword" -ForegroundColor Green

# 2. Fix MonthCalendarControl - suppress unused event warning
$file = "OneManVan.Mobile\Controls\MonthCalendarControl.xaml.cs"
$content = Get-Content $file -Raw
$content = $content -replace 'public event EventHandler<DateTime> DayLongPressed;', '#pragma warning disable CS0067`n    public event EventHandler<DateTime> DayLongPressed;`n#pragma warning restore CS0067'
Set-Content -Path $file -Value $content -NoNewline
Write-Host "Fixed MonthCalendarControl - suppressed unused event warning" -ForegroundColor Green

# 3. Fix AppShell - null check
$file = "OneManVan.Mobile\AppShell.xaml.cs"
$content = Get-Content $file -Raw
$oldPattern = '(\s+)var shell = sender as Shell;\s+shell\.FlyoutIsPresented = false;'
$newPattern = '$1var shell = sender as Shell;$1if (shell != null)$1{$1    shell.FlyoutIsPresented = false;$1}'
$content = $content -replace $oldPattern, $newPattern
Set-Content -Path $file -Value $content -NoNewline
Write-Host "Fixed AppShell - added null check" -ForegroundColor Green

# 4. Fix AddInvoicePage - null check
$file = "OneManVan.Mobile\Pages\AddInvoicePage.xaml.cs"
$content = Get-Content $file -Raw
$content = $content -replace 'CustomerNameLabel\.Text = customerName;', 'CustomerNameLabel.Text = customerName ?? "Unknown";'
Set-Content -Path $file -Value $content -NoNewline
Write-Host "Fixed AddInvoicePage - added null coalescing" -ForegroundColor Green

Write-Host "`nAll manual fixes applied!" -ForegroundColor Cyan
