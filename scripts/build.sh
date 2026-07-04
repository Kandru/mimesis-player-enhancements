#!/usr/bin/env bash
# Build into dist/debug or dist/prod. No game install required.
# To also copy into the game Mods/ folder: COPY_TO_MODS=true MIMESIS_PATH=/path/to/MIMESIS ./scripts/build.sh
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
"$ROOT/scripts/bootstrap-deps.sh"

CONFIG="${1:-Debug}"
shift || true

case "$CONFIG" in
  Debug|debug)   DOTNET_CONFIG="Debug" ;;
  Release|Prod|prod) DOTNET_CONFIG="Release" ;;
  *)             DOTNET_CONFIG="$CONFIG" ;;
esac

EXTRA=()
if [[ "${COPY_TO_MODS:-false}" == "true" ]]; then
  EXTRA+=("-p:CopyToMods=true")
  if [[ -n "${MIMESIS_PATH:-}" ]]; then
    EXTRA+=("-p:GamePath=${MIMESIS_PATH}")
  fi
fi

if [[ "${SKIP_FORMAT:-false}" != "true" ]]; then
  "$ROOT/scripts/format-code.sh"
fi

dotnet build "$ROOT/src/MimesisPlayerEnhancement.sln" -c "$DOTNET_CONFIG" "${EXTRA[@]}" "$@"

OUT_DIR="$ROOT/dist/$([[ "$DOTNET_CONFIG" == "Release" ]] && echo prod || echo debug)"
echo ""
echo "Built: $OUT_DIR/MimesisPlayerEnhancement.dll"
