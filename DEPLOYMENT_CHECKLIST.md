# ? Docker Deployment Checklist

Use this checklist to ensure successful deployment of OneManVan Web to your Proxmox LXC container.

## Pre-Deployment Checklist

### Windows Development Machine
- [ ] PowerShell 5.1 or higher installed
- [ ] Solution builds successfully
- [ ] Network connectivity to 192.168.100.107
- [ ] File transfer method available (SCP/WinSCP/FileZilla)

### Linux Server (192.168.100.107)
- [ ] Proxmox LXC container is running
- [ ] SSH access configured
- [ ] Root or sudo access available
- [ ] Minimum 2GB RAM available
- [ ] Minimum 10GB disk space free
- [ ] Ports 5000 and 1433 not in use
- [ ] Internet connection (for downloading Docker images)

## Deployment Steps

### Step 1: Create Deployment Package ?
- [ ] Navigate to solution root directory in PowerShell
- [ ] Run: `.\create-deployment-package.ps1`
- [ ] Verify `deployment.zip` is created (~5-10 MB)
- [ ] Check PowerShell output for any errors

**Expected Output:**
```
Deployment Package Created Successfully!
Package: deployment.zip (X.XX MB)
```

### Step 2: Transfer Files ?
- [ ] Transfer `deployment.zip` to server
- [ ] Verify file is on server at `/root/deployment.zip`

**Transfer Methods:**
```powershell
# Option A: SCP
scp deployment.zip root@192.168.100.107:/root/

# Option B: Use WinSCP/FileZilla GUI
# Option C: Proxmox web console upload
```

**Verify Transfer:**
```bash
ssh root@192.168.100.107
ls -lh /root/deployment.zip
```

### Step 3: Extract and Deploy ?
- [ ] SSH into server
- [ ] Extract deployment package
- [ ] Navigate to deployment directory
- [ ] Make script executable
- [ ] Run deployment script
- [ ] Monitor deployment output

**Commands:**
```bash
cd /root
unzip deployment.zip -d /opt/onemanvan
cd /opt/onemanvan
chmod +x deploy-to-docker.sh
./deploy-to-docker.sh
```

**Expected Actions:**
- [ ] Docker installed/verified
- [ ] Docker Compose installed/verified
- [ ] Deployment directory created
- [ ] Old containers stopped (if any)
- [ ] New containers built
- [ ] Services started
- [ ] Health checks passing

### Step 4: Verify Deployment ?
- [ ] Containers are running
- [ ] SQL Server is healthy
- [ ] Web UI is accessible
- [ ] Database is initialized
- [ ] No errors in logs

**Verification Commands:**
```bash
# Check container status
docker ps

# Expected output: 2 containers running
# - tradeflow-db (healthy)
# - tradeflow-webui (healthy)

# Check logs
docker logs tradeflow-webui | head -20
docker logs tradeflow-db | head -20

# Test local access
curl http://localhost:5000
```

### Step 5: Test Access ?
- [ ] Web UI loads from browser
- [ ] Login page displays
- [ ] Can log in with default credentials
- [ ] Database connection is working
- [ ] No JavaScript errors in browser console

**Browser Tests:**
1. Navigate to: http://192.168.100.107:5000
2. Login with: admin@onemanvan.local / Admin123!
3. Check Dashboard loads
4. Navigate to Settings
5. Verify Database Configuration section exists

### Step 6: Configure Application ?
- [ ] Change default admin password
- [ ] Configure trade type in Settings
- [ ] Test database connection
- [ ] Verify SQL Server connectivity
- [ ] Configure any custom settings

### Step 7: Security Hardening ?
- [ ] Change SQL Server SA password
- [ ] Update docker-compose-full.yml with new password
- [ ] Configure firewall rules
- [ ] Set up HTTPS (if needed)
- [ ] Disable unnecessary ports
- [ ] Set up regular backups

**Firewall Configuration:**
```bash
# Enable firewall
sudo ufw enable

# Allow SSH
sudo ufw allow 22/tcp

# Allow Web UI
sudo ufw allow 5000/tcp

# Allow SQL Server (optional, if external access needed)
sudo ufw allow 1433/tcp

# Check status
sudo ufw status
```

## Post-Deployment Checklist

### Functionality Tests
- [ ] Create a customer
- [ ] Create an invoice
- [ ] Create a job
- [ ] Create an asset
- [ ] Export data (PDF/Excel/CSV)
- [ ] View reports/dashboard
- [ ] Test mobile app connection (if applicable)

### Performance Tests
- [ ] Web UI loads quickly (<3 seconds)
- [ ] No timeout errors
- [ ] Database queries are fast
- [ ] No memory leaks
- [ ] Resource usage acceptable

**Check Resources:**
```bash
docker stats
```

### Backup Setup
- [ ] Test manual backup
- [ ] Schedule automated backups
- [ ] Verify backup location
- [ ] Test backup restoration
- [ ] Document backup procedure

