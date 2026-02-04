#!/bin/bash
# OneManVan Docker Deployment Script for Linux
# Run this script on your Proxmox LXC container

set -e  # Exit on error

echo "=========================================="
echo "OneManVan Web Application Deployment"
echo "=========================================="
echo ""

# Configuration
DEPLOY_DIR="/opt/onemanvan"
COMPOSE_FILE="docker-compose-full.yml"

# Check if running as root or with sudo
if [ "$EUID" -ne 0 ]; then 
    echo "Please run as root or with sudo"
    exit 1
fi

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    echo "Docker is not installed. Installing Docker..."
    curl -fsSL https://get.docker.com -o get-docker.sh
    sh get-docker.sh
    rm get-docker.sh
    systemctl enable docker
    systemctl start docker
    echo "Docker installed successfully"
fi

# Check if Docker Compose is installed
if ! command -v docker-compose &> /dev/null; then
    echo "Docker Compose is not installed. Installing..."
    curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
    chmod +x /usr/local/bin/docker-compose
    echo "Docker Compose installed successfully"
fi

echo ""
echo "Step 1: Creating deployment directory..."
mkdir -p "$DEPLOY_DIR"
cd "$DEPLOY_DIR"

echo ""
echo "Step 2: Stopping existing containers (if any)..."
if [ -f "$COMPOSE_FILE" ]; then
    docker-compose -f "$COMPOSE_FILE" down || true
fi

echo ""
echo "Step 3: Extracting deployment files..."
# Assuming deployment.zip is in current directory
if [ -f "deployment.zip" ]; then
    unzip -o deployment.zip
    echo "Files extracted successfully"
else
    echo "deployment.zip not found in current directory"
    echo "Please ensure deployment.zip is uploaded to $(pwd)"
    exit 1
fi

echo ""
echo "Step 4: Setting permissions..."
chmod +x deploy-to-docker.sh
chmod 644 "$COMPOSE_FILE"

echo ""
echo "Step 5: Building and starting containers..."
docker-compose -f "$COMPOSE_FILE" up -d --build

echo ""
echo "Step 6: Waiting for services to be healthy..."
sleep 10

echo ""
echo "=========================================="
echo "Deployment Complete!"
echo "=========================================="
echo ""
echo "Services Status:"
docker-compose -f "$COMPOSE_FILE" ps
echo ""
echo "Web UI Access:"
echo "  - Local: http://localhost:5000"
echo "  - Network: http://$(hostname -I | awk '{print $1}'):5000"
echo ""
echo "SQL Server Access:"
echo "  - Server: $(hostname -I | awk '{print $1}'),1433"
echo "  - Username: sa"
echo "  - Password: OneManVan2025!"
echo ""
echo "View logs with: docker-compose -f $COMPOSE_FILE logs -f"
echo "Stop services with: docker-compose -f $COMPOSE_FILE down"
echo "=========================================="
