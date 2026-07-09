<script lang="ts">
  import MinimapView from '$lib/components/minimap/MinimapView.svelte';
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import { t } from '$lib/i18n';
  import { isValidSteamId } from '$lib/utils';

  const focusOptions = $derived(
    dashboard.players.filter((p) => isValidSteamId(p.steamId)),
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

<div class="card p-4">
  <div class="mb-4 flex flex-wrap items-center justify-end gap-3">
    {#if dashboard.status.isHost}
      <label class="flex items-center gap-2 text-sm">
        <input type="checkbox" bind:checked={dashboard.minimapShowAll} onchange={() => dashboard.applyMinimapFilter(true)} />
        {t('dashboard.minimap_show_all')}
      </label>
    {/if}
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
      {#each focusOptions as p}
        <option value={String(p.steamId)}>{p.displayName || p.steamId}</option>
      {/each}
    </select>
  </div>
  <div class="h-[min(70vh,720px)]">
    <MinimapView data={dashboard.minimap} />
  </div>
</div>
