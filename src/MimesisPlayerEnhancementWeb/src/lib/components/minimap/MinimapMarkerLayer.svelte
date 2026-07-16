<script lang="ts">
  import type { MinimapMarkerDto } from '$lib/types';
  import type { MinimapMarkerMotion } from '$lib/minimap/minimapMarkerMotion';
  import { mapPoint, type Viewport } from '$lib/minimap/minimapViewport';

  let {
    markers,
    vp,
    motion,
    motionVersion = 0,
    followSteamId = '',
    blindMode = false,
    mapPivot,
    followedOnly = false,
    followedScale = 1,
  }: {
    markers: MinimapMarkerDto[];
    vp: Viewport;
    motion: MinimapMarkerMotion;
    motionVersion?: number;
    followSteamId?: string;
    blindMode?: boolean;
    mapPivot?: { x: number; y: number };
    followedOnly?: boolean;
    followedScale?: number;
  } = $props();

  const markerRadius = 7.2;
  const markerHeadingPoints = '0,12 4.8,4.8 -4.8,4.8';
  const markerLabelY = 9.6;

  function markerClass(m: MinimapMarkerDto) {
    let cls = 'minimap-marker';
    if (!blindMode && !m.isAlive) cls += ' dead';
    if (m.isLocal) cls += ' local';
    if (m.isHost) cls += ' host';
    return cls;
  }

  function displayPos(marker: MinimapMarkerDto) {
    void motionVersion;
    const motionPos = motion.getDisplayPosition(marker.steamId);
    const x = motionPos?.x ?? marker.x;
    const z = motionPos?.z ?? marker.z;
    return mapPoint(vp, x, z);
  }

  function displayYaw(marker: MinimapMarkerDto) {
    void motionVersion;
    return motion.getDisplayPosition(marker.steamId)?.yaw ?? marker.yaw;
  }

  const followedMarker = $derived(
    followSteamId
      ? markers.find((marker) => String(marker.steamId) === followSteamId) ?? null
      : null,
  );
</script>

{#if followedOnly}
  {#if followedMarker && mapPivot}
    {@const yaw = displayYaw(followedMarker)}
    <g
      class="{markerClass(followedMarker)} minimap-marker-followed"
      transform="translate({mapPivot.x} {mapPivot.y}) scale({followedScale})"
    >
      <g transform="rotate({yaw + 180})">
        <circle r={markerRadius} />
        <polygon class="minimap-heading" points={markerHeadingPoints} />
      </g>
      <text y={markerLabelY} class="minimap-marker-label">
        {(followedMarker.displayName || '').slice(0, 10)}
      </text>
    </g>
  {/if}
{:else}
  <g class="minimap-markers">
    {#each markers as marker (marker.steamId)}
      {#if String(marker.steamId) !== followSteamId}
        {@const pos = displayPos(marker)}
        {@const yaw = displayYaw(marker)}
        <g class={markerClass(marker)} transform="translate({pos.x} {pos.y})">
          <g transform="rotate({yaw + 180})">
            <circle r={markerRadius} />
            <polygon class="minimap-heading" points={markerHeadingPoints} />
          </g>
          <text y={markerLabelY} class="minimap-marker-label">
            {(marker.displayName || '').slice(0, 10)}
          </text>
        </g>
      {/if}
    {/each}
  </g>
{/if}
