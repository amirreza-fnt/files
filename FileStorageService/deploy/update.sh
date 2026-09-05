#!/usr/bin/env bash
# Deploy script for AlmaLinux server.
# Repo path : /root/files/FileStorageService
# App path  : /opt/filestorage
# Usage     : cd /root/files/FileStorageService && git pull && bash deploy/update.sh
set -euo pipefail

REPO_DIR="/root/files/FileStorageService"
APP_DIR="/opt/filestorage"
SERVICE="filestorage"

cd "${REPO_DIR}"

echo "==> Stopping ${SERVICE}"
systemctl stop "${SERVICE}"

echo "==> Publishing Release build to ${APP_DIR}"
dotnet publish "${REPO_DIR}/src/FileStorage.Api" -c Release -o "${APP_DIR}"

echo "==> Fixing permissions"
chown -R filestorage:filestorage "${APP_DIR}" /var/lib/filestorage /var/log/filestorage

echo "==> Starting ${SERVICE} (EF migrations run automatically on startup)"
systemctl start "${SERVICE}"
systemctl status "${SERVICE}" --no-pager

echo "==> Health check"
curl -fsS http://127.0.0.1:6000/api/health
echo
echo "Deploy done."
