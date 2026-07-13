import type { MinimapBoundsDto, MinimapMarkerDto } from '$lib/types';
import {
  computeViewportFromBounds,
  computeViewportFromNormalizedRect,
  computeViewportFromTiles,
  mapPoint,
  VIEW_SIZE,
  type Viewport,
} from './minimapViewport';

export type MinimapNavigation = ReturnType<typeof createMinimapNavigation>;

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
  let zoom = 1;
  let userOverride = false;
  let followSteamId: string | null = null;
  let activeAreaId = '';
  let activeFloorIndex = 0;
  let dragging = false;
  let dragStartX = 0;
  let dragStartY = 0;
  let dragPanX = 0;
  let dragPanY = 0;

  const notify = () => options.onChange?.();

  function resetTransform() {
    panX = 0;
    panY = 0;
    zoom = 1;
    userOverride = false;
  }

  function setBaseViewport(vp: Viewport) {
    const viewportChanged = !viewportEquals(baseViewport, vp);
    if (!viewportChanged) {
      return;
    }

    baseViewport = vp;
    if (!userOverride && !followSteamId) {
      panX = 0;
      panY = 0;
      zoom = 1;
      userOverride = false;
    }
    notify();
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

  function handleWheel(event: WheelEvent) {
    event.preventDefault();
    clearFollowFromInteraction();
    const nextZoom = Math.min(8, Math.max(0.35, zoom * (event.deltaY < 0 ? 1.12 : 0.89)));
    if (Math.abs(nextZoom - zoom) < 1e-6) {
      return;
    }
    zoom = nextZoom;
    notify();
  }

  function handlePointerDown(event: PointerEvent) {
    if (event.button !== 0) return;
    dragging = true;
    dragStartX = event.clientX;
    dragStartY = event.clientY;
    dragPanX = panX;
    dragPanY = panY;
    (event.currentTarget as Element)?.setPointerCapture?.(event.pointerId);
  }

  function handlePointerMove(event: PointerEvent) {
    if (!dragging) return;
    clearFollowFromInteraction();
    const nextPanX = dragPanX + (event.clientX - dragStartX);
    const nextPanY = dragPanY + (event.clientY - dragStartY);
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
    clearFollowFromInteraction();
    const nextZoom = Math.min(8, Math.max(0.35, zoom + delta));
    if (Math.abs(nextZoom - zoom) < 1e-6) {
      return;
    }
    zoom = nextZoom;
    notify();
  }

  function tickFollow(marker: MinimapMarkerDto | null | undefined) {
    if (!followSteamId || !marker || userOverride) return;
    const pos = mapPoint(baseViewport, marker.x, marker.z);
    const cx = VIEW_SIZE * 0.5;
    const cy = VIEW_SIZE * 0.5;
    const nextPanX = zoom * (cx - pos.x);
    const nextPanY = zoom * (cy - pos.y);
    if (Math.abs(nextPanX - panX) < 0.05 && Math.abs(nextPanY - panY) < 0.05) {
      return;
    }
    panX = nextPanX;
    panY = nextPanY;
    notify();
  }

  function getTransform(): string {
    const cx = VIEW_SIZE * 0.5;
    const cy = VIEW_SIZE * 0.5;
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
    getTransform,
    resetTransform,
  };
}
