<script lang="ts">
  import type { MinimapMarkerDto, MinimapPayload } from '$lib/types';
  import { t } from '$lib/i18n';
  import { doorSegment } from '$lib/minimap/minimapDoors';
  import { createMinimapNavigation } from '$lib/minimap/minimapNavigation';
  import { fingerprintMarkers } from '$lib/minimap/minimapThrottler';
  import {
    computeViewportFromBounds,
    computeViewportFromTiles,
    mapPoint,
    mapRect,
    VIEW_SIZE,
    type Viewport,
  } from '$lib/minimap/minimapViewport';
  import { dashboard } from '$lib/stores/dashboard.svelte';

  let { data }: { data: MinimapPayload | null } = $props();

  let viewportEl: HTMLDivElement | undefined = $state();
  let navVersion = $state(0);
  let liveMarkers = $state<MinimapMarkerDto[]>([]);
  let liveTrain = $state<MinimapPayload['train']>(null);

  const navigation = createMinimapNavigation({
    onChange: () => {
      navVersion += 1;
    },
  });

  const isOpenMode = $derived(data?.displayMode === 'open');
  const isHidden = $derived(!data || data.displayMode === 'hidden');
  const borderless = $derived(
    isOpenMode || (data?.areas?.find((a) => a.id === data?.activeAreaId)?.borderless ?? false),
  );

  const layoutKey = $derived(
    `${data?.layoutVersion}:${data?.activeAreaId}:${data?.displayMode}:${data?.tiles?.length ?? 0}`,
  );

  const markerMotionKey = $derived(
    data?.markers?.length ? fingerprintMarkers(data.markers) : '',
  );

  const followSteamId = $derived(
    dashboard.minimapFocusSteamId && dashboard.canFollowMinimapPlayers
      ? dashboard.minimapFocusSteamId
      : '',
  );

  const followedMarker = $derived(
    followSteamId
      ? liveMarkers.find((marker) => String(marker.steamId) === followSteamId) ?? null
      : null,
  );

  const mapCenter = VIEW_SIZE * 0.5;

  const staticLayer = $derived.by(() => {
    void navVersion;
    void layoutKey;
    if (!data || isHidden) return null;
    const tiles = data.tiles || [];
    return {
      vp: navigation.baseViewport,
      tiles,
      tilesById: new Map(tiles.map((tile) => [tile.id, tile])),
      cps: data.connectionPoints || [],
    };
  });

  $effect(() => {
    void layoutKey;
    if (!data || isHidden) return;
    const tiles = data.tiles || [];
    const tight = isOpenMode || data.layoutKind === 'tram';
    const vp = tiles.length
      ? computeViewportFromTiles(tiles, tight ? 0.04 : undefined)
      : computeViewportFromBounds(data.bounds, tight ? 0.04 : undefined);
    navigation.setBaseViewport(vp);
  });

  $effect(() => {
    void markerMotionKey;
    void followSteamId;
    void layoutKey;
    if (!data) {
      liveMarkers = [];
      liveTrain = null;
      navigation.setFollow(null);
      return;
    }

    liveMarkers = data.markers || [];
    liveTrain = data.train ?? null;
    navigation.setFollow(followSteamId || null);

    if (!followSteamId) {
      return;
    }

    const focused = liveMarkers.find((marker) => String(marker.steamId) === followSteamId);
    navigation.tickFollow(focused);
  });

  $effect(() => {
    if (!viewportEl) return;

    let lastWidth = 0;
    let lastHeight = 0;
    let timer: ReturnType<typeof setTimeout> | null = null;

    const observer = new ResizeObserver((entries) => {
      const rect = entries[0]?.contentRect;
      if (!rect) return;
      if (Math.abs(rect.width - lastWidth) < 1 && Math.abs(rect.height - lastHeight) < 1) {
        return;
      }
      lastWidth = rect.width;
      lastHeight = rect.height;

      if (timer) clearTimeout(timer);
      timer = setTimeout(() => {
        if (!navigation.userOverride && !navigation.followSteamId && data && !isHidden) {
          const tiles = data.tiles || [];
          const tight = isOpenMode || data.layoutKind === 'tram';
          if (tiles.length) navigation.fitToTiles(tiles, tight);
          else navigation.fitToBounds(data.bounds, tight);
        }
      }, 150);
    });

    observer.observe(viewportEl);
    return () => {
      if (timer) clearTimeout(timer);
      observer.disconnect();
    };
  });

  function markerClass(m: MinimapMarkerDto, blind: boolean) {
    let cls = 'minimap-marker';
    if (!blind && !m.isAlive) cls += ' dead';
    if (m.isLocal) cls += ' local';
    if (m.isHost) cls += ' host';
    return cls;
  }

  function onWheel(event: WheelEvent) {
    navigation.handleWheel(event);
  }

  function onPointerDown(event: PointerEvent) {
    navigation.handlePointerDown(event);
    if (dashboard.minimapFocusSteamId) {
      dashboard.setMinimapFollow('');
    }
  }

  function trainBoundsRect(vp: Viewport) {
    if (!liveTrain?.spanX || !liveTrain?.spanZ) return null;
    const center = mapPoint(vp, liveTrain.x, liveTrain.z);
    const w = liveTrain.spanX * VIEW_SIZE;
    const h = liveTrain.spanZ * VIEW_SIZE;
    return { x: center.x - w / 2, y: center.y - h / 2, width: w, height: h };
  }
