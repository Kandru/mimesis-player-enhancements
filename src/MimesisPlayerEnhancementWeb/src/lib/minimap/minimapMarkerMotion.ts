import type { MinimapMarkerDto } from '$lib/types';

/** Matches backend minimap SSE cadence (100ms). */
export const MINIMAP_MARKER_SMOOTH_MS = 100;
/** Normalized map units; above this distance markers snap instead of sliding. */
export const MINIMAP_MARKER_TELEPORT_DIST = 0.08;

type MarkerMotionState = {
  displayX: number;
  displayZ: number;
  displayYaw: number;
  fromX: number;
  fromZ: number;
  fromYaw: number;
  toX: number;
  toZ: number;
  toYaw: number;
  animStart: number;
  animDuration: number;
};

function lerp(a: number, b: number, t: number) {
  return a + (b - a) * t;
}

function lerpYaw(from: number, to: number, t: number) {
  let delta = ((to - from + 540) % 360) - 180;
  return from + delta * t;
}

function easeOut(t: number) {
  return 1 - (1 - t) * (1 - t);
}

export function createMinimapMarkerMotion() {
  const states = new Map<string, MarkerMotionState>();

  function removeStale(activeIds: Set<string>) {
    for (const id of states.keys()) {
      if (!activeIds.has(id)) {
        states.delete(id);
      }
    }
  }

  function setTargets(markers: MinimapMarkerDto[], now = performance.now()) {
    const activeIds = new Set<string>();
    for (const marker of markers) {
      const id = String(marker.steamId);
      activeIds.add(id);

      const prev = states.get(id);
      const jump = prev
        ? Math.hypot(marker.x - prev.toX, marker.z - prev.toZ)
        : 0;

      if (!prev || jump > MINIMAP_MARKER_TELEPORT_DIST) {
        states.set(id, {
          displayX: marker.x,
          displayZ: marker.z,
          displayYaw: marker.yaw,
          fromX: marker.x,
          fromZ: marker.z,
          fromYaw: marker.yaw,
          toX: marker.x,
          toZ: marker.z,
          toYaw: marker.yaw,
          animStart: now,
          animDuration: 0,
        });
        continue;
      }

      if (
        Math.abs(marker.x - prev.toX) < 1e-5
        && Math.abs(marker.z - prev.toZ) < 1e-5
        && Math.abs(marker.yaw - prev.toYaw) < 0.5
      ) {
        continue;
      }

      states.set(id, {
        displayX: prev.displayX,
        displayZ: prev.displayZ,
        displayYaw: prev.displayYaw,
        fromX: prev.displayX,
        fromZ: prev.displayZ,
        fromYaw: prev.displayYaw,
        toX: marker.x,
        toZ: marker.z,
        toYaw: marker.yaw,
        animStart: now,
        animDuration: MINIMAP_MARKER_SMOOTH_MS,
      });
    }

    removeStale(activeIds);
  }

  function tick(now = performance.now()) {
    let animating = false;
    for (const state of states.values()) {
      if (state.animDuration <= 0) {
        continue;
      }

      animating = true;
      const rawT = Math.min(1, (now - state.animStart) / state.animDuration);
      const t = easeOut(rawT);
      state.displayX = lerp(state.fromX, state.toX, t);
      state.displayZ = lerp(state.fromZ, state.toZ, t);
      state.displayYaw = lerpYaw(state.fromYaw, state.toYaw, t);

      if (rawT >= 1) {
        state.displayX = state.toX;
        state.displayZ = state.toZ;
        state.displayYaw = state.toYaw;
        state.animDuration = 0;
      }
    }

    return animating;
  }

  function isAnimating() {
    for (const state of states.values()) {
      if (state.animDuration > 0) {
        return true;
      }
    }
    return false;
  }

  function getDisplayMarkers(markers: MinimapMarkerDto[]): MinimapMarkerDto[] {
    return markers.map((marker) => {
      const state = states.get(String(marker.steamId));
      if (!state) {
        return marker;
      }

      return {
        ...marker,
        x: state.displayX,
        z: state.displayZ,
        yaw: state.displayYaw,
      };
    });
  }

  function reset() {
    states.clear();
  }

  return {
    setTargets,
    tick,
    isAnimating,
    getDisplayMarkers,
    reset,
  };
}
