#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
HOST="${HOST:-http://localhost:5000}"
BRUNO_VERSION=4.2.1
cd "$ROOT/realworld/specs/api/bruno"
for folder in */; do
  [ "$folder" = "environments/" ] && continue
  bun x "@usebruno/cli@$BRUNO_VERSION" run "${folder%/}" \
    --env local --env-var "host=$HOST" --sandbox "${BRUNO_SANDBOX:-safe}"
done
