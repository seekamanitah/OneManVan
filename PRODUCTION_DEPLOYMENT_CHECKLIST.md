# Production Deployment Checklist

## ? Pre-Deployment Fixes Applied
- [x] System.IO.Packaging vulnerability patched (8.0.1)
- [x] SQL migration syntax fixed for SQL Server
- [x] Mobile XAML files cleaned
- [x] Build warnings reviewed

## ?? Before Deploying

### Web Application
- [ ] Change SQL Server SA password in docker-compose-full.yml
- [ ] Update connection string with new password
- [ ] Review appsettings.json for production settings
- [ ] Disable detailed error pages (set ASPNETCORE_ENVIRONMENT=Production)
- [ ] Configure HTTPS certificates
- [ ] Set up proper logging

### Database
- [ ] Backup existing database (if any)
- [ ] Review all migration scripts
- [ ] Test migrations on non-production database first
- [ ] Set strong SA password
- [ ] Configure backup schedule
- [ ] Set up monitoring

### Security
- [ ] Change default admin password (admin@onemanvan.local)
- [ ] Review firewall rules
- [ ] Enable SQL Server encryption
- [ ] Use secrets management for passwords
- [ ] Configure SSL/TLS
- [ ] Disable unnecessary ports

### Mobile Application
- [ ] Update connection string to point to production server
- [ ] Test connectivity from mobile devices
- [ ] Verify offline sync works
- [ ] Test on multiple devices

## ?? Deployment Order

1. **Database First**
   `ash
   cd /opt/onemanvan
   docker-compose -f docker-compose-full.yml up -d sqlserver
   docker logs -f tradeflow-db  # Wait for "started"
   `

2. **Verify Database**
   `ash
   docker exec -it tradeflow-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "TradeFlow2025!" -Q "SELECT @@VERSION"
   `

3. **Deploy Web UI**
   `ash
   docker-compose -f docker-compose-full.yml up -d webui
   docker logs -f tradeflow-webui
   `

4. **Test Web Access**
   - Open: http://192.168.100.107:5000
   - Login: admin@onemanvan.local / Admin123!
   - Go to Settings > Database Configuration
   - Verify connection shows: sqlserver:1433

5. **Test Mobile App**
   - Configure: Server: 192.168.100.107
   - Port: 1433
   - Database: TradeFlowFSM
   - Test connection
   - Create test customer

## ?? Post-Deployment Verification

- [ ] Web UI loads without errors
- [ ] Can login with admin account
- [ ] Database connection is green in Settings
- [ ] Can create/read/update/delete customers
- [ ] Mobile app connects successfully
- [ ] Mobile app can sync data
- [ ] PDF generation works
- [ ] Excel export works
- [ ] All navigation works

## ?? Known Issues

1. **Mobile XAML Build Errors**: Clean bin/obj and rebuild
2. **SQL Migration Syntax**: SQLite migrations won't work on SQL Server
3. **Vulnerability**: System.IO.Packaging 6.0.0 in ClosedXML (patched with explicit reference)

## ?? Rollback Plan

If deployment fails:
`ash
# Stop containers
docker-compose -f docker-compose-full.yml down

# Restore from backup (if needed)
docker exec tradeflow-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourPassword" -Q "RESTORE DATABASE TradeFlowFSM FROM DISK='/var/opt/mssql/backup/backup.bak'"

# Start with old configuration
docker-compose -f docker-compose-old.yml up -d
`

## ?? Notes

- All passwords shown are EXAMPLES - change before production
- Database name: TradeFlowFSM (not OneManVanFSM)
- Default SA password: TradeFlow2025! (CHANGE THIS)
- Web UI port: 5000
- SQL Server port: 1433

Generated: 2026-01-27 20:32:34
