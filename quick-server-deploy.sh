#!/bin/bash
# Quick Server Deployment - Copy/Paste into SSH
# Run this on your server: ssh root@192.168.100.107

echo "=========================================="
echo "OneManVan Server Deployment - Quick Start"
echo "=========================================="
echo ""

# Step 1: Stop old containers
cd /opt/onemanvan 2>/dev/null && docker compose -f docker-compose-full.yml down || true

# Step 2: Backup old deployment
if [ -d "/opt/onemanvan" ]; then
    mv /opt/onemanvan /opt/onemanvan-backup-$(date +%Y%m%d-%H%M%S)
fi

# Step 3: Extract new deployment
cd /root
unzip -q deployment-FINAL.zip -d /opt/onemanvan || unzip -q deployment.zip -d /opt/onemanvan

# Step 4: Verify Data folder exists
if [ -f "/opt/onemanvan/OneManVan.Shared/Data/OneManVanDbContext.cs" ]; then
    echo "? Data folder verified"
else
    echo "? ERROR: Data folder missing!"
    exit 1
fi

# Step 5: Ensure database location permissions
sudo mkdir -p /media/onemanvanDB/{sqldata,sqllogs,backups}
sudo chown -R 10001:10001 /media/onemanvanDB 2>/dev/null || true
sudo chmod -R 755 /media/onemanvanDB

# Step 6: Start containers
cd /opt/onemanvan
docker compose -f docker-compose-full.yml up -d --build

echo ""
echo "Waiting for SQL Server to start (90 seconds)..."
sleep 90

# Step 7: Check status
echo ""
echo "=========================================="
echo "Deployment Complete!"
echo "=========================================="
docker ps --format "table {{.Names}}\t{{.Status}}"
echo ""
echo "?? Access: http://192.168.100.107:5000"
echo "?? Login: admin@onemanvan.local / Admin123!"
echo ""
echo "?? View logs:"
echo "  docker logs -f tradeflow-webui"
echo "  docker logs -f tradeflow-db"
echo ""
