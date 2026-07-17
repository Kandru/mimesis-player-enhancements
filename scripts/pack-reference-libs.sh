#!/usr/bin/env bash
# Pack compile-only reference DLLs from a local MIMESIS install into deps/reference/.
# Upload the resulting zip as a GitHub release asset for CI (see README).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
REF_DIR="$ROOT/deps/reference"
GAME_PATH="${MIMESIS_PATH:-${GAME_PATH:-}}"

if [[ -z "$GAME_PATH" && -f "$ROOT/PathConfig.props" ]]; then
  GAME_PATH="$(sed -n 's:.*<GamePath>\(.*\)</GamePath>.*:\1:p' "$ROOT/PathConfig.props" 2>/dev/null | head -1)"
fi

if [[ -z "$GAME_PATH" || ! -d "$GAME_PATH" ]]; then
  echo "Set MIMESIS_PATH or GamePath in PathConfig.props to your game install." >&2
  exit 1
fi

MANAGED="$GAME_PATH/MIMESIS_Data/Managed"
MELON="$GAME_PATH/MelonLoader/net35"

for f in "$MANAGED/Assembly-CSharp.dll" "$MANAGED/FishNet.Runtime.dll" "$MANAGED/UniTask.dll" \
         "$MANAGED/UnityEngine.dll" "$MANAGED/UnityEngine.CoreModule.dll" \
         "$MANAGED/UnityEngine.ImageConversionModule.dll" "$MANAGED/com.rlabrecque.steamworks.net.dll" \
         "$MELON/MelonLoader.dll"; do
  if [[ ! -f "$f" ]]; then
    echo "Missing required file: $f" >&2
    exit 1
  fi
done

mkdir -p "$REF_DIR/Managed" "$REF_DIR/MelonLoader/net35"

cp "$MANAGED/Assembly-CSharp.dll" \
   "$MANAGED/FishNet.Runtime.dll" \
   "$MANAGED/UniTask.dll" \
   "$MANAGED/UnityEngine.dll" \
   "$MANAGED/UnityEngine.CoreModule.dll" \
   "$MANAGED/UnityEngine.ImageConversionModule.dll" \
   "$MANAGED/com.rlabrecque.steamworks.net.dll" \
   "$REF_DIR/Managed/"

cp "$MELON/MelonLoader.dll" "$REF_DIR/MelonLoader/net35/"

cat > "$REF_DIR/README.txt" <<EOF
Compile-only reference assemblies extracted from MIMESIS.
Do not redistribute publicly without checking the game EULA.
Packed from: $GAME_PATH
EOF

VERSION="${REFERENCE_LIBS_VERSION:-1.0.0}"
ZIP="$ROOT/deps/mimesis-reference-libs-${VERSION}.zip"

python3 -c "
import zipfile, os
root = '$REF_DIR'
out = '$ZIP'
with zipfile.ZipFile(out, 'w', zipfile.ZIP_DEFLATED) as zf:
    for dirpath, _, files in os.walk(root):
        for f in files:
            path = os.path.join(dirpath, f)
            arc = os.path.relpath(path, os.path.dirname(root))
            zf.write(path, arc)
"

echo "Packed reference libs to:"
echo "  $REF_DIR"
echo "  $ZIP"
echo "Upload $ZIP as a GitHub release asset (tag: reference-libs-${VERSION})."
