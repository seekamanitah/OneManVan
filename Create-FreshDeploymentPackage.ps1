# ============================================
# Create Fresh Deployment Package
# Complete project folder for clean Docker deployment
# ============================================

param(
    [string]$OutputPath = "$env:USERPROFILE\Desktop\OneManVan-Fresh-Deploy-$(Get-Date -Format 'yyyy-MM-dd-HHmm')"
)

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Creating Fresh Deployment Package" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Get project root
$ProjectRoot = $PSScriptRoot
if (-not (Test-Path "$ProjectRoot\OneManVan.sln")) {
    Write-Host "Error: Must run from project root directory" -ForegroundColor Red
    exit 1
}

Write-Host "Project Root: $ProjectRoot" -ForegroundColor Yellow
Write-Host "Output Path: $OutputPath" -ForegroundColor Yellow
Write-Host ""

# Create output directory
New-Item -Path $OutputPath -ItemType Directory -Force | Out-Null

# ============================================
# Define what to copy
# ============================================

$ItemsToCopy = @(
    # Core projects
    @{
        Source = "OneManVan.Web"
        Type = "Folder"
        Description = "Web application project"
    },
    @{
        Source = "OneManVan.Shared"
        Type = "Folder"
        Description = "Shared models and services"
    },
    
    # Docker files
    @{
        Source = "docker-compose.yml"
        Type = "File"
        Description = "Docker Compose configuration"
    },
    @{
        Source = ".dockerignore"
        Type = "File"
        Description = "Docker ignore file"
    },
    @{
        Source = ".env"
        Type = "File"
        Description = "Environment variables"
    },
    @{
        Source = "docker"
        Type = "Folder"
        Description = "Docker initialization scripts"
    },
    
    # Solution file
    @{
        Source = "OneManVan.sln"
        Type = "File"
        Description = "Solution file"
    },
    
    # Deployment scripts
    @{
        Source = "docker-complete-reset-deploy.sh"
        Type = "File"
        Description = "Linux deployment script"
    },
    @{
        Source = "fix-database-config.sh"
        Type = "File"
        Description = "Database config fix script"
    },
    @{
        Source = "fix-line-endings-server.sh"
        Type = "File"
        Description = "Line endings fix script"
    },
    
    # Documentation
    @{
        Source = "DATABASE_CONFIG_FIX.md"
        Type = "File"
        Description = "Database configuration guide"
    },
    @{
        Source = "DOCKER_COMPLETE_RESET_GUIDE.md"
        Type = "File"
        Description = "Docker deployment guide"
    }
)

# ============================================
# Copy files
# ============================================

Write-Host "Copying project files..." -ForegroundColor Green
Write-Host ""

$CopiedCount = 0
$SkippedCount = 0

foreach ($Item in $ItemsToCopy) {
    $SourcePath = Join-Path $ProjectRoot $Item.Source
    $DestPath = Join-Path $OutputPath $Item.Source
    
    if (Test-Path $SourcePath) {
        Write-Host "  ? $($Item.Source)" -ForegroundColor Green -NoNewline
        Write-Host " - $($Item.Description)" -ForegroundColor Gray
        
        if ($Item.Type -eq "Folder") {
            # Copy folder excluding unnecessary files
            $ExcludePatterns = @(
                "bin",
                "obj",
                ".vs",
                "*.user",
                "*.suo",
                "node_modules",
                "AppData"
            )
            
            # Create destination directory
            New-Item -Path $DestPath -ItemType Directory -Force | Out-Null
            
            # Copy with exclusions
            Get-ChildItem -Path $SourcePath -Recurse | ForEach-Object {
                $ShouldExclude = $false
                foreach ($Pattern in $ExcludePatterns) {
                    if ($_.FullName -like "*\$Pattern\*" -or $_.Name -like $Pattern) {
                        $ShouldExclude = $true
                        break
                    }
                }
                
                if (-not $ShouldExclude) {
                    $RelativePath = $_.FullName.Substring($SourcePath.Length + 1)
                    $TargetPath = Join-Path $DestPath $RelativePath
                    
                    if ($_.PSIsContainer) {
                        New-Item -Path $TargetPath -ItemType Directory -Force | Out-Null
                    } else {
                        $TargetDir = Split-Path $TargetPath -Parent
                        if (-not (Test-Path $TargetDir)) {
                            New-Item -Path $TargetDir -ItemType Directory -Force | Out-Null
                        }
                        Copy-Item $_.FullName -Destination $TargetPath -Force
                    }
                }
            }
        } else {
            # Copy single file
            $DestDir = Split-Path $DestPath -Parent
            if (-not (Test-Path $DestDir)) {
                New-Item -Path $DestDir -ItemType Directory -Force | Out-Null
            }
            Copy-Item $SourcePath -Destination $DestPath -Force
        }
        
        $CopiedCount++
    } else {
        Write-Host "  ? $($Item.Source)" -ForegroundColor Yellow -NoNewline
        Write-Host " - Not found, skipping" -ForegroundColor Gray
        $SkippedCount++
    }
}

