#!/bin/bash
# OneManVan Production Deployment Script for Linux
# Run this script on your Proxmox LXC container

set -e  # Exit on error

echo "=========================================="
echo "OneManVan Production Deployment"
echo "=========================================="
echo ""

# Configuration
DEPLOY_DIR="/opt/onemanvan"
COMPOSE_FILE="docker-compose-production.yml"
ENV_FILE=".env"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Check if running as root or with sudo
if [ "$EUID" -ne 0 ]; then 
    echo -e "${RED}Please run as root or with sudo${NC}"
    exit 1
fi

# Function to prompt for yes/no
confirm() {
    while true; do
        read -p "$1 [y/n] " yn
        case $yn in
            [Yy]* ) return 0;;
            [Nn]* ) return 1;;
            * ) echo "Please answer yes or no.";;
        esac
    done
}

echo -e "${BLUE}Pre-Deployment Checklist${NC}"
echo "===================================="
echo ""

# Check Docker
if ! command -v docker &> /dev/null; then
    echo -e "${YELLOW}Docker is not installed. Installing...${NC}"
    curl -fsSL https://get.docker.com -o get-docker.sh
    sh get-docker.sh
    rm get-docker.sh
    systemctl enable docker
    systemctl start docker
    echo -e "${GREEN}Docker installed successfully${NC}"
else
    echo -e "${GREEN}? Docker is installed${NC}"
fi

# Check Docker Compose
if ! command -v docker-compose &> /dev/null; then
    echo -e "${YELLOW}Docker Compose is not installed. Installing...${NC}"
    curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
    chmod +x /usr/local/bin/docker-compose
    echo -e "${GREEN}Docker Compose installed successfully${NC}"
else
    echo -e "${GREEN}? Docker Compose is installed${NC}"
fi

echo ""
echo -e "${BLUE}Configuration${NC}"
echo "===================================="

# Check for environment file
if [ ! -f "$DEPLOY_DIR/$ENV_FILE" ]; then
    echo -e "${YELLOW}No .env file found. Creating from example...${NC}"
    if [ -f "$DEPLOY_DIR/.env.production.example" ]; then
        cp "$DEPLOY_DIR/.env.production.example" "$DEPLOY_DIR/$ENV_FILE"
        echo -e "${RED}IMPORTANT: Edit $DEPLOY_DIR/$ENV_FILE and change passwords!${NC}"
        
        if confirm "Do you want to edit the .env file now?"; then
            ${EDITOR:-nano} "$DEPLOY_DIR/$ENV_FILE"
        else
            echo -e "${YELLOW}Remember to edit $DEPLOY_DIR/$ENV_FILE before running!${NC}"
            exit 1
        fi
    fi
fi

# Load environment variables
if [ -f "$DEPLOY_DIR/$ENV_FILE" ]; then
    echo -e "${GREEN}? Environment file found${NC}"
    source "$DEPLOY_DIR/$ENV_FILE"
else
    echo -e "${RED}? Environment file not found${NC}"
    exit 1
fi

# Security check
if [ "$SA_PASSWORD" == "OneManVan2025!" ] || [ "$SA_PASSWORD" == "ChangeThisPassword123!" ]; then
    echo -e "${RED}WARNING: You are using a default password!${NC}"
    if ! confirm "This is insecure. Continue anyway?"; then
        echo "Please edit $DEPLOY_DIR/$ENV_FILE and change SA_PASSWORD"
        exit 1
    fi
fi

echo ""
echo -e "${BLUE}Deployment${NC}"
echo "===================================="

# Create directory
mkdir -p "$DEPLOY_DIR"
cd "$DEPLOY_DIR"

# Stop existing containers
if [ -f "$COMPOSE_FILE" ]; then
    echo "Stopping existing containers..."
    docker-compose -f "$COMPOSE_FILE" down || true
fi

