#!/bin/bash
# Quick deployment fix for Docker Compose v2 syntax
# Use this on your server

cd /opt/onemanvan

echo "Stopping containers..."
docker compose -f docker-compose-full.yml down

echo "Removing old images..."
docker compose -f docker-compose-full.yml rm -f

echo "Building and starting..."
docker compose -f docker-compose-full.yml up -d --build

echo ""
echo "Deployment complete! Check status:"
docker compose -f docker-compose-full.yml ps

echo ""
echo "Watch logs:"
echo "  docker compose -f docker-compose-full.yml logs -f webui"
