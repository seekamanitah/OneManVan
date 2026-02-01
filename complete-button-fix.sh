#!/bin/bash
# complete-button-fix.sh - Force complete rebuild with latest code

echo "=========================================="
echo "OneManVan - Complete Button Fix"
echo "=========================================="
echo ""

# Configuration
PROJECT_DIR="${1:-~/OneManVan}"

# Step 1: Navigate to project
cd "$PROJECT_DIR" || {
    echo "? Directory $PROJECT_DIR not found!"
    echo "Usage: ./complete-button-fix.sh [path-to-OneManVan]"
    exit 1
}

echo "Working directory: $(pwd)"
echo ""

# Step 2: Verify we have latest code
echo "=== Step 1: Verify Latest Code ==="
CURRENT_COMMIT=$(git log --oneline -1)
echo "Current commit: $CURRENT_COMMIT"

if ! echo "$CURRENT_COMMIT" | grep -q "4b89ae6\|Add major features"; then
    echo "? You might not have the latest code"
    echo "Pulling latest..."
    git pull origin master
fi

# Step 3: Verify the fix is present
echo ""
echo "=== Step 2: Verify Button Fix ==="
if grep -q '<div id="blazor-error-ui">' OneManVan.Web/Components/App.razor; then
    echo "? Fix is present in App.razor"
else
    echo "? FIX NOT FOUND!"
    echo ""
    echo "Current App.razor content:"
    grep -A2 "blazor-error-ui" OneManVan.Web/Components/App.razor || echo "Pattern not found"
    echo ""
    echo "? CRITICAL: The fix is not in your code!"
    echo ""
    echo "Manual fix required:"
    echo "1. Edit OneManVan.Web/Components/App.razor"
    echo "2. Find: <script id=\"blazor-error-ui\">"
    echo "3. Change to: <div id=\"blazor-error-ui\">"
    echo "4. Find: </script> (matching closing tag)"
    echo "5. Change to: </div>"
    echo ""
    read -p "Press Enter after fixing, or Ctrl+C to exit..."
fi

# Step 4: Stop everything
echo ""
echo "=== Step 3: Stop All Containers ==="
docker-compose down
echo "? Containers stopped"

# Step 5: Remove ALL related images (force rebuild)
echo ""
echo "=== Step 4: Remove Old Images ==="
echo "Removing old images to force rebuild..."

# Remove by name
docker rmi onemanvan-webui 2>/dev/null && echo "  Removed onemanvan-webui" || true
docker rmi tradeflow-webui 2>/dev/null && echo "  Removed tradeflow-webui" || true
docker rmi onemanvan_webui 2>/dev/null && echo "  Removed onemanvan_webui" || true
docker rmi onemanvan-web-1 2>/dev/null && echo "  Removed onemanvan-web-1" || true

# Find and remove any images from this project
PROJECT_IMAGES=$(docker images --format "{{.Repository}}:{{.Tag}}" | grep -E "onemanvan|tradeflow" || true)
if [ -n "$PROJECT_IMAGES" ]; then
    echo "$PROJECT_IMAGES" | xargs -r docker rmi 2>/dev/null || true
fi

echo "? Old images removed"

# Step 6: Clean Docker build cache
echo ""
echo "=== Step 5: Clean Build Cache ==="
docker builder prune -f
echo "? Build cache cleared"

# Step 7: Rebuild with NO CACHE
echo ""
echo "=== Step 6: Rebuild Container ==="
echo "This will take 3-5 minutes..."
echo ""

docker-compose build --no-cache --progress=plain webui 2>&1 | tee build.log

if [ ${PIPESTATUS[0]} -ne 0 ]; then
    echo ""
    echo "? BUILD FAILED!"
    echo ""
    echo "Check build.log for details"
    echo "Common issues:"
    echo "  - Network issues downloading packages"
    echo "  - Dockerfile syntax errors"
    echo "  - Missing dependencies"
    echo ""
    exit 1
fi

echo ""
echo "? Build successful!"

# Step 8: Start containers
echo ""
echo "=== Step 7: Start Containers ==="
docker-compose up -d

