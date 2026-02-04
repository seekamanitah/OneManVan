#!/bin/bash
# OneManVan - Fix Blazor SignalR Connection Issues

echo "========================================="
echo "OneManVan Blazor Connection Fix"
echo "========================================="
echo ""

# Load environment variables
if [ -f .env ]; then
    export $(cat .env | grep -v '^#' | xargs)
    echo "? Loaded .env file"
else
    echo "? No .env file found"
fi

# Determine the actual port
ACTUAL_PORT=${WEBUI_PORT:-7159}
echo "Web UI Port: $ACTUAL_PORT"
echo ""

# Test 1: Check if container is running
echo "=== Test 1: Container Status ==="
if docker ps | grep -q onemanvan-webui; then
    echo "? onemanvan-webui container is running"
    docker ps | grep onemanvan-webui
else
    echo "? Container not running!"
    echo ""
    echo "Start with:"
    echo "  docker-compose up -d"
    exit 1
fi

echo ""

# Test 2: Check if port is accessible
echo "=== Test 2: Health Check ==="
if curl -s http://localhost:$ACTUAL_PORT/health > /dev/null; then
    echo "? App is responding on port $ACTUAL_PORT"
    curl -s http://localhost:$ACTUAL_PORT/health | jq . 2>/dev/null || curl -s http://localhost:$ACTUAL_PORT/health
else
    echo "? App not responding on port $ACTUAL_PORT"
    echo ""
    echo "Check container logs:"
    echo "  docker logs onemanvan-webui"
    exit 1
fi

echo ""

