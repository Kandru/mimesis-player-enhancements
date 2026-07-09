<script lang="ts">
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import { t } from '$lib/i18n';
  import { navigate } from '$lib/utils';

  const links = $derived([
    { route: 'players', label: t('dashboard.nav_players'), connected: true },
    { route: 'minimap', label: t('dashboard.nav_minimap'), connected: true },
    { route: 'leaderboard', label: t('dashboard.nav_leaderboard'), host: true, connected: true },
    { route: 'settings', label: t('dashboard.nav_settings'), host: true, connected: true },
    { route: 'global-settings', label: t('dashboard.nav_global_settings'), offline: true },
    { route: 'donation', label: t('dashboard.nav_donation'), offline: true },
  ]);

  function visible(link: (typeof links)[0]) {
    if (link.connected && !dashboard.status.isConnected) return false;
    if (link.host && !dashboard.status.isHost) return false;
    return true;
  }

  function go(route: string) {
    navigate(route);
  }
</script>

<aside
  class="fixed inset-y-0 left-0 z-40 hidden w-[var(--sidebar-width)] flex-col border-r border-gray-200 bg-white dark:border-gray-700 dark:bg-gray-900 lg:flex"
>
  <div class="flex h-[var(--header-height)] items-center gap-3 border-b border-gray-200 px-5 dark:border-gray-700">
    <div class="flex h-9 w-9 items-center justify-center rounded-lg bg-[var(--brand)] text-sm font-bold text-white">
      MPE
    </div>
    <div>
      <div class="text-sm font-semibold">Mimesis PE</div>
      <div class="text-xs text-gray-500">Web Dashboard</div>
    </div>
  </div>
  <nav class="flex-1 space-y-1 p-4">
    {#each links as link}
      {#if visible(link)}
        <button
          type="button"
          class="flex w-full items-center rounded-lg px-3 py-2.5 text-left text-sm font-medium transition
            {dashboard.route === link.route || (link.route === 'settings' && dashboard.route === 'settings')
              ? 'bg-[var(--brand)]/10 text-[var(--brand)]'
              : 'text-gray-600 hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-800'}"
          onclick={() => go(link.route)}
        >
          {link.label}
        </button>
      {/if}
    {/each}
  </nav>
</aside>
