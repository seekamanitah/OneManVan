#!/bin/bash
# Fix Database Configuration - Switch from SQLite to SQL Server
# This script updates the web app to use SQL Server instead of SQLite

set -e  # Exit on any error

echo "=================================================="
echo "   OneManVan - Database Configuration Fix"
echo "=================================================="
echo ""

# Check if running on server
if [ ! -d "OneManVan.Web" ]; then
    echo "? Error: Must run from project root directory"
    echo "   Current directory: $(pwd)"
    exit 1
fi

echo "? Found OneManVan.Web directory"
echo ""

echo "?? Deployment Checklist:"
echo "========================"
echo ""

# Step 1: Stop containers
echo "1??  Stopping existing containers..."
docker compose down 2>/dev/null || echo "   (No containers running)"
echo "   ? Containers stopped"
echo ""

# Step 2: Clean up old database files
echo "2??  Cleaning up SQLite database files..."
rm -f OneManVan.Web/AppData/app.db* 2>/dev/null || true
rm -f OneManVan.Web/AppData/business.db* 2>/dev/null || true
rm -f OneManVan.Web/Data/*.db* 2>/dev/null || true
echo "   ? SQLite files cleaned"
echo ""

# Step 3: Rebuild web image
echo "3??  Rebuilding web application image..."
docker compose build --no-cache webui
echo "   ? Image rebuilt"
echo ""

# Step 4: Start services
echo "4??  Starting services..."
docker compose up -d
echo "   ? Services started"
echo ""

# Step 5: Wait for SQL Server
echo "5??  Waiting for SQL Server to be ready..."
for i in {1..30}; do
    if docker exec onemanvan-db /opt/mssql-tools18/bin/sqlcmd \
        -S localhost -U sa -P 'OneManVan2025!' -C -Q "SELECT 1" &> /dev/null; then
        echo "   ? SQL Server is ready!"
        break
    else
        echo "   ? Waiting for SQL Server... (attempt $i/30)"
        sleep 2
    fi
done
echo ""

# Step 6: Check web UI
echo "6??  Checking web UI startup..."
sleep 10
docker logs onemanvan-webui --tail 20
echo ""

echo "=================================================="
echo "   ? Configuration Fix Complete!"
echo "=================================================="
echo ""
echo "?? Access your application:"
echo "   http://$(hostname -I | awk '{print $1}'):7159"
echo ""
echo "?? Check status:"
echo "   docker ps"
echo "   docker logs onemanvan-webui"
echo "   docker logs onemanvan-db"
echo ""