**Test Backup:**
```bash
# Create backup
docker exec tradeflow-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "TradeFlow2025!" -Q "BACKUP DATABASE TradeFlowFSM TO DISK='/var/opt/mssql/backup/test.bak'"

# Verify backup created
docker exec tradeflow-db ls -lh /var/opt/mssql/backup/

# Copy to host
docker cp tradeflow-db:/var/opt/mssql/backup/test.bak ./test-backup.bak
```

### Monitoring Setup
- [ ] Set up log rotation
- [ ] Configure monitoring alerts
- [ ] Set up uptime monitoring
- [ ] Configure email notifications
- [ ] Document monitoring procedures

### Documentation
- [ ] Document custom configurations
- [ ] Record passwords securely
- [ ] Note any changes from defaults
- [ ] Create runbook for common tasks
- [ ] Share access information with team

## Troubleshooting Checklist

If deployment fails, check:

### Container Issues
- [ ] Docker service is running: `systemctl status docker`
- [ ] Sufficient disk space: `df -h`
- [ ] Sufficient memory: `free -h`
- [ ] No port conflicts: `netstat -tuln | grep -E '5000|1433'`
- [ ] Container logs: `docker logs tradeflow-webui`

### Network Issues
- [ ] Server is reachable: `ping 192.168.100.107`
- [ ] Ports are open: `telnet 192.168.100.107 5000`
- [ ] Firewall allows traffic: `sudo ufw status`
- [ ] DNS resolution works
- [ ] Routing is correct

### Database Issues
- [ ] SQL Server container is healthy: `docker ps`
- [ ] Database is initialized: Check logs
- [ ] Connection string is correct
- [ ] Credentials are valid
- [ ] No migration errors

### Web UI Issues
- [ ] .NET runtime is correct version
- [ ] All dependencies are included
- [ ] Configuration files are present
- [ ] Environment variables are set
- [ ] No build errors

## Common Issues and Solutions

### Issue: Port 5000 already in use
**Solution:**
```yaml
# Edit docker-compose-full.yml
ports:
  - "8080:8080"  # Change to available port
```

### Issue: SQL Server won't start
**Solution:**
```bash
# Check system requirements
docker logs tradeflow-db
# SQL Server requires 2GB RAM minimum
# Increase LXC container memory in Proxmox
```

### Issue: Can't connect from browser
**Solution:**
```bash
# Check firewall
sudo ufw allow 5000/tcp

# Check container is listening
docker exec tradeflow-webui netstat -tuln | grep 8080

# Check from server first
curl http://localhost:5000
```

### Issue: Database connection failed
**Solution:**
```bash
# Check SQL Server is ready
docker logs tradeflow-db | grep "started"

# Test connection
docker exec tradeflow-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "TradeFlow2025!" -Q "SELECT @@VERSION"
```

## Maintenance Checklist

### Daily
- [ ] Check container status
- [ ] Review error logs
- [ ] Monitor resource usage
- [ ] Verify backups completed

### Weekly
- [ ] Review application logs
- [ ] Check disk space
- [ ] Verify database integrity
- [ ] Update documentation

### Monthly
- [ ] Update Docker images
- [ ] Review security settings
- [ ] Test backup restoration
- [ ] Clean up old backups
- [ ] Review performance metrics

## Upgrade Checklist

When deploying updates:
- [ ] Backup current database
- [ ] Stop existing containers
- [ ] Upload new deployment.zip
- [ ] Extract to new directory
- [ ] Review configuration changes
- [ ] Run deployment script
- [ ] Verify functionality
- [ ] Update documentation

**Upgrade Commands:**
```bash
# 1. Backup
docker exec tradeflow-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "TradeFlow2025!" -Q "BACKUP DATABASE TradeFlowFSM TO DISK='/var/opt/mssql/backup/pre-upgrade.bak'"

# 2. Stop services
cd /opt/onemanvan
docker-compose -f docker-compose-full.yml down

# 3. Deploy new version
# (upload and extract new deployment.zip)

# 4. Start services
docker-compose -f docker-compose-full.yml up -d --build

# 5. Verify
docker ps
curl http://localhost:5000
```

## Success Criteria

Deployment is considered successful when:
- [?] All containers are running and healthy
- [?] Web UI is accessible from network
- [?] Login works with default credentials
- [?] Database connection is established
- [?] No errors in logs
- [?] All CRUD operations work
- [?] Export functionality works
- [?] Mobile app can connect
- [?] Backups are configured
- [?] Monitoring is in place

## Support Resources

- **Quick Reference**: DEPLOYMENT_QUICK_REFERENCE.md
- **Full Guide**: DEPLOYMENT_INSTRUCTIONS.md
- **Visual Guide**: DOCKER_DEPLOYMENT_VISUAL_GUIDE.md
- **Summary**: DOCKER_DEPLOYMENT_SUMMARY.md
- **Database Config**: DATABASE_CONFIG_FEATURE.md

## Final Notes

- ? Keep deployment.zip for backup
- ? Document any custom changes
- ? Store passwords securely
- ? Set up automated backups
- ? Monitor container health
- ? Plan for regular updates

---

**Deployment Complete!** ??

Access your application at: http://192.168.100.107:5000
