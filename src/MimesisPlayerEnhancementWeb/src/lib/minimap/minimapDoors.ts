import type { MinimapConnectionPointDto } from '$lib/types';
import { mapDirection, mapPoint, VIEW_SIZE, type Viewport } from './minimapViewport';

export type DoorSegment = {
  x1: number;
  y1: number;
  x2: number;
  y2: number;
  strokeWidth: number;
};

export function doorSegment(vp: Viewport, point: MinimapConnectionPointDto): DoorSegment {
  const mid = mapPoint(vp, point.x, point.z);
  const dir = mapDirection(vp, point.x, point.z, point.dirX, point.dirZ);
  const half = (point.width ?? 0.04) * VIEW_SIZE * 0.5;
  return {
    x1: mid.x - dir.x * half,
    y1: mid.y - dir.y * half,
    x2: mid.x + dir.x * half,
    y2: mid.y + dir.y * half,
    strokeWidth: 5,
  };
}
