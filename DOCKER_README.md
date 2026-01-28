# ?? OneManVan Docker Deployment Package

Complete Docker deployment solution for deploying OneManVan Web UI to a Proxmox LXC container.

## ?? What's Included

- ? Complete Web application source code
- ? Multi-stage Dockerfile for .NET 10
- ? Docker Compose configuration (SQL Server + Web UI)
- ? Automated deployment scripts
- ? SQL Server initialization scripts
- ? Comprehensive documentation

## ?? Quick Start (3 Steps)

### Step 1: Create Deployment Package
```powershell
# On your Windows machine, from solution root:
.\create-deployment-package.ps1
```

### Step 2: Transfer to Server
```powershell
# Using SCP
scp deployment.zip root@192.168.100.107:/root/

# Or use WinSCP, FileZilla, Proxmox console
```

### Step 3: Deploy
```bash
# SSH into your server
ssh root@192.168.100.107

# One-line deployment
cd /root && unzip deployment.zip -d /opt/onemanvan && cd /opt/onemanvan && chmod +x deploy-to-docker.sh && ./deploy-to-docker.sh
```

## ?? Access

After deployment:
- **Web UI**: http://192.168.100.107:5000
- **SQL Server**: 192.168.100.107:1433

Default credentials:
- **Web Admin**: admin@onemanvan.local / Admin123!
- **SQL Server**: sa / TradeFlow2025!

## ?? Documentation

Comprehensive guides included:

1. **[DOCKER_DEPLOYMENT_SUMMARY.md](DOCKER_DEPLOYMENT_SUMMARY.md)**
   - Complete overview
   - Management commands
   - Troubleshooting

2. **[DEPLOYMENT_INSTRUCTIONS.md](DEPLOYMENT_INSTRUCTIONS.md)**
   - Detailed step-by-step guide
   - Configuration options
   - Security recommendations

3. **[DEPLOYMENT_QUICK_REFERENCE.md](DEPLOYMENT_QUICK_REFERENCE.md)**
   - Quick command reference
   - Common tasks
   - Troubleshooting tips

4. **[DOCKER_DEPLOYMENT_VISUAL_GUIDE.md](DOCKER_DEPLOYMENT_VISUAL_GUIDE.md)**
   - Visual workflow diagrams
   - Architecture overview
   - Data flow charts

5. **[DATABASE_CONFIG_FEATURE.md](DATABASE_CONFIG_FEATURE.md)**
   - Database configuration UI
   - Settings management

6. **[DOCKER_WEBUI_SETUP.md](DOCKER_WEBUI_SETUP.md)**
   - Alternative setup methods
   - Local development setup

## ??? Architecture

```
???????????????????????????????????????
?  Proxmox LXC (192.168.100.107)     ?
???????????????????????????????????????
?                                     ?
?  ?????????????????                 ?
?  ? tradeflow-db  ? Port 1433       ?
?  ? SQL Server    ???????           ?
?  ?????????????????     ?           ?
?                        ?           ?
?  ?????????????????     ?           ?
?  ?tradeflow-webui? Port 5000       ?
?  ? .NET 10 Web   ???????           ?
?  ?????????????????                 ?
?                                     ?
???????????????????????????????????????
```

## ?? Requirements

### Linux Server:
- Docker 20.10+ (script installs if missing)
- 2GB RAM minimum (4GB recommended)
- 10GB disk space minimum
- Ubuntu/Debian/CentOS/Alpine

### Windows Development Machine:
- PowerShell 5.1+
- Network access to server
- SCP client (optional, for file transfer)

## ??? Management

### View Status
```bash
cd /opt/onemanvan
docker-compose -f docker-compose-full.yml ps
```

### View Logs
```bash
docker-compose -f docker-compose-full.yml logs -f webui
```

### Restart
```bash
docker-compose -f docker-compose-full.yml restart
```

### Stop
```bash
docker-compose -f docker-compose-full.yml down
```

### Start
```bash
docker-compose -f docker-compose-full.yml up -d
```

## ?? Configuration

### Database Settings
Configure via Web UI:
1. Navigate to Settings page
2. Scroll to Database Configuration
3. Switch between SQLite and SQL Server
4. Test connection
5. Save and restart

