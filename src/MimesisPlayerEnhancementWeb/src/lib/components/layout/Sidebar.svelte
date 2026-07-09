<script lang="ts">
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import { t } from '$lib/i18n';
  import { isLobbyRoute } from '$lib/playerHelpers';
  import { navigate } from '$lib/utils';

  let {
    mobileOpen = false,
    onclose,
  }: {
    mobileOpen?: boolean;
    onclose?: () => void;
  } = $props();

  const offlineLinks = $derived([
    { route: 'global-settings', label: t('dashboard.nav_global_settings'), icon: 'globe' },
    { route: 'donation', label: t('dashboard.nav_donation'), icon: 'heart' },
  ]);

  function go(route: string) {
    navigate(route);
    onclose?.();
  }

  function openLobby() {
    if (dashboard.status.isHost) go('players');
    else go('minimap');
  }
</script>

<aside class="sidebar {mobileOpen ? 'sidebar-open' : ''}">
  <div class="sidebar-brand">
    <img class="sidebar-logo" src="/img/logo.png" alt="" width="32" height="32" />
    <div class="sidebar-brand-text">
      <div class="sidebar-brand-title">{t('dashboard.brand_title')}</div>
      <div class="sidebar-brand-sub">{t('dashboard.brand_subtitle')}</div>
    </div>
  </div>

  <nav class="sidebar-nav">
    <p class="sidebar-section-label">{t('dashboard.nav_section_game')}</p>
    {#if dashboard.status.isConnected}
      <button
        type="button"
        class="sidebar-link {isLobbyRoute(dashboard.route) ? 'sidebar-link-active' : ''}"
        onclick={openLobby}
      >
        <span class="sidebar-link-icon" aria-hidden="true">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75"><path d="M3 9.5 12 3l9 6.5V20a1 1 0 0 1-1 1h-5v-6H9v6H4a1 1 0 0 1-1-1V9.5z"/></svg>
        </span>
        <span class="sidebar-link-label">{t('dashboard.nav_lobby')}</span>
      </button>
    {/if}

    <p class="sidebar-section-label mt-4">{t('dashboard.nav_section_more')}</p>
    {#each offlineLinks as link}
      <button
        type="button"
        class="sidebar-link {dashboard.route === link.route ? 'sidebar-link-active' : ''}"
        onclick={() => go(link.route)}
      >
        <span class="sidebar-link-icon" aria-hidden="true">
          {#if link.icon === 'globe'}
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75"><circle cx="12" cy="12" r="10"/><line x1="2" y1="12" x2="22" y2="12"/><path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z"/></svg>
          {:else}
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75"><path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"/></svg>
          {/if}
        </span>
        <span class="sidebar-link-label">{link.label}</span>
      </button>
    {/each}
  </nav>
</aside>
