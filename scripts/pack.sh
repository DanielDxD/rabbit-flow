#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUTPUT_DIR="${ROOT_DIR}/artifacts"
CONFIGURATION="${CONFIGURATION:-Release}"

echo "==> Restoring projects..."
dotnet restore "${ROOT_DIR}/RabbitFlow/RabbitFlow.csproj"
dotnet restore "${ROOT_DIR}/tests/RabbitFlow.Tests/RabbitFlow.Tests.csproj"

echo "==> Running tests..."
dotnet test "${ROOT_DIR}/tests/RabbitFlow.Tests/RabbitFlow.Tests.csproj" \
    -c "${CONFIGURATION}" \
    --no-restore

echo "==> Packing RabbitFlow..."
dotnet pack "${ROOT_DIR}/RabbitFlow/RabbitFlow.csproj" \
    -c "${CONFIGURATION}" \
    -o "${OUTPUT_DIR}" \
    /p:IncludeSymbols=true \
    /p:SymbolPackageFormat=snupkg

echo "==> Packages created in ${OUTPUT_DIR}:"
ls -la "${OUTPUT_DIR}"/*.nupkg "${OUTPUT_DIR}"/*.snupkg 2>/dev/null || ls -la "${OUTPUT_DIR}"
