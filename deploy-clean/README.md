# Clean Docker Deployment (Ubuntu)

This folder contains a minimal, secrets-safe Docker deployment bundle for the Web UI + SQL Server.

## What you copy to the server
Copy the entire `deploy-clean/` folder to the server.

## Server prerequisites
On `192.168.100.107` ensure:
- Docker is installed
- Docker Compose v2 is installed (the `docker compose` command)
- Port `7159` (or your `WEBUI_PORT`) is reachable from your LAN

## Transfer to server (from your Windows/Dev machine)

From the repository root:

```powershell
# Copy deploy folder to the server
scp -r .\deploy-clean user@192.168.100.107:/opt/tradeflow/
```

If you don’t have `scp` on Windows, use WinSCP and upload `deploy-clean` to `/opt/tradeflow/deploy-clean`.

## Setup on the server

SSH into the server:

```bash
ssh user@192.168.100.107
```

Then:

```bash
cd /opt/tradeflow/deploy-clean
cp .env.example .env
nano .env
chmod +x reset-and-deploy.sh
./reset-and-deploy.sh
```

## Configure admin login credentials
In `.env`, set:

- `ADMIN_EMAIL`
- `ADMIN_PASSWORD`

These are used by `AdminAccountSeeder` on the first run to create the initial admin user.

## Open the Web UI

From a browser on your LAN:

- `http://192.168.100.107:7159/`

You should be redirected to:

- `/Account/Login`

## Log in
Use the credentials you set in `.env`:

- Email: `ADMIN_EMAIL`
- Password: `ADMIN_PASSWORD`

## Notes
- `reset-and-deploy.sh` performs a FULL reset including deleting the SQL volume.
- If you want to preserve database data between deploys, use `docker compose up -d --build` instead.
