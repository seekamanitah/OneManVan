# Fix for SQL Server Foreign Key Cascade Conflict
# Patch OneManVanDbContext.cs on server

# On your server, run these commands:

cd /opt/onemanvan

# Backup the original file
cp OneManVan.Shared/Data/OneManVanDbContext.cs OneManVan.Shared/Data/OneManVanDbContext.cs.backup

# Fix the Product ReplacementProduct relationship
sed -i 's/.OnDelete(DeleteBehavior.SetNull);/.OnDelete(DeleteBehavior.NoAction); \/\/ Prevent cascade conflict in SQL Server/g' OneManVan.Shared/Data/OneManVanDbContext.cs

# Verify the change
grep -A 3 "ReplacementProduct" OneManVan.Shared/Data/OneManVanDbContext.cs

# Should show:
#   entity.HasOne(e => e.ReplacementProduct)
#       .WithMany()
#       .HasForeignKey(e => e.ReplacementProductId)
#       .OnDelete(DeleteBehavior.NoAction); // Prevent cascade conflict in SQL Server

# Rebuild the webui container
docker compose -f docker-compose-full.yml down webui
docker compose -f docker-compose-full.yml up -d --build webui

# Watch logs
docker logs -f tradeflow-webui