# Backup existing data
if docker volume ls | grep -q onemanvan-sqldata; then
    echo -e "${YELLOW}Existing database volume found${NC}"
    if confirm "Do you want to backup the database first?"; then
        BACKUP_NAME="backup-$(date +%Y%m%d-%H%M%S).bak"
        echo "Creating backup: $BACKUP_NAME"
        docker run --rm \
            -v onemanvan-sqldata:/var/opt/mssql/data \
            -v $(pwd):/backup \
            mcr.microsoft.com/mssql/server:2022-latest \
            /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" \
            -Q "BACKUP DATABASE OneManVanDB TO DISK='/backup/$BACKUP_NAME'" || true
    fi
fi

# Pull latest images
echo "Pulling Docker images..."
docker-compose -f "$COMPOSE_FILE" pull

# Build and start containers
echo "Building and starting containers..."
docker-compose -f "$COMPOSE_FILE" up -d --build

# Wait for services
echo "Waiting for services to start..."
sleep 10

# Check health
echo ""
echo -e "${BLUE}Health Check${NC}"
echo "===================================="

RETRY_COUNT=0
MAX_RETRIES=30

# Wait for SQL Server
echo -n "Waiting for SQL Server."
while [ $RETRY_COUNT -lt $MAX_RETRIES ]; do
if docker exec onemanvan-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -Q "SELECT 1" &> /dev/null; then
        echo ""
        echo -e "${GREEN}? SQL Server is healthy${NC}"
        break
    fi
    echo -n "."
    sleep 2
    RETRY_COUNT=$((RETRY_COUNT+1))
done

if [ $RETRY_COUNT -eq $MAX_RETRIES ]; then
    echo ""
    echo -e "${RED}? SQL Server failed to start${NC}"
    echo "Check logs: docker logs onemanvan-db"
    exit 1
fi

# Wait for Web UI
RETRY_COUNT=0
echo -n "Waiting for Web UI."
while [ $RETRY_COUNT -lt $MAX_RETRIES ]; do
    if curl -f http://localhost:8080/health &> /dev/null; then
        echo ""
        echo -e "${GREEN}? Web UI is healthy${NC}"
        break
    fi
    echo -n "."
    sleep 2
    RETRY_COUNT=$((RETRY_COUNT+1))
done

if [ $RETRY_COUNT -eq $MAX_RETRIES ]; then
    echo ""
    echo -e "${YELLOW}? Web UI may still be starting${NC}"
    echo "Check logs: docker logs onemanvan-webui"
fi

echo ""
echo "=========================================="
echo -e "${GREEN}Deployment Complete!${NC}"
echo "=========================================="
echo ""

# Display status
docker-compose -f "$COMPOSE_FILE" ps

echo ""
echo -e "${BLUE}Access Information${NC}"
echo "===================================="
echo "Web UI:"
echo "  - Local: http://localhost:${WEB_PORT:-5000}"
echo "  - Network: http://$(hostname -I | awk '{print $1}'):${WEB_PORT:-5000}"
echo ""
echo "SQL Server:"
echo "  - Server: $(hostname -I | awk '{print $1}'),${SQL_PORT:-1433}"
echo "  - Database: ${DB_NAME:-OneManVanDB}"
echo "  - Username: sa"
echo "  - Password: ****** (from .env file)"
echo ""
echo "Default Admin Login:"
echo "  - Email: admin@onemanvan.local"
echo "  - Password: Admin123!"
echo "  - ${RED}CHANGE THIS PASSWORD IMMEDIATELY!${NC}"
echo ""
echo -e "${BLUE}Management Commands${NC}"
echo "===================================="
echo "View logs:     docker-compose -f $COMPOSE_FILE logs -f"
echo "Stop:          docker-compose -f $COMPOSE_FILE down"
echo "Start:         docker-compose -f $COMPOSE_FILE up -d"
echo "Restart:       docker-compose -f $COMPOSE_FILE restart"
echo "Status:        docker-compose -f $COMPOSE_FILE ps"
echo ""
echo -e "${BLUE}Next Steps${NC}"
echo "===================================="
echo "1. Access Web UI and change admin password"
echo "2. Go to Settings > Database Configuration"
echo "3. Verify database connection"
echo "4. Configure firewall: ufw allow ${WEB_PORT:-5000}/tcp"
echo "5. Set up automated backups"
echo "6. Configure HTTPS (recommended)"
echo "7. Test mobile app connectivity"
echo ""
echo "=========================================="
