#!/bin/bash
# Server Deployment Script - Run on your server via SSH

echo "=========================================="
echo "OneManVan - Deploy Render Mode Fix"
echo "=========================================="
echo ""

# Navigate to application directory
# UPDATE THIS PATH to your actual installation directory
APP_DIR="/opt/onemanvan"
# or APP_DIR="/root/onemanvan"
# or APP_DIR="/home/user/onemanvan"

cd "$APP_DIR" || { echo "? Directory not found: $APP_DIR"; echo "Update APP_DIR in this script"; exit 1; }

echo "?? Working directory: $(pwd)"
echo ""

# Stop containers
echo "??  Stopping containers..."
docker-compose down

# Pull latest code
echo ""
echo "?? Pulling latest code from GitHub..."
git pull origin master

if [ $? -ne 0 ]; then
    echo "? Git pull failed!"
    echo "Try: git stash && git pull origin master"
    exit 1
fi

echo ""
echo "?? Rebuilding Docker image (this takes 2-5 minutes)..."
docker-compose build --no-cache onemanvan-web

if [ $? -ne 0 ]; then
    echo "? Build failed!"
    exit 1
fi

echo ""
echo "?? Starting containers..."
docker-compose up -d

echo ""
echo "? Waiting for startup (10 seconds)..."
sleep 10

echo ""
echo "?? Container status:"
docker-compose ps

echo ""
echo "=========================================="
echo "? Deployment Complete!"
echo "=========================================="
echo ""
echo "Test at: http://$(hostname -I | awk '{print $1}'):5024"
echo ""
echo "Test Checklist:"
echo "  1. Open login page - should load correctly"
echo "  2. Log in with admin credentials"
echo "  3. Click 'Add Customer' button - should navigate"
echo "  4. All buttons should work"
echo ""
echo "View logs: docker-compose logs -f onemanvan-web"
echo "=========================================="
