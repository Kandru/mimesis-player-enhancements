#!/usr/bin/env bash
# Stage wiki and changelog sources for the web dashboard Docker build.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
WEB_SRC="$ROOT/src/MimesisPlayerEnhancementWeb"

rm -rf "$WEB_SRC/.wiki-src" "$WEB_SRC/.changelog-src"
cp -r "$ROOT/docs/wiki" "$WEB_SRC/.wiki-src"
mkdir -p "$WEB_SRC/.changelog-src"
cp "$ROOT/thunderstore/CHANGELOG.md" "$WEB_SRC/.changelog-src/CHANGELOG.md"
cp "$ROOT/src/Version.cs" "$WEB_SRC/.changelog-src/Version.cs"

echo "Staged web sources → $WEB_SRC/.wiki-src and .changelog-src"
