#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
port="${MARKSTASH_BACKEND_PORT:-5080}"

export ASPNETCORE_URLS="http://localhost:$port"
export Markstash__Backend__ExposeDiagnostics="${MARKSTASH_EXPOSE_DIAGNOSTICS:-false}"

dotnet run \
  --project "$repo_root/src/Markstash.Backend/Markstash.Backend.csproj" \
  --no-launch-profile
