#!/usr/bin/env bash
set -euo pipefail

# Linux-only helper for local Playwright smoke runs.
# Usage:
#   TEST_ADMIN_EMAIL=... TEST_ADMIN_PASSWORD=... \
#   TEST_STAFF_EMAIL=... TEST_STAFF_PASSWORD=... \
#   TEST_CITIZEN_EMAIL=... TEST_CITIZEN_PASSWORD=... \
#   PLAYWRIGHT_TENANT_ID=... \
#   ./tests/e2e-playwright/run-smoke-linux.sh

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
E2E_BIN_DIR="$ROOT_DIR/tests/e2e-playwright/bin/Release/net10.0"

PLAYWRIGHT_BASE_URL="${PLAYWRIGHT_BASE_URL:-http://127.0.0.1:5173}"
PLAYWRIGHT_API_URL="${PLAYWRIGHT_API_URL:-http://127.0.0.1:5258}"
PLAYWRIGHT_BROWSER="${PLAYWRIGHT_BROWSER:-chromium}"
PLAYWRIGHT_BROWSERS_PATH="${PLAYWRIGHT_BROWSERS_PATH:-$ROOT_DIR/tests/e2e-playwright/.pw-browsers}"

mkdir -p "$PLAYWRIGHT_BROWSERS_PATH"

missing=0
for var in \
  TEST_ADMIN_EMAIL TEST_ADMIN_PASSWORD \
  TEST_STAFF_EMAIL TEST_STAFF_PASSWORD \
  TEST_CITIZEN_EMAIL TEST_CITIZEN_PASSWORD \
  PLAYWRIGHT_TENANT_ID; do
  if [[ -z "${!var:-}" ]]; then
    echo "Missing required env var: $var" >&2
    missing=1
  fi
done

if [[ "$missing" -ne 0 ]]; then
  exit 1
fi

echo "Building e2e project..."
dotnet build "$ROOT_DIR/tests/e2e-playwright/e2e-playwright.csproj" -c Release >/dev/null

if [[ "$PLAYWRIGHT_BROWSER" == "chromium" ]]; then
  # No PowerShell required: use bundled Node + Playwright CLI.
  PLAYWRIGHT_BROWSERS_PATH="$PLAYWRIGHT_BROWSERS_PATH" \
  "$E2E_BIN_DIR/.playwright/node/linux-x64/node" \
    "$E2E_BIN_DIR/.playwright/package/cli.js" install chromium
fi

echo "Running smoke tests with PLAYWRIGHT_BROWSER=$PLAYWRIGHT_BROWSER"
echo "Using PLAYWRIGHT_BROWSERS_PATH=$PLAYWRIGHT_BROWSERS_PATH"
PLAYWRIGHT_BASE_URL="$PLAYWRIGHT_BASE_URL" \
PLAYWRIGHT_API_URL="$PLAYWRIGHT_API_URL" \
PLAYWRIGHT_TENANT_ID="$PLAYWRIGHT_TENANT_ID" \
PLAYWRIGHT_BROWSER="$PLAYWRIGHT_BROWSER" \
PLAYWRIGHT_BROWSERS_PATH="$PLAYWRIGHT_BROWSERS_PATH" \
TEST_ADMIN_EMAIL="$TEST_ADMIN_EMAIL" \
TEST_ADMIN_PASSWORD="$TEST_ADMIN_PASSWORD" \
TEST_STAFF_EMAIL="$TEST_STAFF_EMAIL" \
TEST_STAFF_PASSWORD="$TEST_STAFF_PASSWORD" \
TEST_CITIZEN_EMAIL="$TEST_CITIZEN_EMAIL" \
TEST_CITIZEN_PASSWORD="$TEST_CITIZEN_PASSWORD" \
CI=true \
dotnet test "$ROOT_DIR/tests/e2e-playwright/e2e-playwright.csproj" \
  --configuration Release \
  --no-build \
  --settings "$ROOT_DIR/tests/e2e-playwright/playwright.runsettings" \
  -- NUnit.Where="cat == Smoke" \
  --results-directory "$ROOT_DIR/tests/e2e-playwright/test-results/smoke-local-linux" \
  --logger "trx;LogFileName=playwright-smoke-local-linux.trx"
