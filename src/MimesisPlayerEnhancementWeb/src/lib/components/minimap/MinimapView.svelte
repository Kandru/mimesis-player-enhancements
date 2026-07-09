<script lang="ts">
  import type { MinimapConnectionPointDto, MinimapMarkerDto, MinimapPayload, MinimapTileDto } from '$lib/types';
  import { t } from '$lib/i18n';

  const VIEW = 1000;
  const PADDING = 0.08;

  let { data }: { data: MinimapPayload | null } = $props();

  type Viewport = { scale: number; offsetX: number; offsetZ: number };

  function computeViewport(tiles: MinimapTileDto[]): Viewport {
    if (!tiles.length) return { scale: 1, offsetX: 0, offsetZ: 0 };
    let minX = Infinity, minZ = Infinity, maxX = -Infinity, maxZ = -Infinity;
    for (const tile of tiles) {
      minX = Math.min(minX, tile.x);
      minZ = Math.min(minZ, tile.z);
      maxX = Math.max(maxX, tile.x + tile.w);
      maxZ = Math.max(maxZ, tile.z + tile.h);
    }
    const contentW = Math.max(maxX - minX, 0.001);
    const contentH = Math.max(maxZ - minZ, 0.001);
    const scale = Math.min((1 - PADDING * 2) / contentW, (1 - PADDING * 2) / contentH);
    const scaledW = contentW * scale;
    const scaledH = contentH * scale;
    return {
      scale,
      offsetX: (1 - scaledW) * 0.5 - minX * scale,
      offsetZ: (1 - scaledH) * 0.5 - minZ * scale,
    };
  }

  function mapPoint(vp: Viewport, x: number, z: number) {
    const nx = x * vp.scale + vp.offsetX;
    const ny = z * vp.scale + vp.offsetZ;
    return { x: nx * VIEW, y: (1 - ny) * VIEW };
  }

  function mapRect(vp: Viewport, tile: MinimapTileDto) {
    const a = mapPoint(vp, tile.x, tile.z);
    const b = mapPoint(vp, tile.x + tile.w, tile.z + tile.h);
    return {
      x: Math.min(a.x, b.x),
      y: Math.min(a.y, b.y),
      width: Math.max(Math.abs(b.x - a.x), 4),
      height: Math.max(Math.abs(b.y - a.y), 4),
    };
  }

  function doorLine(vp: Viewport, point: MinimapConnectionPointDto, tilesById: Map<string, MinimapTileDto>) {
    const mid = mapPoint(vp, point.x, point.z);
    let dirX = point.dirX;
    let dirY = -point.dirZ;
    const from = tilesById.get(point.fromTileId);
    const to = tilesById.get(point.toTileId);
    if (from && to) {
      const fc = mapPoint(vp, from.x + from.w / 2, from.z + from.h / 2);
      const tc = mapPoint(vp, to.x + to.w / 2, to.z + to.h / 2);
      const len = Math.hypot(tc.x - fc.x, tc.y - fc.y) || 1;
      dirX = (tc.x - fc.x) / len;
      dirY = (tc.y - fc.y) / len;
    } else if (point.dirX != null && point.dirZ != null) {
      const d = mapPoint(vp, point.x + point.dirX * 0.01, point.z + point.dirZ * 0.01);
      const len = Math.hypot(d.x - mid.x, d.y - mid.y) || 1;
      dirX = (d.x - mid.x) / len;
      dirY = (d.y - mid.y) / len;
    }
    const half = (point.width ?? 0.04) * VIEW * 0.5 + 12;
    return {
      x1: mid.x - dirX * half,
      y1: mid.y - dirY * half,
      x2: mid.x + dirX * half,
      y2: mid.y + dirY * half,
      strokeWidth: Math.max(4, (point.width ?? 0.02) * VIEW * 0.8),
    };
  }

  function markerClass(m: MinimapMarkerDto, blind: boolean) {
    let cls = 'minimap-marker';
    if (!blind && !m.isAlive) cls += ' dead';
    if (m.isLocal) cls += ' local';
    if (m.isHost) cls += ' host';
    return cls;
  }

  const rendered = $derived.by(() => {
    if (!data || data.layout.displayMode === 'hidden') return null;
    const tiles = data.layout.tiles || [];
    const vp = computeViewport(tiles);
    const tilesById = new Map(tiles.map((t) => [t.id, t]));
    const cps = (data as MinimapPayload & { connectionPoints?: MinimapConnectionPointDto[] }).connectionPoints || [];
    return { tiles, vp, tilesById, cps, markers: data.markers || [], train: data.train, blind: !!data.blindMode };
  });
