#!/usr/bin/env bash
# Copy l10n/*.json into the mod Assets/Locale/ tree for embedding at compile time.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
L10N_SRC="$ROOT/l10n"
LOCALE_OUT="$ROOT/src/MimesisPlayerEnhancement/Assets/Locale"

if [[ ! -d "$L10N_SRC" ]]; then
  echo "error: missing $L10N_SRC" >&2
  exit 1
fi

shopt -s nullglob
files=("$L10N_SRC"/*.json)
shopt -u nullglob

if [[ ${#files[@]} -eq 0 ]]; then
  echo "error: no locale JSON files in $L10N_SRC" >&2
  exit 1
fi

mkdir -p "$LOCALE_OUT"
cp "${files[@]}" "$LOCALE_OUT/"
echo "Staged ${#files[@]} locale file(s) → $LOCALE_OUT"
