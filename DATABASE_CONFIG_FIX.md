# ?? Database Configuration Fix Summary

## ?? Problem
Your web application was configured to use **SQLite** instead of **SQL Server**, causing:
- `SQLite Error 14: 'unable to open database file'`
- Application trying to access `Data/business.db` instead of SQL Server
- Database connection failures in Docker deployment

## ? Changes Made

### 1. **Updated `appsettings.json`**
**Before:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "DataSource=Data/app.db;Cache=Shared"
  }
}
```

**After:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sqlserver;Database=TradeFlowFSM;User Id=sa;Password=${SA_PASSWORD};TrustServerCertificate=True;",
    "IdentityConnection": "Server=sqlserver;Database=TradeFlowIdentity;User Id=sa;Password=${SA_PASSWORD};TrustServerCertificate=True;"
  }
}
```

### 2. **Updated `Program.cs`**
**Changed Identity Database Configuration:**
```csharp
// OLD (SQLite only):
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// NEW (Detects SQL Server or SQLite):
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (identityConnectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(identityConnectionString);  // Docker
    }
    else
    {
        options.UseSqlite(identityConnectionString);     // Local
    }
});
```

### 3. **Updated `docker-compose.yml`**
**Added Complete Configuration:**
- ? Fixed SQL Server health check (`/opt/mssql-tools18/bin/sqlcmd`)
- ? Added `webui` service with proper environment variables
- ? Configured connection strings for both databases
- ? Added `BusinessConnection` string

### 4. **Created Deployment Script**
**New file: `fix-database-config.sh`**
- Automates the complete fix process
- Stops old containers
- Cleans up SQLite files
- Rebuilds web image
- Starts services with proper configuration

---

## ?? How to Deploy

### **On Your Development Machine:**

```powershell
# 1. Commit changes to Git
git add .
git commit -m "Fix: Switch web app from SQLite to SQL Server"
git push origin master
```

### **On Your Docker Server:**

```bash
# 1. Navigate to deployment folder
cd ~/OneManVan-Deployment-2026-01-29-1930

# 2. Pull latest code
git pull origin master

# OR manually copy updated files:
# - docker-compose.yml
# - OneManVan.Web/appsettings.json
# - OneManVan.Web/Program.cs

# 3. Run the fix script
chmod +x fix-database-config.sh
./fix-database-config.sh

# OR manual deployment:
docker compose down
docker compose build --no-cache webui
docker compose up -d
```

---

## ? Verification Steps

### **1. Check Containers Are Running**
```bash
docker ps

# Expected output:
# - tradeflow-db (healthy)
# - tradeflow-webui (running)
```

### **2. Check Logs**
```bash
# Web UI logs - Should show SQL Server connection
docker logs tradeflow-webui | grep -i "database\|sql"

# Should see:
# ? "Server=sqlserver" (not "Data/business.db")
# ? "Database initialization completed successfully"
# ? No SQLite errors
```

### **3. Verify Databases Exist**
```bash
docker exec tradeflow-db /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'TradeFlow2025!' -C \
  -Q "SELECT name FROM sys.databases"

# Expected output:
# master
# TradeFlowFSM
# TradeFlowIdentity
```

### **4. Test Web Application**
```
http://YOUR_SERVER_IP:7159/
```
- ? Dashboard loads
- ? Can create customers
- ? No database errors

---

## ?? Database Architecture

```
???????????????????????????????????????
?       Docker Containers             ?
???????????????????????????????????????
?                                     ?
?  ????????????????  ??????????????? ?
?  ? tradeflow-   ?  ? tradeflow-  ? ?
?  ?   webui      ????    db       ? ?
?  ?              ?  ? (SQL Server)? ?
?  ????????????????  ??????????????? ?
?         ?                  ?        ?
?         ?                  ?        ?
?    Port 7159          Port 1433    ?
?         ?                  ?        ?
???????????????????????????????????????
          ?                  ?
          ?                  ?
     Web Access      SQL Server
  (Browser/API)       Databases
                           ?
                           ??? TradeFlowFSM
                           ?    (Business Data)
                           ?
                           ??? TradeFlowIdentity
                                (User Accounts)
```

---

## ?? Configuration Priority

The application now uses this priority:

1. **Docker (SQL Server)** - Environment variables in `docker-compose.yml`
2. **Local Development (SQLite)** - `appsettings.json` fallback

This means:
- ? **In Docker**: Uses SQL Server automatically
- ? **In Visual Studio**: Can still use SQLite for development

---

## ?? What Each Database Stores

| Database | Purpose | Tables |
|----------|---------|--------|
| **TradeFlowFSM** | Business data | Customers, Jobs, Invoices, Assets, Products, Inventory |
| **TradeFlowIdentity** | Authentication | AspNetUsers, AspNetRoles, AspNetUserRoles |

---

## ?? Troubleshooting

### **Still Seeing SQLite Errors?**

```bash
# 1. Check connection strings in container
docker exec tradeflow-webui env | grep ConnectionStrings

# Should show:
# ConnectionStrings__DefaultConnection=Server=sqlserver;...
```

### **Web UI Won't Start?**

```bash
# Check detailed logs
docker logs tradeflow-webui --tail 100

# Look for:
# - Connection string being used
# - Database initialization messages
# - Any SQL Server connection errors
```

### **SQL Server Not Ready?**

```bash
# Test SQL Server manually
docker exec tradeflow-db /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'TradeFlow2025!' -C -Q "SELECT @@VERSION"
```

---

## ?? Next Steps

1. ? **Deploy the fix** using the script
2. ? **Verify web app works**
3. ? **Create test data** (customers, jobs, invoices)
4. ? **Test all features** work correctly
5. ? **Document server IP** for team access

---

## ?? Support

If you encounter issues:

1. **Save logs**:
   ```bash
   docker logs tradeflow-webui > webui-logs.txt
   docker logs tradeflow-db > db-logs.txt
   ```

2. **Check configuration**:
   ```bash
   cat docker-compose.yml
   docker exec tradeflow-webui cat /app/appsettings.json
   ```

3. **Restart cleanly**:
   ```bash
   docker compose down -v
   docker compose build --no-cache
   docker compose up -d
   ```

---

**? You're all set!** The web application is now properly configured to use SQL Server in Docker deployments.
