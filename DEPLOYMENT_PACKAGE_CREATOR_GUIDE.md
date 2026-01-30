# Deployment Package Creator - Quick Guide

## ?? Quick Start

### Option 1: Simple Menu (Easiest)

```powershell
.\Quick-Deploy.ps1
```

**Then choose:**
1. Folder only (fastest)
2. Folder + ZIP
3. Folder + ZIP + Git info

### Option 2: Direct Command

```powershell
# Basic (folder only)
.\Create-DeploymentPackage.ps1

# With ZIP file
.\Create-DeploymentPackage.ps1 -ZipPackage

# With ZIP + Git info
.\Create-DeploymentPackage.ps1 -ZipPackage -IncludeGit
```

---

## ?? What Gets Created

### Deployment Folder Contents

```
OneManVan-Deployment-2025-01-29-1234/
??? docker-compose.yml              # Docker configuration
??? docker-compose-production.yml   # Production config
??? .env                            # Environment variables
??? .env.example                    # Example env file
??? .dockerignore                   # Docker ignore rules
??? docker-complete-reset-deploy.sh # Linux deployment script
??? docker-complete-reset-deploy.ps1# Windows deployment script
??? DOCKER_COMPLETE_RESET_GUIDE.md  # Step-by-step guide
??? DEPLOYMENT_QUICK_START.md       # Quick reference
??? DEPLOYMENT_MANIFEST.txt         # Package info
??? OneManVan.Web/                  # Web application
?   ??? Components/
?   ??? wwwroot/
?   ??? Dockerfile
?   ??? ... (all source files)
??? OneManVan.Shared/               # Shared libraries
    ??? Models/
    ??? Services/
    ??? ... (all source files)
```

### What Gets Excluded (Automatically)

- ? `bin/` and `obj/` folders (build artifacts)
- ? `node_modules/` (npm packages)
- ? `.vs/` and `.vscode/` (IDE files)
- ? `TestResults/` (test outputs)
- ? `*.user`, `*.suo` files (user-specific)
- ? Published outputs

---

## ?? Common Usage Scenarios

### Scenario 1: Quick Deploy (Development)

```powershell
# Just create the folder
.\Quick-Deploy.ps1
# Choose option 1

# Copy to server using WinSCP or:
scp -r C:\Users\tech\Desktop\OneManVan-Deployment-* user@server:~/
```

### Scenario 2: Archive for Backup

```powershell
# Create ZIP for archiving
.\Create-DeploymentPackage.ps1 -ZipPackage

# Upload ZIP to server
scp C:\Users\tech\Desktop\OneManVan-Deployment-*.zip user@server:~/
```

### Scenario 3: Production Release

```powershell
# Create full package with version info
.\Create-DeploymentPackage.ps1 -ZipPackage -IncludeGit

# This creates:
# - Deployment folder
# - ZIP file
# - Git commit information
```

---

## ?? Script Parameters

### Create-DeploymentPackage.ps1

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `-ZipPackage` | Switch | `$false` | Create ZIP file of package |
| `-IncludeGit` | Switch | `$false` | Include Git commit info |

### Examples

```powershell
# Basic
.\Create-DeploymentPackage.ps1

# With ZIP
.\Create-DeploymentPackage.ps1 -ZipPackage

# Full package
.\Create-DeploymentPackage.ps1 -ZipPackage -IncludeGit
```

---

## ?? Package Sizes

Typical package sizes:

| Contents | Size (approx) |
|----------|---------------|
| **Folder only** | 50-80 MB |
| **ZIP file** | 15-25 MB (compressed) |
| **With Git info** | +1 KB |

---

## ?? Troubleshooting

### Problem: "Running scripts is disabled"

**Error:**
```
.\Create-DeploymentPackage.ps1 : File cannot be loaded because running scripts is disabled
```

**Solution:**
```powershell
# Run as Administrator
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# Then try again
.\Create-DeploymentPackage.ps1
```

### Problem: "Path too long"

**Error:**
```
Copy-Item : The specified path, file name, or both are too long
```

**Solution:**
- Windows has 260 character path limit
- Move project closer to drive root
- Example: `C:\TradeFlow` instead of `C:\Users\tech\source\repos\TradeFlow`