# Test 3: SignalR negotiate endpoint
echo "=== Test 3: SignalR Endpoint ==="
NEGOTIATE_RESPONSE=$(curl -s -w "\n%{http_code}" http://localhost:$ACTUAL_PORT/_blazor/negotiate)
HTTP_CODE=$(echo "$NEGOTIATE_RESPONSE" | tail -n1)
RESPONSE_BODY=$(echo "$NEGOTIATE_RESPONSE" | head -n-1)

if [ "$HTTP_CODE" = "200" ]; then
    echo "? SignalR endpoint is working"
    echo "Response: $RESPONSE_BODY" | head -c 200
    echo "..."
else
    echo "? SignalR endpoint returned: $HTTP_CODE"
    echo "Response: $RESPONSE_BODY"
fi

echo ""

# Test 4: Check firewall
echo "=== Test 4: Firewall ==="
if command -v ufw &> /dev/null; then
    if sudo ufw status | grep -q "$ACTUAL_PORT.*ALLOW"; then
        echo "? Firewall allows port $ACTUAL_PORT"
    else
        echo "? Firewall may be blocking port $ACTUAL_PORT"
        echo ""
        echo "To fix:"
        echo "  sudo ufw allow $ACTUAL_PORT/tcp"
        echo "  sudo ufw reload"
    fi
else
    echo "? UFW not installed, skipping firewall check"
fi

echo ""

# Test 5: Check if using reverse proxy
echo "=== Test 5: Reverse Proxy Check ==="
if systemctl is-active --quiet nginx; then
    echo "? Nginx is running!"
    echo ""
    echo "If you're accessing through nginx, it needs WebSocket support."
    echo "Add this to your nginx config:"
    echo ""
    cat << 'EOF'
map $http_upgrade $connection_upgrade {
    default upgrade;
    '' close;
}

server {
    listen 80;
    server_name yourdomain.com;
    
    location / {
        proxy_pass http://localhost:7159;  # ? Your actual port
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection $connection_upgrade;
        proxy_set_header Host $host;
        proxy_buffering off;
    }
}
EOF
    echo ""
    echo "Would you like to update nginx config automatically? (y/n)"
    read -r response
    if [[ "$response" =~ ^[Yy]$ ]]; then
        # Backup existing config
        if [ -f /etc/nginx/sites-available/onemanvan ]; then
            sudo cp /etc/nginx/sites-available/onemanvan /etc/nginx/sites-available/onemanvan.backup.$(date +%Y%m%d_%H%M%S)
        fi
        
        # Create new config
        sudo tee /etc/nginx/sites-available/onemanvan > /dev/null << EOF
map \$http_upgrade \$connection_upgrade {
    default upgrade;
    '' close;
}

server {
    listen 80;
    server_name _;
    
    proxy_read_timeout 3600s;
    proxy_send_timeout 3600s;
    
    location / {
        proxy_pass http://localhost:$ACTUAL_PORT;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection \$connection_upgrade;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_buffering off;
        proxy_cache_bypass \$http_upgrade;
    }
}
EOF
        
        # Enable and reload
        sudo ln -sf /etc/nginx/sites-available/onemanvan /etc/nginx/sites-enabled/
        sudo nginx -t && sudo systemctl reload nginx
        echo "? Nginx configuration updated!"
    fi
elif systemctl is-active --quiet apache2; then
    echo "? Apache is running - needs WebSocket support"
    echo "Enable modules:"
    echo "  sudo a2enmod proxy proxy_http proxy_wstunnel"
else
    echo "? No reverse proxy detected"
    echo "Direct Docker access - should work fine"
fi

echo ""

# Test 6: Container logs check
echo "=== Test 6: Recent Container Logs ==="
echo "Last 20 lines:"
docker logs --tail 20 onemanvan-webui
echo ""

# Test 7: Network connectivity
echo "=== Test 7: Network Connectivity ==="
if docker network inspect onemanvan-network &> /dev/null; then
    echo "? Docker network exists"
else
    echo "? Docker network issue"
fi

echo ""
echo "========================================="
echo "Summary & Next Steps"
echo "========================================="
echo ""

# Determine likely issue
if [ "$HTTP_CODE" = "200" ]; then
    if systemctl is-active --quiet nginx || systemctl is-active --quiet apache2; then
        echo "?? LIKELY ISSUE: Reverse proxy blocking WebSocket"
        echo ""
        echo "? FIX: Update reverse proxy config (see above)"
    else
        echo "?? LIKELY ISSUE: Browser can't reach server"
        echo ""
        echo "? FIX OPTIONS:"
        echo "  1. Check firewall: sudo ufw allow $ACTUAL_PORT/tcp"
        echo "  2. Access using: http://YOUR_SERVER_IP:$ACTUAL_PORT"
        echo "  3. Check router/cloud security groups"
    fi
else
    echo "?? LIKELY ISSUE: Application not running correctly"
    echo ""
    echo "? FIX: Check container logs and restart:"
    echo "  docker logs onemanvan-webui"
    echo "  docker-compose restart webui"
fi

echo ""
echo "?? BROWSER DIAGNOSTIC:"
echo "On the server, open browser and:"
echo "  1. Press F12 (DevTools)"
echo "  2. Go to Console tab"
echo "  3. Type: Blazor"
echo "  4. Go to Network tab -> WS filter"
echo "  5. Look for WebSocket connection to _blazor"
echo ""
echo "If no WebSocket = Reverse proxy issue"
echo "If Blazor undefined = Static files not loading"
echo ""

# Create a test HTML file to check connection
echo "=== Creating Quick Test File ==="
cat > /tmp/blazor-test.html << 'TESTEOF'
<!DOCTYPE html>
<html>
<head>
    <title>Blazor Connection Test</title>
    <style>
        body { font-family: Arial; padding: 20px; background: #1e1e1e; color: #fff; }
        .test { margin: 10px 0; padding: 10px; background: #2d2d2d; border-radius: 5px; }
        .pass { border-left: 4px solid #4caf50; }
        .fail { border-left: 4px solid #f44336; }
        code { background: #000; padding: 2px 6px; border-radius: 3px; }
    </style>
</head>
<body>
    <h1>OneManVan - Blazor Connection Test</h1>
    <div id="results"></div>
    
    <script>
        const results = document.getElementById('results');
        
        function addTest(name, passed, details) {
            const div = document.createElement('div');
            div.className = 'test ' + (passed ? 'pass' : 'fail');
            div.innerHTML = `<strong>${passed ? '?' : '?'} ${name}</strong><br>${details}`;
            results.appendChild(div);
        }
        
        // Test 1: Blazor object exists
        const blazorExists = typeof Blazor !== 'undefined';
        addTest('Blazor Object Loaded', blazorExists, 
            blazorExists ? 'Blazor JavaScript loaded successfully' : 'Blazor JS not found - check static files');
        
        // Test 2: Try to load framework JS
        fetch('/_framework/blazor.web.js')
            .then(r => {
                addTest('Blazor Framework JS', r.ok, 'blazor.web.js is accessible');
            })
            .catch(e => {
                addTest('Blazor Framework JS', false, 'Could not load blazor.web.js: ' + e.message);
            });
        
        // Test 3: SignalR negotiate
        fetch('/_blazor/negotiate')
            .then(r => r.json())
            .then(data => {
                const hasTransports = data.availableTransports && data.availableTransports.length > 0;
                addTest('SignalR Negotiate', hasTransports, 
                    'Available transports: ' + data.availableTransports.map(t => t.transport).join(', '));
            })
            .catch(e => {
                addTest('SignalR Negotiate', false, 'Negotiate failed: ' + e.message);
            });
        
        // Test 4: WebSocket support
        const wsSupported = 'WebSocket' in window;
        addTest('Browser WebSocket Support', wsSupported,
            wsSupported ? 'Browser supports WebSocket' : 'Browser does not support WebSocket');
        
        // Instructions
        setTimeout(() => {
            const instructions = document.createElement('div');
            instructions.style = 'margin-top: 30px; padding: 20px; background: #2d2d2d; border-radius: 5px;';
            instructions.innerHTML = `
                <h2>Next Steps:</h2>
                <ol>
                    <li>Open DevTools (F12)</li>
                    <li>Go to <strong>Network</strong> tab</li>
                    <li>Filter by <strong>WS</strong> (WebSocket)</li>
                    <li>Click any button on the main site</li>
                    <li>Look for WebSocket connection to <code>_blazor?id=...</code></li>
                </ol>
                <p><strong>If no WebSocket appears:</strong> Your reverse proxy is blocking the connection</p>
                <p><strong>If connection closes immediately:</strong> Check proxy timeout settings</p>
            `;
            results.appendChild(instructions);
        }, 2000);
    </script>
</body>
</html>
TESTEOF

echo "? Created test file: /tmp/blazor-test.html"
echo ""
echo "To test in browser:"
echo "  1. Copy /tmp/blazor-test.html to your web server root"
echo "  2. Access: http://YOUR_SERVER_IP:$ACTUAL_PORT/blazor-test.html"
echo ""
