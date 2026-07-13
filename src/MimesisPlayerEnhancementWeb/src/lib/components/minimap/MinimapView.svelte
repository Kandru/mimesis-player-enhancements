<script lang="ts">
  import type { MinimapMarkerDto, MinimapPayload } from '$lib/types';
  import { t } from '$lib/i18n';
  import { doorSegment } from '$lib/minimap/minimapDoors';
  import { createMinimapMarkerMotion } from '$lib/minimap/minimapMarkerMotion';
  import {
    createMinimapNavigation,
    MINIMAP_ZOOM_STEP,
  } from '$lib/minimap/minimapNavigation';
  import { fingerprintMarkers } from '$lib/minimap/minimapThrottler';
  import {
    computeViewportFromBounds,
    computeViewportFromTiles,
    mapPoint,
    mapRect,
    measureMinimapSvg,
    VIEW_SIZE,
    type Viewport,
  } from '$lib/minimap/minimapViewport';
  import { dashboard } from '$lib/stores/dashboard.svelte';

  let { data }: { data: MinimapPayload | null } = $props();

  let viewportEl: HTMLDivElement | undefined = $state();
  let svgEl: SVGSVGElement | undefined = $state();
  let navVersion = $state(0);

  const markerMotion = createMinimapMarkerMotion();

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

  const liveMarkers = $derived.by(() => {
    void navVersion;
    void markerMotionKey;
    void layoutKey;
    const markers = data?.markers ?? [];
    return markerMotion.getDisplayMarkers(markers);
  });

  const liveTrain = $derived.by(() => {
    void layoutKey;
    return data?.train ?? null;
  });

  const followSteamId = $derived.by(() => {
    if (data?.blindMode) {
      const local = data.markers?.find((marker) => marker.isLocal);
      return local ? String(local.steamId) : '';
    }
    if (dashboard.minimapFocusSteamId) {
      return dashboard.minimapFocusSteamId;
    }
    const local = data?.markers?.find((marker) => marker.isLocal);
    return local ? String(local.steamId) : '';
  });

  let syncedFollowKey = '';

  const followedMarker = $derived(
    followSteamId
      ? liveMarkers.find((marker) => String(marker.steamId) === followSteamId) ?? null
      : null,
  );

  const mapPivot = $derived.by(() => {
    void navVersion;
    return { x: navigation.pivotX, y: navigation.pivotY };
  });
  const markerRadius = 7.2;
  const markerHeadingPoints = '0,12 4.8,4.8 -4.8,4.8';
  const markerLabelY = 9.6;

  function refreshSvgMetrics(recenterFollow = false) {
    if (!svgEl) return;
    const changed = navigation.setViewMetrics(measureMinimapSvg(svgEl));
    if (!recenterFollow || !changed || !navigation.followSteamId || !data) {
      return;
    }
    const focused = markerMotion.getDisplayMarkers(data.markers ?? []).find(
      (marker) => String(marker.steamId) === navigation.followSteamId,
    );
    navigation.recenterFollow(focused);
  }

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
    void layoutKey;
    markerMotion.setTargets(data?.markers ?? []);
  });

  $effect(() => {
    if (isHidden || !data?.markers?.length) {
      markerMotion.reset();
      return;
    }

    let frame = 0;
    const loop = (now: number) => {
      const animating = markerMotion.tick(now);

      if (navigation.followSteamId) {
        const focused = markerMotion.getDisplayMarkers(data?.markers ?? []).find(
          (marker) => String(marker.steamId) === navigation.followSteamId,
        );
        navigation.tickFollow(focused);
      } else if (animating) {
        navVersion += 1;
      }

      frame = requestAnimationFrame(loop);
    };

    frame = requestAnimationFrame(loop);
    return () => cancelAnimationFrame(frame);
  });

  $effect(() => {
    const nextFollow = followSteamId || null;
    const key = `${nextFollow ?? ''}:${layoutKey}`;

    if (!data) {
      syncedFollowKey = '';
      navigation.setFollow(null);
      return;
    }

    navigation.setFollow(nextFollow);

    if (!nextFollow) {
      syncedFollowKey = '';
      return;
    }

    if (syncedFollowKey === key) {
      return;
    }

    syncedFollowKey = key;
    refreshSvgMetrics();
    const focused = data.markers?.find(
      (marker) => String(marker.steamId) === nextFollow,
    );
    navigation.recenterFollow(focused);
  });

  $effect(() => {
    if (!svgEl) return;
    refreshSvgMetrics();
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
        if (!data || isHidden) return;
        refreshSvgMetrics(true);
        if (navigation.followSteamId) {
          return;
        }
        if (!navigation.userOverride) {
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
    if (followSteamId) return;
    navigation.handlePointerDown(event);
    if (data?.blindMode || !dashboard.canFollowMinimapPlayers) return;
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
      <button type="button" class="btn btn-secondary btn-sm" onclick={() => navigation.zoomBy(-MINIMAP_ZOOM_STEP)} aria-label={t('dashboard.minimap_zoom_out')}>−</button>
      <button type="button" class="btn btn-secondary btn-sm" onclick={() => navigation.zoomBy(MINIMAP_ZOOM_STEP)} aria-label={t('dashboard.minimap_zoom_in')}>+</button>
    </div>
    <div
      class="minimap-viewport-inner{followSteamId ? ' minimap-viewport-following' : ''}"
      role="application"
      aria-label={t('dashboard.nav_minimap')}
      bind:this={viewportEl}
      onwheel={onWheel}
      onpointerdown={onPointerDown}
      onpointermove={navigation.handlePointerMove}
      onpointerup={navigation.handlePointerUp}
      onpointercancel={navigation.handlePointerUp}
    >
      <svg
        class="minimap-svg"
        viewBox="0 0 {VIEW_SIZE} {VIEW_SIZE}"
        preserveAspectRatio="xMidYMid meet"
        bind:this={svgEl}
      >
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
                <title>
                  {point.label
                    || (point.teleporterId
                      ? t('dashboard.minimap_tooltip_teleporter', { target: point.targetAreaId || '?' })
                      : t('dashboard.minimap_tooltip_area', { target: point.targetAreaId || '?' }))}
                </title>
              </g>
              {#if point.destX != null && point.destZ != null}
                {@const dest = mapPoint(rendered.vp, point.destX, point.destZ)}
                <line x1={mid.x} y1={mid.y} x2={dest.x} y2={dest.y} stroke="#f87171" stroke-dasharray="4 4" opacity="0.5" />
              {/if}
            {:else if point.crossFloor}
              {@const line = doorSegment(rendered.vp, point)}
              <line
                x1={line.x1}
                y1={line.y1}
                x2={line.x2}
                y2={line.y2}
                stroke-width={line.strokeWidth}
                class="minimap-connection minimap-connection-stairs"
              >
                <title>{t('dashboard.minimap_tooltip_stairs')} — {point.fromTileId} ↔ {point.toTileId}</title>
              </line>
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

        <g class="minimap-pois">
          {#each data.pointsOfInterest || [] as poi (`${poi.kind}:${poi.x}:${poi.z}`)}
            {@const pos = mapPoint(rendered.vp, poi.x, poi.z)}
            <g class="minimap-poi minimap-poi-{poi.kind}" transform="translate({pos.x} {pos.y})">
              {#if poi.kind === 'vending'}
                <rect x="-6" y="-8" width="12" height="16" rx="2" />
                <rect x="-4" y="-5" width="8" height="5" rx="1" class="minimap-poi-screen" />
                <title>{poi.label || t('dashboard.minimap_tooltip_vending')}</title>
              {:else if poi.kind === 'shower'}
                <rect x="-5" y="-7" width="10" height="6" rx="1.5" />
                <line x1="-3" y1="0" x2="-3" y2="6" />
                <line x1="0" y1="0" x2="0" y2="7" />
                <line x1="3" y1="0" x2="3" y2="6" />
                <title>{poi.label || t('dashboard.minimap_tooltip_shower')}</title>
              {:else if poi.kind === 'save'}
                <rect x="-6" y="-6" width="12" height="12" rx="2" />
                <circle r="2.5" cy="-1" />
                <title>{poi.label || t('dashboard.minimap_tooltip_save')}</title>
              {:else if poi.kind === 'tram_start'}
                <line x1="0" y1="-8" x2="0" y2="8" />
                <rect x="-7" y="-10" width="14" height="4" rx="1.5" />
                <title>{poi.label || t('dashboard.minimap_tooltip_tram_start')}</title>
              {:else}
                <circle r="6" />
                <title>{poi.label || poi.kind}</title>
              {/if}
            </g>
          {/each}
        </g>

        <g class="minimap-markers">
          {#each liveMarkers as marker (marker.steamId)}
            {#if String(marker.steamId) !== followSteamId}
              {@const pos = mapPoint(rendered.vp, marker.x, marker.z)}
              <g class={markerClass(marker, !!data.blindMode)} transform="translate({pos.x} {pos.y})">
                <g transform="rotate({marker.yaw + 180})">
                  <circle r={markerRadius} />
                  <polygon class="minimap-heading" points={markerHeadingPoints} />
                </g>
                <text y={markerLabelY} class="minimap-marker-label">{(marker.displayName || '').slice(0, 10)}</text>
              </g>
            {/if}
          {/each}
        </g>
      </g>
      {#if followedMarker}
        <g
          class="{markerClass(followedMarker, !!data.blindMode)} minimap-marker-followed"
          transform="translate({mapPivot.x} {mapPivot.y})"
        >
          <g transform="rotate({followedMarker.yaw + 180})">
            <circle r={markerRadius} />
            <polygon class="minimap-heading" points={markerHeadingPoints} />
          </g>
          <text y={markerLabelY} class="minimap-marker-label">{(followedMarker.displayName || '').slice(0, 10)}</text>
        </g>
      {/if}
    </svg>
    </div>
  </div>
{/if}
