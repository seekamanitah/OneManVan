# ?? OneManVan Database Reset Script
# This script backs up and deletes the old database
# so Entity Framework can create a new one with the correct schema

Write-Host "`n?? OneManVan Database Reset Utility`n" -ForegroundColor Cyan

# Configuration
$dbFolder = "$env:LOCALAPPDATA\OneManVan"
$dbPath = "$dbFolder\OneManVan.db"
$timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$backupPath = "$dbFolder\OneManVan_backup_$timestamp.db"

# Check if database exists
if (-not (Test-Path $dbPath)) {
    Write-Host "? No existing database found. A new one will be created on next run." -ForegroundColor Green
    Write-Host "`n   Database will be created at: $dbPath`n"
    exit 0
}

# Show current database info
Write-Host "?? Current Database:" -ForegroundColor Yellow
Write-Host "   Location: $dbPath"
$dbInfo = Get-Item $dbPath
Write-Host "   Size: $([math]::Round($dbInfo.Length / 1MB, 2)) MB"
Write-Host "   Created: $($dbInfo.CreationTime)"
Write-Host "   Modified: $($dbInfo.LastWriteTime)`n"

# Confirm action
Write-Host "??  This will:" -ForegroundColor Red
Write-Host "   1. Backup current database to: $backupPath"
Write-Host "   2. Delete current database files"
Write-Host "   3. New database will be created when you restart OneManVan.exe`n"

$confirm = Read-Host "Do you want to continue? (yes/no)"

if ($confirm -ne "yes") {
    Write-Host "`n? Operation cancelled." -ForegroundColor Yellow
    exit 0
}

# Step 1: Check if OneManVan is running
Write-Host "`n?? Checking if OneManVan.exe is running..."
$process = Get-Process -Name "OneManVan" -ErrorAction SilentlyContinue

if ($process) {
    Write-Host "??  OneManVan.exe is currently running!" -ForegroundColor Red
    Write-Host "   Please close the application first.`n"
    
    $killConfirm = Read-Host "Do you want to force close it? (yes/no)"
    if ($killConfirm -eq "yes") {
        Stop-Process -Name "OneManVan" -Force
        Start-Sleep -Seconds 2
        Write-Host "? Process terminated`n" -ForegroundColor Green
    } else {
        Write-Host "`n? Please close OneManVan.exe manually and run this script again." -ForegroundColor Yellow
        exit 1
    }
}

# Step 2: Backup database
Write-Host "?? Creating backup..."
try {
    Copy-Item $dbPath $backupPath -ErrorAction Stop
    Write-Host "? Backup created: $backupPath`n" -ForegroundColor Green
} catch {
    Write-Host "? Failed to create backup: $_" -ForegroundColor Red
    exit 1
}

# Step 3: Delete database files
Write-Host "???  Deleting database files..."
try {
    # Delete main database and associated files
    Remove-Item "$dbFolder\OneManVan.db*" -Force -ErrorAction Stop
    Write-Host "? Database files deleted`n" -ForegroundColor Green
} catch {
    Write-Host "? Failed to delete database: $_" -ForegroundColor Red
    Write-Host "   You may need to run this script as Administrator.`n"
    exit 1
}

# Success summary
Write-Host "`n??????????????????????????????????????????????????????" -ForegroundColor Green
Write-Host "?                                                    ?" -ForegroundColor Green
Write-Host "?  ? Database Reset Complete!                       ?" -ForegroundColor Green
Write-Host "?                                                    ?" -ForegroundColor Green
Write-Host "??????????????????????????????????????????????????????" -ForegroundColor Green

Write-Host "`n?? Next Steps:" -ForegroundColor Cyan
Write-Host "   1. Start OneManVan.exe"
Write-Host "   2. Database will be created automatically"
Write-Host "   3. Go to Settings ? Test Runner ? 'Seed Test Data' (optional)"
Write-Host "   4. Verify all pages load correctly`n"

Write-Host "?? Backup Location:" -ForegroundColor Cyan
Write-Host "   $backupPath`n"

Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
