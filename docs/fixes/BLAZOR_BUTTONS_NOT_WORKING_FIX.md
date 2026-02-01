# Blazor Server - Buttons Not Working Fix

## Symptom
- Web app loads successfully
- Can sign in
- **BUT** clicking buttons (Add Customer, Dark Mode toggle, etc.) does nothing
- **No errors in browser console**

## Root Cause
**Blazor Server SignalR/WebSocket connection is failing**

Blazor Server requires a persistent WebSocket connection for interactivity. Without it:
- Page renders initially (server-side)
- But button clicks can't reach the server
- No JavaScript errors because the JS loaded, but SignalR connection failed silently

---

## ?? Diagnosis Steps

### Step 1: Check Browser Console

1. Open browser DevTools (F12)
2. Go to **Console** tab
3. Type: `Blazor` and press Enter

**Expected**: Should return an object with Blazor properties
**Problem**: Returns `undefined` ? JavaScript not loading

### Step 2: Check Network WebSocket

1. Open DevTools (F12)
2. Go to **Network** tab
3. Filter by **WS** (WebSocket)
4. Reload the page
5. Look for connection to `_blazor?id=...`

**Expected**: WebSocket connection with status 101 (Switching Protocols)
**Problem**: 
- No WebSocket connection ? Reverse proxy blocking
- Status 400/403 ? Configuration issue
- No connection attempt ? Blazor JS not loading

### Step 3: Test SignalR Endpoint

From your server, run:
```bash
curl -i http://localhost:5000/_blazor/negotiate
```

**Expected Response:**
```
HTTP/1.1 200 OK
Content-Type: application/json

{"negotiateVersion":1,"connectionId":"...","availableTransports":[...]}
```

**Problem**: 404 or timeout ? App not running correctly

---

## ?? Solutions

### Solution 1: Fix Nginx Configuration (Most Common)

**On your server, run:**

```bash
# Upload and run the fix script
chmod +x fix-blazor-signalr.sh
sudo ./fix-blazor-signalr.sh
```

**Or manually update nginx:**

```bash
sudo nano /etc/nginx/sites-available/onemanvan
```

Add this configuration:

```nginx
# WebSocket upgrade map (add OUTSIDE server block)
map $http_upgrade $connection_upgrade {
    default upgrade;
    '' close;
}

server {
    listen 80;
    server_name yourdomain.com;
    
    # Increase timeouts for Blazor circuits
    proxy_read_timeout 3600s;
    proxy_send_timeout 3600s;
    
    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        
        # CRITICAL: WebSocket upgrade headers
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection $connection_upgrade;
        
        # Standard headers
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        
        # Disable buffering for real-time
        proxy_buffering off;
        proxy_cache_bypass $http_upgrade;
    }
}
```

**Test and reload:**
```bash
sudo nginx -t
sudo systemctl reload nginx
```

---

### Solution 2: Fix Apache Configuration

```bash
# Enable required modules
sudo a2enmod proxy
sudo a2enmod proxy_http
sudo a2enmod proxy_wstunnel

# Create config
sudo nano /etc/apache2/sites-available/onemanvan.conf
```

Add:
```apache
<VirtualHost *:80>
    ServerName yourdomain.com
    
    ProxyPreserveHost On
    ProxyRequests Off
    
    # WebSocket for SignalR
    ProxyPass /_blazor ws://localhost:5000/_blazor
    ProxyPassReverse /_blazor ws://localhost:5000/_blazor
    
    # HTTP for everything else
    ProxyPass / http://localhost:5000/
    ProxyPassReverse / http://localhost:5000/
    
    SetEnv proxy-sendchunked 1
</VirtualHost>
```

```bash
sudo a2ensite onemanvan
sudo systemctl reload apache2
```

---

### Solution 3: Docker Port Mapping Issue

Check your `docker-compose.yml`:

```yaml
services:
  web:
    ports:
      - "5000:8080"  # ? Ensure this maps external 5000 to internal 8080
```

**If port mapping is wrong:**
```bash
# Fix docker-compose.yml
nano docker-compose.yml

# Restart container
docker-compose down
docker-compose up -d
```

