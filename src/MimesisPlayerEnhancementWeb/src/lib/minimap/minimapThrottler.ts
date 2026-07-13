import type { MinimapMarkerDto } from '$lib/types';

export function fingerprintMarkers(markers: MinimapMarkerDto[]): string {
  return markers
    .map(
      (m) =>
        `${m.steamId}:${m.x.toFixed(3)}:${m.z.toFixed(3)}:${Math.round(m.yaw)}:${m.areaId}:${m.floorIndex ?? 0}:${m.isAlive}`,
    )
    .join('|');
}
