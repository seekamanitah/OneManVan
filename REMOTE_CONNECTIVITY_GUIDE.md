# Remote Connectivity Setup Guide
**OneManVan Web & Mobile Remote Database Connection**

---

## ?? Overview

This guide ensures your OneManVan server can accept **remote connections** from:
- Web UI (from any browser)
- Mobile App (from phones/tablets outside your LAN)
- Desktop App (from remote locations)

---

## ?? Step 1: Deploy Fixed Version to Server

### Transfer Deployment Package

**From Windows (PowerShell):**
```powershell
scp C:\Users\tech\source\repos\TradeFlow\deployment.zip root@192.168.100.107:/root/deployment-FINAL.zip
```

### Deploy on Server

**SSH into your server:**
```sh
ssh root@192.168.100.107
```

**Run the complete deployment script:**
```sh
cd /root
bash deploy-to-server-complete.sh
```

**Or manual deployment:**
```sh
# Stop containers
cd /opt/onemanvan
docker compose -f docker-compose-full.yml down

# Backup old version
mv /opt/onemanvan /opt/onemanvan-backup-$(date +%Y%m%d)

# Extract new deployment
unzip /root/deployment-FINAL.zip -d /opt/onemanvan

# Deploy
cd /opt/onemanvan
docker compose -f docker-compose-full.yml up -d --build

# Watch logs
docker logs -f tradeflow-webui
```

---

## ?? Step 2: Configure Server for Remote Access

### 2.1 Check Docker Network Settings

**Verify ports are exposed:**
```sh
docker ps --format "table {{.Names}}\t{{.Ports}}"
```

**Should show:**
- `tradeflow-webui`: `0.0.0.0:5000->8080/tcp`
- `tradeflow-db`: `0.0.0.0:1433->1433/tcp`

### 2.2 Configure Firewall

**Allow Web UI access (port 5000):**
```sh
# UFW (Ubuntu/Debian)
sudo ufw allow 5000/tcp
sudo ufw allow 1433/tcp
sudo ufw status

# Or iptables
sudo iptables -A INPUT -p tcp --dport 5000 -j ACCEPT
sudo iptables -A INPUT -p tcp --dport 1433 -j ACCEPT
sudo iptables-save
```

**Proxmox Firewall (if using Proxmox):**
```sh
# On Proxmox host
pct config 107  # Check container ID

# Add firewall rules in Proxmox Web UI:
# Datacenter ? Firewall ? Add
# Direction: IN
# Action: ACCEPT
# Protocol: TCP
# Dest. port: 5000,1433
```

### 2.3 Configure SQL Server for Remote Connections

**SQL Server is already configured in docker-compose-full.yml:**
```yaml
sqlserver:
  ports:
    - "1433:1433"  # ? Already exposed to host
  environment:
    - MSSQL_TCP_PORT=1433
```

**Verify SQL Server is listening:**
```sh
docker exec tradeflow-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "TradeFlow2025!" -Q "SELECT @@VERSION" -C
```

---

## ?? Step 3: Test Remote Connection from Mobile/Web

### 3.1 From Mobile App (Outside LAN)

**Prerequisites:**
- Mobile device on cellular data or different Wi-Fi
- Public IP or domain pointing to your server

**Steps in Mobile App:**
1. Open OneManVan Mobile App
2. Go to **Settings** ? **Database Configuration**
3. Change from SQLite to **SQL Server**
4. Enter connection details:
   ```
   Server Address: YOUR_PUBLIC_IP_OR_DOMAIN
   Port: 1433
   Database Name: TradeFlowFSM
   Username: sa
   Password: TradeFlow2025!
   Trust Certificate: ? Enabled
   Encrypt: ? Disabled (for testing)
   ```
5. Tap **Test Connection**
6. If successful, tap **Save**

### 3.2 From Web UI (Outside LAN)

**Access via browser:**
```
http://YOUR_PUBLIC_IP:5000
```

**Or with domain:**
```
http://onemanvan.yourdomain.com:5000
```

**Test login:**
- Email: `admin@onemanvan.local`
- Password: `Admin123!`

---

## ?? Step 4: Verify Remote Connectivity

### 4.1 Test SQL Server Connection from Outside

**From another computer outside your LAN:**
```powershell
# Using sqlcmd (if installed)
sqlcmd -S YOUR_PUBLIC_IP,1433 -U sa -P "TradeFlow2025!" -Q "SELECT @@VERSION"

# Using telnet (to test port)
telnet YOUR_PUBLIC_IP 1433
```

**Expected result:** Connection succeeds, you see SQL Server version

### 4.2 Test Web UI Access

**From mobile browser or external computer:**
```
http://YOUR_PUBLIC_IP:5000/Account/Login
```

**Should see:** Login page loads correctly

---

## ??? Step 5: Secure Remote Connections (Production)

### 5.1 Change Default Passwords

**Update SQL Server SA password:**
```sh
docker exec tradeflow-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "TradeFlow2025!" -Q "ALTER LOGIN sa WITH PASSWORD = 'YOUR_STRONG_PASSWORD_HERE'" -C

# Update docker-compose-full.yml
nano /opt/onemanvan/docker-compose-full.yml
# Change MSSQL_SA_PASSWORD to new password

# Restart
docker compose -f /opt/onemanvan/docker-compose-full.yml restart
```

