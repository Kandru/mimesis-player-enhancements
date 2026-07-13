import type { MinimapBoundsDto, MinimapTileDto } from '$lib/types';

export const VIEW_SIZE = 1000;
export const VIEW_PADDING = 0.08;

export type Viewport = { scale: number; offsetX: number; offsetZ: number };

export type SvgViewMetrics = {
  pivotX: number;
  pivotY: number;
  pxToSvg: number;
};

const DEFAULT_SVG_METRICS: SvgViewMetrics = {
  pivotX: VIEW_SIZE / 2,
  pivotY: VIEW_SIZE / 2,
  pxToSvg: 1,
};

function getSvgRenderedViewport(rect: DOMRect, vbWidth: number, vbHeight: number) {
  const scale = Math.min(rect.width / vbWidth, rect.height / vbHeight);
  const renderedWidth = vbWidth * scale;
  const renderedHeight = vbHeight * scale;
  return {
    left: rect.left + (rect.width - renderedWidth) * 0.5,
    top: rect.top + (rect.height - renderedHeight) * 0.5,
    width: renderedWidth,
    height: renderedHeight,
  };
}

export function measureMinimapSvg(svg: SVGSVGElement | null | undefined): SvgViewMetrics {
  if (!svg) {
    return DEFAULT_SVG_METRICS;
  }

  const rect = svg.getBoundingClientRect();
  if (rect.width <= 0 || rect.height <= 0) {
    return DEFAULT_SVG_METRICS;
  }

  const viewBox = svg.viewBox.baseVal;
  const vbWidth = viewBox.width > 0 ? viewBox.width : VIEW_SIZE;
  const vbHeight = viewBox.height > 0 ? viewBox.height : VIEW_SIZE;
  const rendered = getSvgRenderedViewport(rect, vbWidth, vbHeight);

  const ctm = svg.getScreenCTM();
  if (!ctm) {
    return DEFAULT_SVG_METRICS;
  }

  const inverse = ctm.inverse();
  const point = svg.createSVGPoint();
  point.x = rendered.left + rendered.width * 0.5;
  point.y = rendered.top + rendered.height * 0.5;
  const pivot = point.matrixTransform(inverse);

  point.x = rendered.left + rendered.width * 0.5 + 1;
  point.y = rendered.top + rendered.height * 0.5;
  const next = point.matrixTransform(inverse);
  const pxToSvg = Math.hypot(next.x - pivot.x, next.y - pivot.y) || 1;

  return { pivotX: pivot.x, pivotY: pivot.y, pxToSvg };
}

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
  // Payload bounds are already normalized; markers-only / empty-tile views use the full 0–1 square.
  void bounds;
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
