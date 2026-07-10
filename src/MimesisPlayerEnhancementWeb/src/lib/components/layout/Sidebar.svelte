<script lang="ts">
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import { t } from '$lib/i18n';
  import { navigate } from '$lib/utils';

  const lobbyLinks = $derived([
    { route: 'players', label: t('dashboard.nav_players'), icon: 'users', host: true },
    { route: 'leaderboard', label: t('dashboard.nav_statistics'), icon: 'chart', host: true },
    { route: 'minimap', label: t('dashboard.nav_minimap'), icon: 'map' },
    { route: 'settings', label: t('dashboard.nav_settings'), icon: 'settings', host: true },
  ]);

  const globalLinks = $derived([
    { route: 'global-settings', label: t('dashboard.nav_settings_global'), icon: 'globe' },
    { route: 'donation', label: t('dashboard.nav_donation'), icon: 'heart' },
  ]);

  function isLobbyLinkDisabled(link: { host?: boolean }) {
    if (!dashboard.status.isConnected) return true;
    if (link.host && !dashboard.status.isHost) return true;
    return false;
  }

  function lobbyLinkTitle(link: { host?: boolean }) {
    if (!dashboard.status.isConnected) return t('dashboard.nav_lobby_disabled_hint');
    if (link.host && !dashboard.status.isHost) return t('dashboard.nav_host_only_hint');
    return undefined;
  }

  function isActive(route: string) {
    if (route === 'settings') {
      return dashboard.route === 'settings' || dashboard.route === 'player';
    }
    return dashboard.route === route;
  }

  function go(route: string) {
    dashboard.sidebarOpen = false;
    navigate(route);
  }

  function goHome() {
    dashboard.sidebarOpen = false;
    go('home');
  }
</script>

<aside class="sidebar">
  <button type="button" class="sidebar-brand" onclick={goHome} title={t('dashboard.home_title')}>
    <img class="sidebar-logo" src="/img/logo.png" alt="" width="32" height="32" />
    <div class="sidebar-brand-text">
      <div class="sidebar-brand-title">{t('dashboard.brand_title')}</div>
      <div class="sidebar-brand-sub">{t('dashboard.brand_subtitle')}</div>
    </div>
  </button>

  <nav class="sidebar-nav">
    <p class="sidebar-section-label">{t('dashboard.nav_section_lobby')}</p>
    {#each lobbyLinks as link}
      {@const disabled = isLobbyLinkDisabled(link)}
      <button
        type="button"
        class="sidebar-link {isActive(link.route) ? 'sidebar-link-active' : ''} {disabled ? 'sidebar-link-disabled' : ''}"
        disabled={disabled}
        title={lobbyLinkTitle(link)}
        onclick={() => go(link.route)}
      >
        <span class="sidebar-link-icon" aria-hidden="true">
          {#if link.icon === 'users'}
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75"><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M22 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/></svg>
          {:else if link.icon === 'chart'}
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75"><path d="M3 3v18h18"/><path d="M7 16V9"/><path d="M12 16V5"/><path d="M17 16v-4"/></svg>
          {:else if link.icon === 'map'}
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75"><polygon points="1 6 9 2 15 6 23 2 23 18 15 22 9 18 1 22 1 6"/><line x1="9" y1="2" x2="9" y2="18"/><line x1="15" y1="6" x2="15" y2="22"/></svg>
          {:else}
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75"><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 1 1-4 0v-.09a1.65 1.65 0 0 0-1-1.51 1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 1 1 0-4h.09a1.65 1.65 0 0 0 1.51-1 1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 1 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9c.26.604.852.997 1.51 1H21a2 2 0 1 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z"/></svg>
          {/if}
        </span>
        <span class="sidebar-link-label">{link.label}</span>
      </button>
    {/each}

    <p class="sidebar-section-label sidebar-section-label-spaced">{t('dashboard.nav_section_global')}</p>
    {#each globalLinks as link}
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
