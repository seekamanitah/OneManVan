# Repository Cleanup and Organization Script
# Cleans up temporary files and organizes the repository properly

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "OneManVan Repository Cleanup" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Update .gitignore
Write-Host "Step 1: Updating .gitignore..." -ForegroundColor Yellow

$gitignoreContent = @"
# Build results
[Dd]ebug/
[Dd]ebugPublic/
[Rr]elease/
[Rr]eleases/
x64/
x86/
[Ww][Ii][Nn]32/
[Aa][Rr][Mm]/
[Aa][Rr][Mm]64/
bld/
[Bb]in/
[Oo]bj/
[Ll]og/
[Ll]ogs/

# Visual Studio cache/options
.vs/
.vscode/

# User-specific files
*.rsuser
*.suo
*.user
*.userosscache
*.sln.docstates

# Database files
*.db
*.db-shm
*.db-wal
AppData/

# Environment files
.env
.env.local
.env.*.local
!.env.example
!.env.production.example

# Docker volumes
/media/

# Build outputs
out.txt
OneManVan.Web/out.txt

# Temporary and backup files
*.tmp
*.bak
*.backup
*.backup.*
*~

# Personal notes (don't commit personal info)
*employee info.txt
*employee records.txt
*material list guideline.txt
audit comands.md

# Diagnostic scripts (already archived)
diagnostic-scripts-archive/

# Generated documentation that's outdated
root@*/

# OS files
.DS_Store
Thumbs.db
"@

$gitignoreContent | Out-File -FilePath ".gitignore" -Encoding UTF8 -Force
Write-Host "  Updated .gitignore" -ForegroundColor Green

# Step 2: Remove files that shouldn't be committed
Write-Host ""
Write-Host "Step 2: Removing temporary/personal files..." -ForegroundColor Yellow

$filesToRemove = @(
    "OneManVan.Web/out.txt",
    "employee info.txt",
    "employee records.txt", 
    "audit comands.md"
)

foreach ($file in $filesToRemove) {
    if (Test-Path $file) {
        Remove-Item $file -Force
        Write-Host "  Removed: $file" -ForegroundColor Gray
    }
}

# Step 3: Organize documentation
Write-Host ""
Write-Host "Step 3: Organizing documentation..." -ForegroundColor Yellow

# Create docs folder if it doesn't exist
if (!(Test-Path "docs")) {
    New-Item -ItemType Directory -Path "docs" | Out-Null
}

# Move markdown documentation
$docsToMove = @{
    "BLAZOR_BUTTONS_FIX_RESOLVED.md" = "docs/fixes/"
    "BLAZOR_BUTTONS_NOT_WORKING_FIX.md" = "docs/fixes/"
    "DASHBOARD_CLICKABLE_CARDS.md" = "docs/features/"
    "DASHBOARD_ENHANCEMENT_SUMMARY.md" = "docs/features/"
    "DARK_MODE_IMPLEMENTATION.md" = "docs/features/"
    "EMPLOYEE_WORK_HISTORY_AUTO_TRACKING.md" = "docs/features/"
    "EXPORT_BUTTON_FIX.md" = "docs/fixes/"
    "INVOICE_EDIT_FIX.md" = "docs/fixes/"
    "INVOICE_INVENTORY_INTEGRATION.md" = "docs/features/"
    "INVOICE_PDF_PRIVACY_IMPLEMENTATION.md" = "docs/features/"
    "TAX_AND_LABOR_SETTINGS_ENHANCEMENT.md" = "docs/features/"
    "TAX_RATE_SETTINGS_FIX.md" = "docs/fixes/"
    "PROJECT_AUDIT_REPORT.md" = "docs/audits/"
    "QA-CODE-REVIEW-ISSUES.md" = "docs/audits/"
    "SECURITY_REMEDIATION_COMPLETE.md" = "docs/audits/"
    "SESSION_SUMMARY_FEATURES_AND_FIX.md" = "docs/summaries/"
    "SQL_SERVER_ERROR_FIX.md" = "docs/fixes/"
    "QUICK_REFERENCE.md" = "docs/"
}

foreach ($doc in $docsToMove.GetEnumerator()) {
    if (Test-Path $doc.Key) {
        $destDir = $doc.Value
        if (!(Test-Path $destDir)) {
            New-Item -ItemType Directory -Path $destDir -Force | Out-Null
        }
        Move-Item $doc.Key "$destDir" -Force
        Write-Host "  Moved: $($doc.Key) -> $destDir" -ForegroundColor Gray
    }
}

Write-Host "  Documentation organized into docs/ folder" -ForegroundColor Green

# Step 4: Show what will be committed
Write-Host ""
Write-Host "Step 4: Summary of changes to commit..." -ForegroundColor Yellow
Write-Host ""

git add -A

Write-Host "Files staged for commit:" -ForegroundColor Cyan
git status --short

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Ready to Commit!" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Review the changes above. If everything looks good, run:" -ForegroundColor White
Write-Host ""
Write-Host "  git commit -m 'Fix Blazor buttons issue and add new features'" -ForegroundColor Yellow
Write-Host "  git push origin master" -ForegroundColor Yellow
Write-Host ""
Write-Host "Or use the commit-and-push.ps1 script" -ForegroundColor White
Write-Host ""