---

### Solution 4: Firewall Blocking WebSocket

```bash
# Ubuntu/Debian
sudo ufw allow 5000/tcp
sudo ufw reload

# CentOS/RHEL
sudo firewall-cmd --add-port=5000/tcp --permanent
sudo firewall-cmd --reload

# Check if port is listening
sudo netstat -tlnp | grep 5000
```

---

### Solution 5: Cloudflare WebSocket Issue

If using Cloudflare:

1. Go to Cloudflare Dashboard
2. Select your domain
3. Go to **Network** tab
4. Enable **WebSockets**
5. Or use **Cloudflare Tunnel** (handles WebSocket automatically)

---

### Solution 6: HTTPS/SSL Certificate Issue

If site is HTTPS but WebSocket tries HTTP:

```bash
# Get SSL certificate
sudo certbot --nginx -d yourdomain.com

# Or force HTTP for testing
# In browser, access: http://your-ip:5000 (not https)
```

---

## ? Verification

After applying fix:

1. **Restart everything:**
   ```bash
   docker-compose restart
   sudo systemctl reload nginx  # or apache2
   ```

2. **Test in browser:**
   - Open DevTools ? Network ? WS
   - Reload page
   - You should see WebSocket connection with status 101

3. **Test functionality:**
   - Click "Add Customer" ? Modal should open
   - Click Dark Mode toggle ? Theme should change

4. **Run diagnostic:**
   ```powershell
   # On your local machine
   .\Test-BlazorConnection.ps1
   ```

---

## ?? Still Not Working?

### Check Browser Console for:

```
Failed to start the transport 'WebSockets'
```
? Reverse proxy blocking WebSocket

```
Uncaught (in promise) Error: Invocation canceled due to the underlying connection being closed
```
? Circuit timeout - increase proxy timeouts

```
blazor.web.js:1 Failed to load resource: the server responded with a status of 404
```
? Static files not being served

### Check Server Logs:

```bash
# Docker logs
docker-compose logs web

# Nginx logs
sudo tail -f /var/log/nginx/error.log

# Application logs
docker exec -it onemanvan-web-1 cat /app/logs/app.log
```

---

## ?? Common Scenarios

| Symptom | Cause | Fix |
|---------|-------|-----|
| No errors, buttons don't work | WebSocket blocked | Fix nginx config |
| "Blazor is undefined" | JS not loading | Check static files |
| WebSocket shows 400 | Wrong headers | Add upgrade headers |
| Works locally, not on server | Reverse proxy issue | Run fix-blazor-signalr.sh |
| Works on HTTP, not HTTPS | SSL not configured | Run certbot |
| Intermittent disconnects | Timeout too short | Increase proxy timeouts |

---

## ?? Quick Command Reference

```bash
# Test SignalR endpoint
curl -i http://localhost:5000/_blazor/negotiate

# Check if app is running
docker ps | grep onemanvan

# Check nginx config
sudo nginx -t

# Reload nginx
sudo systemctl reload nginx

# View container logs
docker-compose logs -f web

# Restart everything
docker-compose restart && sudo systemctl reload nginx

# Test from browser console
# Type this in DevTools:
Blazor
```

---

## ?? Emergency Bypass (Development Only)

If you need immediate access for testing:

```bash
# Stop reverse proxy temporarily
sudo systemctl stop nginx

# Access directly on port 5000
# In browser: http://your-server-ip:5000

# This bypasses the proxy to verify the issue
```

?? **Don't leave this in production** - restart nginx when done:
```bash
sudo systemctl start nginx
```

---

## ?? Get More Help

If still stuck, provide this info:

1. **Output of diagnostic script:**
   ```bash
   ./fix-blazor-signalr.sh
   ```

2. **Browser Network tab screenshot** (with WS filter)

3. **Server setup:**
   - Using nginx/apache/direct?
   - HTTP or HTTPS?
   - Cloud provider (AWS/Azure/DigitalOcean)?

4. **Docker logs:**
   ```bash
   docker-compose logs web | tail -100
   ```
