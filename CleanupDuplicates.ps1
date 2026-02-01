# Project Audit Cleanup Script
# Run this script to clean up identified duplications and optimize the project

Write-Host "=== OneManVan Project Cleanup ===" -ForegroundColor Cyan
Write-Host ""

$projectRoot = Split-Path -Parent $PSScriptRoot
if ($projectRoot -eq "") { $projectRoot = "." }

# 1. Remove the duplicate 'root@192.168.100.107' folder
$duplicateFolder = Join-Path $projectRoot "root@192.168.100.107"
if (Test-Path $duplicateFolder) {
    Write-Host "[1/3] Removing duplicate folder: root@192.168.100.107" -ForegroundColor Yellow
    Remove-Item -Path $duplicateFolder -Recurse -Force
    Write-Host "  - Removed duplicate deployment folder" -ForegroundColor Green
} else {
    Write-Host "[1/3] Duplicate folder not found (already clean)" -ForegroundColor Green
}

# 2. List files that may be duplicates between WPF Desktop and Web
Write-Host ""
Write-Host "[2/3] Checking for potential duplicate services..." -ForegroundColor Yellow

$desktopServices = Get-ChildItem -Path (Join-Path $projectRoot "Services") -Filter "*.cs" -ErrorAction SilentlyContinue
$webServices = Get-ChildItem -Path (Join-Path $projectRoot "OneManVan.Web\Services") -Filter "*.cs" -Recurse -ErrorAction SilentlyContinue

$duplicates = @()
foreach ($desktop in $desktopServices) {
    $webMatch = $webServices | Where-Object { $_.Name -eq $desktop.Name }
    if ($webMatch) {
        $duplicates += [PSCustomObject]@{
            Name = $desktop.Name
            DesktopPath = $desktop.FullName
            WebPath = $webMatch.FullName
        }
    }
}

if ($duplicates.Count -gt 0) {
    Write-Host "  Found $($duplicates.Count) potential duplicate service files:" -ForegroundColor Yellow
    foreach ($dup in $duplicates) {
        Write-Host "    - $($dup.Name)" -ForegroundColor Gray
    }
    Write-Host "  Note: These may be intentional platform-specific implementations." -ForegroundColor Gray
} else {
    Write-Host "  No duplicate service files found" -ForegroundColor Green
}

# 3. Report on database tables
Write-Host ""
Write-Host "[3/3] Database Context Summary" -ForegroundColor Yellow

$dbContextPath = Join-Path $projectRoot "OneManVan.Shared\Data\OneManVanDbContext.cs"
if (Test-Path $dbContextPath) {
    $content = Get-Content $dbContextPath -Raw
    $dbSetMatches = [regex]::Matches($content, 'public DbSet<(\w+)>')
    Write-Host "  Total DbSets (tables): $($dbSetMatches.Count)" -ForegroundColor Green
    
    # List categories
    $tables = $dbSetMatches | ForEach-Object { $_.Groups[1].Value }
    Write-Host "  Tables defined:" -ForegroundColor Gray
    foreach ($table in $tables) {
        Write-Host "    - $table" -ForegroundColor DarkGray
    }
}

Write-Host ""
Write-Host "=== Cleanup Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Additional manual steps recommended:" -ForegroundColor Yellow
Write-Host "  1. Review if Desktop 'Services/' folder files are still needed" -ForegroundColor Gray
Write-Host "  2. Consider consolidating shared services into OneManVan.Shared" -ForegroundColor Gray
Write-Host "  3. Run 'dotnet build' to verify no breaking changes" -ForegroundColor Gray
