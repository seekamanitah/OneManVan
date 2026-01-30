# OneManVan - Complete Docker Reset & Redeploy Guide
## Step-by-Step for Beginners

**Date:** 2025-01-29  
**Purpose:** Complete cleanup and fresh deployment to Docker server  
**Audience:** Beginners with little to no command line experience

---

## ?? Table of Contents

1. [What This Guide Does](#what-this-guide-does)
2. [Important Warnings](#important-warnings)
3. [What You Need Before Starting](#what-you-need)
4. [Part 1: Prepare Files on Your Computer](#part-1-prepare-files)
5. [Part 2: Upload Files to Server](#part-2-upload-files)
6. [Part 3: Connect to Your Server](#part-3-connect-to-server)
7. [Part 4: Delete Old Docker Deployment](#part-4-delete-old-deployment)
8. [Part 5: Deploy Fresh Installation](#part-5-deploy-fresh)
9. [Part 6: Verify Everything Works](#part-6-verify)
10. [Troubleshooting](#troubleshooting)

---

## ?? What This Guide Does {#what-this-guide-does}

This guide will help you:
1. ? **Create** a deployment folder on your computer
2. ? **Copy** necessary files to that folder
3. ? **Upload** files to your Docker server
4. ? **Delete** all old OneManVan/TradeFlow Docker containers
5. ? **Remove** all old images and volumes (?? **DATABASE DELETED**)
6. ? **Build** fresh Docker images from your latest code
7. ? **Start** new containers with clean database
8. ? **Test** that everything works

**Time needed:** 15-30 minutes

---

## ?? IMPORTANT WARNINGS {#important-warnings}

### ? ALL DATA WILL BE DELETED!

**This process will permanently delete:**
- All customer records
- All jobs and schedules
- All invoices and estimates
- All assets and products
- All inventory records
- **EVERYTHING in your database**

### ?? Want to Keep Your Data?

**If you want to save your current data, STOP HERE and backup first!**

Ask for help with backup before continuing if you're unsure.

---

## ?? What You Need Before Starting {#what-you-need}

### On Your Computer:
- [ ] Windows 10/11 or Mac/Linux
- [ ] Your OneManVan project folder (C:\Users\tech\source\repos\TradeFlow)
- [ ] PowerShell (Windows) or Terminal (Mac/Linux)
- [ ] Internet connection

### Server Information You Need:
- [ ] Server IP address (example: `192.168.1.100`)
- [ ] Server username (example: `ubuntu` or `root`)
- [ ] Server password or SSH key
- [ ] Server has Docker installed

### Skills Needed:
- ? None! Just follow the steps exactly as written
- ? Copy and paste commands
- ? Press Enter key

---

## ??? Part 1: Prepare Files on Your Computer {#part-1-prepare-files}

### Step 1.1: Create Deployment Folder

**Windows (PowerShell):**

1. Press `Windows Key + X`
2. Click **"Windows PowerShell"** or **"Terminal"**
3. Copy and paste this command, then press Enter:

```powershell
# Create deployment folder on your Desktop
New-Item -Path "$env:USERPROFILE\Desktop\OneManVan-Deployment" -ItemType Directory -Force
```

**What this does:** Creates a new folder called "OneManVan-Deployment" on your Desktop

**Mac/Linux (Terminal):**

1. Press `Cmd + Space` (Mac) and type "Terminal"
2. Copy and paste this command, then press Enter:

```bash
# Create deployment folder on your Desktop
mkdir -p ~/Desktop/OneManVan-Deployment
```

### Step 1.2: Copy Required Files

**IMPORTANT:** Make sure you're NOT inside your project folder when running these commands!

**Windows (PowerShell):**

```powershell
# First, navigate to a safe location (like your home directory)
cd ~

# Set your project location (CHANGE THIS if different!)
$ProjectPath = "C:\Users\tech\source\repos\TradeFlow"
$DeployPath = "$env:USERPROFILE\Desktop\OneManVan-Deployment"

# Check if we're trying to copy to the same location
if ($ProjectPath -eq $DeployPath) {
    Write-Host "ERROR: Project and deployment folders are the same!" -ForegroundColor Red
    Write-Host "Deployment folder created at: $DeployPath" -ForegroundColor Yellow
    exit
}

# Copy all necessary files
Write-Host "Copying files from: $ProjectPath" -ForegroundColor Cyan
Write-Host "To: $DeployPath" -ForegroundColor Cyan
Write-Host ""

Copy-Item "$ProjectPath\docker-compose.yml" -Destination $DeployPath -Force
Copy-Item "$ProjectPath\.env" -Destination $DeployPath -ErrorAction SilentlyContinue -Force
Copy-Item "$ProjectPath\.env.example" -Destination $DeployPath -ErrorAction SilentlyContinue -Force
Copy-Item "$ProjectPath\docker-complete-reset-deploy.sh" -Destination $DeployPath -Force
Copy-Item "$ProjectPath\docker-complete-reset-deploy.ps1" -Destination $DeployPath -Force

# Copy OneManVan.Web folder
Write-Host "Copying OneManVan.Web folder..." -ForegroundColor Yellow
Copy-Item "$ProjectPath\OneManVan.Web" -Destination $DeployPath -Recurse -Force

# Copy OneManVan.Shared folder
Write-Host "Copying OneManVan.Shared folder..." -ForegroundColor Yellow
Copy-Item "$ProjectPath\OneManVan.Shared" -Destination $DeployPath -Recurse -Force

Write-Host ""
Write-Host "? Files copied successfully!" -ForegroundColor Green
Write-Host "?? Deployment folder: $DeployPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next: Go to Part 2 to upload files to server" -ForegroundColor Green
```

**Mac/Linux (Terminal):**

```bash
# First, navigate to a safe location (like your home directory)
cd ~

# Set your project location (CHANGE THIS if different!)
PROJECT_PATH=~/source/repos/TradeFlow
DEPLOY_PATH=~/Desktop/OneManVan-Deployment

# Check if we're trying to copy to the same location
if [ "$PROJECT_PATH" = "$DEPLOY_PATH" ]; then
    echo "ERROR: Project and deployment folders are the same!"
    echo "Deployment folder created at: $DEPLOY_PATH"
    exit 1
fi

# Copy all necessary files
echo "Copying files from: $PROJECT_PATH"
echo "To: $DEPLOY_PATH"
echo ""

cp "$PROJECT_PATH/docker-compose.yml" "$DEPLOY_PATH/"
cp "$PROJECT_PATH/.env" "$DEPLOY_PATH/" 2>/dev/null || true
cp "$PROJECT_PATH/.env.example" "$DEPLOY_PATH/" 2>/dev/null || true
cp "$PROJECT_PATH/docker-complete-reset-deploy.sh" "$DEPLOY_PATH/"
cp "$PROJECT_PATH/docker-complete-reset-deploy.ps1" "$DEPLOY_PATH/"

# Copy OneManVan.Web folder
echo "Copying OneManVan.Web folder..."
cp -r "$PROJECT_PATH/OneManVan.Web" "$DEPLOY_PATH/"

# Copy OneManVan.Shared folder
echo "Copying OneManVan.Shared folder..."
cp -r "$PROJECT_PATH/OneManVan.Shared" "$DEPLOY_PATH/"

echo ""
echo "? Files copied successfully!"
echo "?? Deployment folder: $DEPLOY_PATH"
echo ""
echo "Next: Go to Part 2 to upload files to server"
```

### Step 1.3: Verify Files Were Copied

**Check your Desktop** - you should see a folder called **OneManVan-Deployment** with these files:
- `docker-compose.yml`
- `.env` (or `.env.example`)
- `docker-complete-reset-deploy.sh`
- `docker-complete-reset-deploy.ps1`
- Folder: `OneManVan.Web`
- Folder: `OneManVan.Shared`

? **If you see all these, continue to Part 2!**

---

## ?? Part 2: Upload Files to Server {#part-2-upload-files}

### Step 2.1: Choose Your Upload Method

**Option A: Using WinSCP (Windows - Easiest for Beginners)**

1. Download WinSCP from: https://winscp.net/
2. Install and open WinSCP
3. Click **"New Site"**
4. Fill in:
   - **File protocol:** SFTP
   - **Host name:** Your server IP (example: 192.168.1.100)
   - **User name:** Your server username
   - **Password:** Your server password
5. Click **"Login"**
6. Drag the **OneManVan-Deployment** folder from your Desktop to the server window
7. Wait for upload to complete (5-10 minutes)

**Option B: Using Command Line (All Platforms)**

**Windows (PowerShell):**

```powershell
# CHANGE THESE to match your server
$ServerIP = "192.168.1.100"
$ServerUser = "ubuntu"

# Upload deployment folder
$DeployPath = "$env:USERPROFILE\Desktop\OneManVan-Deployment"

# Use SCP to upload (you'll be asked for password)
scp -r $DeployPath ${ServerUser}@${ServerIP}:~/

Write-Host "? Upload complete!" -ForegroundColor Green
```

**Mac/Linux (Terminal):**

```bash
# CHANGE THESE to match your server
SERVER_IP="192.168.1.100"
SERVER_USER="ubuntu"

# Upload deployment folder
DEPLOY_PATH=~/Desktop/OneManVan-Deployment

# Use SCP to upload (you'll be asked for password)
scp -r $DEPLOY_PATH $SERVER_USER@$SERVER_IP:~/

echo "? Upload complete!"
```

### Step 2.2: Verify Upload

**You should see output like:**
```
OneManVan-Deployment/docker-compose.yml          100%  1234    1.2MB/s   00:01
OneManVan-Deployment/.env                        100%   256    0.2MB/s   00:01
...
```

? **If upload finishes without errors, continue to Part 3!**

---

## ?? Part 3: Connect to Your Server {#part-3-connect-to-server}

### Step 3.1: Open SSH Connection

**Windows (PowerShell):**

```powershell
# CHANGE THESE to match your server
$ServerIP = "192.168.1.100"
$ServerUser = "ubuntu"

# Connect to server (you'll be asked for password)
ssh ${ServerUser}@${ServerIP}
```

**Mac/Linux (Terminal):**

```bash
# CHANGE THESE to match your server
SERVER_IP="192.168.1.100"
SERVER_USER="ubuntu"

# Connect to server (you'll be asked for password)
ssh $SERVER_USER@$SERVER_IP
```

### Step 3.2: You're Now Connected!

**You should see something like:**
```
Welcome to Ubuntu 22.04.1 LTS
ubuntu@server:~$
```

The `$` or `#` at the end means you're connected and ready for commands.

? **If you see this prompt, continue to Part 4!**

---

## ??? Part 4: Delete Old Docker Deployment {#part-4-delete-old-deployment}

**?? WARNING:** This deletes EVERYTHING - all containers, images, volumes, and data!

### Step 4.1: Navigate to Deployment Folder

```bash
# Go to the deployment folder
cd ~/OneManVan-Deployment
```

### Step 4.2: Stop All Running Containers

```bash
# Stop all OneManVan containers
echo "Stopping containers..."
docker ps -a --filter "name=onemanvan" --format "{{.Names}}" | xargs -r docker stop
docker ps -a --filter "name=tradeflow" --format "{{.Names}}" | xargs -r docker stop

echo "? Containers stopped"
```

**What you'll see:**
```
Stopping containers...
tradeflow-webui
tradeflow-sqlserver
? Containers stopped
```

### Step 4.3: Remove All Containers

```bash
# Remove all OneManVan containers
echo "Removing containers..."
docker ps -a --filter "name=onemanvan" --format "{{.Names}}" | xargs -r docker rm -f
docker ps -a --filter "name=tradeflow" --format "{{.Names}}" | xargs -r docker rm -f

echo "? Containers removed"
```

### Step 4.4: Delete All Volumes (?? DATABASE DELETED HERE!)

```bash
# Remove all volumes - THIS DELETES YOUR DATABASE!
echo "??  Deleting volumes (DATABASE DELETION)..."
docker volume ls --filter "name=onemanvan" --format "{{.Name}}" | xargs -r docker volume rm
docker volume ls --filter "name=tradeflow" --format "{{.Name}}" | xargs -r docker volume rm

echo "? Volumes deleted"
```

### Step 4.5: Remove All Images

```bash
# Remove all OneManVan Docker images
echo "Removing images..."
docker images --filter "reference=onemanvan*" --format "{{.Repository}}:{{.Tag}}" | xargs -r docker rmi -f
docker images --filter "reference=tradeflow*" --format "{{.Repository}}:{{.Tag}}" | xargs -r docker rmi -f

echo "? Images removed"
```

### Step 4.6: Clean Up Networks

```bash
# Remove networks
echo "Cleaning networks..."
docker network ls --filter "name=onemanvan" --format "{{.Name}}" | xargs -r docker network rm 2>/dev/null || true
docker network ls --filter "name=tradeflow" --format "{{.Name}}" | xargs -r docker network rm 2>/dev/null || true

echo "? Networks cleaned"
```

### Step 4.7: Final Cleanup

```bash
# Clean up any unused Docker resources
echo "Final cleanup..."
docker system prune -f

echo "? Complete cleanup finished!"
```

? **If you see "Complete cleanup finished!", continue to Part 5!**

---

## ?? Part 5: Deploy Fresh Installation {#part-5-deploy-fresh}

### Step 5.1: Check for .env File

```bash
# Check if .env exists
if [ ! -f .env ]; then
    echo "??  No .env file found. Creating from example..."
    if [ -f .env.example ]; then
        cp .env.example .env
        echo "? Created .env from example"
        echo ""
        echo "?? IMPORTANT: Edit .env with your passwords!"
        echo "Run: nano .env"
        echo "Press Ctrl+X, then Y, then Enter to save"
    else
        echo "? No .env.example found!"
        echo "Create .env manually with these contents:"
        echo ""
        echo "SA_PASSWORD=TradeFlow2025!"
        echo "MSSQL_PID=Developer"
        echo "ASPNETCORE_ENVIRONMENT=Development"
        echo "ASPNETCORE_URLS=http://+:8080"
    fi
else
    echo "? .env file exists"
fi
```

### Step 5.2: Build Fresh Docker Images

```bash
# Build new images (this takes 5-10 minutes)
echo "?? Building Docker images (this will take a few minutes)..."
docker compose build --no-cache

echo "? Images built successfully!"
```

**What you'll see:**
```
?? Building Docker images...
[+] Building 234.5s (28/28) FINISHED
 => [internal] load build definition
 => => transferring dockerfile: 32B
 ...
? Images built successfully!
```

### Step 5.3: Start New Containers

```bash
# Start all services
echo "?? Starting containers..."
docker compose up -d

echo "? Containers started!"
```

**What you'll see:**
```
?? Starting containers...
[+] Running 3/3
 ? Network tradeflow-network      Created
 ? Container tradeflow-sqlserver  Started
 ? Container tradeflow-webui      Started
? Containers started!
```

### Step 5.4: Wait for Services to Start

```bash
# Wait 15 seconds for services to initialize
echo "? Waiting for services to start (15 seconds)..."
sleep 15

echo "? Services should be ready!"
```

### Step 5.5: Check SQL Server is Ready

```bash
# Test SQL Server connection
echo "?? Checking SQL Server..."
for i in {1..30}; do
    if docker exec tradeflow-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'TradeFlow2025!' -Q "SELECT 1" &> /dev/null; then
        echo "? SQL Server is ready!"
        break
    else
        echo "? Waiting for SQL Server... (attempt $i/30)"
        sleep 2
    fi
done
```

### Step 5.6: Check Web UI is Ready

```bash
# Test Web UI
echo "?? Checking Web UI..."
for i in {1..30}; do
    if curl -f http://localhost:7159/health &> /dev/null; then
        echo "? Web UI is ready!"
        break
    else
        echo "? Waiting for Web UI... (attempt $i/30)"
        sleep 2
    fi
done
```

? **If you see both "ready" messages, continue to Part 6!**

---

## ? Part 6: Verify Everything Works {#part-6-verify}

### Step 6.1: Check Running Containers

```bash
# List running containers
docker ps

# You should see TWO containers running:
# - tradeflow-webui
# - tradeflow-sqlserver
```

**Expected output:**
```
CONTAINER ID   IMAGE              PORTS                    NAMES
abc123def456   tradeflow-webui    0.0.0.0:7159->8080/tcp  tradeflow-webui
def456ghi789   mcr.microsoft...   0.0.0.0:1433->1433/tcp  tradeflow-sqlserver
```

### Step 6.2: Check Logs for Errors

```bash
# Check Web UI logs
echo "?? Checking Web UI logs..."
docker logs tradeflow-webui --tail 20

# Look for these SUCCESS messages:
# ? "Now listening on: http://[::]:8080"
# ? "Database initialization completed successfully"
```

### Step 6.3: Test Database Connection

```bash
# Test SQL Server
echo "?? Testing database..."
docker exec tradeflow-sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P 'TradeFlow2025!' \
  -Q "SELECT name FROM sys.databases"

# You should see:
# TradeFlowFSM
# TradeFlowIdentity
```

### Step 6.4: Get Server IP Address

```bash
# Get your server's IP address
echo "?? Your server IP address:"
hostname -I | awk '{print $1}'

# Write this down - you'll need it to access the web app!
```

### Step 6.5: Test Web Access

**On your computer (not server), open a web browser and go to:**

```
http://YOUR_SERVER_IP:7159/
```

**Example:** If your server IP is `192.168.1.100`, go to:
```
http://192.168.1.100:7159/
```

**You should see:**
- ? OneManVan Dashboard
- ? Theme toggle (sun/moon icon) in top right
- ? Navigation menu on left
- ? No error messages

### Step 6.6: Test Features

**Try these to make sure everything works:**

1. **Toggle Dark Mode**
   - Click sun/moon icon in top right
   - Theme should switch between light and dark

2. **Navigate**
   - Click "Customers" in left menu
   - Page should load (will be empty)

3. **Create Test Customer**
   - Click "Add Customer" button
   - Fill in a name
   - Click Save
   - Customer should appear in list

4. **Check Quick Notes**
   - Click "Quick Notes" in left menu
   - Click "New Note"
   - Type some text
   - Click Save
   - Note should appear

? **If all these work, deployment is successful!**

---

## ?? Troubleshooting {#troubleshooting}

### Problem: Can't Connect to Server

**Symptoms:**
- `ssh: connect to host X.X.X.X port 22: Connection refused`
- Timeout when trying to connect

**Solutions:**

1. **Check server is powered on**
2. **Verify IP address is correct:**
   ```bash
   ping YOUR_SERVER_IP
   ```
3. **Check firewall allows SSH (port 22)**
4. **Try from server console** (if you have physical access)

### Problem: Docker Command Not Found

**Symptoms:**
- `bash: docker: command not found`

**Solution:**

```bash
# Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Add your user to docker group
sudo usermod -aG docker $USER

# Log out and back in
exit
# Then reconnect with SSH
```

### Problem: Permission Denied

**Symptoms:**
- `permission denied while trying to connect to the Docker daemon`

**Solution:**

```bash
# Add yourself to docker group
sudo usermod -aG docker $USER

# Log out and back in for changes to take effect
exit
# Reconnect and try again
```

### Problem: Port 7159 Already in Use

**Symptoms:**
- `Error starting userland proxy: listen tcp 0.0.0.0:7159: bind: address already in use`

**Solution:**

```bash
# Find what's using port 7159
sudo lsof -i :7159

# Stop the old container
docker stop <CONTAINER_ID_FROM_ABOVE>

# Try starting again
docker compose up -d
```

### Problem: SQL Server Won't Start

**Symptoms:**
- SQL Server container keeps restarting
- Can't connect to database

**Check logs:**
```bash
docker logs tradeflow-sqlserver --tail 50
```

**Common fixes:**

1. **Password too weak:**
   - Edit `.env` file
   - Change `SA_PASSWORD` to something strong
   - Example: `TradeFlow2025!Strong`
   - Redeploy: `docker compose down && docker compose up -d`

2. **Not enough memory:**
   - SQL Server needs at least 2GB RAM
   - Check with: `free -h`

### Problem: Web UI Shows 500 Error

**Symptoms:**
- White page with "500 Internal Server Error"

**Check logs:**
```bash
docker logs tradeflow-webui --tail 100
```

**Common fixes:**

1. **Database not ready yet:**
   - Wait 30 seconds
   - Refresh browser

2. **Connection string wrong:**
   - Check logs for "unable to connect"
   - Restart services: `docker compose restart`

### Problem: Can't Access from Browser

**Symptoms:**
- Browser says "Can't reach this page"
- Times out

**Solutions:**

1. **Check firewall allows port 7159:**
   ```bash
   sudo ufw allow 7159/tcp
   ```

2. **Verify containers are running:**
   ```bash
   docker ps
   ```

3. **Check you're using correct IP:**
   ```bash
   hostname -I
   ```

4. **Try from server itself:**
   ```bash
   curl http://localhost:7159/health
   ```

### Problem: Old Data Still Appears

**Symptoms:**
- Customers/jobs from before still show up

**Solution:**
```bash
# Volumes weren't deleted properly
# Delete them manually:
docker volume rm $(docker volume ls -q --filter name=tradeflow)

# Then deploy again:
docker compose up -d
```

---

## ?? Common Commands Reference

### View Logs
```bash
# All services
docker compose logs -f

# Just Web UI
docker compose logs -f webui

# Just SQL Server  
docker compose logs -f sqlserver

# Last 50 lines
docker logs tradeflow-webui --tail 50
```

### Restart Services
```bash
# Restart everything
docker compose restart

# Restart one service
docker compose restart webui
```

### Stop Services
```bash
# Stop all
docker compose down

# Stop but keep volumes (keep data)
docker compose stop
```

### Start Services (After Stop)
```bash
docker compose up -d
```

### Check Status
```bash
# List running containers
docker ps

# Check resource usage
docker stats

# Check volumes
docker volume ls

# Check networks
docker network ls
```

### Full Reset (Start Over)
```bash
# Stop everything
docker compose down -v

# Remove all OneManVan stuff
docker system prune -af --volumes

# Start fresh
docker compose up -d
```

---

## ?? Success Checklist

After completing this guide, you should have:

- [ ] ? All old Docker containers removed
- [ ] ? All old volumes deleted (fresh database)
- [ ] ? New Docker images built
- [ ] ? Containers running (check with `docker ps`)
- [ ] ? Web UI accessible in browser
- [ ] ? Can toggle dark mode
- [ ] ? Can navigate all pages
- [ ] ? Can create customers/jobs/notes
- [ ] ? No error messages in logs

**Congratulations!** ?? You've successfully deployed OneManVan to your Docker server!

---

## ?? Getting Help

If you're still stuck:

1. **Check the logs:**
   ```bash
   docker compose logs -f
   ```

2. **Save the logs to a file:**
   ```bash
   docker compose logs > deployment-logs.txt
   ```

3. **Take screenshots of any error messages**

4. **Note what step you're on and what happened**

5. **Ask for help** with all the above information

---

## ?? What You Learned

By following this guide, you learned how to:
- ? Use command line (Terminal/PowerShell)
- ? Copy files between computers
- ? Connect to a server with SSH
- ? Work with Docker containers
- ? Deploy a web application
- ? Troubleshoot common issues

**Great job!** These are valuable skills for managing servers and applications.

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
