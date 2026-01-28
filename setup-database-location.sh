#!/bin/bash
# Setup Database on /media/onemanvanDB
# Copy and paste this entire script into your SSH session

set -e

echo "=========================================="
echo "Setting up Database on /media/onemanvanDB"
echo "=========================================="
echo ""

# 1. Create directory structure
echo "Creating directories..."
sudo mkdir -p /media/onemanvanDB/sqldata
sudo mkdir -p /media/onemanvanDB/sqllogs
sudo mkdir -p /media/onemanvanDB/backups

# 2. Set ownership for mssql user (UID 10001)
echo "Setting ownership..."
sudo chown -R 10001:10001 /media/onemanvanDB

# 3. Set permissions
echo "Setting permissions..."
sudo chmod -R 755 /media/onemanvanDB

# 4. Verify setup
echo ""
echo "Verifying setup..."
ls -ln /media/onemanvanDB
df -h /media

# 5. Update docker-compose-full.yml
echo ""
echo "Updating docker-compose-full.yml..."
cd /opt/onemanvan

# Backup original
cp docker-compose-full.yml docker-compose-full.yml.backup

# Update sqlserver volumes section
sed -i 's|tradeflow-data:/var/opt/mssql/data|/media/onemanvanDB/sqldata:/var/opt/mssql/data|g' docker-compose-full.yml
sed -i 's|tradeflow-log:/var/opt/mssql/log|/media/onemanvanDB/sqllogs:/var/opt/mssql/log|g' docker-compose-full.yml
sed -i 's|tradeflow-backup:/var/opt/mssql/backup|/media/onemanvanDB/backups:/var/opt/mssql/backup|g' docker-compose-full.yml

# Remove old volume definitions (keep only webui-data)
sed -i '/tradeflow-data:/,/name: tradeflow-sqldata/d' docker-compose-full.yml
sed -i '/tradeflow-log:/,/name: tradeflow-sqllogs/d' docker-compose-full.yml
sed -i '/tradeflow-backup:/,/name: tradeflow-backups/d' docker-compose-full.yml

echo ""
echo "Verifying docker-compose changes..."
grep -A 4 "volumes:" docker-compose-full.yml | head -10

# 6. Clean up old Docker volumes
echo ""
echo "Removing old Docker volumes..."
docker volume rm tradeflow-sqldata 2>/dev/null || true
docker volume rm tradeflow-sqllogs 2>/dev/null || true
docker volume rm tradeflow-backups 2>/dev/null || true

# 7. Start containers
echo ""
echo "Starting containers..."
docker compose -f docker-compose-full.yml up -d

# 8. Watch SQL Server start
echo ""
echo "Watching SQL Server logs (Ctrl+C to exit)..."
echo "Look for: 'SQL Server is now ready for client connections'"
echo ""
docker logs -f tradeflow-db

