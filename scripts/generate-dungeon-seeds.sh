#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

echo "==> Building MimesisSeedScanner mod (catalog export)..."
dotnet build src/MimesisSeedScanner.Mod/MimesisSeedScanner.Mod.csproj -c Release

MOD_DLL="src/MimesisSeedScanner.Mod/bin/Release/MimesisSeedScanner.dll"
if [[ -f "$MOD_DLL" ]]; then
  echo "==> Scanner mod built: $MOD_DLL"
  echo "    Copy to MIMESIS/Mods/, launch game, press F10 on main menu to export scan-catalog.json"
  echo "    Output: MelonLoader/UserData/scan-catalog.json"
else
  echo "WARN: Expected scanner DLL at $MOD_DLL"
fi

CATALOG="${CATALOG:-}"
if [[ -z "$CATALOG" ]]; then
  for candidate in \
    "$HOME/.local/share/MelonLoader/Preferences/MIMESIS/scan-catalog.json" \
    "$HOME/.config/MelonLoader/Preferences/MIMESIS/scan-catalog.json" \
    "$HOME/.local/share/Steam/steamapps/common/MIMESIS/UserData/scan-catalog.json" \
    "scan-catalog.json" \
  ; do
    if [[ -f "$candidate" ]]; then
      CATALOG="$candidate"
      break
    fi
  done
fi

INPUT="${1:-}"
if [[ -z "$INPUT" ]]; then
  for candidate in \
    "$HOME/.local/share/MelonLoader/Preferences/MIMESIS/seed-scan-results.json" \
    "$HOME/.config/MelonLoader/Preferences/MIMESIS/seed-scan-results.json" \
    "seed-scan-results.json" \
  ; do
    if [[ -f "$candidate" ]]; then
      INPUT="$candidate"
      break
    fi
  done
fi

if [[ -n "$CATALOG" && -f "$CATALOG" && -z "$INPUT" ]]; then
  echo "==> Running CLI scan from $CATALOG"
  dotnet run --project src/MimesisSeedScanner.Cli -- scan \
    --catalog "$CATALOG" \
    --output seed-scan-results.json \
    "${SCAN_ARGS[@]:-}"
  INPUT="seed-scan-results.json"
fi

if [[ -n "$INPUT" && -f "$INPUT" ]]; then
  echo "==> Running codegen from $INPUT"
  dotnet run --project src/MimesisSeedScanner.Cli -- codegen "$INPUT" \
    --output src/MimesisPlayerEnhancement/Features/DungeonRandomizer/DungeonSeedPools.Generated.cs
else
  echo "==> No scan JSON found. Full workflow:"
  echo "    1. F10 in game → scan-catalog.json"
  echo "    2. dotnet run --project src/MimesisSeedScanner.Cli -- scan --catalog /path/to/scan-catalog.json"
  echo "    3. $0 seed-scan-results.json"
fi
