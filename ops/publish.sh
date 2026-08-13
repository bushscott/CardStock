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

# The API's publish copies the Web project's wwwroot raw -- index.html still
# carrying the '#[.{fingerprint}]' placeholder that only the Blazor WASM
# project's own publish substitutes (verified 2026-08-13: a browser strips
# everything after '#', requests '_framework/blazor.webassembly', 404s, and
# the app never boots). Publish the client through its own pipeline and let
# its processed wwwroot replace the raw copy. The API has no wwwroot of its
# own, so a full replace loses nothing.
WEB_OUT="$(dirname "$OUT")/web"

dotnet publish src/CardStock.Web \
  -c Release \
  -o "$WEB_OUT"

rsync -a --delete "$WEB_OUT/wwwroot/" "$OUT/wwwroot/"

# Fail loudly if index.html references a script the bundle does not contain --
# the exact regression this overlay exists to prevent.
SCRIPT_REF=$(grep -o 'src="_framework/[^"]*"' "$OUT/wwwroot/index.html" | head -1 | sed 's/^src="//; s/"$//')
if [[ "$SCRIPT_REF" == *'{fingerprint}'* || ! -f "$OUT/wwwroot/$SCRIPT_REF" ]]; then
  echo "publish.sh: index.html references '$SCRIPT_REF', which is missing from the bundle" >&2
  exit 1
fi

echo "Published to $OUT"
