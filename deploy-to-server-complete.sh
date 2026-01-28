#!/bin/bash
# Complete Server Deployment Script with All Fixes
# Run this on your server at 192.168.100.107

set -e

echo "=========================================="
echo "OneManVan - Complete Server Deployment"
echo "=========================================="
echo ""

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${YELLOW}Step 1: Stopping existing containers...${NC}"
cd /opt/onemanvan
docker compose -f docker-compose-full.yml down 2>/dev/null || true

echo ""
echo -e "${YELLOW}Step 2: Backing up current deployment...${NC}"
if [ -d "/opt/onemanvan" ]; then
    BACKUP_DIR="/opt/onemanvan-backup-$(date +%Y%m%d-%H%M%S)"
    mv /opt/onemanvan "$BACKUP_DIR"
    echo -e "${GREEN}? Backed up to: $BACKUP_DIR${NC}"
fi

echo ""
echo -e "${YELLOW}Step 3: Extracting new deployment...${NC}"
cd /root
if [ ! -f "deployment.zip" ]; then
    echo -e "${RED}? ERROR: deployment.zip not found in /root/${NC}"
    echo "Please upload deployment.zip to /root/ first"
    exit 1
fi

unzip -q deployment.zip -d /opt/onemanvan
echo -e "${GREEN}? Extracted deployment package${NC}"

echo ""
echo -e "${YELLOW}Step 4: Verifying critical files...${NC}"

# Check Data folder
if [ -f "/opt/onemanvan/OneManVan.Shared/Data/OneManVanDbContext.cs" ]; then
    echo -e "${GREEN}? OneManVanDbContext.cs found${NC}"
else
    echo -e "${RED}? ERROR: OneManVanDbContext.cs missing!${NC}"
    exit 1
fi

# Check docker-compose
if [ -f "/opt/onemanvan/docker-compose-full.yml" ]; then
    echo -e "${GREEN}? docker-compose-full.yml found${NC}"
else
    echo -e "${RED}? ERROR: docker-compose-full.yml missing!${NC}"
    exit 1
fi

# Check Routes.razor
if [ -f "/opt/onemanvan/OneManVan.Web/Components/Routes.razor" ]; then
    echo -e "${GREEN}? Routes.razor found${NC}"
else
    echo -e "${RED}? ERROR: Routes.razor missing!${NC}"
    exit 1
fi

echo ""
echo -e "${YELLOW}Step 5: Setting up database location (/media/onemanvanDB)...${NC}"

# Ensure database directories exist
sudo mkdir -p /media/onemanvanDB/sqldata
sudo mkdir -p /media/onemanvanDB/sqllogs
sudo mkdir -p /media/onemanvanDB/backups

# Set permissions
sudo chown -R 10001:10001 /media/onemanvanDB 2>/dev/null || echo "Note: Could not change ownership (Proxmox mount limitation)"
sudo chmod -R 755 /media/onemanvanDB

echo -e "${GREEN}? Database directories ready${NC}"

echo ""
echo -e "${YELLOW}Step 6: Verifying SQL Server healthcheck configuration...${NC}"

# Check if healthcheck uses correct path
if grep -q "/opt/mssql-tools18/bin/sqlcmd" /opt/onemanvan/docker-compose-full.yml; then
    echo -e "${GREEN}? SQL Server 2022 healthcheck path correct${NC}"
else
    echo -e "${YELLOW}? Warning: Healthcheck might use old path${NC}"
fi

# Check if user: "0:0" is present for Proxmox compatibility
if grep -q 'user: "0:0"' /opt/onemanvan/docker-compose-full.yml; then
    echo -e "${GREEN}? SQL Server running as root (Proxmox compatible)${NC}"
else
    echo -e "${YELLOW}? Warning: SQL Server not configured to run as root${NC}"
fi

echo ""
echo -e "${YELLOW}Step 7: Building and starting containers...${NC}"
cd /opt/onemanvan

# Build with no cache to ensure all fixes are applied
docker compose -f docker-compose-full.yml up -d --build

echo ""
echo -e "${YELLOW}Step 8: Waiting for SQL Server to become healthy...${NC}"
echo "This may take up to 90 seconds..."

# Wait for SQL Server
SECONDS=0
MAX_WAIT=120
while [ $SECONDS -lt $MAX_WAIT ]; do
    STATUS=$(docker inspect --format='{{.State.Health.Status}}' tradeflow-db 2>/dev/null || echo "starting")
    
    if [ "$STATUS" = "healthy" ]; then
        echo -e "${GREEN}? SQL Server is healthy!${NC}"
        break
    fi
    
    echo -n "."
    sleep 5
    
    if [ $SECONDS -ge $MAX_WAIT ]; then
        echo ""
        echo -e "${RED}? SQL Server did not become healthy in time${NC}"
        echo "Check logs: docker logs tradeflow-db"
        exit 1
    fi
done

echo ""
echo -e "${YELLOW}Step 9: Checking Web UI status...${NC}"
sleep 10

WEB_STATUS=$(docker ps --filter "name=tradeflow-webui" --format "{{.Status}}")
if [[ $WEB_STATUS == *"Up"* ]]; then
    echo -e "${GREEN}? Web UI is running${NC}"
else
    echo -e "${RED}? Web UI is not running${NC}"
    echo "Check logs: docker logs tradeflow-webui"
fi

echo ""
echo "=========================================="
echo -e "${GREEN}Deployment Complete!${NC}"
echo "=========================================="
echo ""
echo "?? Container Status:"
docker ps --format "table {{.Names}}\t{{.Status}}"
echo ""
echo "?? Access your application:"
echo "   http://192.168.100.107:5000"
echo ""
echo "?? Default Login:"
echo "   Email: admin@onemanvan.local"
echo "   Password: Admin123!"
echo ""
echo "?? Database Location:"
echo "   /media/onemanvanDB/sqldata/"
echo ""
echo "?? View Logs:"
echo "   docker compose -f /opt/onemanvan/docker-compose-full.yml logs -f"
echo ""
echo "???  Troubleshooting:"
echo "   SQL Server logs: docker logs tradeflow-db"
echo "   Web UI logs:     docker logs tradeflow-webui"
echo ""

# Final health check
echo "Running final health check..."
sleep 5

DB_HEALTHY=$(docker inspect --format='{{.State.Health.Status}}' tradeflow-db 2>/dev/null || echo "unknown")
WEB_RUNNING=$(docker ps --filter "name=tradeflow-webui" --filter "status=running" -q)

if [ "$DB_HEALTHY" = "healthy" ] && [ ! -z "$WEB_RUNNING" ]; then
    echo -e "${GREEN}??? All systems operational! ???${NC}"
    exit 0
else
    echo -e "${YELLOW}? Some services may need attention${NC}"
    if [ "$DB_HEALTHY" != "healthy" ]; then
        echo -e "  ${RED}? Database: $DB_HEALTHY${NC}"
    fi
    if [ -z "$WEB_RUNNING" ]; then
        echo -e "  ${RED}? Web UI: Not running${NC}"
    fi
    exit 1
fi