**Update Web UI admin password:**
```sh
# Access web UI
http://YOUR_IP:5000
# Login as admin
# Go to Account ? Change Password
```

### 5.2 Enable HTTPS (Recommended)

**Option A: Use Reverse Proxy (Nginx)**
```sh
# Install nginx
apt install nginx certbot python3-certbot-nginx

# Configure
cat > /etc/nginx/sites-available/onemanvan << 'EOF'
server {
    listen 80;
    server_name onemanvan.yourdomain.com;
    
    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }
}
EOF

# Enable
ln -s /etc/nginx/sites-available/onemanvan /etc/nginx/sites-enabled/
nginx -t
systemctl reload nginx

# Get SSL certificate
certbot --nginx -d onemanvan.yourdomain.com
```

**Option B: Use Cloudflare Tunnel (Free)**
```sh
# Install cloudflared
wget https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64.deb
dpkg -i cloudflared-linux-amd64.deb

# Login and create tunnel
cloudflared tunnel login
cloudflared tunnel create onemanvan
cloudflared tunnel route dns onemanvan onemanvan.yourdomain.com

# Configure
mkdir -p /etc/cloudflared
cat > /etc/cloudflared/config.yml << 'EOF'
tunnel: YOUR_TUNNEL_ID
credentials-file: /root/.cloudflared/YOUR_TUNNEL_ID.json

ingress:
  - hostname: onemanvan.yourdomain.com
    service: http://localhost:5000
  - service: http_status:404
EOF

# Start as service
cloudflared service install
systemctl start cloudflared
```

### 5.3 Restrict SQL Server Access

**Only allow specific IPs:**
```sh
# In docker-compose-full.yml, change from:
ports:
  - "1433:1433"

# To (only allow from specific IPs):
ports:
  - "YOUR_TRUSTED_IP:1433:1433"
```

**Or use firewall rules:**
```sh
# Only allow SQL connections from specific IP
sudo ufw delete allow 1433/tcp
sudo ufw allow from YOUR_MOBILE_IP to any port 1433
```

---

## ?? Step 6: Troubleshooting Remote Connections

### Issue: Cannot connect to SQL Server from mobile

**Check 1: Firewall**
```sh
# On server
sudo ufw status
netstat -tuln | grep 1433
```

**Check 2: Docker port binding**
```sh
docker port tradeflow-db
# Should show: 1433/tcp -> 0.0.0.0:1433
```

**Check 3: Router port forwarding**
- Ensure your router forwards port 1433 to 192.168.100.107

**Check 4: SQL Server health**
```sh
docker logs tradeflow-db | tail -20
```

### Issue: Web UI shows "Not Found" after login

**Fix: Clear browser cache**
```
Ctrl+Shift+Delete ? Clear cache and cookies
```

**Or use incognito/private browsing mode**

### Issue: Mobile app cannot reach server

**Fix: Use public IP instead of local IP**
- Don't use: `192.168.100.107` (only works on LAN)
- Use: Your public IP (find at https://whatismyip.com)
- Or use: Your domain name

---

## ?? Connection String Examples

### For Local Testing (Same Network)
```
Server=192.168.100.107,1433;Database=TradeFlowFSM;User Id=sa;Password=TradeFlow2025!;TrustServerCertificate=True;Encrypt=False;
```

### For Remote Access (Internet)
```
Server=YOUR_PUBLIC_IP,1433;Database=TradeFlowFSM;User Id=sa;Password=YOUR_STRONG_PASSWORD;TrustServerCertificate=True;Encrypt=False;
```

### For Production (with SSL)
```
Server=onemanvan.yourdomain.com,1433;Database=TradeFlowFSM;User Id=sa;Password=YOUR_STRONG_PASSWORD;TrustServerCertificate=False;Encrypt=True;
```

---

## ? Verification Checklist

- [ ] Deployment package transferred to server
- [ ] Docker containers running (both webui and db healthy)
- [ ] Firewall allows ports 5000 and 1433
- [ ] Can access Web UI from external IP: `http://YOUR_IP:5000`
- [ ] Can login to Web UI with admin account
- [ ] Mobile app can connect to server via Settings ? Database
- [ ] SQL Server accepts remote connections
- [ ] Database stored on `/media/onemanvanDB/`
- [ ] HTTPS enabled (optional but recommended)
- [ ] Default passwords changed (production)

---

## ?? Quick Commands

**View logs:**
```sh
docker logs -f tradeflow-webui
docker logs -f tradeflow-db
```

**Restart containers:**
```sh
cd /opt/onemanvan
docker compose -f docker-compose-full.yml restart
```

**Check container status:**
```sh
docker ps
docker inspect tradeflow-db | grep -A 5 Health
```

**Test SQL connection:**
```sh
docker exec tradeflow-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "TradeFlow2025!" -Q "SELECT DB_NAME()" -C
```

**View database files:**
```sh
ls -lh /media/onemanvanDB/sqldata/
du -sh /media/onemanvanDB/*
```

---

## ?? Next Steps

1. **Deploy to server** using the instructions above
2. **Test local access** at `http://192.168.100.107:5000`
3. **Configure router** port forwarding (if accessing from internet)
4. **Test remote access** from mobile device on cellular
5. **Secure the deployment** with HTTPS and strong passwords
6. **Configure backups** for `/media/onemanvanDB/`

**Your server is ready for remote connections!** ??