# Step 9: Wait for startup
echo ""
echo "Waiting for application to start..."
sleep 15

# Step 10: Check if running
echo ""
echo "=== Step 8: Verify Running ==="
if docker ps | grep -q "webui"; then
    echo "? Container is running"
    
    CONTAINER_NAME=$(docker ps --format "{{.Names}}" | grep -E "webui|onemanvan" | head -1)
    echo "Container: $CONTAINER_NAME"
else
    echo "? Container failed to start!"
    echo ""
    echo "Checking logs..."
    docker-compose logs webui | tail -30
    exit 1
fi

# Step 11: Verify fix in running container
echo ""
echo "=== Step 9: Verify Fix in Container ==="
CONTAINER_NAME=$(docker ps --format "{{.Names}}" | grep -E "webui|onemanvan" | head -1)

if docker exec "$CONTAINER_NAME" test -f /app/OneManVan.Web/Components/App.razor 2>/dev/null; then
    FIX_CHECK=$(docker exec "$CONTAINER_NAME" grep -c '<div id="blazor-error-ui">' /app/OneManVan.Web/Components/App.razor 2>/dev/null || echo "0")
    
    if [ "$FIX_CHECK" -gt 0 ]; then
        echo "? Fix is present in running container"
    else
        echo "? Fix NOT found in container!"
        echo "Container has old code - rebuild failed to use new code"
        exit 1
    fi
else
    echo "? Could not verify file in container"
fi

# Step 12: Test health endpoint
echo ""
echo "=== Step 10: Test Application ==="
sleep 5

if curl -s -f http://localhost:7159/health > /dev/null 2>&1; then
    echo "? Application is responding"
    HEALTH=$(curl -s http://localhost:7159/health)
    echo "Health check: $HEALTH"
else
    echo "? Application not responding on port 7159"
    echo "Check if WEBUI_PORT is different in .env"
    
    # Try common ports
    for port in 5000 8080 7159 80; do
        if curl -s -f http://localhost:$port/health > /dev/null 2>&1; then
            echo "? Found application on port $port"
            break
        fi
    done
fi

echo ""
echo "=========================================="
echo "? Deployment Complete!"
echo "=========================================="
echo ""
echo "?? CRITICAL NEXT STEPS:"
echo ""
echo "1. CLEAR BROWSER CACHE:"
echo "   - Windows/Linux: Ctrl + Shift + Delete"
echo "   - Mac: Cmd + Shift + Delete"
echo "   - Or hard refresh: Ctrl+F5 (Cmd+Shift+R on Mac)"
echo ""
echo "2. OPEN APPLICATION:"
echo "   http://your-server-ip:7159"
echo ""
echo "3. VERIFY IN BROWSER:"
echo "   a) Open DevTools (F12)"
echo "   b) Console tab - type: Blazor"
echo "      Should return an object (not undefined)"
echo "   c) Network tab - filter 'WS'"
echo "      Should see WebSocket connection"
echo ""
echo "4. TEST BUTTONS:"
echo "   - Navigate to Customers"
echo "   - Click 'Add Customer'"
echo "   - Should open form immediately"
echo ""
echo "=========================================="
echo "?? Diagnostic Info"
echo "=========================================="
echo ""
echo "Container status:"
docker ps | grep -E "webui|onemanvan"
echo ""
echo "Recent logs:"
docker-compose logs --tail 10 webui
echo ""
echo "Build log saved to: build.log"
echo ""

# Create verification file
cat > verify-fix.txt << EOF
Deployment Verification - $(date)
=====================================

Commit: $(git log --oneline -1)
Fix present in code: $(grep -q '<div id="blazor-error-ui">' OneManVan.Web/Components/App.razor && echo "YES" || echo "NO")
Container running: $(docker ps | grep -q webui && echo "YES" || echo "NO")
Fix in container: $(docker exec "$CONTAINER_NAME" grep -q '<div id="blazor-error-ui">' /app/OneManVan.Web/Components/App.razor 2>/dev/null && echo "YES" || echo "NO")

Next: Clear browser cache and test!
EOF

echo "Verification saved to: verify-fix.txt"
echo ""
