import type { MinimapPayload, MinimapTileDto } from '$lib/types';
import { doorSegment, type DoorSegment } from './minimapDoors';
import { mapPoint, mapRect, VIEW_SIZE, type Viewport } from './minimapViewport';

export type TileGeometry = {
  id: string;
  label: string;
  rect: { x: number; y: number; width: number; height: number };
  isMainPath: boolean;
  multiFloor: boolean;
  floorState?: MinimapTileDto['floorState'];
};

export type TeleporterGeometry = {
  kind: 'teleporter';
  key: string;
  mid: { x: number; y: number };
  dest: { x: number; y: number } | null;
  title: string;
};

export type DoorConnectionGeometry = {
  kind: 'door' | 'stairs';
  key: string;
  line: DoorSegment;
  title: string;
  inactive: boolean;
};

export type PoiGeometry = {
  key: string;
  kind: string;
  x: number;
  y: number;
  label?: string;
};

export type TrainGeometry = {
  pos: { x: number; y: number };
  yaw: number;
  bounds: { x: number; y: number; width: number; height: number } | null;
};

export type MinimapLayoutGeometry = {
  vp: Viewport;
  tiles: TileGeometry[];
  connections: Array<TeleporterGeometry | DoorConnectionGeometry>;
  pois: PoiGeometry[];
  train: TrainGeometry | null;
};

function connectionKey(fromTileId: string, toTileId: string, x: number, crossFloor?: boolean) {
  return `${fromTileId}|${toTileId}|${x}|${crossFloor ? 'stairs' : 'door'}`;
}

function isConnectionInactive(
  point: { fromTileId: string; toTileId: string; crossFloor?: boolean },
  tileStates: Map<string, MinimapTileDto['floorState']>,
): boolean {
  if (point.crossFloor) {
    return false;
  }
  const fromState = tileStates.get(point.fromTileId);
  const toState = tileStates.get(point.toTileId);
  return fromState === 'inactive' && toState === 'inactive';
}

export function buildLayoutGeometry(
  data: MinimapPayload,
  vp: Viewport,
  borderless: boolean,
): MinimapLayoutGeometry {
  const tiles = data.tiles || [];
  const tileStates = new Map(tiles.map((tile) => [tile.id, tile.floorState]));

  const tileGeometry: TileGeometry[] = tiles.map((tile) => {
    const rect = mapRect(vp, tile);
    return {
      id: tile.id,
      label: tile.label,
      rect,
      isMainPath: tile.isMainPath,
      multiFloor: !!tile.multiFloor,
      floorState: tile.floorState,
    };
  });

  const connections: Array<TeleporterGeometry | DoorConnectionGeometry> = [];
  for (const point of data.connectionPoints || []) {
    const key = connectionKey(point.fromTileId, point.toTileId, point.x, point.crossFloor);
    if (point.crossArea) {
      const mid = mapPoint(vp, point.x, point.z);
      const dest =
        point.destX != null && point.destZ != null
          ? mapPoint(vp, point.destX, point.destZ)
          : null;
      connections.push({
        kind: 'teleporter',
        key,
        mid,
        dest,
        title: point.label || point.targetAreaId || '',
      });
      continue;
    }

    const line = doorSegment(vp, point);
    connections.push({
      kind: point.crossFloor ? 'stairs' : 'door',
      key,
      line,
      title: point.crossFloor
        ? `${point.fromTileId} ↔ ${point.toTileId}`
        : `${point.fromTileId} ↔ ${point.toTileId}`,
      inactive: isConnectionInactive(point, tileStates),
    });
  }

  const pois: PoiGeometry[] = (data.pointsOfInterest || []).map((poi) => {
    const pos = mapPoint(vp, poi.x, poi.z);
    return {
      key: `${poi.kind}:${poi.x}:${poi.z}`,
      kind: poi.kind,
      x: pos.x,
      y: pos.y,
      label: poi.label,
    };
  });

  let train: TrainGeometry | null = null;
  if (data.train) {
    const pos = mapPoint(vp, data.train.x, data.train.z);
    let bounds: TrainGeometry['bounds'] = null;
    if (data.train.spanX && data.train.spanZ) {
      const w = data.train.spanX * VIEW_SIZE;
      const h = data.train.spanZ * VIEW_SIZE;
      bounds = { x: pos.x - w / 2, y: pos.y - h / 2, width: w, height: h };
    }
    train = { pos, yaw: data.train.yaw, bounds: borderless ? bounds : null };
  }

  return { vp, tiles: tileGeometry, connections, pois, train };
}
