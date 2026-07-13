import type { MinimapMarkerDto } from '$lib/types';

export function fingerprintMarkers(markers: MinimapMarkerDto[]): string {
  return markers
    .map(
      (m) =>
        `${m.steamId}:${m.x.toFixed(3)}:${m.z.toFixed(3)}:${Math.round(m.yaw)}:${m.areaId}:${m.floorIndex ?? 0}:${m.isAlive}`,
    )
    .join('|');
}

export function createMinimapThrottler(intervalMs = 100) {
  let lastEmit = 0;
  let pending: (() => void) | null = null;
  let timer: ReturnType<typeof setTimeout> | null = null;

  return {
    schedule(run: () => void) {
      pending = run;
      const now = Date.now();
      if (now - lastEmit >= intervalMs) {
        this.flush();
        return;
      }
      if (!timer) {
        timer = setTimeout(() => this.flush(), intervalMs - (now - lastEmit));
      }
    },
    flush() {
      if (timer) {
        clearTimeout(timer);
        timer = null;
      }
      if (!pending) return;
      const run = pending;
      pending = null;
      lastEmit = Date.now();
      run();
    },
    dispose() {
      if (timer) clearTimeout(timer);
      pending = null;
    },
  };
}
