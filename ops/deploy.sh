#!/usr/bin/env bash
# ops/README.md §5, executable. Publish first: ./ops/publish.sh publish/api
# Then this: syncs the bundle, fixes ownership, restarts the unit, probes health.
# --exclude='appsettings.Production.json*' is load-bearing: the production config
# and its dated backups exist only on the Pi; a bare --delete removes them and the
# service crash-loops on restart with no connection string.
set -euo pipefail
cd "$(dirname "$0")/.."

rsync -az --delete --exclude='appsettings.Production.json*' \
  --rsync-path='sudo rsync' publish/api/ scott@192.168.30.56:/opt/cardstock/api/
ssh scott@192.168.30.56 'sudo chown -R cardstock:cardstock /opt/cardstock/api && sudo systemctl restart cardstock-api'
sleep 3
curl -sf --resolve cardstock.pro:443:192.168.30.56 https://cardstock.pro/healthz/data
echo
