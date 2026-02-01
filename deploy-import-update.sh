#!/bin/bash
# Deploy Import Feature Update to Server
# Run this on your server after pushing to GitHub

set -e

echo "=========================================="
echo "Deploying Import Feature Update"
echo "=========================================="
echo ""

# Navigate to app directory
cd /var/www/onemanvan 2>/dev/null || cd ~/OneManVan 2>/dev/null || {
    echo "Please cd to your OneManVan directory first"
    exit 1
}

echo "Current directory: $(pwd)"
echo ""

# Stop containers
echo "Stopping containers..."
docker-compose down

# Pull latest changes
echo ""
echo "Pulling latest changes from GitHub..."
git pull origin master

# Rebuild the web container
echo ""
echo "Rebuilding web container (this may take a few minutes)..."
docker-compose build --no-cache onemanvan-web

# Start containers
echo ""
echo "Starting containers..."
docker-compose up -d

# Wait for startup
echo ""
echo "Waiting for application to start..."
sleep 10

# Check status
echo ""
echo "Container status:"
docker-compose ps

# Show logs
echo ""
echo "Recent logs:"
docker-compose logs --tail=20 onemanvan-web

echo ""
echo "=========================================="
echo "Deployment Complete!"
echo "=========================================="
echo ""
echo "New Features Available:"
echo "  - Import buttons on all list pages"
echo "  - CSV and Excel import support"
echo "  - Bulk export/import in Settings"
echo ""
echo "Test by visiting:"
echo "  - /customers (click Import button)"
echo "  - /settings (scroll to Export/Import section)"
echo ""
