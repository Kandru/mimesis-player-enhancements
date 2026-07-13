<script lang="ts">
  import type { MinimapPayload } from '$lib/types';
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import { t } from '$lib/i18n';
  import MinimapMarkerLayer from '$lib/components/minimap/MinimapMarkerLayer.svelte';
  import MinimapStaticLayer from '$lib/components/minimap/MinimapStaticLayer.svelte';
  import { createMinimapMarkerMotion } from '$lib/minimap/minimapMarkerMotion';
  import { buildLayoutGeometry } from '$lib/minimap/minimapLayoutGeometry';
  import {
    createMinimapNavigation,
    MINIMAP_ZOOM_STEP,
  } from '$lib/minimap/minimapNavigation';
  import { fingerprintMarkers } from '$lib/minimap/minimapThrottler';
  import {
    computeViewportFromBounds,
    computeViewportFromTiles,
    mapPoint,
    measureMinimapSvg,
    VIEW_SIZE,
  } from '$lib/minimap/minimapViewport';

  let { data }: { data: MinimapPayload | null } = $props();

  let viewportEl: HTMLDivElement | undefined = $state();
  let svgEl: SVGSVGElement | undefined = $state();
  let navVersion = $state(0);
  let motionVersion = $state(0);
  let rafActive = $state(false);

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
    `${data?.layoutVersion}:${data?.activeAreaId}:${data?.displayMode}:${data?.tiles?.length ?? 0}:${data?.activeFloorIndex ?? 0}:${data?.compositeIndoor ?? false}`,
  );

  const markerMotionKey = $derived(
    data?.markers?.length ? fingerprintMarkers(data.markers) : '',
  );

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

  const mapPivot = $derived.by(() => {
    void navVersion;
    return { x: navigation.pivotX, y: navigation.pivotY };
  });

  const navTransform = $derived.by(() => {
    void navVersion;
    return navigation.getTransform();
  });

  const layoutGeometry = $derived.by(() => {
    void layoutKey;
    if (!data || isHidden) return null;
    const tiles = data.tiles || [];
    const tight = isOpenMode || data.layoutKind === 'tram';
    const vp = tiles.length
      ? computeViewportFromTiles(tiles, tight ? 0.04 : undefined)
      : computeViewportFromBounds(data.bounds, tight ? 0.04 : undefined);
    return buildLayoutGeometry(data, vp, borderless);
  });

  function getFollowedMarkerRaw() {
    if (!followSteamId || !data?.markers) return null;
    return data.markers.find((marker) => String(marker.steamId) === followSteamId) ?? null;
  }

  function getFollowedDisplayMarker() {
    const raw = getFollowedMarkerRaw();
    if (!raw) return null;
    const pos = markerMotion.getDisplayPosition(raw.steamId);
    return pos ? { ...raw, x: pos.x, z: pos.z, yaw: pos.yaw } : raw;
  }

  function refreshSvgMetrics(recenterFollow = false) {
    if (!svgEl) return;
    const changed = navigation.setViewMetrics(measureMinimapSvg(svgEl));
    if (!recenterFollow || !changed || !navigation.followSteamId || !data) {
      return;
    }
    navigation.recenterFollow(getFollowedDisplayMarker());
  }

  function scheduleAnimationLoop() {
    rafActive = true;
  }

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
    if (markerMotionKey) {
      scheduleAnimationLoop();
    }
  });

  $effect(() => {
    if (isHidden || !data?.markers?.length) {
      markerMotion.reset();
      rafActive = false;
      return;
    }

    if (!rafActive) {
      return;
    }

    let frame = 0;
    const loop = (now: number) => {
      const animating = markerMotion.tick(now);
      const following = !!navigation.followSteamId;

      if (following) {
        navigation.tickFollow(getFollowedDisplayMarker());
      } else if (animating) {
        motionVersion += 1;
      }

      if (animating || following) {
        frame = requestAnimationFrame(loop);
        return;
      }

      rafActive = false;
    };

    frame = requestAnimationFrame(loop);
    return () => cancelAnimationFrame(frame);
  });

  $effect(() => {
    if (navigation.followSteamId || markerMotionKey) {
      scheduleAnimationLoop();
    }
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
    navigation.recenterFollow(getFollowedMarkerRaw());
    scheduleAnimationLoop();
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
</script>

{#if isHidden}
  <div class="minimap-view-root">
    <div class="flex flex-1 items-center justify-center text-sm text-gray-500">
      {t('dashboard.minimap_unavailable')}
    </div>
  </div>
{:else if layoutGeometry && data}
  <div class="minimap-view-root">
    <div class="minimap-viewport-controls">
      <button
        type="button"
        class="btn btn-secondary btn-sm"
        onclick={() => navigation.zoomBy(-MINIMAP_ZOOM_STEP)}
        aria-label={t('dashboard.minimap_zoom_out')}
      >−</button>
      <button
        type="button"
        class="btn btn-secondary btn-sm"
        onclick={() => navigation.zoomBy(MINIMAP_ZOOM_STEP)}
        aria-label={t('dashboard.minimap_zoom_in')}
      >+</button>
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
          <MinimapStaticLayer geometry={layoutGeometry} {borderless} />
          <MinimapMarkerLayer
            markers={data.markers}
            vp={layoutGeometry.vp}
            motion={markerMotion}
            {motionVersion}
            {followSteamId}
            blindMode={!!data.blindMode}
          />
        </g>
        {#if followSteamId}
          <MinimapMarkerLayer
            markers={data.markers}
            vp={layoutGeometry.vp}
            motion={markerMotion}
            {motionVersion}
            {followSteamId}
            blindMode={!!data.blindMode}
            {mapPivot}
            followedOnly
          />
        {/if}
      </svg>
    </div>
  </div>
{/if}