</script>

{#if !rendered}
  <div class="flex h-full items-center justify-center text-sm text-gray-500">{t('dashboard.minimap_unavailable')}</div>
{:else}
  <svg class="minimap-svg" viewBox="0 0 {VIEW} {VIEW}" preserveAspectRatio="xMidYMid meet">
    <g class="minimap-tiles">
      {#each rendered.tiles as tile (tile.id)}
        {@const rect = mapRect(rendered.vp, tile)}
        <rect
          x={rect.x}
          y={rect.y}
          width={rect.width}
          height={rect.height}
          rx="6"
          class="minimap-tile {tile.isMainPath ? 'main-path' : 'branch'}"
        >
          <title>{tile.label}</title>
        </rect>
        {#if tile.label && rect.width > 36 && rect.height > 18}
          <text
            x={rect.x + rect.width / 2}
            y={rect.y + rect.height / 2}
            class="minimap-tile-label"
          >{tile.label.length > 14 ? tile.label.slice(0, 12) + '…' : tile.label}</text>
        {/if}
      {/each}
    </g>

    <g class="minimap-connection-points">
      {#each rendered.cps as point (point.fromTileId + point.toTileId + point.x)}
        {#if point.crossArea}
          {@const mid = mapPoint(rendered.vp, point.x, point.z)}
          <g class="minimap-teleporter" transform="translate({mid.x} {mid.y})">
            <polygon points="0,-9 9,0 0,9 -9,0" />
            <title>{t('dashboard.minimap_tooltip_teleporter', { target: point.targetAreaId || '?' })}</title>
          </g>
          {#if point.destX != null && point.destZ != null}
            {@const dest = mapPoint(rendered.vp, point.destX, point.destZ)}
            <line x1={mid.x} y1={mid.y} x2={dest.x} y2={dest.y} stroke="#f87171" stroke-dasharray="4 4" opacity="0.5" />
          {/if}
        {:else if point.crossFloor}
          {@const mid = mapPoint(rendered.vp, point.x, point.z)}
          <g class="minimap-stair" transform="translate({mid.x} {mid.y})">
            <circle r="7" />
            <title>{t('dashboard.minimap_tooltip_stairs')}</title>
          </g>
        {:else}
          {@const line = doorLine(rendered.vp, point, rendered.tilesById)}
          <line
            x1={line.x1}
            y1={line.y1}
            x2={line.x2}
            y2={line.y2}
            stroke-width={line.strokeWidth}
            class="minimap-connection"
          >
            <title>{point.fromTileId} ↔ {point.toTileId}</title>
          </line>
        {/if}
      {/each}
    </g>

    {#if rendered.train}
      {@const pos = mapPoint(rendered.vp, rendered.train.x, rendered.train.z)}
      <g class="minimap-train" transform="translate({pos.x} {pos.y}) rotate({rendered.train.yaw + 180})">
        <rect x="-14" y="-8" width="28" height="16" rx="3" fill="#94a3b8" />
        <polygon points="14,-6 22,0 14,6" fill="#94a3b8" />
      </g>
    {/if}

    <g class="minimap-markers">
      {#each rendered.markers as marker (marker.steamId)}
        {@const pos = mapPoint(rendered.vp, marker.x, marker.z)}
        <g class={markerClass(marker, rendered.blind)} transform="translate({pos.x} {pos.y})">
          <g transform="rotate({marker.yaw + 180})">
            <circle r="10" />
            <polygon class="minimap-heading" points="0,16 6,6 -6,6" />
          </g>
          <text y="24" class="minimap-marker-label">{(marker.displayName || '').slice(0, 10)}</text>
        </g>
      {/each}
    </g>
  </svg>
{/if}
