# OneManVan - Complete Docker Reset & Redeploy Guide

**Date:** 2025-01-29  
**Purpose:** Complete cleanup and fresh deployment to Docker server

---

## ?? What This Does

This deployment process will:
1. ? **Stop** all OneManVan/TradeFlow containers
2. ? **Remove** all containers
3. ? **Delete** all volumes (?? **DATABASE WILL BE LOST**)
4. ? **Remove** all images
5. ? **Clean** all networks
6. ? **Build** fresh images from latest code
7. ? **Deploy** new containers
8. ? **Initialize** database with fresh schema

---

## ?? IMPORTANT WARNINGS

### ? Data Loss
**ALL DATA WILL BE DELETED!** This includes:
- Customer records
- Jobs
- Invoices
- Estimates
- Assets
- Products
- All historical data

### ?? Backup First (If Needed)
If you want to preserve any data, **backup before running**:
```bash
# Backup database
docker exec tradeflow-sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P 'TradeFlow2025!' \
  -Q "BACKUP DATABASE TradeFlowFSM TO DISK = '/var/opt/mssql/backup/TradeFlowFSM.bak'"

# Copy backup out
docker cp tradeflow-sqlserver:/var/opt/mssql/backup/TradeFlowFSM.bak ./backup/
```

---

## ?? Deployment Methods

### Method 1: Automated Script (Linux/Mac)

```bash
# Make executable
chmod +x docker-complete-reset-deploy.sh

# Run script
./docker-complete-reset-deploy.sh
```

### Method 2: Automated Script (Windows)

```powershell
# Run PowerShell as Administrator
.\docker-complete-reset-deploy.ps1
```

### Method 3: Manual Step-by-Step

See "Manual Deployment Steps" section below.

---

## ?? Pre-Deployment Checklist

- [ ] **Docker is running**
- [ ] **You have the latest code** (`git pull`)
- [ ] **`.env` file is configured** (or use `.env.example`)
- [ ] **You understand all data will be deleted**
- [ ] **You have admin/sudo access**
- [ ] **Ports 7159 and 1433 are available**

---

## ?? Configuration Files Needed

### 1. `.env` File

Create `.env` with your configuration:

```env
# SQL Server Configuration
SA_PASSWORD=TradeFlow2025!
MSSQL_PID=Developer

# Application Configuration
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:8080
```

### 2. `docker-compose.yml`

Use the provided `docker-compose.yml` in the project root.

---

## ?? Manual Deployment Steps

### Step 1: Stop All Containers

```bash
# Stop OneManVan containers
docker ps -a --filter "name=onemanvan" --format "{{.Names}}" | xargs docker stop
docker ps -a --filter "name=tradeflow" --format "{{.Names}}" | xargs docker stop
```

### Step 2: Remove All Containers

```bash
docker ps -a --filter "name=onemanvan" --format "{{.Names}}" | xargs docker rm -f
docker ps -a --filter "name=tradeflow" --format "{{.Names}}" | xargs docker rm -f
```

### Step 3: Remove All Volumes (?? DATA DELETION)

```bash
docker volume ls --filter "name=onemanvan" --format "{{.Name}}" | xargs docker volume rm
docker volume ls --filter "name=tradeflow" --format "{{.Name}}" | xargs docker volume rm
```

### Step 4: Remove All Images

```bash
docker images --filter "reference=onemanvan*" --format "{{.Repository}}:{{.Tag}}" | xargs docker rmi -f
docker images --filter "reference=tradeflow*" --format "{{.Repository}}:{{.Tag}}" | xargs docker rmi -f
```

### Step 5: Remove Networks

```bash
docker network ls --filter "name=onemanvan" --format "{{.Name}}" | xargs docker network rm
docker network ls --filter "name=tradeflow" --format "{{.Name}}" | xargs docker network rm
```

### Step 6: Prune System

```bash
docker system prune -f
```

### Step 7: Build Fresh Images

```bash
docker compose build --no-cache
```

### Step 8: Start Services

```bash
docker compose up -d
```

### Step 9: Verify Deployment

```bash
# Check containers are running
docker ps

# Check logs
docker compose logs -f

# Test Web UI
curl http://localhost:7159/health
```

---

## ? Post-Deployment Verification

### 1. Check Container Status

```bash
docker ps
```

**Expected output:**
```
CONTAINER ID   IMAGE                    STATUS          PORTS
xxxxx          tradeflow-webui          Up 2 minutes    0.0.0.0:7159->8080/tcp
xxxxx          mcr.microsoft.com/mssql  Up 2 minutes    0.0.0.0:1433->1433/tcp
```

### 2. Check Logs

```bash
# Web UI logs
docker logs tradeflow-webui --tail 50

# SQL Server logs
docker logs tradeflow-sqlserver --tail 50
```

**Look for:**
- ? "Now listening on: http://[::]:8080"
- ? "Database initialization completed successfully"
- ? "Identity database initialized"
- ? "Business database initialized"

### 3. Test Web Access

**Open browser:**
- http://localhost:7159/

**Expected:**
- ? Dashboard loads
- ? Theme toggle works
- ? No errors in browser console

### 4. Test Database Connection

```bash
docker exec -it tradeflow-sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P 'TradeFlow2025!' \
  -Q "SELECT name FROM sys.databases"
```