### Problem: Script hangs or slow

**Cause:** Large `node_modules/` or build artifacts

**Solution:**
- Script already excludes these
- If still slow, clean build artifacts first:
```powershell
# Clean all bin/obj folders
Get-ChildItem -Path . -Include bin,obj -Recurse -Force | Remove-Item -Recurse -Force
```

### Problem: Files missing from package

**Check:**
1. Are files in `.gitignore`?
2. Are files in excluded directories?
3. Run with verbose output:
```powershell
$VerbosePreference = "Continue"
.\Create-DeploymentPackage.ps1
```

---

## ?? Where Files Go

| File Type | Source | Destination |
|-----------|--------|-------------|
| **Config files** | Project root | Deployment folder root |
| **Scripts** | Project root | Deployment folder root |
| **Web app** | `OneManVan.Web/` | `Deployment/OneManVan.Web/` |
| **Shared libs** | `OneManVan.Shared/` | `Deployment/OneManVan.Shared/` |
| **Manifest** | Generated | `Deployment/DEPLOYMENT_MANIFEST.txt` |

---

## ?? After Package Creation

### 1. Verify Package

Check `DEPLOYMENT_MANIFEST.txt` for:
- ? Creation date
- ? File count
- ? Package size

### 2. Update Configuration

**Edit `.env` file:**
```env
# Change these before deployment!
SA_PASSWORD=YourStrongPassword123!
ASPNETCORE_ENVIRONMENT=Production
```

### 3. Transfer to Server

**Option A: WinSCP (GUI)**
1. Download: https://winscp.net/
2. Connect to server
3. Drag folder to server

**Option B: Command Line**
```powershell
# Upload folder
scp -r "C:\Users\tech\Desktop\OneManVan-Deployment-*" user@server:~/

# Or upload ZIP
scp "C:\Users\tech\Desktop\OneManVan-Deployment-*.zip" user@server:~/
# Then on server: unzip OneManVan-Deployment-*.zip
```

### 4. Deploy on Server

```bash
# SSH to server
ssh user@server

# Navigate to folder
cd ~/OneManVan-Deployment-*

# Make script executable
chmod +x docker-complete-reset-deploy.sh

# Run deployment
./docker-complete-reset-deploy.sh
```

---

## ?? Tips & Tricks

### Tip 1: Automation

Create a scheduled task to package nightly:
```powershell
# Save as batch file: nightly-package.bat
powershell.exe -ExecutionPolicy Bypass -File "C:\path\to\Create-DeploymentPackage.ps1" -ZipPackage
```

### Tip 2: Quick Deploy Alias

Add to PowerShell profile:
```powershell
# Add to: $PROFILE
function Deploy-OneManVan {
    Set-Location "C:\Users\tech\source\repos\TradeFlow"
    .\Create-DeploymentPackage.ps1 -ZipPackage
}

# Then use:
Deploy-OneManVan
```

### Tip 3: Compare Packages

```powershell
# See what changed between packages
$old = "OneManVan-Deployment-2025-01-28-1200"
$new = "OneManVan-Deployment-2025-01-29-1234"

Compare-Object `
    (Get-ChildItem $old -Recurse | Select-Object -ExpandProperty Name) `
    (Get-ChildItem $new -Recurse | Select-Object -ExpandProperty Name)
```

### Tip 4: Clean Old Packages

```powershell
# Remove packages older than 7 days
Get-ChildItem "$env:USERPROFILE\Desktop\OneManVan-Deployment-*" | 
    Where-Object { $_.CreationTime -lt (Get-Date).AddDays(-7) } | 
    Remove-Item -Recurse -Force
```

---

## ?? Best Practices

1. **Always review `.env`** before deploying
2. **Test on staging server** first if possible
3. **Keep old packages** for 7 days in case of rollback
4. **Document changes** in commit messages
5. **Backup production data** before deploying

---

## ?? Need Help?

1. **Check logs:** Look at script output carefully
2. **Verify files:** Open `DEPLOYMENT_MANIFEST.txt`
3. **Test locally:** Try `docker compose up` locally first
4. **Review guide:** Read `DOCKER_COMPLETE_RESET_GUIDE.md`

---

**Created with ?? for easy OneManVan deployments!**
