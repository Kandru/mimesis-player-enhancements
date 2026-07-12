<script lang="ts">
  import MinimapView from '$lib/components/minimap/MinimapView.svelte';
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import { t } from '$lib/i18n';
  import { isValidSteamId } from '$lib/utils';

  const focusOptions = $derived(
    dashboard.players.filter((p) => p.playerUid && isValidSteamId(p.steamId)),
  );

  function setFocus(steamId: string) {
    dashboard.minimapFocusSteamId = steamId;
    localStorage.setItem('minimapFocusSteamId', steamId);
    dashboard.applyMinimapFilter(true);
  }

  function clearFocus() {
    dashboard.minimapFocusSteamId = '';
    localStorage.removeItem('minimapFocusSteamId');
    dashboard.applyMinimapFilter(true);
  }
</script>

<div class="minimap-page">
  <div class="card minimap-card">
    <div class="minimap-toolbar">
      <select
        class="input max-w-xs"
        value={dashboard.minimapFocusSteamId}
        onchange={(e) => {
          const v = (e.currentTarget as HTMLSelectElement).value;
          if (v) setFocus(v);
          else clearFocus();
        }}
      >
        <option value="">{t('dashboard.minimap_follow_local')}</option>
        {#each focusOptions as p (p.steamId)}
          <option value={String(p.steamId)}>{p.displayName}</option>
        {/each}
      </select>
    </div>
    <div class="minimap-viewport">
      <MinimapView data={dashboard.minimap} />
    </div>
  </div>
</div>