### Environment Variables
Edit `docker-compose-full.yml`:
```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Production
  - ConnectionStrings__BusinessConnection=Server=sqlserver,1433;...
```

### Ports
Change exposed ports in `docker-compose-full.yml`:
```yaml
ports:
  - "5000:8080"  # Change left port number
```

## ?? Security

### For Production:
- ?? Change default passwords
- ?? Enable HTTPS
- ?? Configure firewall
- ?? Set up regular backups
- ?? Use secrets management
- ?? Enable SQL encryption

## ?? Troubleshooting

### Web UI won't start
```bash
docker logs tradeflow-webui
```

### SQL Server connection failed
```bash
docker logs tradeflow-db
# Wait 30-60 seconds for initialization
```

### Can't access from network
```bash
sudo ufw allow 5000/tcp
```

### Port already in use
Edit port in `docker-compose-full.yml`

## ?? Package Contents

When you run `create-deployment-package.ps1`, it creates `deployment.zip` containing:

```
deployment.zip/
??? OneManVan.Web/          # Web application
??? OneManVan.Shared/       # Shared libraries
??? docker/init/            # SQL scripts
??? docker-compose-full.yml # Docker config
??? deploy-to-docker.sh     # Deployment script
??? README.md               # Full instructions
??? QUICKSTART.txt          # Quick reference
```

## ?? Deployment Process

What happens when you run `deploy-to-docker.sh`:

1. ? Checks/installs Docker
2. ? Checks/installs Docker Compose
3. ? Creates deployment directory
4. ? Stops old containers
5. ? Builds new Web UI image
6. ? Pulls SQL Server image
7. ? Starts containers with health checks
8. ? Initializes databases
9. ? Displays access information

## ?? Resource Usage

- **Deployment Package**: ~5-10 MB compressed
- **Extracted**: ~50-100 MB source files
- **Running Containers**: ~2-3 GB (includes SQL Server)

## ?? Updates

To update the application:

1. Make changes on Windows
2. Run `create-deployment-package.ps1`
3. Transfer new `deployment.zip`
4. Extract to `/opt/onemanvan`
5. Run: `docker-compose -f docker-compose-full.yml up -d --build`

## ?? Backup

### Manual Backup
```bash
docker exec tradeflow-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "TradeFlow2025!" -Q "BACKUP DATABASE TradeFlowFSM TO DISK='/var/opt/mssql/backup/backup.bak'"
```

### Copy Backup to Host
```bash
docker cp tradeflow-db:/var/opt/mssql/backup/backup.bak ./
```

## ?? Learning Resources

- **[DOCKER_DEPLOYMENT_VISUAL_GUIDE.md](DOCKER_DEPLOYMENT_VISUAL_GUIDE.md)** - Visual diagrams
- **[DEPLOYMENT_INSTRUCTIONS.md](DEPLOYMENT_INSTRUCTIONS.md)** - Complete guide
- **Docker Docs**: https://docs.docker.com
- **ASP.NET Core Docker**: https://learn.microsoft.com/aspnet/core/host-and-deploy/docker

## ? Features

### Web Application:
- ? Full CRUD operations
- ? Customer management
- ? Invoice generation
- ? Job tracking
- ? Asset management
- ? PDF/Excel/CSV export
- ? Responsive design

### Docker Deployment:
- ? Isolated containers
- ? Persistent storage
- ? Health monitoring
- ? Automatic restarts
- ? Easy updates
- ? Network isolation

## ?? Support

For issues:
1. Check logs: `docker logs tradeflow-webui`
2. Review: [DEPLOYMENT_INSTRUCTIONS.md](DEPLOYMENT_INSTRUCTIONS.md)
3. See: [DEPLOYMENT_QUICK_REFERENCE.md](DEPLOYMENT_QUICK_REFERENCE.md)

## ?? Notes

- First deployment takes longer (building images)
- SQL Server needs 30-60s to initialize
- Default configs are for development/testing
- Change passwords before production use
- Web UI includes database configuration UI
- No manual connection string editing needed

## ?? Success!

After successful deployment:
1. ? Access http://192.168.100.107:5000
2. ? Login with admin@onemanvan.local / Admin123!
3. ? Configure trade settings
4. ? Add your first customer
5. ? Connect mobile app to 192.168.100.107:1433

---

**Ready to deploy?** Run `.\create-deployment-package.ps1` to get started!
