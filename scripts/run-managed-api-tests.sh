#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"
API_URL="${API_URL:-http://localhost:5050}"
WORK="$(mktemp -d "${TMPDIR:-/tmp}/conduit-api.XXXXXX")"
SERVER_PID=""
cleanup() {
  local status=$?
  trap - EXIT
  if [ -n "$SERVER_PID" ]; then
    kill "$SERVER_PID" 2>/dev/null || true
    wait "$SERVER_PID" 2>/dev/null || true
  fi
  if [ "$status" -ne 0 ] && [ -f "$WORK/server.log" ]; then
    cat "$WORK/server.log" >&2
  fi
  rm -f "$WORK/realworld.db" "$WORK/realworld.db-shm" "$WORK/realworld.db-wal" "$WORK/server.log"
  rmdir "$WORK"
  exit "$status"
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

# Refuse to accidentally test against someone else's already-running API.
if curl --silent --max-time 2 "$API_URL/api/tags" >/dev/null; then
  echo "An HTTP server is already listening at $API_URL; choose a different API_URL." >&2
  exit 1
fi

dotnet build src/Conduit/Conduit.csproj -p:RestoreLockedMode=true
Jwt__SigningKey="${Jwt__SigningKey:-$(openssl rand -base64 32)}" \
  Database__Provider=sqlite ConnectionStrings__Conduit="Data Source=$WORK/realworld.db" \
  ApiPrefix=api ASPNETCORE_URLS="$API_URL" \
  dotnet src/Conduit/bin/Debug/net10.0/Conduit.dll >"$WORK/server.log" 2>&1 &
SERVER_PID=$!

ready=false
deadline=$((SECONDS + 120))
while [ "$SECONDS" -lt "$deadline" ]; do
  if ! kill -0 "$SERVER_PID" 2>/dev/null; then
    echo "API exited before becoming ready." >&2
    exit 1
  fi
  if curl --fail --silent --max-time 2 "$API_URL/api/tags" >/dev/null; then
    ready=true
    break
  fi
  sleep 0.5
done
if [ "$ready" != true ]; then
  echo "API did not become ready within the startup deadline." >&2
  exit 1
fi

HOST="$API_URL" bash "$@"
