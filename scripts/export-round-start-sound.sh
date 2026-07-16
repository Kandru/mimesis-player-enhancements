#!/usr/bin/env bash
# Export the vanilla dungeon-landing melody (Sound_UI_TramStopBGM_01) from MIMESIS Unity assets.
#
# Output: src/MimesisPlayerEnhancement/Assets/RoundStartSound/vanilla.wav
#
# Usage:
#   ./scripts/export-round-start-sound.sh
#   ./scripts/export-round-start-sound.sh --game /path/to/MIMESIS
#   MIMESIS_PATH=/path/to/MIMESIS ./scripts/export-round-start-sound.sh
#
# UnityPy is required to read Unity .assets / .bundle files. If it is not installed
# locally (pip install UnityPy), this script falls back to Docker — same pattern as
# build-webdashboard.sh.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUTPUT="$ROOT/src/MimesisPlayerEnhancement/Assets/RoundStartSound/vanilla.wav"
GAME_PATH=""
PYTHON_ARGS=()

usage() {
  cat <<EOF
Export dungeon landing melody (Sound_UI_TramStopBGM_01) into mod embedded assets.

Usage: $(basename "$0") [options]

Options:
  --game <path>       MIMESIS install root (else MIMESIS_PATH / PathConfig.props)
  --output <path>     Output file (default: Assets/RoundStartSound/vanilla.wav)
  --docker            Force Docker (python:3.12-slim + pip install UnityPy)
  --local             Force local python3 (fail if UnityPy missing)
  -h, --help          Show this help

Requires UnityPy (local pip install) or Docker for the fallback.
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --game)
      [[ $# -ge 2 ]] || { echo "Missing value for --game" >&2; exit 1; }
      GAME_PATH="$2"
      PYTHON_ARGS+=(--game "$2")
      shift 2
      ;;
    --output)
      [[ $# -ge 2 ]] || { echo "Missing value for --output" >&2; exit 1; }
      OUTPUT="$2"
      PYTHON_ARGS+=(--output "$2")
      shift 2
      ;;
    --docker) FORCE_DOCKER=true; shift ;;
    --local) FORCE_LOCAL=true; shift ;;
    -h|--help) usage; exit 0 ;;
    -*) echo "Unknown option: $1" >&2; usage >&2; exit 1 ;;
    *) echo "Unknown argument: $1" >&2; usage >&2; exit 1 ;;
  esac
done

FORCE_DOCKER="${FORCE_DOCKER:-false}"
FORCE_LOCAL="${FORCE_LOCAL:-false}"

resolve_game_path() {
  if [[ -n "$GAME_PATH" ]]; then
    GAME_PATH="$(cd "$GAME_PATH" && pwd)"
    return 0
  fi

  local candidate="${MIMESIS_PATH:-${GAME_PATH:-}}"
  if [[ -z "$candidate" && -f "$ROOT/PathConfig.props" ]]; then
    candidate="$(grep -oP '(?<=<GamePath>)[^<]+' "$ROOT/PathConfig.props" | head -1 || true)"
  fi

  if [[ -n "$candidate" && -d "$candidate/MIMESIS_Data" ]]; then
    GAME_PATH="$(cd "$candidate" && pwd)"
    PYTHON_ARGS+=(--game "$GAME_PATH")
    return 0
  fi

  echo "Could not find MIMESIS install." >&2
  echo "Set MIMESIS_PATH, GamePath in PathConfig.props, or pass --game." >&2
  exit 1
}

resolve_python() {
  if [[ -x /usr/bin/python3 ]]; then
    echo /usr/bin/python3
    return 0
  fi
  if command -v python3 >/dev/null 2>&1; then
    command -v python3
    return 0
  fi
  return 1
}

has_local_unitypy() {
  local py="$1"
  "$py" -c "import UnityPy" 2>/dev/null
}

run_local() {
  local py="$1"
  "$py" "$ROOT/scripts/export-round-start-sound.py" "${PYTHON_ARGS[@]}"
}

run_docker() {
  if ! command -v docker >/dev/null 2>&1; then
    echo "Docker not found. Install UnityPy locally instead:" >&2
    echo "  python3 -m pip install --user UnityPy" >&2
    exit 1
  fi

  local host_out="$OUTPUT"
  local rel_out
  rel_out="$(realpath --relative-to="$ROOT" "$host_out")"
  local container_out="/repo/$rel_out"

  echo "UnityPy not available locally — exporting via Docker…"
  docker run --rm \
    --user "$(id -u):$(id -g)" \
    -v "$GAME_PATH:/game:ro" \
    -v "$ROOT:/repo" \
    python:3.12-slim \
    bash -lc "pip install -q -t /tmp/unitypy UnityPy && PYTHONPATH=/tmp/unitypy python /repo/scripts/export-round-start-sound.py --game /game --output '$container_out'"
}

if [[ ${#PYTHON_ARGS[@]} -eq 0 || " ${PYTHON_ARGS[*]} " != *" --output "* ]]; then
  PYTHON_ARGS+=(--output "$OUTPUT")
fi

resolve_game_path

if [[ "$FORCE_DOCKER" == true ]]; then
  run_docker
  exit 0
fi

PY="$(resolve_python || true)"
if [[ "$FORCE_LOCAL" == true ]]; then
  [[ -n "$PY" ]] || { echo "python3 not found" >&2; exit 1; }
  has_local_unitypy "$PY" || {
    echo "UnityPy not installed. Run: $PY -m pip install --user UnityPy" >&2
    exit 1
  }
  run_local "$PY"
  exit 0
fi

if [[ -n "$PY" ]] && has_local_unitypy "$PY"; then
  run_local "$PY"
else
  run_docker
fi
