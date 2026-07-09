<script lang="ts">
  import Toggle from '$lib/components/Toggle.svelte';
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import { t } from '$lib/i18n';
  import { getHeaderSearchPlaceholder, isHeaderSearchVisible } from '$lib/headerSearch';
  import { getPageTitle } from '$lib/pageTitles';
  import { navigate } from '$lib/utils';

  const pageTitle = $derived.by(() =>
    getPageTitle(
      dashboard.route,
      dashboard.settingsSubRoute,
      dashboard.playerStats?.displayName || dashboard.playerStats?.steamId,
    ),
  );

  const showSearch = $derived(
    isHeaderSearchVisible(dashboard.route, dashboard.settingsSubRoute),
  );

  const searchPlaceholder = $derived(
    getHeaderSearchPlaceholder(dashboard.route, dashboard.settingsSubRoute),
  );

  const subtitle = $derived.by(() => {
    if (dashboard.apiError) return t('dashboard.subtitle_api_error');
    if (!dashboard.status.isConnected) {
      return dashboard.status.modVersion
        ? `v${dashboard.status.modVersion} · ${t('dashboard.subtitle_waiting')}`
        : t('dashboard.subtitle_waiting');
    }
    const parts: string[] = [];
    if (dashboard.status.modVersion) parts.push(`v${dashboard.status.modVersion}`);
    parts.push(
      dashboard.status.isHost ? t('dashboard.subtitle_host') : t('dashboard.subtitle_client'),
    );
    if (dashboard.status.saveSlotId >= 0) {
      parts.push(t('dashboard.subtitle_savegame', { slot: dashboard.status.saveSlotId }));
    }
    return parts.join(' · ');
  });
</script>

<header class="app-header">
  <div class="header-start">
    <button
      type="button"
      class="header-brand-btn lg:hidden"
      title={t('dashboard.home_title')}
      onclick={() => navigate('home')}
    >
      <img class="header-brand-logo" src="/img/logo.png" alt="" width="28" height="28" />
    </button>
    <button
      type="button"
      class="header-icon-btn lg:hidden"
      aria-label={dashboard.mobileSidebarOpen ? 'Close menu' : 'Open menu'}
      aria-expanded={dashboard.mobileSidebarOpen}
      onclick={() => (dashboard.mobileSidebarOpen = !dashboard.mobileSidebarOpen)}
    >
      {#if dashboard.mobileSidebarOpen}
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
      {:else}
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="3" y1="6" x2="21" y2="6"/><line x1="3" y1="12" x2="21" y2="12"/><line x1="3" y1="18" x2="21" y2="18"/></svg>
      {/if}
    </button>
    <div class="header-title-block">
      <h1 class="header-title">{pageTitle}</h1>
      <p class="header-subtitle">{subtitle}</p>
    </div>
  </div>

  {#if showSearch}
    <label class="header-search">
      <span class="header-search-icon" aria-hidden="true">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/></svg>
      </span>
      <input
        class="header-search-input"
        type="search"
        placeholder={searchPlaceholder}
        bind:value={dashboard.headerSearchQuery}
        aria-label={searchPlaceholder}
      />
    </label>
  {/if}

  <div class="header-actions">
    {#if dashboard.status.isConnected}
      <span class="badge {dashboard.status.isHost ? 'badge-host' : 'badge-guest'}">
        {dashboard.status.isHost ? t('dashboard.role_host') : t('dashboard.role_guest')}
      </span>
    {/if}

    {#if dashboard.status.isHost && dashboard.status.isConnected}
      <div class="header-blind-toggle" title={t('dashboard.blind_mode_title')}>
        <span class="header-blind-label">{t('dashboard.blind_mode')}</span>
        <Toggle
          checked={dashboard.playerBlindModeUserEnabled}
          label={t('dashboard.blind_mode')}
          onchange={() => dashboard.togglePlayerBlindMode()}
        />
      </div>
    {/if}

    <button
      type="button"
      class="header-icon-btn"
      title={dashboard.darkMode ? t('dashboard.theme_light') : t('dashboard.theme_dark')}
      onclick={() => dashboard.toggleDarkMode()}
    >
      {#if dashboard.darkMode}
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="5"/><line x1="12" y1="1" x2="12" y2="3"/><line x1="12" y1="21" x2="12" y2="23"/><line x1="4.22" y1="4.22" x2="5.64" y2="5.64"/><line x1="18.36" y1="18.36" x2="19.78" y2="19.78"/><line x1="1" y1="12" x2="3" y2="12"/><line x1="21" y1="12" x2="23" y2="12"/><line x1="4.22" y1="19.78" x2="5.64" y2="18.36"/><line x1="18.36" y1="5.64" x2="19.78" y2="4.22"/></svg>
      {:else}
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/></svg>
      {/if}
    </button>
  </div>
</header>
