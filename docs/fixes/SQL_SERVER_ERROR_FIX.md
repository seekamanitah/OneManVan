# SQL Server Connection Error - Troubleshooting Guide

## Error Message
```
Microsoft.Data.SqlClient.SqlException: A network-related or instance-specific error occurred 
while establishing a connection to SQL Server. The server was not found or was not accessible.
(provider: Named Pipes Provider, error: 40 - Could not open a connection to SQL Server)

Inner Exception: Win32Exception: The network path was not found.
```

---

## Root Cause

Your `appsettings.json` is configured for **SQL Server (Docker mode)**, but:
1. Docker is not running, OR
2. SQL Server container is not started, OR
3. `SA_PASSWORD` environment variable is not set

---

## Quick Fix Options

### Option 1: Switch to SQLite (Recommended for Local Development)

**Run this PowerShell command:**
```powershell
.\SwitchDatabaseMode.ps1 -Mode SQLite
```

**Or manually edit `OneManVan.Web\appsettings.json`:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=AppData/OneManVan.db",
    "IdentityConnection": "Data Source=AppData/Identity.db"
  }
}
```

**Benefits:**
- ? No Docker required
- ? Works immediately
- ? Fast startup
- ? Good for development

---

### Option 2: Start Docker with SQL Server

**1. Make sure Docker Desktop is running**

**2. Start the SQL Server container:**
```powershell
docker-compose up -d
```

**3. Check if SQL Server is ready:**
```powershell
docker ps
# Should show "sqlserver" container running

docker logs sqlserver
# Should show "SQL Server is now ready for client connections"
```

**4. Run the app:**
```powershell
cd OneManVan.Web
dotnet run
```

---

### Option 3: Set Environment Variable (Without Docker)

If you have SQL Server installed locally:

```powershell
$env:SA_PASSWORD = "YourStrongPassword123!"
cd OneManVan.Web
dotnet run
```

---

## How the Fix Works

The updated `Program.cs` now:

1. **Checks for `SA_PASSWORD` environment variable**
   - If present ? Use SQL Server
   - If missing ? Fall back to SQLite automatically

2. **Logs the database mode:**
   ```
   [INFO] SQL Server configured but SA_PASSWORD not set - using SQLite
   ```

3. **No more crashes** - graceful fallback to SQLite

---

## Verification

After fixing, you should see:

**SQLite Mode:**
```
[INFO] SQL Server configured but SA_PASSWORD not set - using SQLite for Identity
[INFO] SQL Server configured but SA_PASSWORD not set - using SQLite for Business database
info: Program[0]
      Database initialization attempt 1/5
info: Program[0]
      Identity database initialized successfully
info: Program[0]
      Business database initialized successfully
```

**SQL Server Mode:**
```
[INFO] Using SQL Server for Business database (Docker mode)
info: Program[0]
      Database initialization attempt 1/5
info: Program[0]
      Identity database initialized successfully
info: Program[0]
      Business database initialized successfully
```

---

## Database Files Location

### SQLite Mode:
```
OneManVan.Web/
  AppData/
    OneManVan.db      ? Business data
    Identity.db       ? User accounts
    database.config   ? Settings
```

### SQL Server Mode:
- Data stored in Docker container
- Volumes: `sqlserver_data`

---

## Switch Between Modes Anytime

**Switch to SQLite:**
```powershell
.\SwitchDatabaseMode.ps1 -Mode SQLite
```

**Switch to SQL Server:**
```powershell
.\SwitchDatabaseMode.ps1 -Mode SQLServer
docker-compose up -d
```

---

## Common Scenarios

### "I want to develop locally without Docker"
? Use **Option 1: Switch to SQLite**

### "I'm deploying to production server"
? Use **Option 2: Start Docker with SQL Server**

### "I have SQL Server installed on my machine"
? Use **Option 3: Set Environment Variable**

---

## Troubleshooting Connection Strings

### Check Current Configuration:
```powershell
Get-Content OneManVan.Web\appsettings.json | Select-String "ConnectionStrings" -Context 5
```

### SQLite Pattern:
```
"DefaultConnection": "Data Source=AppData/OneManVan.db"
```

### SQL Server Pattern:
```
"DefaultConnection": "Server=sqlserver;Database=TradeFlowFSM;..."
```

---

## Docker Quick Commands

**Start SQL Server:**
```powershell
docker-compose up -d
```

**Stop SQL Server:**
```powershell
docker-compose down
```

**Check logs:**
```powershell
docker logs sqlserver
```

**Connect to SQL Server:**
```powershell
docker exec -it sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourPassword
```

---

## Performance Impact

| Mode | Startup Time | Best For |
|------|--------------|----------|
| SQLite | ~2 seconds | Local development, testing |
| SQL Server | ~5-10 seconds (first time) | Production, team collaboration |

---

## Next Steps

1. ? Choose your database mode
2. ? Run the app
3. ? Verify the logs show successful connection
4. ? Access the app at https://localhost:5001

---

## Still Having Issues?

**Check these:**

1. **Firewall blocking SQL Server?**
   - SQL Server uses port 1433
   - Docker needs ports: 80, 443, 1433, 5432

2. **Docker not installed?**
   - Install Docker Desktop: https://www.docker.com/products/docker-desktop

3. **SQLite database locked?**
   - Close any DB Browser or other tools
   - Delete `AppData/*.db` and restart

4. **Connection string typo?**
   - Run `.\SwitchDatabaseMode.ps1` to reset

---

## Prevention

Add this to your environment setup docs:

```markdown
## Database Setup

### Local Development (Recommended)
1. Run: `.\SwitchDatabaseMode.ps1 -Mode SQLite`
2. Start app: `dotnet run`

### Production / Docker
1. Ensure Docker is running
2. Run: `docker-compose up -d`
3. Start app: `dotnet run`
```

---

**The app is now resilient and will automatically fall back to SQLite if SQL Server is unavailable!**
