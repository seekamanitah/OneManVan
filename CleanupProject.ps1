# Project Cleanup Script
# Deletes all .md files except hvacWarranty.md and MobileUIDesignSystem.md
# Also cleans up git repository

param(
    [switch]$DryRun,
    [switch]$SkipGit
)

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  OneManVan Project Cleanup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if ($DryRun) {
    Write-Host "DRY RUN MODE - No files will be deleted" -ForegroundColor Yellow
    Write-Host ""
}

# Files to keep
$keepFiles = @(
    "hvacWarranty.md",
    "MobileUIDesignSystem.md",
    "README.md"  # Keep README if exists
)

# Find all .md files
Write-Host "[1/4] Finding .md files..." -ForegroundColor Yellow
$allMdFiles = Get-ChildItem -Path . -Filter "*.md" -File -Recurse | Where-Object {
    # Exclude node_modules, bin, obj, .vs folders
    $_.FullName -notmatch "\\node_modules\\" -and
    $_.FullName -notmatch "\\bin\\" -and
    $_.FullName -notmatch "\\obj\\" -and
    $_.FullName -notmatch "\\.vs\\" -and
    $_.FullName -notmatch "\\packages\\"
}

Write-Host "      Found $($allMdFiles.Count) .md files" -ForegroundColor Green
Write-Host ""

# Filter files to delete
$filesToDelete = $allMdFiles | Where-Object {
    $fileName = $_.Name
    $keepFiles -notcontains $fileName
}

Write-Host "[2/4] Files to delete: $($filesToDelete.Count)" -ForegroundColor Yellow
Write-Host ""

if ($filesToDelete.Count -eq 0) {
    Write-Host "No files to delete!" -ForegroundColor Green
    exit 0
}

# Show files to be deleted
Write-Host "Files that will be deleted:" -ForegroundColor Cyan
$filesToDelete | ForEach-Object {
    Write-Host "  - $($_.Name)" -ForegroundColor DarkGray
}
Write-Host ""

# Files to keep
Write-Host "Files that will be KEPT:" -ForegroundColor Green
$allMdFiles | Where-Object { $keepFiles -contains $_.Name } | ForEach-Object {
    Write-Host "  ? $($_.Name)" -ForegroundColor Green
}
Write-Host ""

if (-not $DryRun) {
    # Confirm deletion
    $confirmation = Read-Host "Delete $($filesToDelete.Count) files? (yes/no)"
    if ($confirmation -ne 'yes') {
        Write-Host "Cleanup cancelled" -ForegroundColor Yellow
        exit 0
    }
    Write-Host ""
}

# Delete files
Write-Host "[3/4] Deleting files..." -ForegroundColor Yellow
$deletedCount = 0
$errorCount = 0

foreach ($file in $filesToDelete) {
    try {
        if (-not $DryRun) {
            Remove-Item $file.FullName -Force
        }
        Write-Host "  Deleted: $($file.Name)" -ForegroundColor DarkGray
        $deletedCount++
    }
    catch {
        Write-Host "  ERROR: Could not delete $($file.Name): $($_.Exception.Message)" -ForegroundColor Red
        $errorCount++
    }
}

Write-Host ""
Write-Host "Deleted: $deletedCount files" -ForegroundColor Green
if ($errorCount -gt 0) {
    Write-Host "Errors: $errorCount files" -ForegroundColor Red
}
Write-Host ""

# Git cleanup
if (-not $SkipGit -and -not $DryRun) {
    Write-Host "[4/4] Cleaning up Git repository..." -ForegroundColor Yellow
    
    # Check if git repo exists
    if (Test-Path ".git") {
        try {
            # Stage deleted files
            Write-Host "  Staging deleted files..." -ForegroundColor DarkGray
            git add -A
            
            # Show status
            Write-Host ""
            Write-Host "Git status:" -ForegroundColor Cyan
            git status --short
            Write-Host ""
            
            # Commit
            $commitMsg = "docs: cleanup - removed $deletedCount documentation .md files"
            Write-Host "  Creating commit: $commitMsg" -ForegroundColor DarkGray
            git commit -m $commitMsg
            
            Write-Host ""
            Write-Host "Git cleanup complete!" -ForegroundColor Green
            Write-Host ""
            Write-Host "To push changes to remote:" -ForegroundColor Cyan
            Write-Host "  git push origin master" -ForegroundColor Yellow
        }
        catch {
            Write-Host "  Git error: $($_.Exception.Message)" -ForegroundColor Red
            Write-Host "  You may need to commit manually" -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "  No .git directory found - skipping git cleanup" -ForegroundColor Yellow
    }
}
else {
    Write-Host "[4/4] Skipping git cleanup" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Cleanup Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

# Summary
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Files deleted:  $deletedCount" -ForegroundColor White
Write-Host "  Files kept:     $($keepFiles.Count)" -ForegroundColor White
Write-Host "  Errors:         $errorCount" -ForegroundColor White
Write-Host ""

# Show kept files
Write-Host "Kept files:" -ForegroundColor Cyan
Get-ChildItem -Path . -Filter "*.md" -File | Where-Object {
    $_.FullName -notmatch "\\node_modules\\" -and
    $_.FullName -notmatch "\\bin\\" -and
    $_.FullName -notmatch "\\obj\\"
} | ForEach-Object {
    Write-Host "  ? $($_.Name)" -ForegroundColor Green
}
Write-Host ""
