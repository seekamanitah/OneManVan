# OneManVan Web - Docker Deployment Instructions

## Overview
This guide walks you through deploying the OneManVan Web application to your Proxmox LXC container running Docker at `192.168.100.107`.

## Prerequisites

### On Your Windows Machine:
- PowerShell 5.1 or higher
- Network access to 192.168.100.107

### On Your Linux Server (192.168.100.107):
- Proxmox LXC container running Linux (Ubuntu/Debian recommended)
- Docker and Docker Compose (script will install if missing)
- Minimum 2GB RAM, 10GB disk space
- SSH access enabled

## Quick Start

### Option 1: Automated Deployment (Recommended)

#### Step 1: Create Deployment Package on Windows
```powershell
# Run from the solution root directory
.\create-deployment-package.ps1
```

This creates `deployment.zip` containing:
- OneManVan.Web application files
- OneManVan.Shared library files
- Dockerfile
- docker-compose-full.yml
- Deployment scripts
- SQL initialization scripts

#### Step 2: Transfer to Linux Server
```powershell
# Using SCP (if available)
scp deployment.zip root@192.168.100.107:/root/

# OR using WinSCP, FileZilla, or similar GUI tool
# Upload deployment.zip to /root/ or /opt/
```

#### Step 3: Deploy on Linux Server
```bash
# SSH into your server
ssh root@192.168.100.107

# Navigate to upload location
cd /root

# Extract and run deployment
unzip deployment.zip -d /opt/onemanvan
cd /opt/onemanvan
chmod +x deploy-to-docker.sh
./deploy-to-docker.sh
```

The script will:
- Install Docker and Docker Compose (if needed)
- Build the Web application container
- Start SQL Server and Web UI containers
- Configure networking
- Display access information

### Option 2: Manual Deployment

#### Step 1: Prepare Server
```bash
# Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo systemctl enable docker
sudo systemctl start docker

# Install Docker Compose
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose
```

#### Step 2: Create Deployment Directory
```bash
sudo mkdir -p /opt/onemanvan
cd /opt/onemanvan
```

#### Step 3: Upload Files
Upload these files to `/opt/onemanvan/`:
- `deployment.zip` (contains all necessary files)

Or manually create the structure:
```
/opt/onemanvan/
??? OneManVan.Web/
?   ??? Dockerfile
?   ??? (all Web project files)
??? OneManVan.Shared/
?   ??? (all Shared project files)
??? docker-compose-full.yml
??? .dockerignore
??? docker/
    ??? init/
        ??? 01-create-database.sql
        ??? 02-seed-data.sql
        ??? 03-add-warranty-claims.sql
```

#### Step 4: Build and Run
```bash
cd /opt/onemanvan
docker-compose -f docker-compose-full.yml up -d --build
```

## Accessing the Application

After deployment, access the Web UI:

- **From Local Network:** `http://192.168.100.107:5000`
- **From the Server:** `http://localhost:5000`

Default admin credentials (created on first run):
- Username: `admin@onemanvan.local`
- Password: `Admin123!`

## Configuration

### Change SQL Server Password
Edit `docker-compose-full.yml`:
```yaml
environment:
  - MSSQL_SA_PASSWORD=YourNewPassword123!
```

Then update the Web UI connection string:
```yaml
- ConnectionStrings__BusinessConnection=Server=sqlserver,1433;Database=TradeFlowFSM;User Id=sa;Password=YourNewPassword123!;...
```

### Change Web UI Port
Edit `docker-compose-full.yml`:
```yaml
ports:
  - "8080:8080"  # Change 8080 (left) to your desired port
```

### Persist Data
Data is automatically persisted in Docker volumes:
- `tradeflow-sqldata` - Database files
- `tradeflow-webui-data` - Web app configuration and SQLite fallback

## Management Commands

### View Logs
```bash
# All services
docker-compose -f docker-compose-full.yml logs -f

# Web UI only
docker-compose -f docker-compose-full.yml logs -f webui

# SQL Server only
docker-compose -f docker-compose-full.yml logs -f sqlserver
```

