# Image-based Deployment (Option A)

This deployment method lets you deploy to `192.168.100.107` without copying the repo source.
You build the `webui` image locally, export it as a `.tar`, copy it to the server, load it, and run Docker Compose.

## Local machine steps (build the image tar)

From the repo root.

### Windows (PowerShell)

```powershell
# Build + export the image to deploy-image/tradeflow-webui.tar
.\deploy-image\build-and-export-image.ps1 -ImageName "tradeflow-webui:latest" -OutputTar "tradeflow-webui.tar"
```

### Linux/macOS (bash)

```bash
chmod +x deploy-image/build-and-export-image.sh
./deploy-image/build-and-export-image.sh tradeflow-webui:latest tradeflow-webui.tar
```

This produces:
- `deploy-image/tradeflow-webui.tar`

## Transfer to server (192.168.100.107)

Copy the entire `deploy-image/` folder:

```bash
scp -r deploy-image root@192.168.100.107:/opt/OneManVan/
```

(If you prefer another username, replace `root`.)

## Server steps (load image + deploy)

SSH to server:

```bash
ssh root@192.168.100.107
```

Then:

```bash
cd /opt/OneManVan/deploy-image

# Create .env
cp .env.example .env
nano .env

# Load the WebUI image tar
docker load -i tradeflow-webui.tar

# Fix line endings if edited on Windows (safe to run even if already LF)
sed -i 's/\r$//' reset-and-deploy.sh

chmod +x reset-and-deploy.sh
./reset-and-deploy.sh
```

## Log in

Open:
- `http://192.168.100.107:7159/`

Log in with:
- Email = `ADMIN_EMAIL` from `.env`
- Password = `ADMIN_PASSWORD` from `.env`

## Notes

- `reset-and-deploy.sh` performs a FULL reset: it deletes the SQL volume `tradeflow-sqldata`.
- For future redeploys without wiping DB:

```bash
docker compose --env-file .env up -d
```