Write-Host ""
Write-Host "Copied: $CopiedCount items" -ForegroundColor Green
if ($SkippedCount -gt 0) {
    Write-Host "Skipped: $SkippedCount items" -ForegroundColor Yellow
}
Write-Host ""

# ============================================
# Create deployment README
# ============================================

Write-Host "Creating deployment README..." -ForegroundColor Green

$ReadmeContent = @"
# OneManVan - Fresh Deployment Package
**Created:** $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
**Package Type:** Complete Docker Deployment

---

## ?? What's Included

This package contains everything needed for a fresh Docker deployment:

- ? OneManVan.Web (Web application)
- ? OneManVan.Shared (Shared libraries)
- ? docker-compose.yml (Docker configuration)
- ? Dockerfile (Web app container)
- ? Database initialization scripts
- ? Deployment automation scripts
- ? Complete documentation

---

## ?? Quick Deployment Steps

### **1. Upload to Server**

**Using WinSCP (Windows):**
1. Connect to your server (192.168.50.107)
2. Upload this entire folder to ``~/OneManVan-Fresh-Deploy``

**Using SCP (Command Line):**
``````powershell
scp -r "$(Split-Path -Parent $MyInvocation.MyCommand.Path)" root@192.168.50.107:~/OneManVan-Fresh-Deploy
``````

---

### **2. Deploy on Server**

``````sh
# SSH to server
ssh root@192.168.50.107

# Navigate to deployment folder
cd ~/OneManVan-Fresh-Deploy

# Make scripts executable
chmod +x *.sh

# Run complete deployment
./docker-complete-reset-deploy.sh

# OR manual deployment
docker compose down -v
docker compose build --no-cache
docker compose up -d
``````

---

### **3. Verify Deployment**

``````sh
# Check containers
docker ps

# Check logs
docker logs tradeflow-webui --tail 50
docker logs tradeflow-db --tail 50

# Verify databases
docker exec -it tradeflow-db /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'TradeFlow2025!' -C \
  -Q "SELECT name FROM sys.databases"

# Access application
# http://YOUR_SERVER_IP:7159/
``````

---

## ?? Configuration Files

### **docker-compose.yml**
- SQL Server 2022 (Express)
- Web UI (.NET 10 Blazor)
- Network configuration
- Volume mounts

### **appsettings.json**
- SQL Server connection strings
- Application settings
- Environment-specific config

### **Program.cs**
- Detects Docker/SQL Server automatically
- Initializes databases on startup
- Seeds admin account

---

## ??? Database Configuration

### **Automatic Database Creation**

The web application automatically creates:
- ``TradeFlowFSM`` (Business data)
- ``TradeFlowIdentity`` (User accounts)

All tables and initial data are created on first startup.

### **Connection Strings**

``````
DefaultConnection: Server=sqlserver;Database=TradeFlowFSM;User Id=sa;Password=TradeFlow2025!;TrustServerCertificate=True;
IdentityConnection: Server=sqlserver;Database=TradeFlowIdentity;User Id=sa;Password=TradeFlow2025!;TrustServerCertificate=True;
``````

---

## ?? Default Admin Account

| Field | Value |
|-------|-------|
| **Email** | admin@onemanvan.local |
| **Password** | Admin123! |

?? **Change password immediately after first login!**

---

## ?? Troubleshooting

