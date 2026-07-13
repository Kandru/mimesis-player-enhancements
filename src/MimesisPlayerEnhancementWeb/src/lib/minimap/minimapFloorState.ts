import type { MinimapAreaDto, MinimapTileDto } from '$lib/types';

export type MinimapFloorState = 'active' | 'inactive' | 'connector';

export function isIndoorAreaId(areaId: string): boolean {
  return areaId === 'indoor' || areaId.startsWith('indoor-');
}

export function listIndoorAreas(areas: MinimapAreaDto[]): MinimapAreaDto[] {
  return areas.filter((area) => isIndoorAreaId(area.id));
}

export function resolveTileFloorState(
  tile: MinimapTileDto,
  activeFloorIndex: number,
): MinimapFloorState {
  if (tile.multiFloor || (tile.floorSpan?.length ?? 0) > 1) {
    return 'connector';
  }
  const floorIndex = tile.floorIndex ?? 0;
  if (floorIndex === activeFloorIndex || tile.floorSpan?.includes(activeFloorIndex)) {
    return 'active';
  }
  return 'inactive';
}

export function applyFloorStateToTiles(
  tiles: MinimapTileDto[],
  activeFloorIndex: number,
): MinimapTileDto[] {
  return tiles.map((tile) => ({
    ...tile,
    floorState: resolveTileFloorState(tile, activeFloorIndex),
  }));
}

export function mergeIndoorComposite(
  indoorAreas: MinimapAreaDto[],
  activeFloorIndex: number,
): { tiles: MinimapTileDto[]; connectionPoints: MinimapAreaDto['connectionPoints'] } {
  const tilesById = new Map<string, MinimapTileDto>();
  const connectionKeys = new Set<string>();
  const connectionPoints: MinimapAreaDto['connectionPoints'] = [];

  for (const area of indoorAreas) {
    for (const tile of area.tiles || []) {
      if (!tilesById.has(tile.id)) {
        tilesById.set(tile.id, tile);
      }
    }
    for (const point of area.connectionPoints || []) {
      const key = `${point.fromTileId}|${point.toTileId}|${point.x}|${point.crossFloor ? 'stairs' : 'door'}`;
      if (connectionKeys.has(key)) {
        continue;
      }
      connectionKeys.add(key);
      connectionPoints.push(point);
    }
  }

  const tiles = applyFloorStateToTiles([...tilesById.values()], activeFloorIndex);
  return { tiles, connectionPoints };
}

export function shouldUseIndoorComposite(
  layoutKind: string,
  areas: MinimapAreaDto[],
): boolean {
  return layoutKind === 'dungeon' && listIndoorAreas(areas).length >= 2;
}
