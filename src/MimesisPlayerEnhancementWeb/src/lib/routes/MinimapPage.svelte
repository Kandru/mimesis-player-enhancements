<script lang="ts">
  import MinimapView from '$lib/components/minimap/MinimapView.svelte';
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import { t } from '$lib/i18n';
  import { isValidSteamId } from '$lib/utils';

  const focusOptions = $derived(
    dashboard.players.filter((p) => p.playerUid && isValidSteamId(p.steamId)),
  );

  const areaOptions = $derived(dashboard.minimapRaw?.areas || dashboard.minimap?.areas || []);

  function toggleFocus(steamId: string) {
    const id = String(steamId);
    if (dashboard.minimapFocusSteamId === id) {
      dashboard.setMinimapFollow('');
    } else {
      dashboard.setMinimapFollow(id);
    }
  }

  function onAreaChange(event: Event) {
    const areaId = (event.currentTarget as HTMLSelectElement).value;
    dashboard.setMinimapArea(areaId);
  }
</script>

<div class="minimap-page">
  <div class="card minimap-card">
    <div class="minimap-toolbar">
      <label class="minimap-toolbar-label">
        <span>{t('dashboard.minimap_area')}</span>
        <select class="input max-w-xs" value={dashboard.minimapAreaId} onchange={onAreaChange}>
          {#each areaOptions as area (area.id)}
            <option value={area.id}>{area.label}</option>
          {/each}
        </select>
      </label>
    </div>

    {#if dashboard.canFollowMinimapPlayers}
      <div class="minimap-player-chips">
        {#each focusOptions as p (p.steamId)}
          <button
            type="button"
            class="minimap-player-chip {dashboard.minimapFocusSteamId === String(p.steamId) ? 'active' : ''}"
            onclick={() => toggleFocus(String(p.steamId))}
          >
            {p.displayName}
          </button>
        {/each}
      </div>
    {/if}

    <div class="minimap-viewport">
      <MinimapView data={dashboard.minimap} />
    </div>
  </div>
</div>