### Stop Services
```bash
docker-compose -f docker-compose-full.yml down
```

### Start Services
```bash
docker-compose -f docker-compose-full.yml up -d
```

### Restart Services
```bash
docker-compose -f docker-compose-full.yml restart
```

### Update Application
```bash
# Stop services
docker-compose -f docker-compose-full.yml down

# Rebuild with new code
docker-compose -f docker-compose-full.yml up -d --build
```

### Backup Database
```bash
# Backup to file
docker exec tradeflow-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "TradeFlow2025!" -Q "BACKUP DATABASE TradeFlowFSM TO DISK='/var/opt/mssql/backup/TradeFlowFSM.bak'"

# Copy backup to host
docker cp tradeflow-db:/var/opt/mssql/backup/TradeFlowFSM.bak ./backup-$(date +%Y%m%d).bak
```

### Database Access
```bash
# Connect to SQL Server
docker exec -it tradeflow-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "TradeFlow2025!"
```

## Troubleshooting

### Web UI Won't Start
```bash
# Check logs
docker-compose -f docker-compose-full.yml logs webui

# Common issues:
# 1. SQL Server not ready - wait 30 seconds and check again
# 2. Port 5000 in use - change port in docker-compose-full.yml
```

### Can't Connect from Other Devices
```bash
# Check if firewall is blocking port 5000
sudo ufw status
sudo ufw allow 5000/tcp

# Or if using iptables
sudo iptables -A INPUT -p tcp --dport 5000 -j ACCEPT
```

### SQL Server Health Check Failing
```bash
# Check SQL Server logs
docker logs tradeflow-db

# Restart SQL Server container
docker restart tradeflow-db
```

### Container Out of Memory
```bash
# Check memory usage
docker stats

# Increase LXC container memory in Proxmox
# Or add memory limits to docker-compose-full.yml
```

## Security Recommendations

### For Production Use:

1. **Change Default Passwords**
   - SQL Server SA password
   - Admin account password

2. **Use HTTPS**
   - Configure SSL certificate
   - Update ASPNETCORE_URLS to use HTTPS

3. **Firewall Configuration**
   ```bash
   sudo ufw enable
   sudo ufw allow 22/tcp    # SSH
   sudo ufw allow 5000/tcp  # Web UI
   sudo ufw allow 1433/tcp  # SQL Server (if external access needed)
   ```

4. **Regular Backups**
   - Automate database backups
   - Backup Docker volumes

5. **Update Regularly**
   ```bash
   docker-compose -f docker-compose-full.yml pull
   docker-compose -f docker-compose-full.yml up -d
   ```

## Network Access

The deployment exposes:
- **Port 5000**: Web UI (HTTP)
- **Port 1433**: SQL Server (optional, can be internal only)

To access from other devices on your LAN:
- Web UI: `http://192.168.100.107:5000`
- Mobile app can connect to: `192.168.100.107:1433`

## File Structure After Deployment

```
/opt/onemanvan/
??? OneManVan.Web/
?   ??? Dockerfile
?   ??? OneManVan.Web.csproj
?   ??? Program.cs
?   ??? appsettings.json
?   ??? Components/...
??? OneManVan.Shared/
?   ??? OneManVan.Shared.csproj
?   ??? Data/
?   ??? Models/
?   ??? Services/
??? docker-compose-full.yml
??? .dockerignore
??? docker/
    ??? init/
        ??? 01-create-database.sql
        ??? 02-seed-data.sql
        ??? 03-add-warranty-claims.sql
```

## Support

For issues or questions:
1. Check logs: `docker-compose -f docker-compose-full.yml logs -f`
2. Verify containers are running: `docker ps`
3. Check server resources: `docker stats`

## Next Steps

After successful deployment:
1. Log in to Web UI: `http://192.168.100.107:5000`
2. Go to Settings page
3. Verify database connection
4. Configure trade type
5. Add your first customer
6. Configure mobile app to connect to `192.168.100.107:1433`
