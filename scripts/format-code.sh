#!/usr/bin/env bash
# Apply C# style and analyzer fixes (unused usings, import order, whitespace, etc.).
# Uses .editorconfig rules. Safe to run before every build.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SLN="$ROOT/src/MimesisPlayerEnhancement.sln"

VERIFY=false
while [[ $# -gt 0 ]]; do
  case "$1" in
    --verify) VERIFY=true; shift ;;
    -h|--help)
      echo "Usage: $0 [--verify]"
      echo "  --verify  Check formatting without modifying files (non-zero exit if changes needed)"
      exit 0
      ;;
    *) echo "Unknown option: $1" >&2; exit 2 ;;
  esac
done

ARGS=("$SLN" "--verbosity" "minimal")
if [[ "$VERIFY" == "true" ]]; then
  ARGS+=("--verify-no-changes")
fi

dotnet format "${ARGS[@]}"
