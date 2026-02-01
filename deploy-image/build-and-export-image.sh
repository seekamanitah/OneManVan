#!/usr/bin/env bash
set -euo pipefail

IMAGE_NAME="${1:-tradeflow-webui:latest}"
OUTPUT_TAR="${2:-tradeflow-webui.tar}"

echo "Building WebUI image: ${IMAGE_NAME}"
docker build -f OneManVan.Web/Dockerfile -t "${IMAGE_NAME}" .

echo "Saving image to tar: deploy-image/${OUTPUT_TAR}"
docker save "${IMAGE_NAME}" -o "deploy-image/${OUTPUT_TAR}"

echo "Done. Tar created at deploy-image/${OUTPUT_TAR}"
