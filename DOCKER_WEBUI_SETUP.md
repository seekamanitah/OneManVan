# Connecting Web UI to Docker SQL Server

## Overview
This guide explains how to run the OneManVan Web UI on your local machine while connecting to the SQL Server database running in Docker at `192.168.100.107`.

**Good News:** The Web UI already has a built-in database configuration interface! You can change database settings directly from the Settings page.

## Prerequisites
- Docker container with SQL Server running at `192.168.100.107:1433`
- .NET 10 SDK installed on your local machine
- SQL Server accessible from your network (firewall rules configured)

## Quick Start - Using the UI

### Step 1: Start the Web Application
```powershell
cd C:\Users\tech\source\repos\TradeFlow\OneManVan.Web
dotnet run
```

### Step 2: Configure Database in Settings
1. Navigate to `https://localhost:5001`
2. Log in (or create an account)
3. Go to **Settings** page
4. Scroll to **Database Configuration** section
5. Configure your SQL Server connection:
   - **Database Type**: SQL Server (Remote)
   - **Server Address**: `192.168.100.107`
   - **Port**: `1433`
   - **Database Name**: `TradeFlowFSM`
   - **Username**: `sa`
   - **Password**: `TradeFlow2025!`
   - **Trust Server Certificate**: ? (checked)
   - **Use Encryption**: ? (unchecked for local network)

6. Click **Test Connection** to verify
7. Click **Save & Apply**
8. **Restart the application** for changes to take effect

### Step 3: Verify Connection
After restart, the application will use the SQL Server database at `192.168.100.107`.

## Alternative: Manual Configuration

If you prefer to configure via `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "DataSource=Data/app.db;Cache=Shared",
    "BusinessConnection": "Server=192.168.100.107,1433;Database=TradeFlowFSM;User Id=sa;Password=TradeFlow2025!;TrustServerCertificate=True;Encrypt=False;"
  }
}
```

This will override the UI settings.

## Configuration Files

The application stores database configuration in:
- **UI Settings**: `OneManVan.Web/AppData/database-config.json`
- **Manual Override**: `OneManVan.Web/appsettings.Development.json`

## Testing Connection from Command Line
```powershell
# Test connection to SQL Server
Test-NetConnection -ComputerName 192.168.100.107 -Port 1433
```

## Running the Web Application
```powershell
cd C:\Users\tech\source\repos\TradeFlow\OneManVan.Web
dotnet run
```

Default URL: `https://localhost:5001` or `http://localhost:5000`

## Troubleshooting

### Cannot Connect to SQL Server
```powershell
# Check if SQL Server port is accessible
Test-NetConnection -ComputerName 192.168.100.107 -Port 1433

# Verify SQL Server is accepting remote connections
# Connect with SSMS or Azure Data Studio to:
# Server: 192.168.100.107,1433
# Username: sa
# Password: TradeFlow2025!
```

### Database Not Initialized
```sql
-- Connect to SQL Server and verify database exists
USE TradeFlowFSM;
GO
SELECT * FROM INFORMATION_SCHEMA.TABLES;
```

### Firewall Issues
If running in Docker on Windows:
- Ensure Windows Firewall allows port 1433
- Docker Desktop network settings may need adjustment
- Try disabling Windows Firewall temporarily for testing

### Connection String Issues
Verify the connection string format:
- Use comma for port: `192.168.100.107,1433` (not colon)
- Add `TrustServerCertificate=True` for self-signed certificates
- Add `Encrypt=False` if SSL issues persist

## Alternative: Run Web UI in Docker

To run the entire stack (including Web UI) in Docker:

### Option 1: Add Web Service to docker-compose.yml
```yaml
services:
  webui:
    build:
      context: .
      dockerfile: OneManVan.Web/Dockerfile
    ports:
      - "5000:8080"
    environment:
      - ConnectionStrings__BusinessConnection=Server=sqlserver,1433;Database=TradeFlowFSM;User Id=sa;Password=TradeFlow2025!;TrustServerCertificate=True
    depends_on:
      - sqlserver
    networks:
      - tradeflow-network
```

### Option 2: Access from Other Devices on LAN

To access the Web UI from other devices (phones, tablets):
1. Run the Web UI with:
   ```powershell
   dotnet run --urls "http://0.0.0.0:5000"
   ```
2. Access from other devices using your machine's IP:
   ```
   http://192.168.100.XXX:5000
   ```
   (Replace XXX with your development machine's IP)

## Security Notes

?? **For Production:**
- Change the SA password immediately
- Use Windows Authentication or dedicated SQL users (not SA)
- Enable SSL/TLS encryption
- Use User Secrets or Azure Key Vault for connection strings
- Configure proper firewall rules
- Use strong passwords

## Current Configuration

- **SQL Server Host**: 192.168.100.107
- **SQL Server Port**: 1433
- **Database**: TradeFlowFSM
- **Username**: sa
- **Password**: TradeFlow2025! (change for production!)
- **Environment**: Development

## Next Steps

1. ? Restore NuGet packages
2. ? Verify SQL Server connectivity
3. ? Run database migrations
4. ? Start Web UI
5. ? Test application functionality
6. Consider containerizing the Web UI for consistency
7. Set up reverse proxy (nginx/IIS) for production