### **Container Won't Start**

``````sh
# Check logs
docker logs tradeflow-webui --tail 100
docker logs tradeflow-db --tail 100

# Check configuration
cat docker-compose.yml

# Restart
docker compose restart
``````

### **Database Connection Error**

``````sh
# Test SQL Server
docker exec -it tradeflow-db /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'TradeFlow2025!' -C \
  -Q "SELECT @@VERSION"

# Check connection strings
docker exec tradeflow-webui env | grep ConnectionStrings
``````

### **Port Already in Use**

``````sh
# Find what's using port 7159
sudo lsof -i :7159

# Stop conflicting process
docker stop <CONTAINER_ID>
``````

---

## ?? File Structure

``````
OneManVan-Fresh-Deploy/
??? OneManVan.Web/              # Web application
?   ??? Components/             # Razor components
?   ??? Controllers/            # API controllers
?   ??? Services/               # Application services
?   ??? wwwroot/                # Static files
?   ??? Dockerfile              # Container definition
?   ??? appsettings.json        # Configuration
?   ??? Program.cs              # Application startup
?
??? OneManVan.Shared/           # Shared libraries
?   ??? Data/                   # Database context
?   ??? Models/                 # Data models
?   ??? Services/               # Shared services
?   ??? Utilities/              # Helper classes
?
??? docker/                     # Docker initialization
?   ??? init/                   # Database init scripts
?       ??? 01-create-database.sql
?       ??? 02-seed-data.sql
?
??? docker-compose.yml          # Docker configuration
??? .dockerignore               # Docker ignore rules
??? .env                        # Environment variables
?
??? docker-complete-reset-deploy.sh   # Deployment script
??? fix-database-config.sh            # Config fix script
?
??? DATABASE_CONFIG_FIX.md            # Config guide
??? DOCKER_COMPLETE_RESET_GUIDE.md    # Deployment guide
??? README.md                         # This file
``````

---

## ?? What Gets Created

### **Docker Containers**

| Container | Image | Ports |
|-----------|-------|-------|
| tradeflow-db | SQL Server 2022 | 1433 |
| tradeflow-webui | .NET 10 Blazor | 7159 |

### **Databases**

| Database | Purpose | Auto-Created |
|----------|---------|--------------|
| TradeFlowFSM | Business data | ? Yes |
| TradeFlowIdentity | User accounts | ? Yes |

### **Network**

| Name | Driver | Purpose |
|------|--------|---------|
| tradeflow-net | bridge | Container networking |

---

## ? Success Checklist

After deployment:

