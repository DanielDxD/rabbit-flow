#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUTPUT_DIR="${ROOT_DIR}/artifacts"
NUGET_SOURCE="${NUGET_SOURCE:-https://api.nuget.org/v3/index.json}"

if [[ -z "${NUGET_API_KEY:-}" ]]; then
    echo "Error: NUGET_API_KEY environment variable is not set."
    echo "Create an API key at https://www.nuget.org/account/apikeys"
    exit 1
fi

"${ROOT_DIR}/scripts/pack.sh"

echo "==> Publishing to ${NUGET_SOURCE}..."
dotnet nuget push "${OUTPUT_DIR}"/*.nupkg \
    --api-key "${NUGET_API_KEY}" \
    --source "${NUGET_SOURCE}" \
    --skip-duplicate

if compgen -G "${OUTPUT_DIR}"/*.snupkg > /dev/null; then
    echo "==> Publishing symbol package..."
    dotnet nuget push "${OUTPUT_DIR}"/*.snupkg \
        --api-key "${NUGET_API_KEY}" \
        --source "${NUGET_SOURCE}" \
        --skip-duplicate
fi

echo "==> Done! Package published successfully."
