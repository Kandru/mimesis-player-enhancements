#!/usr/bin/env bash
# Stage and zip a Thunderstore package from dist/prod/MimesisPlayerEnhancement.dll
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
THUNDERSTORE_DIR="$ROOT/thunderstore"
OUT_DIR="$ROOT/dist/thunderstore"
BUILD_OUT="$ROOT/dist/prod/MimesisPlayerEnhancement.dll"
VERSION_FILE="$ROOT/src/Version.cs"

VERSION="$(grep -E 'ModuleVersion\s*=\s*"' "$VERSION_FILE" | sed -E 's/.*ModuleVersion\s*=\s*"([^"]+)".*/\1/')"
if [[ -z "$VERSION" ]]; then
  echo "error: could not read ModuleVersion from $VERSION_FILE" >&2
  exit 1
fi

if [[ ! -f "$BUILD_OUT" ]]; then
  echo "error: build output missing: $BUILD_OUT" >&2
  exit 1
fi

ZIP_NAME="mpe${VERSION}.zip"
ZIP_PATH="$OUT_DIR/$ZIP_NAME"
STAGING="$(mktemp -d)"
cleanup() { rm -rf "$STAGING"; }
trap cleanup EXIT

echo "==> Staging Thunderstore metadata (v${VERSION})"
cp "$THUNDERSTORE_DIR/README.md" "$STAGING/README.md"
cp "$THUNDERSTORE_DIR/CHANGELOG.md" "$STAGING/CHANGELOG.md"

if [[ -f "$THUNDERSTORE_DIR/icon.png" ]]; then
  cp "$THUNDERSTORE_DIR/icon.png" "$STAGING/icon.png"
elif [[ -f "$ROOT/logo.png" ]]; then
  if command -v magick >/dev/null 2>&1; then
    magick "$ROOT/logo.png" -resize 256x256 "$STAGING/icon.png"
  elif command -v convert >/dev/null 2>&1; then
    convert "$ROOT/logo.png" -resize 256x256 "$STAGING/icon.png"
  else
    echo "error: install ImageMagick or add thunderstore/icon.png" >&2
    exit 1
  fi
else
  echo "error: missing thunderstore/icon.png and logo.png" >&2
  exit 1
fi

python3 -c 'import json,sys; t,o,v=sys.argv[1:4]; m=json.load(open(t,encoding="utf-8")); m["version_number"]=v; json.dump(m,open(o,"w",encoding="utf-8"),indent=2); open(o,"a",encoding="utf-8").write("\n")' \
  "$THUNDERSTORE_DIR/manifest.json" "$STAGING/manifest.json" "$VERSION"

echo "==> Adding mod assembly"
cp "$BUILD_OUT" "$STAGING/"

mkdir -p "$OUT_DIR"
rm -f "$ZIP_PATH"

echo "==> Creating $ZIP_PATH"
( cd "$STAGING" && zip -rq "$ZIP_PATH" . )

echo ""
echo "Thunderstore package ready: $ZIP_PATH"
unzip -l "$ZIP_PATH"
