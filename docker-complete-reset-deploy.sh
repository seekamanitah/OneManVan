#!/bin/bash
# Complete Docker Cleanup and Fresh Deployment Script for OneManVan
# This script completely removes all old containers, images, volumes and redeploys

set -e  # Exit on any error

echo "=================================================="
echo "OneManVan - Complete Docker Reset & Redeploy"
echo "=================================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
PROJECT_NAME="onemanvan"
COMPOSE_FILE="docker-compose.yml"
ENV_FILE=".env"

echo -e "${YELLOW}??  WARNING: This will COMPLETELY DELETE all OneManVan Docker resources!${NC}"
echo "This includes:"
echo "  - All containers (running and stopped)"
echo "  - All images"
echo "  - All volumes (DATABASE WILL BE DELETED!)"
echo "  - All networks"
echo ""
read -p "Are you sure you want to continue? (type 'yes' to confirm): " confirm

if [ "$confirm" != "yes" ]; then
    echo -e "${RED}? Cancelled by user${NC}"
    exit 1
fi

echo ""
echo -e "${BLUE}Step 1: Stopping all OneManVan containers...${NC}"
# Stop all containers with onemanvan in the name
docker ps -a --filter "name=onemanvan" --format "{{.Names}}" | xargs -r docker stop
docker ps -a --filter "name=tradeflow" --format "{{.Names}}" | xargs -r docker stop
echo -e "${GREEN}? Containers stopped${NC}"

echo ""
echo -e "${BLUE}Step 2: Removing all OneManVan containers...${NC}"
docker ps -a --filter "name=onemanvan" --format "{{.Names}}" | xargs -r docker rm -f
docker ps -a --filter "name=tradeflow" --format "{{.Names}}" | xargs -r docker rm -f
echo -e "${GREEN}? Containers removed${NC}"

echo ""
echo -e "${BLUE}Step 3: Removing all OneManVan volumes (DATABASE DELETION)...${NC}"
docker volume ls --filter "name=onemanvan" --format "{{.Name}}" | xargs -r docker volume rm
docker volume ls --filter "name=tradeflow" --format "{{.Name}}" | xargs -r docker volume rm
echo -e "${GREEN}? Volumes removed${NC}"

echo ""
echo -e "${BLUE}Step 4: Removing all OneManVan images...${NC}"
docker images --filter "reference=onemanvan*" --format "{{.Repository}}:{{.Tag}}" | xargs -r docker rmi -f
docker images --filter "reference=tradeflow*" --format "{{.Repository}}:{{.Tag}}" | xargs -r docker rmi -f
echo -e "${GREEN}? Images removed${NC}"

echo ""
echo -e "${BLUE}Step 5: Removing OneManVan networks...${NC}"
docker network ls --filter "name=onemanvan" --format "{{.Name}}" | xargs -r docker network rm 2>/dev/null || true
docker network ls --filter "name=tradeflow" --format "{{.Name}}" | xargs -r docker network rm 2>/dev/null || true
echo -e "${GREEN}? Networks removed${NC}"

echo ""
echo -e "${BLUE}Step 6: Pruning Docker system...${NC}"
docker system prune -f
echo -e "${GREEN}? System pruned${NC}"

echo ""
echo -e "${GREEN}=================================================="
echo "? Complete cleanup finished!"
echo "=================================================="
echo ""
echo -e "${YELLOW}Starting fresh deployment...${NC}"
echo ""

# Check if .env exists
if [ ! -f "$ENV_FILE" ]; then
    echo -e "${YELLOW}??  .env file not found. Creating from example...${NC}"
    if [ -f ".env.example" ]; then
        cp .env.example .env
        echo -e "${GREEN}? Created .env from .env.example${NC}"
        echo -e "${YELLOW}??  Please edit .env with your configuration before continuing${NC}"
        read -p "Press Enter after editing .env to continue..."
    else
        echo -e "${RED}? .env.example not found. Please create .env manually${NC}"
        exit 1
    fi
fi

echo ""
echo -e "${BLUE}Step 7: Building fresh Docker images...${NC}"
docker compose -f $COMPOSE_FILE build --no-cache
echo -e "${GREEN}? Images built${NC}"

echo ""
echo -e "${BLUE}Step 8: Starting containers...${NC}"
docker compose -f $COMPOSE_FILE up -d
echo -e "${GREEN}? Containers started${NC}"

echo ""
echo -e "${BLUE}Step 9: Waiting for services to be ready...${NC}"
sleep 10

# Wait for SQL Server
echo "Waiting for SQL Server..."
until docker exec tradeflow-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'TradeFlow2025!' -Q "SELECT 1" &> /dev/null
do
    echo -n "."
    sleep 2
done
echo ""
echo -e "${GREEN}? SQL Server is ready${NC}"

# Wait for Web UI
echo "Waiting for Web UI..."
sleep 5
until curl -f http://localhost:7159/health &> /dev/null
do
    echo -n "."
    sleep 2
done
echo ""
echo -e "${GREEN}? Web UI is ready${NC}"

echo ""
echo -e "${GREEN}=================================================="
echo "? DEPLOYMENT COMPLETE!"
echo "=================================================="
echo ""
echo "Access your application:"
echo -e "  ${BLUE}Web UI:${NC}      http://localhost:7159"
echo -e "  ${BLUE}SQL Server:${NC}  localhost:1433"
echo -e "  ${BLUE}Username:${NC}    sa"
echo -e "  ${BLUE}Password:${NC}    TradeFlow2025!"
echo ""
echo "View logs:"
echo "  docker compose logs -f"
echo ""
echo "Stop services:"
echo "  docker compose down"
echo ""
echo -e "${GREEN}Happy testing! ??${NC}"
