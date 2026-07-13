<script lang="ts">
  import { t } from '$lib/i18n';
  import type { MinimapLayoutGeometry } from '$lib/minimap/minimapLayoutGeometry';

  let {
    geometry,
    borderless = false,
  }: {
    geometry: MinimapLayoutGeometry;
    borderless?: boolean;
  } = $props();

  function tileClass(tile: MinimapLayoutGeometry['tiles'][number]) {
    let cls = `minimap-tile ${tile.isMainPath ? 'main-path' : 'branch'}`;
    if (tile.multiFloor) cls += ' multi-floor';
    if (tile.floorState === 'inactive') cls += ' inactive-floor';
    else if (tile.floorState === 'active') cls += ' active-floor';
    return cls;
  }

  function poiTooltip(kind: string, label?: string) {
    if (label) return label;
    const keys: Record<string, string> = {
      vending: 'dashboard.minimap_tooltip_vending',
      shower: 'dashboard.minimap_tooltip_shower',
      save: 'dashboard.minimap_tooltip_save',
      tram_start: 'dashboard.minimap_tooltip_tram_start',
    };
    const key = keys[kind];
    return key ? t(key) : kind;
  }
</script>

{#if !borderless}
  <g class="minimap-tiles">
    {#each geometry.tiles as tile (tile.id)}
      <rect
        x={tile.rect.x}
        y={tile.rect.y}
        width={tile.rect.width}
        height={tile.rect.height}
        rx="6"
        class={tileClass(tile)}
      >
        <title>{tile.label}{tile.multiFloor ? ` (${t('dashboard.minimap_multi_floor')})` : ''}</title>
      </rect>
      {#if tile.label && tile.rect.width > 36 && tile.rect.height > 18}
        <text
          x={tile.rect.x + tile.rect.width / 2}
          y={tile.rect.y + tile.rect.height / 2}
          class="minimap-tile-label"
        >
          {tile.label.length > 14 ? tile.label.slice(0, 12) + '…' : tile.label}
        </text>
      {/if}
    {/each}
  </g>
{/if}

<g class="minimap-connection-points">
  {#each geometry.connections as conn (conn.key)}
    {#if conn.kind === 'teleporter'}
      <g class="minimap-teleporter" transform="translate({conn.mid.x} {conn.mid.y})">
        <polygon points="0,-9 9,0 0,9 -9,0" />
        <title>{conn.title}</title>
      </g>
      {#if conn.dest}
        <line
          x1={conn.mid.x}
          y1={conn.mid.y}
          x2={conn.dest.x}
          y2={conn.dest.y}
          stroke="#f87171"
          stroke-dasharray="4 4"
          opacity="0.5"
        />
      {/if}
    {:else}
      <line
        x1={conn.line.x1}
        y1={conn.line.y1}
        x2={conn.line.x2}
        y2={conn.line.y2}
        stroke-width={conn.line.strokeWidth}
        class="minimap-connection{conn.kind === 'stairs' ? ' minimap-connection-stairs' : ''}{conn.inactive ? ' inactive-floor' : ''}"
      >
        <title>
          {conn.kind === 'stairs'
            ? `${t('dashboard.minimap_tooltip_stairs')} — ${conn.title}`
            : conn.title}
        </title>
      </line>
    {/if}
  {/each}
</g>

{#if geometry.train}
  {#if geometry.train.bounds}
    <rect
      x={geometry.train.bounds.x}
      y={geometry.train.bounds.y}
      width={geometry.train.bounds.width}
      height={geometry.train.bounds.height}
      rx="8"
      class="minimap-tram-bounds"
    />
  {/if}
  <g
    class="minimap-train"
    transform="translate({geometry.train.pos.x} {geometry.train.pos.y}) rotate({geometry.train.yaw + 180})"
  >
    <rect x="-14" y="-8" width="28" height="16" rx="3" fill="#94a3b8" />
    <polygon points="14,-6 22,0 14,6" fill="#94a3b8" />
    <title>{t('dashboard.minimap_tooltip_train')}</title>
  </g>
{/if}

<g class="minimap-pois">
  {#each geometry.pois as poi (poi.key)}
    <g class="minimap-poi minimap-poi-{poi.kind}" transform="translate({poi.x} {poi.y})">
      {#if poi.kind === 'vending'}
        <rect x="-6" y="-8" width="12" height="16" rx="2" />
        <rect x="-4" y="-5" width="8" height="5" rx="1" class="minimap-poi-screen" />
      {:else if poi.kind === 'shower'}
        <rect x="-5" y="-7" width="10" height="6" rx="1.5" />
        <line x1="-3" y1="0" x2="-3" y2="6" />
        <line x1="0" y1="0" x2="0" y2="7" />
        <line x1="3" y1="0" x2="3" y2="6" />
      {:else if poi.kind === 'save'}
        <rect x="-6" y="-6" width="12" height="12" rx="2" />
        <circle r="2.5" cy="-1" />
      {:else if poi.kind === 'tram_start'}
        <line x1="0" y1="-8" x2="0" y2="8" />
        <rect x="-7" y="-10" width="14" height="4" rx="1.5" />
      {:else}
        <circle r="6" />
      {/if}
      <title>{poiTooltip(poi.kind, poi.label)}</title>
    </g>
  {/each}
</g>
