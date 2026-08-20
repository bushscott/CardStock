#!/usr/bin/env bash
# Runs as root on successful issue/renew (certbot sets RENEWED_LINEAGE).
# Installed at /etc/letsencrypt/renewal-hooks/deploy/cardstock.sh — see
# ops/README.md. Copies the PEMs where the cardstock user can read them,
# then restarts the unit (Kestrel does not hot-reload cert files).
set -euo pipefail
install -d -m 750 -o root -g cardstock /etc/cardstock/tls
install -m 640 -o root -g cardstock "$RENEWED_LINEAGE/fullchain.pem" /etc/cardstock/tls/fullchain.pem
install -m 640 -o root -g cardstock "$RENEWED_LINEAGE/privkey.pem"  /etc/cardstock/tls/privkey.pem
systemctl restart cardstock-api
