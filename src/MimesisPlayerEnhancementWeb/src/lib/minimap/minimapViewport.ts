import type { MinimapBoundsDto, MinimapTileDto } from '$lib/types';

export const VIEW_SIZE = 1000;
export const VIEW_PADDING = 0.08;

export type Viewport = { scale: number; offsetX: number; offsetZ: number };

export function computeViewportFromTiles(tiles: MinimapTileDto[], padding = VIEW_PADDING): Viewport {
  if (!tiles.length) return { scale: 1, offsetX: 0, offsetZ: 0 };
  let minX = Infinity;
  let minZ = Infinity;
  let maxX = -Infinity;
  let maxZ = -Infinity;
  for (const tile of tiles) {
    minX = Math.min(minX, tile.x);
    minZ = Math.min(minZ, tile.z);
    maxX = Math.max(maxX, tile.x + tile.w);
    maxZ = Math.max(maxZ, tile.z + tile.h);
  }
  return computeViewportFromExtents(minX, minZ, maxX, maxZ, padding);
}

export function computeViewportFromBounds(
  bounds: MinimapBoundsDto | null | undefined,
  padding = VIEW_PADDING,
): Viewport {
  if (!bounds) return { scale: 1, offsetX: 0, offsetZ: 0 };
  return computeViewportFromNormalizedRect(0, 0, 1, 1, padding);
}

export function computeViewportFromNormalizedRect(
  minX: number,
  minZ: number,
  maxX: number,
  maxZ: number,
  padding = VIEW_PADDING,
): Viewport {
  const contentW = Math.max(maxX - minX, 0.001);
  const contentH = Math.max(maxZ - minZ, 0.001);
  const scale = Math.min((1 - padding * 2) / contentW, (1 - padding * 2) / contentH);
  const scaledW = contentW * scale;
  const scaledH = contentH * scale;
  return {
    scale,
    offsetX: (1 - scaledW) * 0.5 - minX * scale,
    offsetZ: (1 - scaledH) * 0.5 - minZ * scale,
  };
}

function computeViewportFromExtents(
  minX: number,
  minZ: number,
  maxX: number,
  maxZ: number,
  padding: number,
): Viewport {
  return computeViewportFromNormalizedRect(minX, minZ, maxX, maxZ, padding);
}

export function mapPoint(vp: Viewport, x: number, z: number) {
  const nx = x * vp.scale + vp.offsetX;
  const ny = z * vp.scale + vp.offsetZ;
  return { x: nx * VIEW_SIZE, y: (1 - ny) * VIEW_SIZE };
}

export function mapRect(vp: Viewport, tile: MinimapTileDto) {
  const a = mapPoint(vp, tile.x, tile.z);
  const b = mapPoint(vp, tile.x + tile.w, tile.z + tile.h);
  return {
    x: Math.min(a.x, b.x),
    y: Math.min(a.y, b.y),
    width: Math.max(Math.abs(b.x - a.x), 4),
    height: Math.max(Math.abs(b.y - a.y), 4),
  };
}

export function mapDirection(vp: Viewport, x: number, z: number, dirX: number, dirZ: number) {
  const origin = mapPoint(vp, x, z);
  const tip = mapPoint(vp, x + dirX * 0.01, z + dirZ * 0.01);
  const len = Math.hypot(tip.x - origin.x, tip.y - origin.y) || 1;
  return { x: (tip.x - origin.x) / len, y: (tip.y - origin.y) / len };
}
