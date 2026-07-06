#!/usr/bin/env bash
# Downloads pinned external dependencies into deps/ for local builds and CI.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DEPS_DIR="$ROOT/deps"
REF_DIR="$DEPS_DIR/reference"
REFERENCE_LIBS_VERSION="${REFERENCE_LIBS_VERSION:-1.0.0}"
REFERENCE_LIBS_ZIP="mimesis-reference-libs-${REFERENCE_LIBS_VERSION}.zip"

reference_libs_present() {
  [[ -f "$REF_DIR/Managed/Assembly-CSharp.dll" && -f "$REF_DIR/MelonLoader/net35/MelonLoader.dll" ]]
}

if reference_libs_present; then
  exit 0
fi

mkdir -p "$REF_DIR/Managed" "$REF_DIR/MelonLoader/net35"

# --- Reference assemblies (compile-only; no game install required on CI) ---
ensure_reference_libs() {

  local zip_path="$DEPS_DIR/$REFERENCE_LIBS_ZIP"
  if [[ -f "$zip_path" ]]; then
    echo "Extracting $REFERENCE_LIBS_ZIP..."
    unzip -qo "$zip_path" -d "$DEPS_DIR"
    return 0
  fi

  if [[ -n "${REFERENCE_LIBS_URL:-}" ]]; then
    echo "Downloading reference libs from REFERENCE_LIBS_URL..."
    tmp_zip="$(mktemp)"
    curl -fsSL "$REFERENCE_LIBS_URL" -o "$tmp_zip"
    unzip -qo "$tmp_zip" -d "$DEPS_DIR"
    rm -f "$tmp_zip"
    return 0
  fi

  if [[ -n "${GITHUB_REPOSITORY:-}" ]]; then
    local gh_url="https://github.com/${GITHUB_REPOSITORY}/releases/download/reference-libs-${REFERENCE_LIBS_VERSION}/${REFERENCE_LIBS_ZIP}"
    echo "Trying GitHub release asset: $gh_url"
    if tmp_zip="$(mktemp)" && curl -fsSL "$gh_url" -o "$tmp_zip"; then
      unzip -qo "$tmp_zip" -d "$DEPS_DIR"
      rm -f "$tmp_zip"
      return 0
    fi
    rm -f "$tmp_zip" 2>/dev/null || true
  fi

  if [[ -n "${MIMESIS_PATH:-}" ]] || [[ -f "$ROOT/PathConfig.props" ]]; then
    echo "Packing reference libs from local game install..."
    "$ROOT/scripts/pack-reference-libs.sh"
    return 0
  fi

  echo "Reference assemblies not found." >&2
  echo "CI: upload ${REFERENCE_LIBS_ZIP} to a GitHub release tagged reference-libs-${REFERENCE_LIBS_VERSION}," >&2
  echo "     or set REFERENCE_LIBS_URL to a direct download URL." >&2
  echo "Local: set MIMESIS_PATH and re-run bootstrap, or run scripts/pack-reference-libs.sh" >&2
  return 1
}

ensure_reference_libs

echo "Bootstrap complete."
