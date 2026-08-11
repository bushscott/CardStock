#!/usr/bin/env bash
# Publishes the API self-contained for the Pi, mirroring the crawler's
# ops/publish.sh. There is no database step here, ever -- migrations are a
# separate, deliberate act run by a human (see README section 2).
set -euo pipefail

OUT="${1:-publish/api}"

dotnet publish src/CardStock.Api \
  -c Release \
  -r linux-arm64 \
  --self-contained \
  -o "$OUT"

echo "Published to $OUT"
