#!/usr/bin/env bash
# Run on the AlmaLinux server after "git pull" (internal mirror / offline deploy).
# Applies code + restarts the service; EF migrations run automatically on startup.
set -euo pipefail

APP_DIR="/opt/filestorage"
REPO_DIR="$(cd "$(dirname "$0")/.." && pwd)"

echo "==> Publishing to ${APP_DIR}"
dotnet publish "${REPO_DIR}/src/FileStorage.Api" -c Release -o "${APP_DIR}"

echo "==> Fixing permissions"
chown -R filestorage:filestorage "${APP_DIR}" /var/lib/filestorage /var/log/filestorage

echo "==> Restarting service"
systemctl restart filestorage
systemctl status filestorage --no-pager

echo "==> Health check"
curl -fsS http://127.0.0.1:6000/api/health
echo
echo "Done."
