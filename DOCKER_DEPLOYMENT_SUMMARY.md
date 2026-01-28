# Docker Deployment Package - Summary

## What You Have Now

A complete Docker deployment solution for OneManVan Web application that can be deployed to your Proxmox LXC container at `192.168.100.107`.

## Files Created

### Docker Configuration:
1. **OneManVan.Web/Dockerfile** - Multi-stage .NET 10 Docker build
2. **docker-compose-full.yml** - Full stack deployment (SQL Server + Web UI)
3. **.dockerignore** - Build optimization

### Deployment Scripts:
4. **create-deployment-package.ps1** - PowerShell script to create deployment.zip
5. **create-deployment-package.bat** - Windows batch alternative
6. **deploy-to-docker.sh** - Automated Linux deployment script

### Documentation:
7. **DEPLOYMENT_INSTRUCTIONS.md** - Complete deployment guide
8. **DEPLOYMENT_QUICK_REFERENCE.md** - Quick reference card
9. **DATABASE_CONFIG_FEATURE.md** - Database configuration feature docs
10. **DOCKER_WEBUI_SETUP.md** - Initial Docker setup guide

### Helper Files:
11. **exclude.txt** - File exclusion list for batch script

## How to Deploy (3 Simple Steps)

### Step 1: Create Deployment Package (On Windows)
```powershell
# Option A: PowerShell (Recommended)
.\create-deployment-package.ps1

# Option B: Command Prompt
create-deployment-package.bat
```

This creates `deployment.zip` (~5-10 MB) containing everything needed.

### Step 2: Transfer to Linux Server
```powershell
# Using SCP (if available)
scp deployment.zip root@192.168.100.107:/root/

# Or use WinSCP, FileZilla, or Proxmox web console
```

### Step 3: Deploy on Linux Server
```bash
# SSH into server
ssh root@192.168.100.107

# One-line deployment
cd /root && unzip deployment.zip -d /opt/onemanvan && cd /opt/onemanvan && chmod +x deploy-to-docker.sh && ./deploy-to-docker.sh
```

## What Gets Deployed

### Containers:
- **tradeflow-db** - SQL Server 2022 Express (Port 1433)
- **tradeflow-webui** - OneManVan Web UI (Port 5000)

### Persistent Storage:
- **tradeflow-sqldata** - SQL Server data files
- **tradeflow-sqllogs** - SQL Server log files
- **tradeflow-backups** - SQL Server backups
- **tradeflow-webui-data** - Web UI configuration

### Network:
- **tradeflow-net** - Private Docker network for container communication

## Access Points

After deployment:

### Web Application:
- **LAN Access**: http://192.168.100.107:5000
- **Local Access**: http://localhost:5000 (from server)

### SQL Server:
- **Connection**: 192.168.100.107,1433
- **Username**: sa
- **Password**: TradeFlow2025!
- **Database**: TradeFlowFSM

### Default Web Admin:
- **Username**: admin@onemanvan.local
- **Password**: Admin123!

## Key Features

### Web UI Capabilities:
? Full database configuration UI in Settings
? Switch between SQLite and SQL Server from browser
? Test database connections before saving
? No manual connection string editing needed
? Automatic database initialization
? Docker health checks for reliability

### Docker Benefits:
? Isolated environment
? Easy updates (rebuild container)
? Persistent data storage
? Automatic restart on failure
? Network isolation and security
? Resource management

### Deployment Features:
? Single command deployment
? Automatic Docker installation
? Health monitoring
? Logging infrastructure
? Backup capabilities

## Management Commands

### View Status
```bash
cd /opt/onemanvan
docker-compose -f docker-compose-full.yml ps
```

### View Logs
```bash
# All services
docker-compose -f docker-compose-full.yml logs -f

# Web UI only
docker-compose -f docker-compose-full.yml logs -f webui

# SQL Server only
docker-compose -f docker-compose-full.yml logs -f sqlserver
```

### Restart Services
```bash
docker-compose -f docker-compose-full.yml restart
```

### Stop Services
```bash
docker-compose -f docker-compose-full.yml down
```

### Start Services
```bash
docker-compose -f docker-compose-full.yml up -d
```

### Update Application
```bash
# After uploading new code
docker-compose -f docker-compose-full.yml down
docker-compose -f docker-compose-full.yml up -d --build
```

## Troubleshooting

### Can't Connect to Web UI
1. Check if containers are running: `docker ps`
2. Check logs: `docker logs tradeflow-webui`
3. Verify firewall: `sudo ufw allow 5000/tcp`
4. Try from server: `curl http://localhost:5000`

### SQL Server Connection Issues
1. Check SQL container: `docker logs tradeflow-db`
2. Test connection: `docker exec -it tradeflow-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "TradeFlow2025!"`
3. Wait for initialization (takes 30-60 seconds on first start)

### Port Already in Use
Edit `docker-compose-full.yml` and change:
```yaml
ports:
  - "5000:8080"  # Change 5000 to another port like 8080
```

## Configuration Priority

The Web application uses database settings in this order:

1. **appsettings.json** (highest priority)
2. **Database Config UI** settings (/opt/onemanvan/AppData/database-config.json)
3. **Environment Variables** (from docker-compose)
4. **SQLite fallback** (default)

## Security Notes

### For Production:
1. Change SQL Server SA password
2. Change default admin password
3. Enable HTTPS
4. Configure firewall rules
5. Set up regular backups
6. Use secrets management
7. Enable SQL Server encryption

### Current Setup:
?? Default passwords (development only)
?? HTTP only (no HTTPS)
?? Ports exposed to LAN
?? SA account enabled

## Next Steps

1. ? Create deployment package
2. ? Transfer to server
3. ? Deploy containers
4. Test Web UI access
5. Configure trade settings
6. Add first customer
7. Test mobile app connection
8. Set up regular backups

## Package Size

The deployment.zip will be approximately:
- **5-10 MB** - Compressed source code
- **Extracts to**: ~50-100 MB (source files)
- **Running containers**: ~2-3 GB (including SQL Server)

## System Requirements

### Minimum:
- 2 GB RAM
- 2 CPU cores
- 10 GB disk space
- Linux kernel 3.10+
- Docker 20.10+

### Recommended:
- 4 GB RAM
- 4 CPU cores  
- 20 GB disk space
- SSD storage

## Additional Resources

All documentation included in deployment package:
- **README.md** - Full deployment instructions
- **QUICKSTART.txt** - Quick start guide
- Full source code for customization

## Support

Build Status: ? Web project compiles successfully

Note: Some Mobile and Migration SQL errors exist but don't affect Web deployment.

## Ready to Deploy?

Run this command to create your deployment package:
```powershell
.\create-deployment-package.ps1
```

Then follow the on-screen instructions!
