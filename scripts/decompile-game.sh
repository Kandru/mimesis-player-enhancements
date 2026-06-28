#!/usr/bin/env bash
# Decompile MIMESIS game logic assemblies to local C# source for browsing (not for git).
#
# Requires: ilspycmd (dotnet tool install -g ilspycmd)
#
# Output: deps/decompiled/<AssemblyName>/   (gitignored)
#
# Usage:
#   ./scripts/decompile-game.sh                    # Assembly-CSharp*.dll (default)
#   ./scripts/decompile-game.sh --all-managed      # every Managed/*.dll except runtime/engine
#   ./scripts/decompile-game.sh FishNet.Runtime.dll
#   ./scripts/decompile-game.sh --force            # re-decompile even when DLL unchanged
#   ./scripts/decompile-game.sh --game /path/to/MIMESIS
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT_ROOT="$ROOT/deps/decompiled"
MANAGED=""
FORCE=false
ALL_MANAGED=false
EXTRA_DLLS=()

usage() {
  cat <<EOF
Decompile MIMESIS managed assemblies to deps/decompiled/ (gitignored).

Usage: $(basename "$0") [options] [AssemblyName.dll ...]

Options:
  --game <path>       MIMESIS install root (else MIMESIS_PATH / PathConfig.props / bootstrap)
  --managed <path>    MIMESIS_Data/Managed directory
  --all-managed       Decompile all Managed/*.dll except BCL / Unity engine modules
  --force             Re-decompile even when the source DLL has not changed
  -h, --help          Show this help

Default (no DLL args): Assembly-CSharp.dll and Assembly-CSharp-firstpass.dll

Requires ilspycmd: dotnet tool install -g ilspycmd
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --game)
      [[ $# -ge 2 ]] || { echo "Missing value for --game" >&2; exit 1; }
      MANAGED="$2/MIMESIS_Data/Managed"
      shift 2
      ;;
    --managed)
      [[ $# -ge 2 ]] || { echo "Missing value for --managed" >&2; exit 1; }
      MANAGED="$2"
      shift 2
      ;;
    --all-managed) ALL_MANAGED=true; shift ;;
    --force) FORCE=true; shift ;;
    -h|--help) usage; exit 0 ;;
    --) shift; EXTRA_DLLS+=("$@"); break ;;
    -*) echo "Unknown option: $1" >&2; usage >&2; exit 1 ;;
    *) EXTRA_DLLS+=("$1"); shift ;;
  esac
done

resolve_managed_dir() {
  if [[ -n "$MANAGED" ]]; then
    MANAGED="$(cd "$MANAGED" && pwd)"
    return 0
  fi

  local game_path="${MIMESIS_PATH:-${GAME_PATH:-}}"
  if [[ -z "$game_path" && -f "$ROOT/PathConfig.props" ]]; then
    game_path="$(grep -oP '(?<=<GamePath>)[^<]+' "$ROOT/PathConfig.props" | head -1 || true)"
  fi

  if [[ -n "$game_path" && -d "$game_path/MIMESIS_Data/Managed" ]]; then
    MANAGED="$(cd "$game_path/MIMESIS_Data/Managed" && pwd)"
    return 0
  fi

  local bootstrap="$ROOT/deps/reference/Managed"
  if [[ -d "$bootstrap" ]]; then
    MANAGED="$(cd "$bootstrap" && pwd)"
    return 0
  fi

  echo "Could not find game assemblies." >&2
  echo "Set MIMESIS_PATH, GamePath in PathConfig.props, pass --game/--managed," >&2
  echo "or run ./scripts/bootstrap-deps.sh" >&2
  exit 1
}

resolve_ilspycmd() {
  if command -v ilspycmd >/dev/null 2>&1; then
    command -v ilspycmd
    return 0
  fi

  local tool_path="${HOME}/.dotnet/tools/ilspycmd"
  if [[ -x "$tool_path" ]]; then
    echo "$tool_path"
    return 0
  fi

  echo "ilspycmd not found." >&2
  echo "Install with: dotnet tool install -g ilspycmd" >&2
  exit 1
}

is_skipped_runtime_dll() {
  local name="$1"
  case "$name" in
    mscorlib.dll|netstandard.dll|Mono.Security.dll) return 0 ;;
    System.*.dll|System.*.resources.dll) return 0 ;;
    UnityEngine*.dll|Unity.*.dll) return 0 ;;
  esac
  return 1
}

collect_default_dlls() {
  local dll
  for dll in Assembly-CSharp.dll Assembly-CSharp-firstpass.dll; do
    if [[ -f "$MANAGED/$dll" ]]; then
      DLLS+=("$dll")
    fi
  done
}

collect_all_managed_dlls() {
  local path name
  for path in "$MANAGED"/*.dll; do
    [[ -f "$path" ]] || continue
    name="$(basename "$path")"
    is_skipped_runtime_dll "$name" && continue
    DLLS+=("$name")
  done
}

decompile_dll() {
  local dll_name="$1"
  local dll_path="$MANAGED/$dll_name"
  local out_dir="$OUT_ROOT/${dll_name%.dll}"
  local stamp_file="$out_dir/.source-stamp"

  if [[ ! -f "$dll_path" ]]; then
    echo "Skip missing: $dll_path" >&2
    return 0
  fi

  if [[ "$FORCE" != true && -f "$stamp_file" ]]; then
    local stamped_path stamped_mtime current_mtime
    read -r stamped_path < "$stamp_file" || true
    stamped_mtime="$(stat -c %Y "$stamp_file" 2>/dev/null || stat -f %m "$stamp_file")"
    current_mtime="$(stat -c %Y "$dll_path" 2>/dev/null || stat -f %m "$dll_path")"
    if [[ "$stamped_path" == "$dll_path" && "$current_mtime" -le "$stamped_mtime" && -d "$out_dir" ]]; then
      echo "Up to date: $dll_name -> $out_dir"
      return 0
    fi
  fi

  echo "Decompiling: $dll_name -> $out_dir"
  rm -rf "$out_dir"
  mkdir -p "$out_dir"
  "$ILSPYCMD" -p -o "$out_dir" "$dll_path"
  printf '%s\n' "$dll_path" > "$stamp_file"
}

resolve_managed_dir
ILSPYCMD="$(resolve_ilspycmd)"
mkdir -p "$OUT_ROOT"

DLLS=()
if [[ ${#EXTRA_DLLS[@]} -gt 0 ]]; then
  DLLS=("${EXTRA_DLLS[@]}")
elif [[ "$ALL_MANAGED" == true ]]; then
  collect_all_managed_dlls
else
  collect_default_dlls
fi

if [[ ${#DLLS[@]} -eq 0 ]]; then
  echo "No assemblies to decompile in $MANAGED" >&2
  exit 1
fi

echo "Managed folder: $MANAGED"
echo "Output root:    $OUT_ROOT"
echo "Assemblies:     ${#DLLS[@]}"
echo ""

for dll in "${DLLS[@]}"; do
  decompile_dll "$dll"
done

cat > "$OUT_ROOT/README.txt" <<EOF
Decompiled MIMESIS managed assemblies (local reference only — do not commit).
Generated by scripts/decompile-game.sh from: $MANAGED
EOF

echo ""
echo "Done. Browse sources under: $OUT_ROOT"
