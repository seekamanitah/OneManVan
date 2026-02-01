# Server Deployment Guide - Blazor SignalR Fix

## ?? **Overview**

This guide covers deploying the critical Blazor Interactive Server fix to your production/remote server.

**What Changed:**
- ? App.razor: Added `@rendermode="InteractiveServer"` to enable SignalR
- ? Program.cs: Cookie configuration for SignalR authentication
- ? Program.cs: Fixed middleware order
- ? Routes.razor: Better authentication handling

**Result:** All `@onclick` button events now work properly!

---

## ?? **Prerequisites**

Before deploying:
1. ? Changes committed to GitHub
2. ? SSH access to server
3. ? Docker installed on server
4. ? Backup of current database

---

## ?? **Deployment Method 1: Docker Pull & Rebuild (Recommended)**

### **On Your Local Machine:**

#### **Step 1: Push to GitHub**

```powershell
# Windows PowerShell
.\Push-BlazorFix-GitHub.ps1
```

```bash
# Linux/Mac
chmod +x push-blazor-fix-to-github.sh
./push-blazor-fix-to-github.sh
```

#### **Step 2: Verify Push**

Visit: https://github.com/seekamanitah/OneManVan/commits/master

Verify the latest commit shows: "Fix: Enable Blazor Interactive Server for @onclick events"

---

### **On Your Server (via SSH):**

#### **Step 1: Connect to Server**

```bash
ssh your-user@your-server-ip
```

#### **Step 2: Navigate to Application Directory**

```bash
cd /path/to/OneManVan  # Update with your actual path
```

#### **Step 3: Stop Running Containers**

```bash
docker-compose down
```

#### **Step 4: Pull Latest Code from GitHub**

```bash
git pull origin master
```

**Expected output:**
```
Updating abc1234..def5678
Fast-forward
 OneManVan.Web/Components/App.razor        | 3 ++-
 OneManVan.Web/Program.cs                  | 25 +++++++++++++++++++++
 OneManVan.Web/Components/Routes.razor     | 12 +++++++++-
 3 files changed, 38 insertions(+), 2 deletions(-)
```

#### **Step 5: Rebuild Docker Image**

```bash
docker-compose build --no-cache onemanvan-web
```

**This takes 2-5 minutes. Expected output:**
```
[+] Building 234.5s (15/15) FINISHED
 => [internal] load build definition from Dockerfile
 => => transferring dockerfile: 1.23kB
 ...
 => exporting to image
 => => writing image sha256:...
```

#### **Step 6: Restart Containers**

```bash
docker-compose up -d
```

**Expected output:**
```
[+] Running 2/2
 ? Container onemanvan-sqlserver  Started
 ? Container onemanvan-web        Started
```

#### **Step 7: Verify Deployment**

```bash
# Check logs
docker-compose logs -f onemanvan-web

# Look for:
# [Blazor] Starting up Blazor server-side...
# Application started. Press Ctrl+C to shut down.
```

**Press Ctrl+C to exit logs**

#### **Step 8: Test in Browser**

1. Open browser to: `http://your-server-ip:5024`
2. Log in
3. Open DevTools (F12) ? Console
4. Run diagnostic:

```javascript
setTimeout(() => {
    const blazorConn = performance.getEntriesByType('resource')
        .filter(e => e.name.includes('_blazor'));
    console.log("SignalR:", blazorConn.length > 0 ? "? CONNECTED" : "? FAILED");
}, 3000);
```

5. **Test button:** Click "Add Customer" - should navigate immediately ?

---

## ?? **Deployment Method 2: Manual File Update (If Git Pull Fails)**

If you can't pull from GitHub or want to manually update:

### **Step 1: Create Updated Files**

On your **local machine**, create these files:

#### **File 1: App.razor.new**

```bash
cat > App.razor.new << 'EOF'
@using Microsoft.AspNetCore.Components.Web

<!DOCTYPE html>
<html lang="en">
<head>
    <!-- ... keep existing head content ... -->
</head>
<body>
    <Routes @rendermode="InteractiveServer" />
    <div id="blazor-error-ui">
        An unhandled error has occurred.
        <a href="" class="reload">Reload</a>
        <a class="dismiss">??</a>
    </div>
    <script src="@Assets["_framework/blazor.web.js"]"></script>
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js"></script>
    <script src="js/pwa.js"></script>
</body>
</html>
EOF
```

### **Step 2: Upload to Server**

```bash
# Copy file to server
scp App.razor.new your-user@your-server-ip:/tmp/

# SSH to server
ssh your-user@your-server-ip

# Backup current file
cd /path/to/OneManVan/OneManVan.Web/Components
cp App.razor App.razor.backup

# Replace with new version
cp /tmp/App.razor.new App.razor

# Rebuild
cd /path/to/OneManVan
docker-compose down
docker-compose build --no-cache onemanvan-web
docker-compose up -d
```

---

## ?? **Deployment Method 3: Complete Docker Reset (Nuclear Option)**

If other methods fail or you want a clean slate:

