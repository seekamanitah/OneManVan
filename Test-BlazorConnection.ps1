# Blazor SignalR Connection Diagnostic Script
# Run this on your SERVER to check connection status

Write-Host "=======================================" -ForegroundColor Cyan
Write-Host "Blazor SignalR Connection Diagnostic" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host ""

# Function to test endpoint
function Test-BlazorEndpoint {
    param([string]$Url)
    
    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5
        return @{
            Success = $true
            StatusCode = $response.StatusCode
            Content = $response.Content
        }
    } catch {
        return @{
            Success = $false
            Error = $_.Exception.Message
        }
    }
}

# Test 1: Basic health check
Write-Host "Test 1: Health Check" -ForegroundColor Yellow
$health = Test-BlazorEndpoint "http://localhost:5000/health"
if ($health.Success) {
    Write-Host "? Server is responding" -ForegroundColor Green
    Write-Host "  Status: $($health.StatusCode)" -ForegroundColor Gray
} else {
    Write-Host "? Server not responding!" -ForegroundColor Red
    Write-Host "  Error: $($health.Error)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Possible fixes:" -ForegroundColor Yellow
    Write-Host "  1. Check if Docker container is running: docker ps" -ForegroundColor White
    Write-Host "  2. Check if port 5000 is exposed: docker-compose.yml" -ForegroundColor White
    Write-Host "  3. Restart container: docker-compose restart" -ForegroundColor White
    exit 1
}

Write-Host ""

# Test 2: Blazor negotiate endpoint
Write-Host "Test 2: SignalR Negotiate Endpoint" -ForegroundColor Yellow
$negotiate = Test-BlazorEndpoint "http://localhost:5000/_blazor/negotiate"
if ($negotiate.Success) {
    Write-Host "? SignalR endpoint is accessible" -ForegroundColor Green
    
    # Try to parse JSON
    try {
        $json = $negotiate.Content | ConvertFrom-Json
        if ($json.availableTransports) {
            Write-Host "? Available transports found:" -ForegroundColor Green
            foreach ($transport in $json.availableTransports) {
                Write-Host "  - $($transport.transport)" -ForegroundColor Gray
            }
        }
    } catch {
        Write-Host "? Could not parse negotiate response" -ForegroundColor Yellow
    }
} else {
    Write-Host "? SignalR negotiate failed!" -ForegroundColor Red
    Write-Host "  Error: $($negotiate.Error)" -ForegroundColor Red
}

Write-Host ""

# Test 3: Static files
Write-Host "Test 3: Static Files (JavaScript)" -ForegroundColor Yellow
$js = Test-BlazorEndpoint "http://localhost:5000/_framework/blazor.web.js"
if ($js.Success) {
    Write-Host "? Blazor JavaScript is loading" -ForegroundColor Green
} else {
    Write-Host "? Blazor JavaScript not accessible!" -ForegroundColor Red
    Write-Host "  This could explain why clicks don't work" -ForegroundColor Yellow
}

Write-Host ""

# Test 4: Check for reverse proxy
Write-Host "Test 4: Reverse Proxy Detection" -ForegroundColor Yellow
Write-Host "? Cannot auto-detect reverse proxy from this machine" -ForegroundColor Yellow
Write-Host ""
Write-Host "If you're using a reverse proxy (nginx/apache):" -ForegroundColor White
Write-Host "  1. Upload fix-blazor-signalr.sh to your server" -ForegroundColor White
Write-Host "  2. Run: chmod +x fix-blazor-signalr.sh && sudo ./fix-blazor-signalr.sh" -ForegroundColor White

Write-Host ""
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host "Browser Tests" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "On your SERVER, open browser DevTools (F12) and run:" -ForegroundColor White
Write-Host ""
Write-Host "// Check if Blazor is loaded" -ForegroundColor Gray
Write-Host "Blazor" -ForegroundColor Yellow
Write-Host ""
Write-Host "// Check WebSocket connection" -ForegroundColor Gray
Write-Host "// Go to Network tab -> WS filter -> Look for '_blazor?id='" -ForegroundColor Gray
Write-Host ""
Write-Host "If no WebSocket connection appears:" -ForegroundColor Red
Write-Host "  ? Your reverse proxy is blocking WebSocket connections" -ForegroundColor Red
Write-Host "  ? Run the fix-blazor-signalr.sh script on the server" -ForegroundColor Red
Write-Host ""
Write-Host "If Blazor is undefined:" -ForegroundColor Red
Write-Host "  ? JavaScript files not loading (check static files)" -ForegroundColor Red
Write-Host "  ? Check browser console for 404 errors on .js files" -ForegroundColor Red
Write-Host ""

# Display summary
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host "Quick Fixes" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. If using nginx - add WebSocket support:" -ForegroundColor White
Write-Host @"
   location / {
       proxy_pass http://localhost:5000;
       proxy_http_version 1.1;
       proxy_set_header Upgrade `$http_upgrade;
       proxy_set_header Connection `$connection_upgrade;
   }
   
   map `$http_upgrade `$connection_upgrade {
       default upgrade;
       '' close;
   }
"@ -ForegroundColor Gray

Write-Host ""
Write-Host "2. If using Docker directly - ensure ports are exposed:" -ForegroundColor White
Write-Host "   ports:" -ForegroundColor Gray
Write-Host "     - '5000:8080'" -ForegroundColor Gray

Write-Host ""
Write-Host "3. Check firewall:" -ForegroundColor White
Write-Host "   sudo ufw allow 5000/tcp" -ForegroundColor Gray

Write-Host ""
