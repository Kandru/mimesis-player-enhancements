<script lang="ts">
  import type { Snippet } from 'svelte';
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import { t } from '$lib/i18n';
  import { isLobbyRoute } from '$lib/playerHelpers';
  import { navigate } from '$lib/utils';

  let { children }: { children: Snippet } = $props();

  const lobbyLinks = $derived([
    { route: 'players', label: t('dashboard.nav_players'), host: true },
    { route: 'minimap', label: t('dashboard.nav_minimap') },
    { route: 'leaderboard', label: t('dashboard.nav_leaderboard'), host: true },
    { route: 'settings', label: t('dashboard.nav_settings'), host: true },
  ]);

  function visible(link: { host?: boolean }) {
    if (link.host && !dashboard.status.isHost) return false;
    return true;
  }

  function isActive(route: string) {
    if (route === 'settings') {
      return dashboard.route === 'settings' || dashboard.route === 'player';
    }
    return dashboard.route === route;
  }

  function go(route: string) {
    navigate(route);
  }
</script>

{#if dashboard.status.isConnected && isLobbyRoute(dashboard.route)}
  <div class="lobby-layout">
    <nav class="lobby-nav" aria-label={t('dashboard.nav_lobby')}>
      {#each lobbyLinks as link}
        {#if visible(link)}
          <button
            type="button"
            class="lobby-nav-item {isActive(link.route) ? 'lobby-nav-item-active' : ''}"
            onclick={() => go(link.route)}
          >
            {link.label}
          </button>
        {/if}
      {/each}
    </nav>
    <div class="lobby-content">
      {@render children()}
    </div>
  </div>
{:else}
  {@render children()}
{/if}
