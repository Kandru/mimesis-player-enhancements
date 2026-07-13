import type { MinimapBoundsDto, MinimapMarkerDto } from '$lib/types';
import {
  computeViewportFromBounds,
  computeViewportFromNormalizedRect,
  computeViewportFromTiles,
  mapPoint,
  VIEW_SIZE,
  type SvgViewMetrics,
  type Viewport,
} from './minimapViewport';

export type MinimapNavigation = ReturnType<typeof createMinimapNavigation>;

export const DEFAULT_MINIMAP_ZOOM = 2.4;
export const MINIMAP_ZOOM_STEP = 0.2;

function viewportEquals(a: Viewport, b: Viewport) {
  return (
    Math.abs(a.scale - b.scale) < 1e-6
    && Math.abs(a.offsetX - b.offsetX) < 1e-6
    && Math.abs(a.offsetZ - b.offsetZ) < 1e-6
  );
}

export function createMinimapNavigation(options: {
  onChange?: () => void;
}) {
  let baseViewport: Viewport = { scale: 1, offsetX: 0, offsetZ: 0 };
  let panX = 0;
  let panY = 0;
  let zoom = DEFAULT_MINIMAP_ZOOM;
  let userOverride = false;
  let followSteamId: string | null = null;
  let activeAreaId = '';
  let activeFloorIndex = 0;
  let dragging = false;
  let dragStartX = 0;
  let dragStartY = 0;
  let dragPanX = 0;
  let dragPanY = 0;
  let viewMetrics: SvgViewMetrics = {
    pivotX: VIEW_SIZE * 0.5,
    pivotY: VIEW_SIZE * 0.5,
    pxToSvg: 1,
  };

  const notify = () => options.onChange?.();

  function setViewMetrics(metrics: SvgViewMetrics) {
    const changed =
      Math.abs(metrics.pivotX - viewMetrics.pivotX) > 0.25
      || Math.abs(metrics.pivotY - viewMetrics.pivotY) > 0.25
      || Math.abs(metrics.pxToSvg - viewMetrics.pxToSvg) > 0.001;
    if (!changed) {
      return false;
    }

    viewMetrics = metrics;
    notify();
    return true;
  }

  function resetTransform() {
    panX = 0;
    panY = 0;
    zoom = DEFAULT_MINIMAP_ZOOM;
    userOverride = false;
  }

  function setBaseViewport(vp: Viewport) {
    const viewportChanged = !viewportEquals(baseViewport, vp);
    if (!viewportChanged) {
      return false;
    }

    baseViewport = vp;
    if (!userOverride && !followSteamId) {
      panX = 0;
      panY = 0;
      zoom = DEFAULT_MINIMAP_ZOOM;
      userOverride = false;
    }
    notify();
    return true;
  }

  function fitToTiles(tiles: Parameters<typeof computeViewportFromTiles>[0], tight = false) {
    setBaseViewport(
      tiles.length
        ? computeViewportFromTiles(tiles, tight ? 0.04 : undefined)
        : computeViewportFromBounds(null),
    );
  }

  function fitToBounds(_bounds: MinimapBoundsDto | null | undefined, tight = false) {
    setBaseViewport(computeViewportFromNormalizedRect(0, 0, 1, 1, tight ? 0.04 : undefined));
  }

  function setFollow(steamId: string | null) {
    const next = steamId || null;
    if (followSteamId === next) {
      return;
    }

    followSteamId = next;
    if (followSteamId) {
      userOverride = false;
    }
    notify();
  }

  function setArea(areaId: string, floorIndex = 0) {
    activeAreaId = areaId;
    activeFloorIndex = floorIndex;
    resetTransform();
    notify();
  }

  function clearFollowFromInteraction() {
    let changed = false;
    if (followSteamId) {
      followSteamId = null;
      changed = true;
    }
    if (!userOverride) {
      userOverride = true;
      changed = true;
    }
    if (changed) {
      notify();
    }
  }

  function clampZoom(value: number) {
    return Math.min(8, Math.max(0.35, value));
  }

  function setZoom(nextZoom: number) {
    const clamped = clampZoom(nextZoom);
    if (Math.abs(clamped - zoom) < 1e-6) {
      return false;
    }
    zoom = clamped;
    notify();
    return true;
  }

  function handleWheel(event: WheelEvent) {
    event.preventDefault();
    if (!followSteamId) {
      clearFollowFromInteraction();
    }
    setZoom(zoom * (event.deltaY < 0 ? 1.12 : 0.89));
  }

  function handlePointerDown(event: PointerEvent) {
    if (event.button !== 0 || followSteamId) return;
    dragging = true;
    dragStartX = event.clientX;
    dragStartY = event.clientY;
    dragPanX = panX;
    dragPanY = panY;
    (event.currentTarget as Element)?.setPointerCapture?.(event.pointerId);
  }

  function handlePointerMove(event: PointerEvent) {
    if (!dragging || followSteamId) return;
    clearFollowFromInteraction();
    const dx = (event.clientX - dragStartX) * viewMetrics.pxToSvg;
    const dy = (event.clientY - dragStartY) * viewMetrics.pxToSvg;
    const nextPanX = dragPanX + dx;
    const nextPanY = dragPanY + dy;
    if (Math.abs(nextPanX - panX) < 0.5 && Math.abs(nextPanY - panY) < 0.5) {
      return;
    }
    panX = nextPanX;
    panY = nextPanY;
    notify();
  }

  function handlePointerUp(event: PointerEvent) {
    dragging = false;
    (event.currentTarget as Element)?.releasePointerCapture?.(event.pointerId);
  }

  function zoomBy(delta: number) {
    if (!followSteamId) {
      clearFollowFromInteraction();
    }
    setZoom(zoom + delta);
  }

  function tickFollow(marker: MinimapMarkerDto | null | undefined, force = false) {
    if (!followSteamId || !marker || userOverride) return;
    const pos = mapPoint(baseViewport, marker.x, marker.z);
    const cx = viewMetrics.pivotX;
    const cy = viewMetrics.pivotY;
    const nextPanX = zoom * (cx - pos.x);
    const nextPanY = zoom * (cy - pos.y);
    if (
      !force
      && Math.abs(nextPanX - panX) < 0.05
      && Math.abs(nextPanY - panY) < 0.05
    ) {
      return;
    }
    panX = nextPanX;
    panY = nextPanY;
    notify();
  }

  function recenterFollow(marker: MinimapMarkerDto | null | undefined) {
    tickFollow(marker, true);
  }

  function getTransform(): string {
    const cx = viewMetrics.pivotX;
    const cy = viewMetrics.pivotY;
    return `translate(${cx + panX}, ${cy + panY}) scale(${zoom}) translate(${-cx}, ${-cy})`;
  }

  return {
    get baseViewport() {
      return baseViewport;
    },
    get panX() {
      return panX;
    },
    get panY() {
      return panY;
    },
    get zoom() {
      return zoom;
    },
    get userOverride() {
      return userOverride;
    },
    get followSteamId() {
      return followSteamId;
    },
    get activeAreaId() {
      return activeAreaId;
    },
    get activeFloorIndex() {
      return activeFloorIndex;
    },
    get pivotX() {
      return viewMetrics.pivotX;
    },
    get pivotY() {
      return viewMetrics.pivotY;
    },
    setViewMetrics,
    setBaseViewport,
    fitToTiles,
    fitToBounds,
    setFollow,
    setArea,
    clearFollowFromInteraction,
    handleWheel,
    handlePointerDown,
    handlePointerMove,
    handlePointerUp,
    zoomBy,
    tickFollow,
    recenterFollow,
    getTransform,
    resetTransform,
  };
}