</script>

{#if isHidden}
  <div class="minimap-view-root">
    <div class="flex flex-1 items-center justify-center text-sm text-gray-500">{t('dashboard.minimap_unavailable')}</div>
  </div>
{:else if staticLayer && data}
  {@const rendered = staticLayer}
  {@const navTransform = (void navVersion, navigation.getTransform())}
  <div class="minimap-view-root">
    <div class="minimap-viewport-controls">
      <button type="button" class="btn btn-secondary btn-sm" onclick={() => navigation.zoomBy(-0.2)} aria-label={t('dashboard.minimap_zoom_out')}>−</button>
      <button type="button" class="btn btn-secondary btn-sm" onclick={() => navigation.zoomBy(0.2)} aria-label={t('dashboard.minimap_zoom_in')}>+</button>
    </div>
    <div
      class="minimap-viewport-inner"
      role="application"
      aria-label={t('dashboard.nav_minimap')}
      bind:this={viewportEl}
      onwheel={onWheel}
      onpointerdown={onPointerDown}
      onpointermove={navigation.handlePointerMove}
      onpointerup={navigation.handlePointerUp}
      onpointercancel={navigation.handlePointerUp}
    >
      <svg class="minimap-svg" viewBox="0 0 {VIEW_SIZE} {VIEW_SIZE}" preserveAspectRatio="xMidYMid meet">
      <g transform={navTransform}>
        {#if !borderless}
          <g class="minimap-tiles">
            {#each rendered.tiles as tile (tile.id)}
              {@const rect = mapRect(rendered.vp, tile)}
              <rect
                x={rect.x}
                y={rect.y}
                width={rect.width}
                height={rect.height}
                rx="6"
                class="minimap-tile {tile.isMainPath ? 'main-path' : 'branch'}{tile.multiFloor ? ' multi-floor' : ''}"
              >
                <title>{tile.label}{tile.multiFloor ? ` (${t('dashboard.minimap_multi_floor')})` : ''}</title>
              </rect>
              {#if tile.label && rect.width > 36 && rect.height > 18}
                <text x={rect.x + rect.width / 2} y={rect.y + rect.height / 2} class="minimap-tile-label">
                  {tile.label.length > 14 ? tile.label.slice(0, 12) + '…' : tile.label}
                </text>
              {/if}
            {/each}
          </g>
        {/if}

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
              {@const line = doorSegment(rendered.vp, point)}
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

        {#if liveTrain}
          {@const pos = mapPoint(rendered.vp, liveTrain.x, liveTrain.z)}
          {@const tramBox = trainBoundsRect(rendered.vp)}
          {#if tramBox && borderless}
            <rect x={tramBox.x} y={tramBox.y} width={tramBox.width} height={tramBox.height} rx="8" class="minimap-tram-bounds" />
          {/if}
          <g class="minimap-train" transform="translate({pos.x} {pos.y}) rotate({liveTrain.yaw + 180})">
            <rect x="-14" y="-8" width="28" height="16" rx="3" fill="#94a3b8" />
            <polygon points="14,-6 22,0 14,6" fill="#94a3b8" />
            <title>{t('dashboard.minimap_tooltip_train')}</title>
          </g>
        {/if}

        <g class="minimap-markers">
          {#each liveMarkers as marker (marker.steamId)}
            {#if String(marker.steamId) !== followSteamId}
              {@const pos = mapPoint(rendered.vp, marker.x, marker.z)}
              <g class={markerClass(marker, !!data.blindMode)} transform="translate({pos.x} {pos.y})">
                <g transform="rotate({marker.yaw + 180})">
                  <circle r="10" />
                  <polygon class="minimap-heading" points="0,16 6,6 -6,6" />
                </g>
                <text y="24" class="minimap-marker-label">{(marker.displayName || '').slice(0, 10)}</text>
              </g>
            {/if}
          {/each}
        </g>
      </g>
      {#if followedMarker}
        <g
          class="{markerClass(followedMarker, !!data.blindMode)} minimap-marker-followed"
          transform="translate({mapCenter} {mapCenter})"
        >
          <g transform="rotate({followedMarker.yaw + 180})">
            <circle r="10" />
            <polygon class="minimap-heading" points="0,16 6,6 -6,6" />
          </g>
          <text y="24" class="minimap-marker-label">{(followedMarker.displayName || '').slice(0, 10)}</text>
        </g>
      {/if}
    </svg>
    </div>
  </div>
{/if}
