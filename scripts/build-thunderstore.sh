#!/usr/bin/env bash
# Build a Thunderstore-ready zip: dist/thunderstore/mpe<version>.zip
#
# Package layout (MelonLoader 0.7+ via Thunderstore clients):
#   manifest.json, icon.png, README.md  — zip root (required by Thunderstore)
#   MimesisPlayerEnhancement.dll        — mod assembly (web dashboard assets embedded)
# See: https://wiki.thunderstore.io/mods/creating-a-package
#      https://wiki.thunderstore.io/mods/packaging-your-mods
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
THUNDERSTORE_DIR="$ROOT/thunderstore"
OUT_DIR="$ROOT/dist/thunderstore"
VERSION_FILE="$ROOT/src/Version.cs"
BUILD_OUT="$ROOT/dist/prod"

read_version() {
  local version
  version="$(grep -E 'ModuleVersion\s*=\s*"' "$VERSION_FILE" | sed -E 's/.*ModuleVersion\s*=\s*"([^"]+)".*/\1/')"
  if [[ -z "$version" ]]; then
    echo "Could not read ModuleVersion from $VERSION_FILE" >&2
    exit 1
  fi
  printf '%s' "$version"
}

ensure_icon() {
  local dest="$1"
  if [[ -f "$THUNDERSTORE_DIR/icon.png" ]]; then
    cp "$THUNDERSTORE_DIR/icon.png" "$dest/icon.png"
    return
  fi

  if [[ ! -f "$ROOT/logo.png" ]]; then
    echo "Missing thunderstore/icon.png and logo.png — need a 256x256 PNG icon." >&2
    exit 1
  fi

  if command -v magick >/dev/null 2>&1; then
    magick "$ROOT/logo.png" -resize 256x256 "$dest/icon.png"
  elif command -v convert >/dev/null 2>&1; then
    convert "$ROOT/logo.png" -resize 256x256 "$dest/icon.png"
  else
    echo "Install ImageMagick (magick) to generate icon.png from logo.png, or add thunderstore/icon.png." >&2
    exit 1
  fi
}

write_manifest() {
  local dest="$1"
  local version="$2"
  python3 - "$THUNDERSTORE_DIR/manifest.json" "$dest/manifest.json" "$version" <<'PY'
import json
import sys

template_path, out_path, version = sys.argv[1:4]
with open(template_path, encoding="utf-8") as f:
    manifest = json.load(f)
manifest["version_number"] = version
with open(out_path, "w", encoding="utf-8") as f:
    json.dump(manifest, f, indent=2)
    f.write("\n")
PY
}

VERSION="$(read_version)"
ZIP_NAME="mpe${VERSION}.zip"
ZIP_PATH="$OUT_DIR/$ZIP_NAME"

echo "==> Building release (v${VERSION})"
REF="$ROOT/deps/reference"
if [[ ! -f "$REF/Managed/Assembly-CSharp.dll" || ! -f "$REF/MelonLoader/net35/MelonLoader.dll" ]]; then
  "$ROOT/scripts/bootstrap-deps.sh"
fi
dotnet build "$ROOT/src/MimesisPlayerEnhancement/MimesisPlayerEnhancement.csproj" -c Release

if [[ ! -f "$BUILD_OUT/MimesisPlayerEnhancement.dll" ]]; then
  echo "Build output missing: $BUILD_OUT/MimesisPlayerEnhancement.dll" >&2
  exit 1
fi

STAGING="$(mktemp -d)"
cleanup() { rm -rf "$STAGING"; }
trap cleanup EXIT

echo "==> Staging Thunderstore package"
cp "$THUNDERSTORE_DIR/README.md" "$STAGING/README.md"
cp "$THUNDERSTORE_DIR/CHANGELOG.md" "$STAGING/CHANGELOG.md"
ensure_icon "$STAGING"
write_manifest "$STAGING" "$VERSION"

cp "$BUILD_OUT/MimesisPlayerEnhancement.dll" "$STAGING/"

mkdir -p "$OUT_DIR"
rm -f "$ZIP_PATH"

echo "==> Creating $ZIP_PATH"
(
  cd "$STAGING"
  zip -rq "$ZIP_PATH" .
)

echo ""
echo "Thunderstore package: $ZIP_PATH"
echo "Contents:"
unzip -l "$ZIP_PATH"