**Expected databases:**
- `master`
- `TradeFlowFSM`
- `TradeFlowIdentity`

---

## ?? Troubleshooting

### Issue: Port Already in Use

```bash
# Find process using port 7159
lsof -i :7159
# or
netstat -ano | findstr :7159

# Kill process
kill -9 <PID>
```

### Issue: SQL Server Won't Start

```bash
# Check logs
docker logs tradeflow-sqlserver

# Remove SA_PASSWORD restriction (temp fix)
# Edit docker-compose.yml and change:
MSSQL_SA_PASSWORD=YourStrongPassword123!
```

### Issue: Web UI Shows 500 Error

```bash
# Check Web logs
docker logs tradeflow-webui --tail 100

# Common fixes:
# 1. Database not ready yet - wait 30 seconds
# 2. Connection string wrong - check .env
# 3. Migrations failed - check logs
```

### Issue: Cannot Connect to SQL Server

```bash
# Test connection
docker exec tradeflow-sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P 'TradeFlow2025!' -Q "SELECT 1"

# If fails, restart SQL Server
docker restart tradeflow-sqlserver
```

---

## ?? What Gets Created

### Docker Resources

| Resource | Name | Purpose |
|----------|------|---------|
| **Container** | `tradeflow-webui` | Blazor Web UI |
| **Container** | `tradeflow-sqlserver` | SQL Server 2022 |
| **Volume** | `tradeflow-sqldata` | SQL Server data |
| **Network** | `tradeflow-network` | Container networking |
| **Image** | `onemanvan-web:latest` | Web UI image |

### Databases

| Database | Purpose |
|----------|---------|
| `TradeFlowIdentity` | User authentication |
| `TradeFlowFSM` | Business data (Customers, Jobs, etc.) |

### Default Admin Account

| Field | Value |
|-------|-------|
| **Email** | `admin@onemanvan.local` |
| **Password** | `Admin123!` |

---

## ?? Common Operations

### View Logs

```bash
# All logs
docker compose logs -f

# Specific service
docker compose logs -f webui
docker compose logs -f sqlserver
```

### Restart Services

```bash
# Restart all
docker compose restart

# Restart one service
docker compose restart webui
```

### Stop Services

```bash
docker compose down
```

### Start Services (After Stop)

```bash
docker compose up -d
```

### Rebuild After Code Changes

```bash
docker compose down
docker compose build --no-cache
docker compose up -d
```

---

## ?? Files Included in Deployment

```
OneManVan/
??? docker-compose.yml                    # Main compose file
??? .env                                  # Environment variables
??? .env.example                          # Example env file
??? docker-complete-reset-deploy.sh       # Linux/Mac script
??? docker-complete-reset-deploy.ps1      # Windows script
??? OneManVan.Web/
?   ??? Dockerfile                        # Web UI Dockerfile
?   ??? (application files)
??? docker/
    ??? init/
        ??? 01-create-database.sql        # Database init
        ??? 02-seed-data.sql              # Seed data
```

---

## ?? Testing Checklist

After deployment, test:

- [ ] **Home page loads** (http://localhost:7159/)
- [ ] **Theme toggle works** (light/dark mode)
- [ ] **Dashboard displays metrics**
- [ ] **Navigation menu works**
- [ ] **Can create customer**
- [ ] **Can create job**
- [ ] **Can create invoice**
- [ ] **Calendar page loads**
- [ ] **Quick Notes work**
- [ ] **Settings page accessible**
- [ ] **No console errors**

---

## ?? Production Deployment Notes

### For Production Server:

1. **Change passwords** in `.env`
2. **Use production compose file**: `docker-compose-production.yml`
3. **Set ASPNETCORE_ENVIRONMENT=Production**
4. **Configure HTTPS** (reverse proxy)
5. **Set up backups** (automated)
6. **Configure monitoring**

### Security Checklist:

- [ ] Change `SA_PASSWORD`
- [ ] Change admin default password
- [ ] Enable HTTPS
- [ ] Configure firewall
- [ ] Set up SSL certificates
- [ ] Disable ports 1433 externally
- [ ] Use secrets management

---

## ?? Support & Next Steps

### After Successful Deployment:

1. ? **Test all features** (use testing checklist above)
2. ? **Create test data** (customers, jobs, etc.)
3. ? **Verify dark mode** on all pages
4. ? **Test mobile responsiveness**
5. ? **Document any issues**

### If Issues Occur:

1. **Check logs**: `docker compose logs -f`
2. **Verify configuration**: Review `.env` file
3. **Check ports**: Ensure 7159 and 1433 are free
4. **Restart services**: `docker compose restart`
5. **Full reset**: Run cleanup script again

---

## ?? Changelog

**Version:** Latest (2025-01-29)

**New Features:**
- ? Dark mode toggle
- ? Scheduled Jobs dashboard card
- ? Unscheduled Jobs dashboard card
- ? Quick Notes preview on dashboard
- ? Quick Notes page fully functional
- ? Calendar dark mode fixed
- ? Navigation icon alignment fixed

**Improvements:**
- ? Authentication temporarily disabled for development
- ? All pages work without login
- ? Full dark theme coverage
- ? Clean code (deleted unused files)

---

**Ready to deploy!** Run the script and your Docker server will have a fresh, clean installation of OneManVan with all the latest features! ??