### **On Server:**

```bash
cd /path/to/OneManVan

# Stop and remove everything
docker-compose down -v

# Pull latest code
git pull origin master

# Remove old images
docker rmi $(docker images -q onemanvan-web)

# Complete rebuild
docker-compose build --no-cache
docker-compose up -d

# Verify
docker-compose logs -f onemanvan-web
```

---

## ? **Post-Deployment Verification**

### **Test Checklist:**

| Test | Expected Result | Pass/Fail |
|------|-----------------|-----------|
| 1. Application loads | Homepage appears | ? |
| 2. Can log in | Login successful | ? |
| 3. Console check | `Blazor.platform` = "server" | ? |
| 4. WebSocket check | SignalR connected (101) | ? |
| 5. Add Customer button | Navigates to form | ? |
| 6. Add Job button | Navigates to form | ? |
| 7. Add Invoice button | Navigates to form | ? |
| 8. Dark mode toggle | Theme changes | ? |

### **Console Diagnostic:**

```javascript
console.log("=== Production Test ===");
console.log("Blazor:", typeof Blazor !== 'undefined' ? "?" : "?");
console.log("Platform:", Blazor?.platform || "UNDEFINED");

setTimeout(() => {
    const blazor = performance.getEntriesByType('resource')
        .filter(e => e.name.includes('_blazor'));
    console.log("SignalR:", blazor.length > 0 && blazor[0].responseStatus === 101 ? "? WORKING" : "? FAILED");
}, 3000);
```

**Expected:**
```
Blazor: ?
Platform: server
SignalR: ? WORKING
```

---

## ?? **Troubleshooting**

### **Issue: Buttons Still Don't Work**

**Diagnostic:**
```bash
# Check container logs
docker logs onemanvan-web --tail 50

# Look for errors like:
# - "Connection refused"
# - "Authentication failed"
# - "SignalR connection failed"
```

**Solutions:**
1. **Clear browser cache:** Ctrl+Shift+Delete ? Clear cached images and files
2. **Hard refresh:** Ctrl+F5
3. **Use Incognito:** Ctrl+Shift+N
4. **Restart container:** `docker-compose restart onemanvan-web`

---

### **Issue: WebSocket Connection Failed**

**Check firewall:**
```bash
# On server
sudo ufw status

# Allow port if needed
sudo ufw allow 5024/tcp
```

**Check Docker network:**
```bash
docker network inspect onemanvan_default
```

---

### **Issue: Authentication Redirects**

**Verify cookie settings in logs:**
```bash
docker logs onemanvan-web | grep -i "cookie\|signalr\|blazor"
```

Should see:
```
[INFO] Cookie SameSite: Lax
[INFO] SignalR hub configured
```

---

## ?? **Rollback Procedure**

If deployment fails and you need to rollback:

```bash
cd /path/to/OneManVan

# Option 1: Git rollback
git log --oneline  # Find previous commit hash
git reset --hard abc1234  # Replace with commit hash before fix

# Option 2: Use backup
cp OneManVan.Web/Components/App.razor.backup OneManVan.Web/Components/App.razor

# Rebuild
docker-compose down
docker-compose build --no-cache
docker-compose up -d
```

---

## ?? **Deployment Timeline**

| Step | Duration | Notes |
|------|----------|-------|
| 1. Git pull | 10 seconds | Fast |
| 2. Docker build | 2-5 minutes | Depends on server speed |
| 3. Container restart | 30 seconds | Includes DB startup |
| 4. Verification | 2 minutes | Manual testing |
| **Total** | **5-10 minutes** | **Typical deployment** |

---

## ?? **Quick Deployment Script**

Save this as `deploy-blazor-fix.sh` on your **server**:

```bash
#!/bin/bash
echo "Deploying Blazor SignalR fix..."

cd /path/to/OneManVan || exit 1

# Backup
cp OneManVan.Web/Components/App.razor App.razor.backup.$(date +%Y%m%d%H%M%S)

# Update
git pull origin master || exit 1

# Rebuild
docker-compose down
docker-compose build --no-cache onemanvan-web
docker-compose up -d

# Wait for startup
sleep 10

# Check status
docker-compose ps

echo ""
echo "? Deployment complete!"
echo "Test at: http://$(hostname -I | awk '{print $1}'):5024"
```

**Run with:**
```bash
chmod +x deploy-blazor-fix.sh
./deploy-blazor-fix.sh
```

---

## ?? **Support**

If deployment fails:
1. Check logs: `docker-compose logs onemanvan-web`
2. Verify GitHub: https://github.com/seekamanitah/OneManVan
3. Review this guide's troubleshooting section
4. Consider rollback if critical

---

## ? **Success Criteria**

Deployment is successful when:
- ? Application starts without errors
- ? Login works
- ? `Blazor.platform` returns "server"
- ? SignalR WebSocket connects (Status 101)
- ? All "Add" buttons navigate properly
- ? Dark mode toggle works
- ? Forms submit successfully

**Once verified, deployment is complete!** ??
