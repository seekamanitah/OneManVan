# ?? Blazor Fix - Quick Deploy Reference

## **Local ? GitHub ? Server**

### **1?? Push to GitHub (Local Machine)**

```powershell
# Windows
.\Push-BlazorFix-GitHub.ps1
```

```bash
# Linux/Mac
./push-blazor-fix-to-github.sh
```

---

### **2?? Deploy to Server (SSH)**

```bash
# Connect
ssh user@server-ip

# Navigate
cd /path/to/OneManVan

# Deploy (copy these 4 commands)
docker-compose down
git pull origin master
docker-compose build --no-cache onemanvan-web
docker-compose up -d

# Verify
docker-compose logs -f onemanvan-web
```

---

### **3?? Test**

**Browser Console (F12):**
```javascript
setTimeout(() => {
    const conn = performance.getEntriesByType('resource')
        .filter(e => e.name.includes('_blazor'));
    console.log(conn.length > 0 && conn[0].responseStatus === 101 ? "? WORKING" : "? FAILED");
}, 3000);
```

**Manual Test:**
- Click "Add Customer" ? Should navigate ?

---

## **Quick Fix If Buttons Still Broken**

```bash
# On server
docker-compose restart onemanvan-web

# On browser
# Ctrl+Shift+Delete ? Clear cache ? Hard refresh (Ctrl+F5)
```

---

## **Rollback**

```bash
git reset --hard HEAD~1
docker-compose down
docker-compose build --no-cache
docker-compose up -d
```

---

## **Files Changed**

1. `OneManVan.Web/Components/App.razor` - Added `@rendermode="InteractiveServer"`
2. `OneManVan.Web/Program.cs` - Cookie config for SignalR
3. `OneManVan.Web/Components/Routes.razor` - Better auth handling

---

## **What This Fixes**

? **Before:** Buttons with `@onclick` don't respond  
? **After:** All interactive features work (SignalR circuit enabled)

---

**Full Guide:** `SERVER_DEPLOYMENT_GUIDE.md`
