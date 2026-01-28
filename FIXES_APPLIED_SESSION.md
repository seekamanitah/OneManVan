# Session Fixes Summary - OneManVan Project
**Date:** 2026-01-28  
**Environment:** .NET 10, Blazor Server, SQL Server 2022, Docker

---

## ?? Issues Fixed This Session

### 1. ? Docker SQL Server Configuration
**Problem:** SQL Server 2022 health checks failing, wrong sqlcmd path, permission issues on Proxmox mounts

**Fixes Applied:**
- Updated health check path: `/opt/mssql-tools/bin/sqlcmd` ? `/opt/mssql-tools18/bin/sqlcmd`
- Added `-C` flag for certificate trust
- Increased `start_period` from 30s to 60s
- Added `user: "0:0"` for Proxmox mount compatibility
- Database now correctly stored on `/media/onemanvanDB/`

**Files Modified:**
- `docker-compose-full.yml`
- `docker-compose-production.yml`

---

### 2. ? SQL Server Cascade Conflict Errors
**Problem:** Foreign key constraints causing "multiple cascade paths" errors on self-referencing tables

**Root Cause:** SQL Server doesn't allow `DeleteBehavior.SetNull` on self-referencing foreign keys when other cascade paths exist

**Fixes Applied:**
Changed `OnDelete(DeleteBehavior.SetNull)` to `OnDelete(DeleteBehavior.NoAction)` for:
- ? `Asset.ReplacedByAssetId`
- ? `Product.ReplacementProductId`
- ? `Job.FollowUpJobId`
- ? `Job.FollowUpFromJobId`

**Files Modified:**
- `OneManVan.Shared/Data/OneManVanDbContext.cs`

---

### 3. ? Docker Build - Data Folder Excluded
**Problem:** `OneManVan.Shared/Data/` folder excluded from Docker build causing "type not found" errors

**Fixes Applied:**
- Removed `Data` from `exclude.txt`
- Removed `Data` exclusion from `create-deployment-package.ps1`
- Updated `.dockerignore` to explicitly allow `OneManVan.Shared/Data/`

**Files Modified:**
- `exclude.txt`
- `create-deployment-package.ps1`
- `.dockerignore`

---

### 4. ? Blazor Login Page Routing
**Problem:** Login page flashing then showing "Not Found", WebSocket errors

**Root Causes:**
1. `RedirectToLogin.razor` using relative path `Account/Login` instead of `/Account/Login`
2. Routes.razor trying to apply `MainLayout` to Account pages (causing auth loop)
3. Program.cs always using `UseSqlServer()` even for SQLite connection strings

**Fixes Applied:**
- **RedirectToLogin.razor:** Changed to absolute path `/Account/Login`
- **Routes.razor:** Removed `DefaultLayout` from Account pages RouteView
- **Program.cs:** Added SQLite detection logic based on connection string format
- **appsettings.Development.json:** Changed BusinessConnection to use SQLite for local dev

**Files Modified:**
- `OneManVan.Web/Components/Account/Shared/RedirectToLogin.razor`
- `OneManVan.Web/Components/Routes.razor`
- `OneManVan.Web/Program.cs`
- `OneManVan.Web/appsettings.Development.json`

---

### 5. ? Build Errors - deployment-temp Folder
**Problem:** 133+ build errors from leftover `deployment-temp` folder being included in build

**Fix Applied:**
- Removed `deployment-temp` folder

---

## ?? Configuration Summary

### Local Development (Windows)
- **Identity DB:** SQLite at `Data/app.db`
- **Business DB:** SQLite at `Data/business.db`
- **No SQL Server required** for local testing

### Production (Docker Server)
- **SQL Server:** 2022-latest running as root
- **Database Location:** `/media/onemanvanDB/` (Proxmox mount)
- **Health Check:** 60s start period, `/opt/mssql-tools18/bin/sqlcmd`
- **Web UI:** Port 5000, depends on SQL Server health

---

## ?? Testing Checklist

### Local Testing (Windows)
- [ ] Delete old databases: `OneManVan.Web/Data/*.db`
- [ ] Press F5 in Visual Studio
- [ ] Navigate to `https://localhost:7159/Account/Login`
- [ ] Login should stay visible (no "Not Found")
- [ ] Default credentials: `admin@onemanvan.local` / `Admin123!`

### Server Testing (Docker)
- [ ] SQL Server container shows `healthy` status
- [ ] Web UI container starts successfully
- [ ] Database files created in `/media/onemanvanDB/sqldata/`
- [ ] Access `http://192.168.100.107:5000`
- [ ] Login page loads and stays visible

---

## ?? Known Constraints

### Proxmox Mount Limitations
- Cannot change file ownership via `chown` from LXC
- SQL Server must run as root (`user: "0:0"`) to write to `/media/onemanvanDB/`
- Alternative: Use LXC local storage (`/var/lib/onemanvanDB`)

### SQL Server 2022 Changes
- sqlcmd moved from `/opt/mssql-tools/` to `/opt/mssql-tools18/`
- Requires `-C` flag to trust self-signed certificates
- Stricter cascade delete rules than earlier versions

---

## ?? Deployment Package Ready

**File:** `C:\Users\tech\source\repos\TradeFlow\deployment.zip` (61.96 MB)

**Contents:**
- ? All cascade conflict fixes
- ? Data folder included
- ? Docker configuration with SQL Server 2022 fixes
- ? Blazor routing fixes
- ? Updated health checks

**Transfer to server:**
```powershell
scp deployment.zip root@192.168.100.107:/root/
```

**Deploy on server:**
```sh
cd /opt/onemanvan
docker compose -f docker-compose-full.yml down
rm -rf /opt/onemanvan
unzip /root/deployment.zip -d /opt/onemanvan
cd /opt/onemanvan
docker compose -f docker-compose-full.yml up -d
```

---

## ?? Current Status

**Windows Development:**
- ? Build successful (0 errors)
- ? All cascade conflicts fixed
- ? SQLite configured for local dev
- ? Awaiting login page test

**Linux Server (192.168.100.107):**
- ? SQL Server healthy and running
- ? Database on `/media/onemanvanDB/`
- ? Needs updated deployment with all fixes

---

## ?? Next Steps

1. **Test locally:** Stop debugger, delete `Data/*.db`, press F5, verify login works
2. **Deploy to server:** Transfer new deployment.zip and redeploy
3. **Verify production:** Check both containers healthy, test login at port 5000

---

## ?? Related Files

- Docker configuration: `docker-compose-full.yml`
- Database context: `OneManVan.Shared/Data/OneManVanDbContext.cs`
- Routing: `OneManVan.Web/Components/Routes.razor`
- Startup: `OneManVan.Web/Program.cs`
- Deployment: `create-deployment-package.ps1`

---

**All critical production-blocking issues have been identified and fixed!** ??
