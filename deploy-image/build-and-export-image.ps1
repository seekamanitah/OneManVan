param(
  [string]$ImageName = "onemanvan-webui:latest",
  [string]$OutputTar = "onemanvan-webui.tar"
)

$ErrorActionPreference = 'Stop'

Write-Host "Building WebUI image: $ImageName" -ForegroundColor Cyan

docker build -f OneManVan.Web/Dockerfile -t $ImageName .

Write-Host "Saving image to tar: $OutputTar" -ForegroundColor Cyan

docker save $ImageName -o (Join-Path "deploy-image" $OutputTar)

Write-Host "Done. Tar created at deploy-image/$OutputTar" -ForegroundColor Green