- [ ] Both containers running (``docker ps``)
- [ ] SQL Server healthy
- [ ] Both databases created
- [ ] Web UI accessible (http://YOUR_IP:7159)
- [ ] Dashboard loads
- [ ] Can create customers/jobs
- [ ] Dark mode toggle works
- [ ] No errors in logs

---

## ?? Need Help?

1. **Check logs:**
   ``````sh
   docker compose logs -f
   ``````

2. **Review configuration:**
   ``````sh
   cat docker-compose.yml
   docker exec tradeflow-webui cat /app/appsettings.json
   ``````

3. **Clean restart:**
   ``````sh
   docker compose down -v
   docker compose build --no-cache
   docker compose up -d
   ``````

---

## ?? Security Notes

### **For Production:**

1. ? Change SA_PASSWORD in docker-compose.yml
2. ? Change admin account password
3. ? Enable HTTPS (reverse proxy)
4. ? Configure firewall
5. ? Set up SSL certificates
6. ? Disable SQL Server port 1433 externally
7. ? Enable authentication
8. ? Set up regular backups

---

**?? Your OneManVan deployment package is ready!**

Upload this folder to your server and run the deployment script to get started.
"@

$ReadmeContent | Out-File -FilePath (Join-Path $OutputPath "README.md") -Encoding UTF8

Write-Host "  ? Created README.md" -ForegroundColor Green
Write-Host ""

# ============================================
# Create quick deploy script for server
# ============================================

Write-Host "Creating server deployment script..." -ForegroundColor Green

$ServerDeployScript = @'
#!/bin/bash
# OneManVan - Fresh Deployment Script
# Run this on your Docker server

set -e  # Exit on any error

echo "============================================"
echo "   OneManVan Fresh Deployment"
echo "============================================"
echo ""

# Check Docker is installed
if ! command -v docker &> /dev/null; then
    echo "? Docker not found. Installing..."
    curl -fsSL https://get.docker.com -o get-docker.sh
    sudo sh get-docker.sh
    sudo usermod -aG docker $USER
    echo "? Docker installed. Please log out and back in, then run this script again."
    exit 1
fi

# Check Docker Compose
if ! command -v docker compose &> /dev/null; then
    echo "? Docker Compose not found."
    exit 1
fi

echo "? Docker and Docker Compose found"
echo ""

# Stop any existing containers
echo "?? Stopping existing containers..."
docker compose down -v 2>/dev/null || true
docker stop $(docker ps -aq --filter "name=tradeflow") 2>/dev/null || true
docker rm $(docker ps -aq --filter "name=tradeflow") 2>/dev/null || true
echo "? Cleaned up existing containers"
echo ""

# Create database directory
echo "?? Setting up database storage..."
sudo mkdir -p /media/onemanvanDB/log
sudo mkdir -p /media/onemanvanDB/backup
sudo chown -R 10001:0 /media/onemanvanDB
sudo chmod -R 755 /media/onemanvanDB
echo "? Database storage ready"
echo ""

# Build fresh images
echo "?? Building Docker images (this takes 3-5 minutes)..."
docker compose build --no-cache
echo "? Images built"
echo ""

# Start services
echo "?? Starting services..."
docker compose up -d
echo "? Services started"
echo ""

# Wait for SQL Server
echo "? Waiting for SQL Server to initialize (60 seconds)..."
sleep 60

# Check health
echo "?? Checking container health..."
docker ps
echo ""

# Test SQL Server
echo "?? Testing SQL Server..."
if docker exec -it tradeflow-db /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P 'TradeFlow2025!' -C \
    -Q "SELECT @@VERSION" &> /dev/null; then
    echo "? SQL Server is ready!"
else
    echo "??  SQL Server not ready yet. Check logs:"
    echo "   docker logs tradeflow-db"
fi
echo ""

# Wait for Web UI
echo "? Waiting for Web UI to initialize (30 seconds)..."
sleep 30

# Check databases
echo "?? Checking databases..."
docker exec -it tradeflow-db /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'TradeFlow2025!' -C \
  -Q "SELECT name FROM sys.databases"
echo ""

# Get server IP
SERVER_IP=$(hostname -I | awk '{print $1}')

echo "============================================"
echo "   ? Deployment Complete!"
echo "============================================"
echo ""
echo "?? Access your application:"
echo "   http://$SERVER_IP:7159/"
echo ""
echo "?? Default admin account:"
echo "   Email: admin@onemanvan.local"
echo "   Password: Admin123!"
echo ""
echo "?? Check status:"
echo "   docker ps"
echo "   docker logs tradeflow-webui"
echo "   docker logs tradeflow-db"
echo ""
echo "?? Your OneManVan application is ready!"
'@

# Create deploy.sh with Unix line endings (LF)
$ServerDeployScriptPath = Join-Path $OutputPath "deploy.sh"
$ServerDeployScript -replace "`r`n", "`n" | Out-File -FilePath $ServerDeployScriptPath -Encoding UTF8 -NoNewline

Write-Host "  ? Created deploy.sh" -ForegroundColor Green
Write-Host ""

# ============================================
# Create Windows upload script
# ============================================

Write-Host "Creating Windows upload script..." -ForegroundColor Green

$WindowsUploadScript = @"
# Upload Deployment Package to Server
# Run this on your Windows machine

`$ServerIP = "192.168.50.107"
`$ServerUser = "root"
`$PackagePath = "$OutputPath"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "   Upload OneManVan to Server" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Package: `$PackagePath" -ForegroundColor Yellow
Write-Host "Server: `$ServerUser@`$ServerIP" -ForegroundColor Yellow
Write-Host ""

# Check SCP is available
if (-not (Get-Command scp -ErrorAction SilentlyContinue)) {
    Write-Host "? SCP not found. Please install OpenSSH or use WinSCP." -ForegroundColor Red
    Write-Host ""
    Write-Host "Download WinSCP: https://winscp.net/" -ForegroundColor Yellow
    exit 1
}

Write-Host "?? Uploading package..." -ForegroundColor Green
Write-Host "(This may take 2-5 minutes depending on connection speed)" -ForegroundColor Gray
Write-Host ""

# Upload
scp -r "`$PackagePath" `${ServerUser}@`${ServerIP}:~/OneManVan-Fresh-Deploy

if (`$LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "? Upload complete!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "1. SSH to server: ssh `$ServerUser@`$ServerIP" -ForegroundColor White
    Write-Host "2. Navigate: cd ~/OneManVan-Fresh-Deploy" -ForegroundColor White
    Write-Host "3. Make executable: chmod +x deploy.sh" -ForegroundColor White
    Write-Host "4. Deploy: ./deploy.sh" -ForegroundColor White
} else {
    Write-Host ""
    Write-Host "? Upload failed!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Try using WinSCP instead:" -ForegroundColor Yellow
    Write-Host "1. Download from: https://winscp.net/" -ForegroundColor White
    Write-Host "2. Connect to: `$ServerIP" -ForegroundColor White
    Write-Host "3. Upload folder: `$PackagePath" -ForegroundColor White
}
"@

$WindowsUploadScript | Out-File -FilePath (Join-Path $OutputPath "Upload-To-Server.ps1") -Encoding UTF8

Write-Host "  ? Created Upload-To-Server.ps1" -ForegroundColor Green
Write-Host ""

# ============================================
# Summary
# ============================================

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "   ? Package Created Successfully!" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "?? Package Location:" -ForegroundColor Green
Write-Host "   $OutputPath" -ForegroundColor White
Write-Host ""

Write-Host "?? Package Contents:" -ForegroundColor Green
Write-Host "   - OneManVan.Web (Web application)" -ForegroundColor White
Write-Host "   - OneManVan.Shared (Shared libraries)" -ForegroundColor White
Write-Host "   - docker-compose.yml (Docker config)" -ForegroundColor White
Write-Host "   - Dockerfile (Container definition)" -ForegroundColor White
Write-Host "   - Database initialization scripts" -ForegroundColor White
Write-Host "   - Deployment automation scripts" -ForegroundColor White
Write-Host "   - Complete documentation (README.md)" -ForegroundColor White
Write-Host ""

Write-Host "?? Next Steps:" -ForegroundColor Green
Write-Host ""
Write-Host "   Option 1: Upload with PowerShell" -ForegroundColor Cyan
Write-Host "   ---------------------------------" -ForegroundColor Gray
Write-Host "   1. Open: $OutputPath\Upload-To-Server.ps1" -ForegroundColor White
Write-Host "   2. Edit ServerIP if needed" -ForegroundColor White
Write-Host "   3. Run the script" -ForegroundColor White
Write-Host ""

Write-Host "   Option 2: Upload with WinSCP" -ForegroundColor Cyan
Write-Host "   ----------------------------" -ForegroundColor Gray
Write-Host "   1. Download WinSCP: https://winscp.net/" -ForegroundColor White
Write-Host "   2. Connect to: 192.168.50.107" -ForegroundColor White
Write-Host "   3. Upload folder: $OutputPath" -ForegroundColor White
Write-Host ""

Write-Host "   Then on server:" -ForegroundColor Cyan
Write-Host "   ---------------" -ForegroundColor Gray
Write-Host "   ssh root@192.168.50.107" -ForegroundColor White
Write-Host "   cd ~/OneManVan-Fresh-Deploy" -ForegroundColor White
Write-Host "   sudo apt-get install -y dos2unix" -ForegroundColor White
Write-Host "   dos2unix deploy.sh" -ForegroundColor White
Write-Host "   chmod +x deploy.sh" -ForegroundColor White
Write-Host "   ./deploy.sh" -ForegroundColor White
Write-Host ""

Write-Host "?? Documentation:" -ForegroundColor Green
Write-Host "   Open: $OutputPath\README.md" -ForegroundColor White
Write-Host ""

Write-Host "? Package is ready for deployment!" -ForegroundColor Green
