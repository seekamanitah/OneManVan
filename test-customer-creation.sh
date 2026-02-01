#!/bin/bash
# Automated verification of customer creation functionality

echo "=========================================="
echo "Customer & User Creation - Automated Test"
echo "=========================================="
echo ""

# Configuration
SERVER_URL="${1:-http://localhost:7159}"
ADMIN_EMAIL="${2:-admin@onemanvan.local}"
ADMIN_PASSWORD="${3:-Admin123!}"

echo "Testing against: $SERVER_URL"
echo ""

# Test 1: Server is reachable
echo "=== Test 1: Server Health Check ==="
if curl -s -f "$SERVER_URL/health" > /dev/null 2>&1; then
    echo "? Server is healthy"
else
    echo "? Server is not responding!"
    echo "  Start with: docker-compose up -d"
    exit 1
fi
echo ""

# Test 2: Home page loads
echo "=== Test 2: Home Page ==="
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$SERVER_URL/")
if [ "$HTTP_CODE" = "200" ]; then
    echo "? Home page loads (HTTP $HTTP_CODE)"
else
    echo "? Home page failed (HTTP $HTTP_CODE)"
fi
echo ""

# Test 3: Customers page loads
echo "=== Test 3: Customers Page ==="
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$SERVER_URL/customers")
if [ "$HTTP_CODE" = "200" ] || [ "$HTTP_CODE" = "302" ]; then
    echo "? Customers page accessible (HTTP $HTTP_CODE)"
    if [ "$HTTP_CODE" = "302" ]; then
        echo "  (Redirected to login - authentication required)"
    fi
else
    echo "? Customers page failed (HTTP $HTTP_CODE)"
fi
echo ""

# Test 4: Login page loads
echo "=== Test 4: Login Page ==="
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$SERVER_URL/Account/Login")
if [ "$HTTP_CODE" = "200" ]; then
    echo "? Login page loads"
else
    echo "? Login page issue (HTTP $HTTP_CODE)"
fi
echo ""

# Test 5: Static resources
echo "=== Test 5: Static Resources ==="

# Check Blazor JavaScript
if curl -s -f "$SERVER_URL/_framework/blazor.web.js" > /dev/null 2>&1; then
    echo "? Blazor JS loaded"
else
    echo "? Blazor JS missing - this will break buttons!"
fi

# Check Bootstrap
if curl -s -f "$SERVER_URL/lib/bootstrap/dist/css/bootstrap.min.css" > /dev/null 2>&1; then
    echo "? Bootstrap CSS loaded"
else
    echo "? Bootstrap CSS missing"
fi

# Check app CSS
if curl -s -f "$SERVER_URL/app.css" > /dev/null 2>&1; then
    echo "? App CSS loaded"
else
    echo "? App CSS missing"
fi

echo ""

# Test 6: SignalR negotiate endpoint
echo "=== Test 6: SignalR Connection ==="
NEGOTIATE_RESPONSE=$(curl -s "$SERVER_URL/_blazor/negotiate")
if echo "$NEGOTIATE_RESPONSE" | grep -q "availableTransports"; then
    echo "? SignalR negotiate endpoint working"
    echo "  Available transports found"
else
    echo "? SignalR negotiate failed"
    echo "  Response: $NEGOTIATE_RESPONSE"
fi
echo ""

# Test 7: Check if we can reach customer pages without auth
echo "=== Test 7: Customer Routes ==="

# New customer page
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$SERVER_URL/customers/new")
if [ "$HTTP_CODE" = "200" ] || [ "$HTTP_CODE" = "302" ]; then
    echo "? New customer route exists (HTTP $HTTP_CODE)"
else
    echo "? New customer route issue (HTTP $HTTP_CODE)"
fi

echo ""

# Test 8: Database connectivity (if accessible)
echo "=== Test 8: Database Check ==="
if command -v docker &> /dev/null; then
    if docker ps | grep -q tradeflow-db; then
        echo "? Database container running"
        
        # Try to query
        DB_PASSWORD="${SA_PASSWORD:-YourStrongPassword123!}"
        QUERY="SELECT COUNT(*) as CustomerCount FROM TradeFlowFSM.dbo.Customers"
        
        RESULT=$(docker exec tradeflow-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$DB_PASSWORD" -C -Q "$QUERY" -h -1 -W 2>&1)
        
        if echo "$RESULT" | grep -qE '[0-9]+'; then
            COUNT=$(echo "$RESULT" | tr -d ' ' | tr -d '\n')
            echo "? Database accessible"
            echo "  Current customers: $COUNT"
        else
            echo "? Could not query database"
        fi
    else
        echo "? Database container not found"
    fi
else
    echo "? Docker not available for database check"
fi
echo ""

# Test 9: Container logs check
echo "=== Test 9: Application Logs ==="
if command -v docker &> /dev/null; then
    if docker ps | grep -q tradeflow-webui; then
        echo "Recent application logs:"
        docker logs --tail 10 tradeflow-webui 2>&1 | head -10
        
        # Check for errors
        ERROR_COUNT=$(docker logs --tail 100 tradeflow-webui 2>&1 | grep -i "error\|exception\|fail" | wc -l)
        if [ "$ERROR_COUNT" -gt 0 ]; then
            echo ""
            echo "? Found $ERROR_COUNT error/exception lines in recent logs"
            echo "  Check full logs: docker logs tradeflow-webui"
        else
            echo "? No recent errors in logs"
        fi
    else
        echo "? Web container not found"
    fi
fi
echo ""

# Summary
echo "=========================================="
echo "Test Summary"
echo "=========================================="
echo ""
echo "? Next Steps:"
echo "  1. Open browser: $SERVER_URL"
echo "  2. Open DevTools (F12)"
echo "  3. Navigate to Customers"
echo "  4. Click 'Add Customer' button"
echo "  5. Fill form and save"
echo ""
echo "Browser Tests:"
echo "  - Console tab: Check for errors"
echo "  - Network tab (WS): Check WebSocket connection"
echo "  - Test: Type 'Blazor' in console (should not be undefined)"
echo ""
echo "Manual Test Checklist:"
echo "  [ ] Add Customer button works"
echo "  [ ] Customer form loads"
echo "  [ ] Form saves successfully"
echo "  [ ] Customer appears in list"
echo "  [ ] Edit customer works"
echo "  [ ] Validation shows errors"
echo "  [ ] Dark mode toggles"
echo ""

# Create test report file
REPORT_FILE="test-report-$(date +%Y%m%d-%H%M%S).txt"
{
    echo "Test Report - $(date)"
    echo "================================"
    echo ""
    echo "Server: $SERVER_URL"
    echo "Tests executed: $(date)"
    echo ""
    echo "Automated Tests: See output above"
    echo ""
    echo "Manual Tests Required:"
    echo "  [ ] Button click test"
    echo "  [ ] Form submission test"
    echo "  [ ] Data persistence test"
} > "$REPORT_FILE"

echo "Test report saved to: $REPORT_FILE"
echo ""
