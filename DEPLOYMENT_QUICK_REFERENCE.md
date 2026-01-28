# OneManVan Docker Deployment - Quick Reference

## Package Contents

This deployment package includes everything needed to run OneManVan Web on Docker:

### Files Included:
- **OneManVan.Web/** - Complete Web application source code
- **OneManVan.Shared/** - Shared business logic and data models
- **docker-compose-full.yml** - Docker Compose configuration
- **deploy-to-docker.sh** - Automated Linux deployment script
- **OneManVan.Web/Dockerfile** - Multi-stage Docker build configuration
- **.dockerignore** - Docker build exclusions
- **docker/init/** - SQL Server initialization scripts
- **.env** - Environment configuration

## Transfer Methods

### Method 1: SCP (Secure Copy)
```bash
# From Windows PowerShell
scp deployment.zip root@192.168.100.107:/root/

# Or if you have a specific user
scp deployment.zip username@192.168.100.107:/home/username/
```

### Method 2: WinSCP (GUI Tool)
1. Download WinSCP: https://winscp.net/
2. Connect to 192.168.100.107
3. Drag and drop deployment.zip to `/root/` or `/opt/`

### Method 3: FileZilla (SFTP)
1. Download FileZilla: https://filezilla-project.org/
2. Connect via SFTP to 192.168.100.107
3. Upload deployment.zip

### Method 4: Proxmox Web Interface
1. Log into Proxmox web UI
2. Select your LXC container
3. Go to Console
4. Use `wget` or `curl` to download from a web server
   ```bash
   # If you host the file on a local web server
   wget http://your-windows-ip/deployment.zip
   ```

## Installation Steps (Copy & Paste)

### Complete Installation (One Command Block)
```bash
cd /root && \
unzip deployment.zip -d /opt/onemanvan && \
cd /opt/onemanvan && \
chmod +x deploy-to-docker.sh && \
./deploy-to-docker.sh
```

### Step-by-Step Installation
```bash
# 1. Navigate to home directory
cd /root

# 2. Extract deployment package
unzip deployment.zip -d /opt/onemanvan

# 3. Navigate to installation directory
cd /opt/onemanvan

# 4. Make deployment script executable
chmod +x deploy-to-docker.sh

# 5. Run deployment
./deploy-to-docker.sh
```

## Post-Installation

### Access the Application
- **Web UI**: http://192.168.100.107:5000
- **SQL Server**: 192.168.100.107:1433

### Default Credentials
- **Username**: admin@onemanvan.local
- **Password**: Admin123!

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
```

### Stop/Start Services
```bash
# Stop
docker-compose -f docker-compose-full.yml down

# Start
docker-compose -f docker-compose-full.yml up -d

# Restart
docker-compose -f docker-compose-full.yml restart
```

## Troubleshooting

### Port Already in Use
```bash
# Change port 5000 to something else in docker-compose-full.yml
# Edit the file
nano /opt/onemanvan/docker-compose-full.yml

# Find the line:
#   - "5000:8080"
# Change to:
#   - "8080:8080"  # or any available port

# Restart
docker-compose -f docker-compose-full.yml up -d
```

### Can't Access from Network
```bash
# Check firewall
sudo ufw status
sudo ufw allow 5000/tcp

# Or disable firewall temporarily for testing
sudo ufw disable
```

### Container Won't Start
```bash
# Check Docker is running
sudo systemctl status docker

# Start Docker if needed
sudo systemctl start docker

# Check logs for errors
docker logs tradeflow-webui
```

## Configuration Changes

### Change SQL Server Password
Edit `/opt/onemanvan/docker-compose-full.yml`:
```yaml
# Find and change this line:
- MSSQL_SA_PASSWORD=TradeFlow2025!
# To:
- MSSQL_SA_PASSWORD=YourNewPassword!

# Also update the connection string:
- ConnectionStrings__BusinessConnection=Server=sqlserver,1433;Database=TradeFlowFSM;User Id=sa;Password=YourNewPassword!;...
```

Then restart:
```bash
docker-compose -f docker-compose-full.yml down
docker-compose -f docker-compose-full.yml up -d
```

## System Requirements

### Minimum:
- 2GB RAM
- 2 CPU cores
- 10GB disk space
- Linux kernel 3.10+

### Recommended:
- 4GB RAM
- 4 CPU cores
- 20GB disk space

### Required Ports:
- 5000 (Web UI)
- 1433 (SQL Server)

## Support

For detailed instructions, see:
- **DEPLOYMENT_INSTRUCTIONS.md** (included in package)
- Or check the original source repository

## Quick Commands Reference

```bash
# View container status
docker ps

# View all containers (including stopped)
docker ps -a

# Check resource usage
docker stats

# Remove everything and start fresh
docker-compose -f docker-compose-full.yml down -v
docker-compose -f docker-compose-full.yml up -d --build

# Backup database
docker exec tradeflow-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "TradeFlow2025!" -Q "BACKUP DATABASE TradeFlowFSM TO DISK='/var/opt/mssql/backup/backup.bak'"

# Access SQL Server
docker exec -it tradeflow-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "TradeFlow2025!"
```
